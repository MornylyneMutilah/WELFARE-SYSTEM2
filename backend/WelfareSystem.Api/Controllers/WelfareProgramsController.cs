using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Models.DTOs.WelfareProgram;
using WelfareSystem.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using System.Security.Claims;
using System.Text.Json;
using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WelfareProgramsController : ControllerBase
{
    private readonly IWelfareProgramService _welfareProgramService;
    private readonly AppDbContext _context;

    public WelfareProgramsController(IWelfareProgramService welfareProgramService, AppDbContext context)
    {
        _welfareProgramService = welfareProgramService;
        _context = context;
    }

    private async Task Audit(int id, string action, string reason, object? before, object? after)
    {
        var actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _context.AuditLogs.Add(new AuditLog { UserId = actor, Action = action, EntityType = "WelfareProgram", EntityId = id.ToString(),
            Details = JsonSerializer.Serialize(new { reason = string.IsNullOrWhiteSpace(reason) ? action : reason.Trim(), before, after }) });
        await _context.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPrograms()
    {
        var programs = await _welfareProgramService.GetAllProgramsAsync();
        return Ok(programs);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActivePrograms()
    {
        var programs = await _welfareProgramService.GetActiveProgramsAsync();
        return Ok(programs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgramById(int id)
    {
        var program = await _welfareProgramService.GetProgramByIdAsync(id);
        if (program == null)
        {
            return NotFound(new { message = "Welfare program not found" });
        }

        return Ok(program);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateProgram([FromBody] CreateWelfareProgramRequest request)
    {
        if (!DocumentListValidator.IsValid(request.RequiredDocuments)) return BadRequest(new { message = "Required documents must be a JSON list of document names, or an empty list." });
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await using var tx = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542002)");
        if (await _context.WelfarePrograms.AnyAsync(p => p.Name.ToLower() == request.Name.Trim().ToLower())) return Conflict(new { message = "A program with this name already exists." });
        var program = await _welfareProgramService.CreateProgramAsync(request);
        if (program == null)
        {
            return BadRequest(new { message = "Program creation failed" });
        }

        await Audit(program.Id, "ProgramCreated", request.Reason, null, program);
        if (tx != null) await tx.CommitAsync();
        return CreatedAtAction(nameof(GetProgramById), new { id = program.Id }, program);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateProgram(int id, [FromBody] UpdateWelfareProgramRequest request)
    {
        if (!DocumentListValidator.IsValid(request.RequiredDocuments)) return BadRequest(new { message = "Required documents must be a JSON list of document names, or an empty list." });
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await using var tx = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542002)");
        var before = await _welfareProgramService.GetProgramByIdAsync(id);
        if (before == null) return NotFound(new { message = "Program not found." });
        if (request.ExpectedUpdatedAt.HasValue && Math.Abs((request.ExpectedUpdatedAt.Value.ToUniversalTime() - before.UpdatedAt).Ticks) > 9)
            return Conflict(new { message = "This program was changed elsewhere. Refresh and reopen the form." });
        if (await _context.WelfarePrograms.AnyAsync(p => p.Id != id && p.Name.ToLower() == request.Name.Trim().ToLower())) return Conflict(new { message = "A program with this name already exists." });
        var program = await _welfareProgramService.UpdateProgramAsync(id, request);
        if (program == null)
        {
            return BadRequest(new { message = "Update failed. Program not found." });
        }

        await Audit(id, "ProgramUpdated", request.Reason, before, program);
        if (tx != null) await tx.CommitAsync();
        return Ok(program);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateProgram(int id)
    {
        await using var tx = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542002)");
        var before = await _welfareProgramService.GetProgramByIdAsync(id);
        var success = await _welfareProgramService.DeactivateProgramAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Welfare program not found" });
        }

        await Audit(id, "ProgramDeactivated", "Administrator deactivated the program", before, await _welfareProgramService.GetProgramByIdAsync(id));
        if (tx != null) await tx.CommitAsync();
        return Ok(new { message = "Welfare program deactivated successfully" });
    }
}
