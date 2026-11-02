using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public class AnomaliesController(AppDbContext context, IAnomalyDetectionService detection) : ControllerBase
{
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private async Task<IQueryable<Anomaly>> ScopedQueryAsync()
    {
        var actor = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == ActorId && u.IsActive);
        var query = context.Anomalies.AsNoTracking();
        return actor?.Role.Name switch
        {
            "Administrator" or "CommitteeMember" or "Management" => query,
            "HROfficer" => query.Where(a => a.User != null && a.User.DepartmentId == actor.DepartmentId),
            "Employee" => query.Where(a => a.UserId == actor.Id),
            _ => query.Where(a => false)
        };
    }

    private async Task<bool> CanManageAsync() => await context.Users.AnyAsync(u =>
        u.Id == ActorId && u.IsActive && (u.Role.Name == "Administrator" || u.Role.Name == "HROfficer" ||
        u.Role.Name == "CommitteeMember" || u.Role.Name == "Management"));

    private static object Summary(Anomaly a) => new
    {
        a.Id, a.AnomalyType, a.Severity, a.Description, a.ApplicationId, a.UserId,
        a.Status, a.DetectedAt, a.ResolvedAt, a.ResolvedBy, a.ResolutionNotes, a.AnomalyScore
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? severity = null, [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var query = await ScopedQueryAsync();
        if (!string.IsNullOrWhiteSpace(severity)) {
            var severities = severity.ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(a => severities.Contains(a.Severity.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(status)) {
            var statuses = status.ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(a => statuses.Contains(a.Status.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(a => a.AnomalyType.ToLower() == type.ToLower());
        return Ok((await query.OrderByDescending(a => a.DetectedAt).Take(100).ToListAsync()).Select(Summary));
    }

    [HttpGet("{id:int}"), HttpGet("{id:int}/details")]
    public async Task<IActionResult> GetById(int id)
    {
        var a = await (await ScopedQueryAsync()).FirstOrDefaultAsync(a => a.Id == id);
        if (a == null) return NotFound();
        return Ok(new { a.Id, a.AnomalyType, a.Severity, a.Description, a.ApplicationId, a.UserId,
            a.Status, a.DetectedAt, a.ResolvedAt, a.ResolvedBy, a.ResolutionNotes, a.AnomalyScore,
            DetectionDetails = await CanManageAsync() ? a.DetectionDetails : null });
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(int applicationId) => Ok(
        (await (await ScopedQueryAsync()).Where(a => a.ApplicationId == applicationId)
            .OrderByDescending(a => a.DetectedAt).ToListAsync()).Select(Summary));

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetByUserId(int userId) => Ok(
        (await (await ScopedQueryAsync()).Where(a => a.UserId == userId)
            .OrderByDescending(a => a.DetectedAt).ToListAsync()).Select(Summary));

    [HttpGet("department/{departmentId:int}")]
    public async Task<IActionResult> GetByDepartmentId(int departmentId) => Ok(
        (await (await ScopedQueryAsync()).Where(a => a.User != null && a.User.DepartmentId == departmentId)
            .OrderByDescending(a => a.DetectedAt).ToListAsync()).Select(Summary));

    [HttpGet("stats/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var query = await ScopedQueryAsync();
        var severityCounts = await query.GroupBy(a => a.Severity).ToDictionaryAsync(g => g.Key, g => g.Count());
        var typeCounts = await query.GroupBy(a => a.AnomalyType).ToDictionaryAsync(g => g.Key, g => g.Count());
        var statusCounts = await query.GroupBy(a => a.Status).ToDictionaryAsync(g => g.Key, g => g.Count());
        return Ok(new { severityCounts, typeCounts, statusCounts, totalAnomalies = severityCounts.Values.Sum() });
    }

    [HttpPost("{id:int}/resolve")]
    public Task<IActionResult> Resolve(int id, [FromBody] DecisionRequest request) => Decide(id, "Resolved", request.Notes);
    [HttpPost("{id:int}/investigate")]
    public Task<IActionResult> Investigate(int id) => Decide(id, "Investigating", "Investigation started");
    [HttpPost("{id:int}/dismiss")]
    public Task<IActionResult> Dismiss(int id, [FromBody] DecisionRequest request) => Decide(id, "Dismissed", request.Notes);

    private async Task<IActionResult> Decide(int id, string status, string notes)
    {
        if (!await CanManageAsync()) return Forbid();
        var visible = await (await ScopedQueryAsync()).FirstOrDefaultAsync(a => a.Id == id);
        if (visible == null) return NotFound();
        if (visible.Status is "Resolved" or "Dismissed") return Conflict(new { message = "This anomaly is already closed." });
        if (status == "Resolved") await detection.MarkAsResolvedAsync(id, ActorId, notes);
        else if (status == "Dismissed") await detection.DismissAnomalyAsync(id, ActorId, notes);
        else await detection.MarkAsInvestigatingAsync(id);
        context.AuditLogs.Add(new AuditLog { UserId = ActorId, Action = $"Anomaly{status}",
            EntityType = "Anomaly", EntityId = id.ToString(), Details = System.Text.Json.JsonSerializer.Serialize(new { notes }) });
        await context.SaveChangesAsync();
        return Ok(new { message = $"Anomaly marked as {status.ToLowerInvariant()}" });
    }

    [HttpPost("detect/application/{applicationId:int}")]
    public async Task<IActionResult> DetectForApplication(int applicationId)
    {
        if (!await CanManageAsync()) return Forbid();
        var actor = await context.Users.Include(u => u.Role).SingleAsync(u => u.Id == ActorId);
        var app = await context.WelfareApplications.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app == null || (actor.Role.Name == "HROfficer" && app.User.DepartmentId != actor.DepartmentId)) return NotFound();
        if (app.Status == 1) return BadRequest(new { message = "Submit the draft before running detection." });
        return Ok((await detection.DetectAllAnomaliesForApplicationAsync(applicationId)).Select(Summary));
    }

    public class DecisionRequest
    {
        [Required, MaxLength(2000)]
        public string Notes { get; set; } = string.Empty;
    }
}
