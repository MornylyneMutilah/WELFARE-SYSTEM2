using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Services;
namespace WelfareSystem.Api.Controllers;
[ApiController, Route("api/operations"), Authorize(Policy = "AdminOnly")]
public class OperationsController(AppDbContext db, IConfiguration config, IHostEnvironment environment, MaintenanceGate gate, IAccountMail mail) : ControllerBase {
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<bool> Admin() => db.Users.AnyAsync(u => u.Id == ActorId && u.IsActive && u.Role.Name == "Administrator");
    [HttpGet("status")] public async Task<IActionResult> Status() {
        if (!await Admin()) return Forbid();
        var connection = new Npgsql.NpgsqlConnectionStringBuilder(db.Database.GetDbConnection().ConnectionString);
        var databaseIdentityHash = ExternalAuthFlowStore.Hash($"{(connection.Host ?? "").ToLowerInvariant()}|{connection.Port}|{connection.Database}|{connection.Username}");
        return Ok(new { environment = environment.EnvironmentName, maintenance = gate.Enabled, activeWrites = gate.ActiveWriters,
            databaseIdentityHash,
            pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToArray(), emailDelivery = mail.Mode,
            queuedAccountEmails = Directory.Exists(Path.Combine(environment.ContentRootPath, "App_Data", "MailPending")) ? Directory.GetFiles(Path.Combine(environment.ContentRootPath, "App_Data", "MailPending"), "*.eml").Length : 0,
            scanner = string.IsNullOrWhiteSpace(config["Documents:ScannerHost"]) ? environment.IsDevelopment() ? "Development: not scanned" : "Unavailable: uploads fail closed" : "Configured: verify daemon and signatures operationally",
            unverifiedEmploymentRecords = await db.Users.CountAsync(u => u.IsActive && u.EmploymentVerifiedAt == null),
            accountsRequiringPasswordChange = await db.Users.CountAsync(u => u.IsActive && u.MustChangePassword),
            historicalApprovedApplications = await db.WelfareApplications.CountAsync(a => a.Status == 8),
            modelMinimumApprovedApplicationsPerProgram = 50,
            backupNote = "Use the coordinated backup script with this single API instance. Backups and restore drills require an operator; health checks do not prove recovery readiness." });
    }
    [HttpPost("maintenance")] public async Task<IActionResult> Maintenance(MaintenanceRequest request) {
        if (!await Admin()) return Forbid();
        var drained = await gate.Set(request.Enabled, HttpContext.RequestAborted);
        db.AuditLogs.Add(new() { UserId = ActorId, Action = request.Enabled ? "MaintenanceEnabled" : "MaintenanceDisabled", EntityType = "Operations", EntityId = "local-instance", Details = JsonSerializer.Serialize(new { request.Reason, drained }) }); await db.SaveChangesAsync();
        return drained ? Ok(new { enabled = gate.Enabled, activeWrites = gate.ActiveWriters }) : StatusCode(503, new { message = "Maintenance enabled but writes have not drained. Do not start a backup yet.", enabled = true });
    }
    public class MaintenanceRequest { public bool Enabled { get; set; } [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = ""; }
}
