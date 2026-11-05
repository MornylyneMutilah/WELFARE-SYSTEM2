using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services;

namespace WelfareSystem.Api.Controllers;

[ApiController, Route("api/records"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RecordsController(AppDbContext db) : ControllerBase
{
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private async Task<IQueryable<ApplicationReview>?> Decisions(string scope) {
        var actor = await db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == ActorId && u.IsActive);
        if (actor?.Role.Name is not ("HROfficer" or "CommitteeMember" or "Management" or "Administrator")) return null;
        var query = db.ApplicationReviews.AsNoTracking();
        if (actor.Role.Name != "Administrator") query = query.Where(r => r.ReviewerRole == actor.Role.Name);
        if (actor.Role.Name == "HROfficer") query = query.Where(r => r.Application.User.DepartmentId == actor.DepartmentId || r.ReviewerId == ActorId);
        if (scope == "mine") query = query.Where(r => r.ReviewerId == ActorId);
        return query;
    }
    private static IQueryable<ApplicationReview> Filter(IQueryable<ApplicationReview> query, string? search, string? stage, string? decision, DateTime? from, DateTime? to) {
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLower(); query = query.Where(r => r.ApplicationId.ToString() == term || r.Id.ToString() == term || r.Application.User.FirstName.ToLower().Contains(term) || r.Application.User.LastName.ToLower().Contains(term) || r.Application.WelfareProgram.Name.ToLower().Contains(term)); }
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(r => r.ReviewerRole == stage);
        if (!string.IsNullOrWhiteSpace(decision)) query = query.Where(r => r.Recommendation == decision);
        if (from.HasValue) query = query.Where(r => r.ReviewedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(r => r.ReviewedAt < to.Value.ToUniversalTime().Date.AddDays(1));
        return query;
    }
    private static IQueryable<DecisionRecord> Project(IQueryable<ApplicationReview> query) => query.Select(r => new DecisionRecord {
        Id = r.Id, ApplicationId = r.ApplicationId, EmployeeName = r.Application.User.FirstName + " " + r.Application.User.LastName,
        ProgramName = r.Application.WelfareProgram.Name, Amount = r.Application.RequestedAmount, ReviewerId = r.ReviewerId,
        ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName, Stage = r.ReviewerRole,
        Decision = r.Recommendation, Comments = r.Comments, ReviewedAt = r.ReviewedAt, CurrentStatus = r.Application.Status
    });
    [HttpGet("decisions"), Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> List(string scope = "team", string? search = null, string? stage = null, string? decision = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 25) {
        var query = await Decisions(scope); if (query == null) return Forbid();
        query = Filter(query, search, stage, decision, from, to);
        page = Math.Clamp(page, 1, 100000); pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(new { total = await query.CountAsync(), page, pageSize, items = await Project(query.OrderByDescending(r => r.ReviewedAt).ThenByDescending(r => r.Id)).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync() });
    }
    [HttpGet("decisions/{id:int}")]
    public async Task<IActionResult> Receipt(int id) {
        var visible = await ApplicationAccess.Visible(db, ActorId);
        var query = db.ApplicationReviews.AsNoTracking().Where(r => visible.Any(a => a.Id == r.ApplicationId));
        var own = await Decisions("mine");
        if (own != null) query = db.ApplicationReviews.AsNoTracking().Where(r => visible.Any(a => a.Id == r.ApplicationId) || own.Any(o => o.Id == r.Id));
        var record = await Project(query.Where(r => r.Id == id)).FirstOrDefaultAsync(); if (record == null) return NotFound();
        var logs = await db.AuditLogs.AsNoTracking().Where(a => a.Action == "ApplicationReview" && a.EntityType == "WelfareApplication" && a.EntityId == record.ApplicationId.ToString()).Select(a => a.Details).ToListAsync();
        JsonElement? snapshot = null;
        foreach (var details in logs) { try { using var json = JsonDocument.Parse(details); if (json.RootElement.TryGetProperty("reviewId", out var key) && key.GetInt32() == id && json.RootElement.TryGetProperty("snapshot", out var s)) { snapshot = s.Clone(); break; } } catch (JsonException) { } }
        return Ok(new { reference = $"WEL-DEC-{id:D6}", record, snapshot, historicalSnapshotAvailable = snapshot.HasValue });
    }
    [HttpGet("decisions.csv"), Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> Export(string scope = "team", string? search = null, string? stage = null, string? decision = null, DateTime? from = null, DateTime? to = null) {
        var query = await Decisions(scope); if (query == null) return Forbid();
        query = Filter(query, search, stage, decision, from, to);
        if (await query.CountAsync() > 10000) return BadRequest(new { message = "Narrow the date range to export at most 10,000 decisions." });
        var rows = await Project(query.OrderByDescending(r => r.ReviewedAt)).ToListAsync();
        var data = new List<IEnumerable<string?>> { new[] { "Decision reference", "Application ID", "Employee (current)", "Program (current)", "Requested amount", "Stage", "Decision", "Reviewer (current)", "Decision time UTC", "Notes", "Current application status" } };
        data.AddRange(rows.Select(r => new[] { $"WEL-DEC-{r.Id:D6}", r.ApplicationId.ToString(), r.EmployeeName, r.ProgramName, r.Amount.ToString(CultureInfo.InvariantCulture), r.Stage, r.Decision, r.ReviewerName, r.ReviewedAt.ToString("O"), r.Comments, r.CurrentStatus.ToString() }));
        await AuditExport("DecisionRegisterExport", rows.Count);
        return File(CsvExport.Bytes(data), "text/csv; charset=utf-8", "welfare-decisions.csv");
    }
    [HttpGet("applications/{id:int}")]
    public async Task<IActionResult> Timeline(int id) {
        if (!await (await ApplicationAccess.Visible(db, ActorId)).AnyAsync(a => a.Id == id)) return NotFound();
        var history = await db.ApplicationStatusHistories.AsNoTracking().Where(h => h.ApplicationId == id).OrderBy(h => h.ChangedAt)
            .Select(h => new { h.Id, h.Status, h.ChangedBy, h.ChangedAt, h.Notes, actorName = db.Users.Where(u => u.Id == h.ChangedBy).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() }).ToListAsync();
        var assessments = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "WelfareApplication" && a.EntityId == id.ToString() && a.Action == "EligibilityAssessed")
            .OrderBy(a => a.Timestamp).Select(a => new { a.Id, a.Timestamp, a.Details }).ToListAsync();
        return Ok(new { history, assessments });
    }
    [HttpGet("applications.csv")]
    public async Task<IActionResult> ApplicationsCsv(DateTime? from = null, DateTime? to = null) {
        var query = await ApplicationAccess.Visible(db, ActorId);
        if (from.HasValue) query = query.Where(a => a.SubmittedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(a => a.SubmittedAt < to.Value.ToUniversalTime().Date.AddDays(1));
        if (await query.CountAsync() > 10000) return BadRequest(new { message = "Narrow the date range to at most 10,000 applications." });
        var rows = await query.OrderByDescending(a => a.SubmittedAt).Select(a => new { a.Id, Employee = a.User.FirstName + " " + a.User.LastName, Program = a.WelfareProgram.Name, a.RequestedAmount, a.Status, a.IsEligible, a.SubmittedAt }).ToListAsync();
        var data = new List<IEnumerable<string?>> { new[] { "Application ID", "Employee", "Program", "Requested amount (not paid)", "Status code", "Eligible", "Draft creation or submission UTC" } };
        data.AddRange(rows.Select(a => new[] { a.Id.ToString(), a.Employee, a.Program, a.RequestedAmount.ToString(CultureInfo.InvariantCulture), a.Status.ToString(), a.IsEligible.ToString(), a.SubmittedAt.ToString("O") }));
        await AuditExport("ApplicationReportExport", rows.Count);
        return File(CsvExport.Bytes(data), "text/csv; charset=utf-8", "welfare-applications.csv");
    }
    private async Task AuditExport(string action, int count) { db.AuditLogs.Add(new AuditLog { UserId = ActorId, Action = action, EntityType = "Report", EntityId = "export", Details = JsonSerializer.Serialize(new { count }) }); await db.SaveChangesAsync(); }
}

public class DecisionRecord {
    public int Id { get; set; } public int ApplicationId { get; set; } public string EmployeeName { get; set; } = "";
    public string ProgramName { get; set; } = ""; public decimal Amount { get; set; } public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = ""; public string Stage { get; set; } = ""; public string Decision { get; set; } = "";
    public string Comments { get; set; } = ""; public DateTime ReviewedAt { get; set; } public int CurrentStatus { get; set; }
}
