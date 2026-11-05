using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
namespace WelfareSystem.Api.Controllers;
[ApiController, Route("api/detection-settings"), Authorize(Policy = "AdminOnly")]
public class DetectionSettingsController(AppDbContext db) : ControllerBase {
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<bool> Admin() => db.Users.AnyAsync(u => u.Id == ActorId && u.IsActive && u.Role.Name == "Administrator");
    [HttpGet] public async Task<IActionResult> Get() => !await Admin() ? Forbid() : Ok(await db.DetectionSettings.AsNoTracking().FirstOrDefaultAsync() ?? new DetectionSettings());
    [HttpPut] public async Task<IActionResult> Put(SettingsRequest request) {
        if (!await Admin()) return Forbid();
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542005)");
        var settings = await db.DetectionSettings.FirstOrDefaultAsync();
        if (settings == null) { settings = new DetectionSettings(); db.DetectionSettings.Add(settings); }
        var before = JsonSerializer.Serialize(settings);
        settings.FrequencyDays = request.FrequencyDays; settings.FrequencyLimit = request.FrequencyLimit; settings.DuplicateDays = request.DuplicateDays; settings.AmountPercentage = request.AmountPercentage; settings.EnableExperimentalModel = request.EnableExperimentalModel; settings.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new() { UserId = ActorId, Action = "DetectionSettingsChanged", EntityType = "DetectionSettings", EntityId = "global", Details = JsonSerializer.Serialize(new { request.Reason, before, after = request }) });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return Ok(settings);
    }
    public class SettingsRequest {
        [Range(1, 365)] public int FrequencyDays { get; set; } = 30;
        [Range(1, 100)] public int FrequencyLimit { get; set; } = 3;
        [Range(1, 365)] public int DuplicateDays { get; set; } = 7;
        [Range(1, 1000)] public double AmountPercentage { get; set; } = 50;
        public bool EnableExperimentalModel { get; set; }
        [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = "";
    }
}
