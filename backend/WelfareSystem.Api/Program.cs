using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Middleware;
using WelfareSystem.Api.Repositories;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services;
using WelfareSystem.Api.Services.Interfaces;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true)
        .AddEnvironmentVariables();
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins((builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/'))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IWelfareProgramRepository, WelfareProgramRepository>();
builder.Services.AddScoped<IWelfareApplicationRepository, WelfareApplicationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEligibilityRuleRepository, EligibilityRuleRepository>();
builder.Services.AddScoped<IApplicationDocumentRepository, ApplicationDocumentRepository>();
builder.Services.AddScoped<IApplicationStatusHistoryRepository, ApplicationStatusHistoryRepository>();
builder.Services.AddScoped<IApplicationReviewRepository, ApplicationReviewRepository>();
builder.Services.AddScoped<IAnomalyRepository, AnomalyRepository>();
builder.Services.AddScoped<IAnomalyRuleRepository, AnomalyRuleRepository>();

// Register Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("ExternalAuth", client => client.Timeout = TimeSpan.FromSeconds(20))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ExternalAuthFlowStore>();
builder.Services.AddSingleton<ExternalAuthSettings>();
builder.Services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
builder.Services.AddRateLimiter(options => {
    options.AddPolicy("account-actions", http => RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("external-auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 40, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
builder.Services.AddScoped<ExternalAuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserAdministrationService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IWelfareProgramService, WelfareProgramService>();
builder.Services.AddScoped<IEligibilityRuleService, EligibilityRuleService>();
builder.Services.AddScoped<IWelfareApplicationService, WelfareApplicationService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddSingleton<PrivateDocumentStore>();
builder.Services.AddSingleton<IFileScanner, FileScanner>();
builder.Services.AddScoped<AccountRecoveryService>();
builder.Services.AddSingleton<AccountMail>();
builder.Services.AddSingleton<IAccountMail>(services => services.GetRequiredService<AccountMail>());
builder.Services.AddHostedService<AccountMail>(services => services.GetRequiredService<AccountMail>());
builder.Services.AddSingleton<MaintenanceGate>();
builder.Services.AddScoped<IStatusHistoryService, StatusHistoryService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
builder.Services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<NotificationTriggerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"];
var key = Encoding.UTF8.GetBytes(jwtKey!);
if (!builder.Environment.IsDevelopment() && (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__SecretKey")) || key.Length < 32))
    throw new InvalidOperationException("Set a private Jwt__SecretKey environment secret of at least 32 bytes for production.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents {
        OnTokenValidated = async validation => {
            var db = validation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await AccountTokenValidator.IsCurrentAsync(validation.Principal, db))
                validation.Fail("This account or session is no longer active. Please sign in again.");
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization with role-based policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee"));
    options.AddPolicy("HROnly", policy => policy.RequireRole("HROfficer"));
    options.AddPolicy("CommitteeOnly", policy => policy.RequireRole("CommitteeMember"));
    options.AddPolicy("ManagementOnly", policy => policy.RequireRole("Management"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    
    // Combined policies
    options.AddPolicy("HRAndAbove", policy => 
        policy.RequireRole("HROfficer", "CommitteeMember", "Management", "Administrator"));
    options.AddPolicy("ManagementAndAbove", policy => 
        policy.RequireRole("Management", "Administrator"));
});

// Learn more about configuring Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply schema migrations before starting the production API.");
    var demo = await db.Users.Where(u => u.IsActive && u.Email.EndsWith("@welfare.edu")).Select(u => u.PasswordHash).ToListAsync();
    if (demo.Any(hash => BCrypt.Net.BCrypt.Verify("Password123!", hash))) throw new InvalidOperationException("Change or disable development demonstration credentials before production startup.");
}

// Seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Custom JWT middleware
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.Use(async (http, next) => {
    var writes = !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && !HttpMethods.IsOptions(http.Request.Method);
    writes |= http.Request.Path.Value?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true || http.Request.Path.Value?.EndsWith("/content", StringComparison.OrdinalIgnoreCase) == true;
    if (writes && http.Request.Path != "/api/operations/maintenance" && http.Request.Path != "/api/auth/login" && http.Request.Path != "/api/auth/refresh" && http.Request.Path != "/api/auth/logout") {
        using var lease = http.RequestServices.GetRequiredService<MaintenanceGate>().TryWrite();
        if (lease == null) { http.Response.StatusCode = 503; await http.Response.WriteAsJsonAsync(new { message = "Maintenance is in progress. Your records are safe; retry this action shortly." }); return; }
        await next(); return;
    }
    await next();
});
app.Use(async (http, next) => {
    if (http.User.Identity?.IsAuthenticated == true && http.Request.Path != "/api/account/security" && http.Request.Path != "/api/account/change-password" && http.Request.Path != "/api/account/complete" && http.Request.Path != "/api/account/forgot-password" && http.Request.Path != "/api/auth/logout" && !(http.Request.Path == "/api/users/me" && HttpMethods.IsGet(http.Request.Method))) {
        var idText = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idText, out var id) && await http.RequestServices.GetRequiredService<AppDbContext>().Users.AnyAsync(u => u.Id == id && u.MustChangePassword)) {
            http.Response.StatusCode = 403; await http.Response.WriteAsJsonAsync(new { message = "Change your initial password from Account security before continuing.", code = "PASSWORD_CHANGE_REQUIRED" }); return;
        }
    }
    await next();
});
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapGet("/health/ready", async (AppDbContext db, MaintenanceGate gate) => {
    try { var ready = !gate.Enabled && await db.Database.CanConnectAsync() && !(await db.Database.GetPendingMigrationsAsync()).Any(); return ready ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
    catch { return Results.StatusCode(503); }
}).AllowAnonymous();

app.Run();
