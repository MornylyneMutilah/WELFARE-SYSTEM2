using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;
[ApiController, Route("api/amendments"), Authorize]
public class AmendmentsController(AppDbContext db, IWelfareApplicationService applications) : ControllerBase {
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpGet] public async Task<IActionResult> List(int? applicationId = null) {
        var visible = await ApplicationAccess.Visible(db, ActorId);
        var query = db.DecisionAmendments.AsNoTracking().Where(x => db.ApplicationReviews.Any(r => r.Id == x.ReviewId && visible.Any(a => a.Id == r.ApplicationId)));
        if (applicationId.HasValue) query = query.Where(x => db.ApplicationReviews.Any(r => r.Id == x.ReviewId && r.ApplicationId == applicationId));
        return Ok(await query.OrderByDescending(x => x.CreatedAt).Take(200).Select(x => new { entry = x, applicationId = db.ApplicationReviews.Where(r => r.Id == x.ReviewId).Select(r => r.ApplicationId).First(), requester = db.Users.Where(u => u.Id == x.RequestedBy).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(), resolver = db.Users.Where(u => u.Id == x.ResolvedBy).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() }).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Create(ChangeRequest request) {
        if (request.Kind is not ("Appeal" or "Correction")) return BadRequest();
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542004)");
        var actor = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == ActorId && u.IsActive); if (actor == null) return Forbid();
        var review = await db.ApplicationReviews.AsNoTracking().Include(r => r.Application).FirstOrDefaultAsync(r => r.Id == request.ReviewId); if (review == null) return NotFound();
        var visible = await ApplicationAccess.Visible(db, ActorId); if (!await visible.AnyAsync(a => a.Id == review.ApplicationId)) return NotFound();
        if (request.Kind == "Appeal" && (review.Application.UserId != ActorId || review.Recommendation != "Reject" || review.Application.Status is not (6 or 9))) return BadRequest(new { message = "Only the owner of a currently rejected application may appeal its rejection." });
        if (request.Kind == "Correction" && (review.ReviewerId != ActorId || request.ProposedNotes.Trim().Length < 3)) return BadRequest(new { message = "Only the original reviewer can request a correction addendum, with proposed notes." });
        if (await db.DecisionAmendments.AnyAsync(x => x.ReviewId == request.ReviewId && x.Kind == request.Kind && (x.Status == "Pending" || (x.Kind == "Appeal" && x.Status == "Approved")))) return Conflict(new { message = "An active or accepted request already exists for this decision." });
        var entry = new DecisionAmendment { ReviewId = request.ReviewId, RequestedBy = ActorId, Kind = request.Kind, Reason = request.Reason.Trim(), ProposedNotes = request.ProposedNotes.Trim() }; db.DecisionAmendments.Add(entry);
        db.AuditLogs.Add(new() { UserId = ActorId, Action = request.Kind + "Requested", EntityType = "WelfareApplication", EntityId = review.ApplicationId.ToString(), Details = JsonSerializer.Serialize(request) });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return Ok(entry);
    }
    [HttpPost("{id:int}/decision"), Authorize(Policy = "ManagementAndAbove")]
    public async Task<IActionResult> Resolve(int id, ResolveRequest request) {
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542004)");
        if (!await db.Users.AnyAsync(u => u.Id == ActorId && u.IsActive && (u.Role.Name == "Administrator" || u.Role.Name == "Management"))) return Forbid();
        var entry = await db.DecisionAmendments.FirstOrDefaultAsync(x => x.Id == id); if (entry == null) return NotFound(); await db.Entry(entry).ReloadAsync();
        var review = await db.ApplicationReviews.Include(r => r.Application).FirstAsync(r => r.Id == entry.ReviewId);
        if (entry.Status != "Pending" || ActorId == entry.RequestedBy || ActorId == review.ReviewerId) return Conflict(new { message = "An independent manager or administrator must resolve a pending request." });
        if (request.Approve && entry.Kind == "Appeal") {
            var original = review.Application;
            var draft = await applications.CreateApplicationAsync(original.UserId, new() { WelfareProgramId = original.WelfareProgramId, RequestedAmount = original.RequestedAmount, Reason = original.Reason });
            if (draft == null) return BadRequest(new { message = "The program must be active before a reconsideration draft can be created." });
            entry.ReconsiderationApplicationId = draft.Id;
        }
        entry.Status = request.Approve ? "Approved" : "Rejected"; entry.ResolvedBy = ActorId; entry.ResolvedAt = DateTime.UtcNow; entry.Resolution = request.Reason.Trim();
        db.AuditLogs.Add(new() { UserId = ActorId, Action = entry.Kind + entry.Status, EntityType = "WelfareApplication", EntityId = review.ApplicationId.ToString(), Details = JsonSerializer.Serialize(new { amendmentId = id, entry.Resolution, entry.ProposedNotes, entry.ReconsiderationApplicationId }) });
        db.Notifications.Add(new() { UserId = entry.RequestedBy, Title = entry.Kind + " " + entry.Status, Message = entry.Resolution + (entry.ReconsiderationApplicationId.HasValue ? $" Review and submit reconsideration draft #{entry.ReconsiderationApplicationId}." : " Original decision remains preserved."), Type = "DecisionAmendment", EntityType = "WelfareApplication", EntityId = review.ApplicationId.ToString() });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return Ok(entry);
    }
    public class ChangeRequest { [Range(1, int.MaxValue)] public int ReviewId { get; set; } [Required] public string Kind { get; set; } = "Appeal"; [Required, MinLength(3), MaxLength(2000)] public string Reason { get; set; } = ""; [MaxLength(2000)] public string ProposedNotes { get; set; } = ""; }
    public class ResolveRequest { public bool Approve { get; set; } [Required, MinLength(3), MaxLength(900)] public string Reason { get; set; } = ""; }
}
