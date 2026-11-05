using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using WelfareSystem.Api.Models.DTOs.Auth;
using WelfareSystem.Api.Controllers;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories;
using WelfareSystem.Api.Services;

// Integration checks use the configured local PostgreSQL database and roll back every fixture.
// No database creation, reset, migrations, or existing-record changes occur here.
var configPath = args.SingleOrDefault() ?? throw new ArgumentException("Pass the path to appsettings.Local.json.");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
await using var transaction = await db.Database.BeginTransactionAsync();
var passed = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    Console.WriteLine("PASS: " + message);
    passed++;
}
try
{
    var suffix = Guid.NewGuid().ToString("N");
    var departments = new[] {
        new Department { Name = "Regression A", Code = "RA" + suffix[..6] },
        new Department { Name = "Regression B", Code = "RB" + suffix[..6] }
    };
    db.Departments.AddRange(departments);
    await db.SaveChangesAsync();
    var roles = await db.Roles.ToDictionaryAsync(r => r.Name);
    User MakeUser(string role, int dept, string key) => new() {
        Email = $"regression-{key}-{suffix}@example.invalid", EmployeeId = key + suffix,
        FirstName = "Regression", LastName = key, PasswordHash = "unused-in-checks",
        DepartmentId = dept, RoleId = roles[role].Id, IsActive = true, HireDate = DateTime.UtcNow.AddYears(-3), EmploymentVerifiedAt = DateTime.UtcNow
    };
    var employee = MakeUser("Employee", departments[0].Id, "employee");
    var other = MakeUser("Employee", departments[1].Id, "other");
    var hr = MakeUser("HROfficer", departments[0].Id, "hr");
    var admin = MakeUser("Administrator", departments[0].Id, "admin");
    db.Users.AddRange(employee, other, hr, admin);
    var program = new WelfareProgram { Name = "Regression " + suffix, Category = suffix, MaxAmount = 10000, RequiredDocuments = "[]" };
    db.WelfarePrograms.Add(program);
    await db.SaveChangesAsync();
    var detection = new AnomalyDetectionService(db, new AnomalyRepository(db), new WelfareApplicationRepository(db), new UserRepository(db));
    async Task<WelfareApplication> AddApplication(User owner, decimal amount = 100, int status = 2, WelfareProgram? selected = null)
    {
        var app = new WelfareApplication { UserId = owner.Id, WelfareProgramId = (selected ?? program).Id,
            RequestedAmount = amount, Status = status, Reason = "Regression fixture", SubmittedAt = DateTime.UtcNow };
        db.WelfareApplications.Add(app);
        await db.SaveChangesAsync();
        return app;
    }
    var first = await AddApplication(employee);
    Check(await detection.DetectDuplicateApplicationAsync(employee.Id, program.Id) == null, "A first submission is not its own duplicate");
    await AddApplication(employee, status: 1);
    Check(await detection.DetectDuplicateApplicationAsync(employee.Id, program.Id) == null, "Drafts are excluded from duplicates");
    var second = await AddApplication(employee);
    Check(await detection.DetectDuplicateApplicationAsync(employee.Id, program.Id) != null, "A second recent submission is flagged");
    var count = await db.Anomalies.CountAsync(a => a.UserId == employee.Id);
    await detection.DetectDuplicateApplicationAsync(employee.Id, program.Id);
    Check(count == await db.Anomalies.CountAsync(a => a.UserId == employee.Id), "Repeated detection does not duplicate flags");
    Check(await detection.DetectCrossProgramAnomalyAsync(employee.Id) == null, "Repeated applications to one program are not cross-program claims");
    var expensive = await AddApplication(employee, 500);
    Check(await detection.DetectAmountAnomalyAsync(expensive.Id) != null, "Amount baseline excludes the application being evaluated");
    Check(await detection.DetectApplicationFrequencyAnomalyAsync(employee.Id) == null, "Three submissions plus a draft do not exceed the frequency limit");
    await AddApplication(employee);
    Check(await detection.DetectApplicationFrequencyAnomalyAsync(employee.Id) != null, "Four submitted applications trigger frequency detection");
    var secondProgram = new WelfareProgram { Name = "Regression second " + suffix, Category = suffix, MaxAmount = 10000, RequiredDocuments = "[]" };
    db.WelfarePrograms.Add(secondProgram);
    await db.SaveChangesAsync();
    await AddApplication(employee, selected: secondProgram);
    Check(await detection.DetectCrossProgramAnomalyAsync(employee.Id) != null, "Distinct programs in one category trigger a cross-program flag");
    var foreignApp = await AddApplication(other);
    var foreignFlag = new Anomaly { UserId = other.Id, ApplicationId = foreignApp.Id, AnomalyType = "Regression", Severity = "High", Description = "Foreign fixture" };
    var ownFlag = new Anomaly { UserId = employee.Id, ApplicationId = first.Id, AnomalyType = "Regression", Severity = "High", Description = "Own fixture" };
    db.Anomalies.AddRange(foreignFlag, ownFlag);
    await db.SaveChangesAsync();
    AnomaliesController As(User user) => new(db, detection) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, "Administrator") // Spoofed/stale role claim must not expand DB permissions.
            }, "Regression"))
        }}
    };
    Check(await As(employee).GetById(foreignFlag.Id) is NotFoundResult, "Employees cannot read other employees' anomaly details");
    Check(await As(employee).Investigate(ownFlag.Id) is ForbidResult, "Employees cannot mutate anomalies despite a privileged role claim");
    Check(await As(hr).GetById(foreignFlag.Id) is NotFoundResult, "HR cannot read another department's flags");
    Check(await As(hr).Investigate(foreignFlag.Id) is NotFoundResult, "HR cannot mutate another department's flags");
    Check(await As(admin).Investigate(ownFlag.Id) is OkObjectResult, "Administrators can investigate visible anomalies");
    Check(await As(admin).Resolve(ownFlag.Id, new() { Notes = "Checked supporting evidence" }) is OkObjectResult, "Investigators can resolve with notes");
    Check(ownFlag.ResolvedBy == admin.Id, "Resolver identity comes from the authenticated actor");
    Check(await db.AuditLogs.AnyAsync(a => a.EntityType == "Anomaly" && a.EntityId == ownFlag.Id.ToString() && a.UserId == admin.Id), "Investigator decisions are audited");
    Check(await As(admin).Dismiss(ownFlag.Id, new() { Notes = "Should not overwrite" }) is ConflictObjectResult, "Closed decisions cannot be silently overwritten");
    var summary = (OkObjectResult)await As(employee).GetSummary();
    var json = JsonSerializer.SerializeToElement(summary.Value);
    Check(json.GetProperty("statusCounts").GetProperty("Open").GetInt32() > 0, "Summary counts open status separately from severity");
    var applications = new ApplicationsController(null!, null!, db) { ControllerContext = As(employee).ControllerContext };
    Check(await applications.GetAllApplications() is ForbidResult, "A stale privileged token cannot expose all applications after demotion");
    Check(await applications.GetApplicationById(foreignApp.Id) is NotFoundResult, "Employees cannot read another user's application");
    Check(await applications.SubmitApplication(foreignApp.Id) is NotFoundResult, "Employees cannot submit another user's application");
    Check(await applications.CancelApplication(foreignApp.Id) is NotFoundResult, "Employees cannot cancel another user's application");
    Check(await applications.UpdateApplication(foreignApp.Id, new() { RequestedAmount = 10, Reason = "Unauthorized" }) is NotFoundResult,
        "Employees cannot edit another user's application");
    var submissionService = new WelfareApplicationService(new WelfareApplicationRepository(db), new WelfareProgramRepository(db),
        new UserRepository(db), new RuleEngineService(), detection,
        new NotificationTriggerService(new NotificationService(new NotificationRepository(db), db)), db);
    var draft = await AddApplication(employee, 3000, status: 1);
    draft.SubmittedAt = DateTime.UtcNow.AddDays(-90);
    await db.SaveChangesAsync();
    Check((await submissionService.SubmitApplicationAsync(draft.Id))?.Status == 2, "Draft submission succeeds with detection integrated");
    Check(draft.SubmittedAt > DateTime.UtcNow.AddMinutes(-1), "Frequency windows use the actual submission time");
    Check(await db.Anomalies.AnyAsync(a => a.ApplicationId == draft.Id && a.AnomalyType == "AmountAnomaly"),
        "Submission automatically persists amount anomalies");
    Check(await db.ApplicationStatusHistories.CountAsync(h => h.ApplicationId == draft.Id) == 1,
        "Submission records one status transition");
    Check(await submissionService.SubmitApplicationAsync(draft.Id) == null, "Repeated submission is rejected");
    var tokenConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Jwt:SecretKey"] = "Regression-only-signing-key-with-at-least-32-characters",
        ["Jwt:Issuer"] = "Regression", ["Jwt:Audience"] = "Regression", ["Jwt:ExpirationMinutes"] = "5"
    }).Build();
    var auth = new AuthService(new UserRepository(db), new DepartmentRepository(db), new PasswordHasher(), new TokenService(tokenConfig), db);
    var registration = new RegisterRequest { Email = $"register-{suffix}@example.invalid", EmployeeId = "reg" + suffix,
        FirstName = "Regression", LastName = "Registration", Password = "Regression-test-only-123!",
        DepartmentId = departments[0].Id, RequestedRole = "Administrator" };
    var created = await auth.RegisterAsync(registration);
    Check(created?.User.Role == "Employee", "Requesting Administrator during registration still creates an Employee");
    Check(await db.RoleAccessRequests.AnyAsync(r => r.UserId == created!.User.Id && r.Status == "Pending"),
        "Privileged registration requests remain pending approval");
    Check(await auth.RegisterAsync(registration) == null, "Duplicate registration is rejected");
    Check(await auth.LoginAsync(new() { Email = registration.Email, Password = registration.Password }) != null,
        "Existing email/password login succeeds for the registered account");
    Check(await auth.LoginAsync(new() { Email = registration.Email, Password = "wrong-password" }) == null,
        "Incorrect login credentials are rejected");
    var profilePayload = JsonSerializer.Deserialize<WelfareSystem.Api.Models.DTOs.User.UpdateProfileRequest>(
        JsonSerializer.Serialize(new { email = employee.Email, firstName = "Edited", lastName = employee.LastName,
            roleId = admin.RoleId, departmentId = other.DepartmentId, isActive = false }), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    var profile = await new UserService(new UserRepository(db), new PasswordHasher(), db).UpdateProfileAsync(employee.Id, profilePayload);
    Check(profile?.FirstName == "Edited", "Profile edits save personal fields");
    Check(profile?.RoleName == "Employee" && profile.DepartmentId == departments[0].Id && profile.IsActive,
        "Profile payload cannot change role, department, or active status");
    var committee = MakeUser("CommitteeMember", departments[0].Id, "committee");
    var manager = MakeUser("Management", departments[0].Id, "manager");
    var foreignHr = MakeUser("HROfficer", departments[1].Id, "foreignhr");
    db.Users.AddRange(committee, manager, foreignHr);
    await db.SaveChangesAsync();
    var notifier = new NotificationTriggerService(new NotificationService(new NotificationRepository(db), db));
    var reviewService = new ReviewService(db, notifier);
    var dashboards = new DashboardService(db);
    var workflow = new WorkflowService(db, reviewService);
    Check(!await workflow.CanTransitionStatusAsync(2, 8, "Management"), "Management cannot skip HR and committee approval");
    Check(!await workflow.CanTransitionStatusAsync(8, 4, "HROfficer"), "HR cannot reopen a finalized application");
    var hrBefore = (await dashboards.GetHRDashboardStatsAsync(hr.Id)).PendingReview;
    await AddApplication(other);
    Check((await dashboards.GetHRDashboardStatsAsync(hr.Id)).PendingReview == hrBefore, "HR dashboard excludes other departments");
    var committeeBefore = await dashboards.GetCommitteeDashboardStatsAsync(committee.Id);
    var managementBefore = await dashboards.GetManagementDashboardStatsAsync(manager.Id);
    var employeeBefore = await dashboards.GetEmployeeDashboardStatsAsync(employee.Id);
    var adminBefore = await dashboards.GetAdminDashboardStatsAsync();
    var flow = await AddApplication(employee, 75, status: 1);
    Check((await dashboards.GetHRDashboardStatsAsync(hr.Id)).PendingReview == hrBefore, "Draft creation does not add an HR review item");
    await submissionService.SubmitApplicationAsync(flow.Id);
    Check((await dashboards.GetHRDashboardStatsAsync(hr.Id)).PendingReview == hrBefore + 1, "Submission increments the correct HR queue");
    Check((await dashboards.GetEmployeeDashboardStatsAsync(employee.Id)).PendingApplications == employeeBefore.PendingApplications + 1,
        "Submission increments employee pending count");
    Check(await reviewService.CreateReviewAsync(flow.Id, manager.Id, "Management", new() { Recommendation = "Approve", Comments = "Too early" }) == null,
        "Management review is rejected before committee endorsement");
    Check(await reviewService.CreateReviewAsync(flow.Id, foreignHr.Id, "HROfficer", new() { Recommendation = "Approve", Comments = "Wrong department" }) == null,
        "HR cannot approve another department's application");
    Check(await reviewService.CreateReviewAsync(flow.Id, employee.Id, "HROfficer", new() { Recommendation = "Approve", Comments = "Self review" }) == null,
        "Employees cannot self-review or impersonate HR");
    var hrDecision = await reviewService.CreateReviewAsync(flow.Id, hr.Id, "Administrator", new() { Recommendation = "Approve", Comments = "HR evidence verified" });
    Check(hrDecision?.ReviewerRole == "HROfficer" && flow.Status == 4, "HR approval uses the database role and forwards to committee");
    Check((await dashboards.GetHRDashboardStatsAsync(hr.Id)).PendingReview == hrBefore, "HR approval removes one HR pending item");
    Check((await dashboards.GetCommitteeDashboardStatsAsync(committee.Id)).PendingReview == committeeBefore.PendingReview + 1,
        "HR approval adds one committee pending item");
    Check(await reviewService.CreateReviewAsync(flow.Id, hr.Id, "HROfficer", new() { Recommendation = "Reject", Comments = "Repeated decision" }) == null,
        "A duplicate or stale HR decision cannot overwrite committee stage");
    Check(await reviewService.CreateReviewAsync(flow.Id, committee.Id, "CommitteeMember", new() { Recommendation = "Approve", Comments = "Committee endorsement" }) != null && flow.Status == 5,
        "Committee endorsement forwards to management");
    Check((await dashboards.GetCommitteeDashboardStatsAsync(committee.Id)).PendingReview == committeeBefore.PendingReview,
        "Committee endorsement removes one committee pending item");
    Check((await dashboards.GetManagementDashboardStatsAsync(manager.Id)).PendingApproval == managementBefore.PendingApproval + 1,
        "Committee endorsement adds one management pending item");
    Check((await dashboards.GetEmployeeDashboardStatsAsync(employee.Id)).PendingApplications == employeeBefore.PendingApplications + 1,
        "Employee still sees pending while management approval is outstanding");
    Check((await dashboards.GetAdminDashboardStatsAsync()).PendingApplications == adminBefore.PendingApplications + 1,
        "Administrator pending includes committee-endorsed applications");
    Check(await reviewService.CreateReviewAsync(flow.Id, manager.Id, "Management", new() { Recommendation = "Approve", Comments = "Final approval" }) != null && flow.Status == 8,
        "Management records final approval");
    Check((await dashboards.GetManagementDashboardStatsAsync(manager.Id)).PendingApproval == managementBefore.PendingApproval,
        "Final approval removes management pending item");
    var finalEmployee = await dashboards.GetEmployeeDashboardStatsAsync(employee.Id);
    Check(finalEmployee.PendingApplications == employeeBefore.PendingApplications && finalEmployee.ApprovedApplications == employeeBefore.ApprovedApplications + 1,
        "Final approval updates employee pending and approved counters together");
    Check((await dashboards.GetAdminDashboardStatsAsync()).PendingApplications == adminBefore.PendingApplications,
        "Final approval removes administrator pending item");
    Check((await dashboards.GetCommitteeDashboardStatsAsync(committee.Id)).ApprovedByCommittee == committeeBefore.ApprovedByCommittee + 1,
        "Committee endorsement total remains after management finalizes");
    var editDecision = new WelfareSystem.Api.Models.DTOs.Review.UpdateReviewRequest { Recommendation = "Reject", Comments = "Illegal change" };
    Check(await reviewService.UpdateReviewAsync(hrDecision!.Id, editDecision) == null,
        "Review editing cannot silently reverse a completed decision");
    var rejectedFlow = await AddApplication(employee, 76, status: 1);
    await submissionService.SubmitApplicationAsync(rejectedFlow.Id);
    await reviewService.CreateReviewAsync(rejectedFlow.Id, hr.Id, "HROfficer", new() { Recommendation = "Approve", Comments = "HR checked" });
    Check(await reviewService.CreateReviewAsync(rejectedFlow.Id, committee.Id, "CommitteeMember", new() { Recommendation = "Reject", Comments = "Insufficient evidence" }) != null && rejectedFlow.Status == 6,
        "Committee rejection stops the workflow");
    Check((await dashboards.GetEmployeeDashboardStatsAsync(employee.Id)).RejectedApplications == employeeBefore.RejectedApplications + 1,
        "Committee rejection appears in employee rejected count");
    Check(await reviewService.CreateReviewAsync(rejectedFlow.Id, manager.Id, "Management", new() { Recommendation = "Approve", Comments = "Should fail" }) == null,
        "Management cannot approve a committee-rejected application");
    Check((await dashboards.GetManagementDashboardStatsAsync(manager.Id)).RejectedByManagement == managementBefore.RejectedByManagement,
        "Committee rejection is not mislabeled as management rejection");
    Check(await db.Notifications.AnyAsync(n => n.UserId == employee.Id && n.Message.Contains("Insufficient evidence")),
        "Rejection notification includes the reviewer's reason");
    Check(await db.Anomalies.AnyAsync(a => a.ApplicationId == flow.Id && a.AnomalyType == "ApplicationFrequency"),
        "Submission associates frequency anomalies with the reviewed application");
    var administration = new UserAdministrationService(db, new PasswordHasher());
    async Task<bool> AdminRejected(Func<Task> action, int? status = null)
    {
        try { await action(); return false; }
        catch (UserAdministrationException error) { return status == null || error.Status == status; }
    }
    var adminCreated = await administration.CreateAsync(new() {
        Email = $"admin-created-{suffix}@example.invalid", FirstName = "Managed", LastName = "Account",
        EmployeeId = "managed" + suffix, DepartmentId = departments[0].Id, RoleId = roles["Employee"].Id,
        HireDate = DateTime.UtcNow.AddYears(-2), Password = "Regression-only-Password123!", Reason = "Create regression account"
    }, admin.Id);
    Check(adminCreated.IsActive && adminCreated.HireDate.Date == DateTime.UtcNow.AddYears(-2).Date,
        "Administrator creates an active user with a verified hire date");
    WelfareSystem.Api.Models.DTOs.User.UpdateUserRequest Edit(WelfareSystem.Api.Models.DTOs.User.UserResponse value) => new() {
        Email = value.Email, FirstName = value.FirstName, LastName = value.LastName, EmployeeId = value.EmployeeId,
        DepartmentId = value.DepartmentId, RoleId = value.RoleId, IsActive = value.IsActive, HireDate = value.HireDate,
        Reason = "Verified account correction", ExpectedUpdatedAt = value.UpdatedAt
    };
    var badEdit = Edit(adminCreated); badEdit.HireDate = DateTime.UtcNow.AddDays(1);
    Check(await AdminRejected(async () => { await administration.UpdateAsync(adminCreated.Id, badEdit, admin.Id); }), "Future hire dates are rejected");
    badEdit = Edit(adminCreated); badEdit.Email = admin.Email;
    Check(await AdminRejected(async () => { await administration.UpdateAsync(adminCreated.Id, badEdit, admin.Id); }, 409), "Duplicate email changes are rejected without overwriting users");
    badEdit = Edit(adminCreated); badEdit.EmployeeId = admin.EmployeeId;
    Check(await AdminRejected(async () => { await administration.UpdateAsync(adminCreated.Id, badEdit, admin.Id); }, 409), "Duplicate employee IDs are rejected");
    badEdit = Edit(adminCreated); badEdit.RoleId = int.MaxValue;
    Check(await AdminRejected(async () => { await administration.UpdateAsync(adminCreated.Id, badEdit, admin.Id); }), "Unknown role IDs are rejected");
    Check(await AdminRejected(async () => { await administration.SetStatusAsync(adminCreated.Id, false, "Not authorized", adminCreated.UpdatedAt, employee.Id); }, 403),
        "An employee cannot administer account status");
    Check(await AdminRejected(async () => { await administration.SetStatusAsync(admin.Id, false, "Self suspension attempt", null, admin.Id); }),
        "Administrator self-suspension is blocked");
    var selfEdit = new WelfareSystem.Api.Models.DTOs.User.UpdateUserRequest { Email = admin.Email, FirstName = admin.FirstName,
        LastName = admin.LastName, EmployeeId = admin.EmployeeId, DepartmentId = admin.DepartmentId, RoleId = roles["Employee"].Id,
        IsActive = true, HireDate = admin.HireDate, Reason = "Self demotion attempt" };
    Check(await AdminRejected(async () => { await administration.UpdateAsync(admin.Id, selfEdit, admin.Id); }), "Administrator self-demotion is blocked");
    var initialManagedLogin = await auth.LoginAsync(new() { Email = adminCreated.Email, Password = "Regression-only-Password123!" });
    ClaimsPrincipal TokenPrincipal(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        return handler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters {
            ValidateIssuer = true, ValidIssuer = "Regression", ValidateAudience = true, ValidAudience = "Regression",
            ValidateIssuerSigningKey = true, IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(tokenConfig["Jwt:SecretKey"]!)), ValidateLifetime = true
        }, out _);
    }
    Check(await AccountTokenValidator.IsCurrentAsync(TokenPrincipal(initialManagedLogin!.Token), db), "A valid active account access token is accepted");
    Check(!await AccountTokenValidator.IsCurrentAsync(TokenPrincipal(initialManagedLogin.RefreshToken), db), "Refresh tokens cannot be used as bearer access tokens");
    Check(await auth.RefreshTokenAsync(initialManagedLogin.Token) == null, "Access tokens cannot be exchanged as refresh tokens");
    var correction = Edit(adminCreated); correction.FirstName = "Corrected"; correction.DepartmentId = departments[1].Id;
    correction.RoleId = roles["HROfficer"].Id; correction.HireDate = DateTime.UtcNow.AddYears(-4);
    var corrected = await administration.UpdateAsync(adminCreated.Id, correction, admin.Id);
    Check(corrected.FirstName == "Corrected" && corrected.RoleName == "HROfficer" && corrected.DepartmentId == departments[1].Id,
        "Administrator can update details, role, and department");
    Check(corrected.HireDate.Date == DateTime.UtcNow.AddYears(-4).Date, "Administrator can correct hire dates");
    Check(!await AccountTokenValidator.IsCurrentAsync(TokenPrincipal(initialManagedLogin.Token), db), "Role and department changes invalidate existing access tokens");
    Check(await auth.RefreshTokenAsync(initialManagedLogin.RefreshToken) == null, "Role and department changes invalidate existing refresh tokens");
    Check(await AdminRejected(async () => { await administration.UpdateAsync(corrected.Id, Edit(adminCreated), admin.Id); }, 409),
        "Stale edit forms cannot overwrite newer administrator changes");
    var currentLogin = await auth.LoginAsync(new() { Email = corrected.Email, Password = "Regression-only-Password123!" });
    Check(currentLogin?.User.Role == "HROfficer", "Fresh login uses the newly assigned role");
    var suspended = await administration.SetStatusAsync(corrected.Id, false, "Suspension regression test", corrected.UpdatedAt, admin.Id);
    Check(!suspended.IsActive, "Administrator can suspend an account without deleting it");
    Check(await auth.LoginAsync(new() { Email = corrected.Email, Password = "Regression-only-Password123!" }) == null, "Suspended users cannot sign in");
    Check(!await AccountTokenValidator.IsCurrentAsync(TokenPrincipal(currentLogin!.Token), db), "Suspension blocks an already-issued access token on its next request");
    Check(await auth.RefreshTokenAsync(currentLogin.RefreshToken) == null, "Suspension blocks token refresh");
    var reactivated = await administration.SetStatusAsync(corrected.Id, true, "Reactivation regression test", suspended.UpdatedAt, admin.Id);
    Check(reactivated.IsActive && reactivated.RoleName == "HROfficer", "Reactivation preserves the user's assigned role");
    Check(!await AccountTokenValidator.IsCurrentAsync(TokenPrincipal(currentLogin.Token), db), "Reactivation does not resurrect previously invalidated sessions");
    var restoredLogin = await auth.LoginAsync(new() { Email = corrected.Email, Password = "Regression-only-Password123!" });
    Check(restoredLogin != null && await AccountTokenValidator.IsCurrentAsync(TokenPrincipal(restoredLogin.Token), db), "Reactivated users can sign in with a fresh valid session");
    var administrationAudit = await db.AuditLogs.Where(a => a.EntityType == "User" && a.EntityId == corrected.Id.ToString()).ToListAsync();
    Check(administrationAudit.Any(a => a.Action == "UserCreated") && administrationAudit.Any(a => a.Action == "UserSuspended") && administrationAudit.Any(a => a.Action == "UserReactivated"),
        "Creation, suspension, and reactivation have audit records");
    Check(administrationAudit.All(a => a.UserId == admin.Id && !a.Details.Contains("Regression-only-Password123!")), "Audit records identify the administrator and never store the password");
    Check(await db.Users.AnyAsync(u => u.Id == corrected.Id), "Account status changes preserve the user record");
    var listedManaged = (await new UserService(new UserRepository(db), new PasswordHasher(), db).GetAllUsersAsync()).Single(u => u.Id == corrected.Id);
    Check(listedManaged.RoleName == "HROfficer" && !string.IsNullOrEmpty(listedManaged.DepartmentName),
        "Administrator user listings include role and department names for filtering");
    var programService = new WelfareProgramService(new WelfareProgramRepository(db));
    var programsController = new WelfareProgramsController(programService, db) { ControllerContext = As(admin).ControllerContext };
    var createdProgramResult = (CreatedAtActionResult)await programsController.CreateProgram(new() {
        Name = "Managed program " + suffix, Description = "Regression welfare program", Category = "Regression",
        MaxAmount = 5000, MinServicePeriod = 6, RequiredDocuments = "[\"Supporting letter\"]", Reason = "Create fixture program"
    });
    var managedProgram = (WelfareSystem.Api.Models.DTOs.WelfareProgram.WelfareProgramResponse)createdProgramResult.Value!;
    Check(managedProgram.IsActive && managedProgram.MaxAmount == 5000 && managedProgram.MinServicePeriod == 6,
        "Administrator creates an active program with amount and service requirements");
    Check((await programService.GetActiveProgramsAsync()).Any(p => p.Id == managedProgram.Id), "New programs appear in the employee active-program listing");
    Check(await programsController.CreateProgram(new() { Name = managedProgram.Name, Description = "Duplicate", Category = "Regression",
        MaxAmount = 10, MinServicePeriod = 0, RequiredDocuments = "[]" }) is ConflictObjectResult, "Duplicate program names are rejected");
    Check(await programService.CreateProgramAsync(new() { Name = "Invalid documents", Description = "Invalid", Category = "Regression",
        MaxAmount = 10, RequiredDocuments = "not-json" }) == null, "Malformed document requirements are rejected by the backend");
    Check(await programService.CreateProgramAsync(new() { Name = "Invalid amount", Description = "Invalid", Category = "Regression",
        MaxAmount = -1, RequiredDocuments = "[]" }) == null, "Invalid maximum amounts are rejected by the backend");
    var managedDraft = await submissionService.CreateApplicationAsync(employee.Id, new() { WelfareProgramId = managedProgram.Id,
        RequestedAmount = 100, Reason = "Regression new program draft" });
    Check(managedDraft != null, "Employees can create applications for a newly created active program");
    WelfareSystem.Api.Models.DTOs.WelfareProgram.UpdateWelfareProgramRequest ProgramUpdate(bool active, DateTime expected) => new() {
        Name = managedProgram.Name, Description = "Updated program", Category = "Regression", MaxAmount = 6000,
        MinServicePeriod = 3, RequiredDocuments = "[]", IsActive = active, Reason = "Program update regression", ExpectedUpdatedAt = expected
    };
    Check(await programsController.UpdateProgram(managedProgram.Id, ProgramUpdate(true, managedProgram.UpdatedAt.AddDays(-1))) is ConflictObjectResult,
        "Stale program edits cannot overwrite newer changes");
    var updatedResult = (OkObjectResult)await programsController.UpdateProgram(managedProgram.Id, ProgramUpdate(false, managedProgram.UpdatedAt));
    var inactiveProgram = (WelfareSystem.Api.Models.DTOs.WelfareProgram.WelfareProgramResponse)updatedResult.Value!;
    Check(!inactiveProgram.IsActive && inactiveProgram.MaxAmount == 6000, "Administrators can edit program rules and deactivate the program");
    Check(!(await programService.GetActiveProgramsAsync()).Any(p => p.Id == managedProgram.Id), "Inactive programs disappear from active listings");
    Check(await submissionService.SubmitApplicationAsync(managedDraft!.Id) == null, "A deactivated program cannot receive new draft submissions");
    Check(await db.WelfareApplications.AnyAsync(a => a.Id == managedDraft.Id), "Program deactivation preserves existing application records");
    var restoredProgram = await programsController.UpdateProgram(managedProgram.Id, ProgramUpdate(true, inactiveProgram.UpdatedAt));
    Check(restoredProgram is OkObjectResult && (await programService.GetActiveProgramsAsync()).Any(p => p.Id == managedProgram.Id), "Administrators can reactivate programs");
    Check(await db.AuditLogs.AnyAsync(a => a.EntityType == "WelfareProgram" && a.EntityId == managedProgram.Id.ToString() && a.UserId == admin.Id),
        "Program changes have administrator-attributed audit records");
    await SocialAuthChecks.Run(db, created!.User.Id, registration.Password, corrected.Id, "Regression-only-Password123!", new TokenService(tokenConfig), Check);
    await EvidenceRecordsChecks.Run(db, employee, other, hr, committee, manager, admin, submissionService, reviewService, Check);
    await OperationalWorkflowChecks.Run(db, employee, other, hr, committee, manager, admin, submissionService, reviewService, Check);
    // A fresh request context must load names without relying on EF's previously tracked fixture entities.
    await using (var listingDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.Database.GetDbConnection()).Options)) {
        await listingDb.Database.UseTransactionAsync(transaction.GetDbTransaction());
        var listingRepository = new WelfareApplicationRepository(listingDb);
        var personalApplications = (await listingRepository.GetByUserIdAsync(employee.Id)).ToList();
        Check(personalApplications.Count > 0 && personalApplications.All(a => !string.IsNullOrWhiteSpace(a.WelfareProgram?.Name) && !string.IsNullOrWhiteSpace(a.User?.FirstName)),
            "Personal application listings load program and employee names in a fresh request context");
        listingDb.ChangeTracker.Clear();
        var programApplications = (await listingRepository.GetByWelfareProgramIdAsync(program.Id)).ToList();
        Check(programApplications.Count > 0 && programApplications.All(a => !string.IsNullOrWhiteSpace(a.WelfareProgram?.Name)),
            "Program application listings load program names without tracked navigation entities");
        listingDb.ChangeTracker.Clear();
        var submittedApplications = (await listingRepository.GetByStatusAsync(2)).ToList();
        Check(submittedApplications.Count > 0 && submittedApplications.All(a => !string.IsNullOrWhiteSpace(a.WelfareProgram?.Name)),
            "Status application listings retain program names in a fresh request context");
    }
    Console.WriteLine($"{passed} regression checks passed.");
}
finally
{
    await transaction.RollbackAsync();
    Console.WriteLine("All regression fixtures rolled back.");
}
