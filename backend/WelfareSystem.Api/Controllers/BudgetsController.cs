using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Controllers;
[ApiController, Route("api/budgets"), Authorize(Policy = "ManagementAndAbove")]
public class BudgetsController(AppDbContext db) : ControllerBase {
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<bool> Role(string role) => db.Users.AnyAsync(u => u.Id == ActorId && u.IsActive && u.Role.Name == role);
    [HttpGet] public async Task<IActionResult> List(int? year = null) {
        if (!await Role("Administrator") && !await Role("Management")) return Forbid();
        var selected = year ?? DateTime.UtcNow.Year;
        if (selected < 2000 || selected > 2200) return BadRequest();
        var start = new DateTime(selected, 1, 1, 0, 0, 0, DateTimeKind.Utc); var end = start.AddYears(1);
        var committed = await db.WelfareApplications.Where(a => a.Status == 8 && a.UpdatedAt >= start && a.UpdatedAt < end).GroupBy(a => a.WelfareProgramId).Select(g => new { id = g.Key, total = g.Sum(a => a.RequestedAmount) }).ToDictionaryAsync(x => x.id, x => x.total);
        var budgets = await db.ProgramBudgets.AsNoTracking().Where(b => b.Year == selected).ToListAsync();
        var programs = await db.WelfarePrograms.AsNoTracking().OrderBy(p => p.Name).Select(p => new { p.Id, p.Name }).ToListAsync();
        return Ok(programs.Select(p => { var budget = budgets.FirstOrDefault(b => b.WelfareProgramId == p.Id); var spent = committed.GetValueOrDefault(p.Id); return new { programId = p.Id, programName = p.Name, year = selected, budget, committed = spent, remaining = (budget?.AllocatedAmount ?? 0) - spent }; }));
    }
    [HttpPost] public async Task<IActionResult> Propose(BudgetRequest request) {
        if (!await Role("Administrator")) return Forbid();
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542003)");
        if (!await db.WelfarePrograms.AnyAsync(p => p.Id == request.ProgramId)) return BadRequest(new { message = "Select an existing program." });
        var budget = await db.ProgramBudgets.FirstOrDefaultAsync(b => b.WelfareProgramId == request.ProgramId && b.Year == request.Year);
        if (budget == null) { budget = new ProgramBudget { WelfareProgramId = request.ProgramId, Year = request.Year }; db.ProgramBudgets.Add(budget); }
        else if (budget.ProposalStatus == "Pending") return Conflict(new { message = "Resolve the existing pending budget proposal first." });
        budget.ProposedAmount = request.Amount; budget.ProposalStatus = "Pending"; budget.RequestedBy = ActorId; budget.Reason = request.Reason.Trim(); budget.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new() { UserId = ActorId, Action = "BudgetProposed", EntityType = "WelfareProgram", EntityId = request.ProgramId.ToString(), Details = JsonSerializer.Serialize(request) });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return Ok(budget);
    }
    [HttpPost("{id:int}/decision")] public async Task<IActionResult> Decide(int id, BudgetDecision request) {
        if (!await Role("Management")) return Forbid();
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542003)");
        var budget = await db.ProgramBudgets.FirstOrDefaultAsync(b => b.Id == id); if (budget == null) return NotFound();
        await db.Entry(budget).ReloadAsync();
        if (budget.ProposalStatus != "Pending" || budget.RequestedBy == ActorId) return Conflict(new { message = "This proposal has been decided, or you cannot decide your own proposal." });
        var before = budget.AllocatedAmount;
        if (request.Approve) budget.AllocatedAmount = budget.ProposedAmount;
        budget.ProposalStatus = request.Approve ? "Approved" : "Rejected"; budget.ApprovedBy = ActorId; budget.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new() { UserId = ActorId, Action = request.Approve ? "BudgetApproved" : "BudgetRejected", EntityType = "WelfareProgram", EntityId = budget.WelfareProgramId.ToString(), Details = JsonSerializer.Serialize(new { budget.Year, before, budget.AllocatedAmount, request.Reason }) });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return Ok(budget);
    }
    public class BudgetRequest { [Range(1, int.MaxValue)] public int ProgramId { get; set; } [Range(2000, 2200)] public int Year { get; set; } [Range(typeof(decimal), "0", "1000000000")] public decimal Amount { get; set; } [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = ""; }
    public class BudgetDecision { public bool Approve { get; set; } [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = ""; }
}
