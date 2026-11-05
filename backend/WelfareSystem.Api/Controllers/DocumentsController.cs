using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Models.DTOs.Document;
using WelfareSystem.Api.Services;

namespace WelfareSystem.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DocumentsController(AppDbContext db, PrivateDocumentStore storage, ILogger<DocumentsController> logger) : ControllerBase
{
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private async Task<bool> Visible(int id) => await (await ApplicationAccess.Visible(db, ActorId)).AnyAsync(a => a.Id == id);
    private DocumentResponse Map(ApplicationDocument d) => new() { Id = d.Id, ApplicationId = d.ApplicationId, FileName = d.FileName,
        FileType = d.FileType, FileSize = d.FileSize, UploadedAt = d.UploadedAt, RequirementName = d.RequirementName, ScanStatus = string.IsNullOrWhiteSpace(d.ScanStatus) ? "Legacy/Unknown" : d.ScanStatus, ContentAvailable = storage.Exists(d.FilePath) };

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDocumentById(int id) {
        var d = await db.ApplicationDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        return d == null || !await Visible(d.ApplicationId) ? NotFound() : Ok(Map(d));
    }
    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetDocumentsByApplicationId(int applicationId) {
        if (!await Visible(applicationId)) return NotFound();
        return Ok((await db.ApplicationDocuments.AsNoTracking().Where(d => d.ApplicationId == applicationId).ToListAsync()).Select(Map));
    }
    [HttpGet("{id:int}/content")]
    public async Task<IActionResult> Download(int id) {
        var d = await db.ApplicationDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (d == null || !await Visible(d.ApplicationId)) return NotFound();
        var stream = storage.Open(d.FilePath);
        if (stream == null) return NotFound(new { message = "File content is unavailable. Older metadata-only uploads must be supplied again while the application is a draft." });
        db.AuditLogs.Add(new AuditLog { UserId = ActorId, Action = "DocumentDownloaded", EntityType = "WelfareApplication", EntityId = d.ApplicationId.ToString(), Details = System.Text.Json.JsonSerializer.Serialize(new { documentId = id }) });
        try { await db.SaveChangesAsync(); } catch { stream.Dispose(); throw; }
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, d.FileType, d.FileName, enableRangeProcessing: false);
    }
    [HttpPost("application/{applicationId:int}"), RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(int applicationId, [FromForm] IFormFile file, [FromForm] string requirement = "") {
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"WelfareApplications\" WHERE \"Id\" = {applicationId} FOR UPDATE");
        var app = await db.WelfareApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == ActorId && a.User.IsActive);
        if (app == null) return NotFound();
        if (app.Status != 1) return BadRequest(new { message = "Supporting documents can only be changed on your own draft. Submitted evidence is retained unchanged." });
        var used = await db.ApplicationDocuments.Where(d => d.ApplicationId == applicationId).SumAsync(d => (long?)d.FileSize) ?? 0;
        if (used + (file?.Length ?? 0) > 50 * 1024 * 1024 || await db.ApplicationDocuments.CountAsync(d => d.ApplicationId == applicationId) >= 20) return BadRequest(new { message = "A draft can hold at most 20 files and 50 MB of evidence. Remove unnecessary draft evidence first." });
        var program = await db.WelfarePrograms.AsNoTracking().FirstAsync(p => p.Id == app.WelfareProgramId);
        string[] requirements;
        try { requirements = System.Text.Json.JsonSerializer.Deserialize<string[]>(program.RequiredDocuments) ?? []; }
        catch (System.Text.Json.JsonException) { return BadRequest(new { message = "Program document requirements need administrator correction." }); }
        if (requirement.Length > 200 || (!string.IsNullOrEmpty(requirement) && !requirements.Contains(requirement, StringComparer.OrdinalIgnoreCase))) return BadRequest(new { message = "Select one of this program's document requirements, or additional evidence." });
        string? key = null;
        try {
            if (file == null) return BadRequest(new { message = "Choose a file." });
            var saved = await storage.Save(file, HttpContext.RequestAborted); key = saved.Key;
            var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl)) throw new InvalidDataException("Choose a filename of at most 255 characters without control characters.");
            var doc = new ApplicationDocument { ApplicationId = applicationId, FileName = name, FileType = saved.ContentType, FileSize = saved.Size, FilePath = key, ContentHash = saved.Sha256, RequirementName = requirement, ScanStatus = saved.ScanStatus };
            db.ApplicationDocuments.Add(doc);
            db.AuditLogs.Add(new AuditLog { UserId = ActorId, Action = "DocumentUploaded", EntityType = "WelfareApplication", EntityId = applicationId.ToString(), Details = System.Text.Json.JsonSerializer.Serialize(new { fileName = name, size = saved.Size, sha256 = saved.Sha256 }) });
            await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync();
            return CreatedAtAction(nameof(GetDocumentById), new { id = doc.Id }, Map(doc));
        } catch (InvalidDataException error) { if (key != null) storage.Remove(key); return BadRequest(new { message = error.Message }); }
        catch (Exception error) when (error is IOException or DbUpdateException) {
            if (key != null) storage.Remove(key);
            logger.LogError(error, "Document persistence failed for application {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "The file could not be stored. Please retry." });
        } catch { if (key != null) storage.Remove(key); throw; }
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDocument(int id) {
        var document = await db.ApplicationDocuments.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null) return NotFound();
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"WelfareApplications\" WHERE \"Id\" = {document.ApplicationId} FOR UPDATE");
        if (!await db.WelfareApplications.AnyAsync(a => a.Id == document.ApplicationId && a.UserId == ActorId && a.User.IsActive)) return NotFound();
        if (!await db.WelfareApplications.AnyAsync(a => a.Id == document.ApplicationId && a.Status == 1)) return BadRequest(new { message = "Submitted evidence cannot be deleted." });
        db.ApplicationDocuments.Remove(document);
        db.AuditLogs.Add(new AuditLog { UserId = ActorId, Action = "DraftDocumentRemoved", EntityType = "WelfareApplication", EntityId = document.ApplicationId.ToString(), Details = System.Text.Json.JsonSerializer.Serialize(new { documentId = id, document.FileName }) });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync();
        try { storage.Remove(document.FilePath); } catch (IOException error) { logger.LogWarning(error, "Orphan document needs storage cleanup: {DocumentId}", id); }
        return Ok(new { message = "Draft document removed." });
    }
}
