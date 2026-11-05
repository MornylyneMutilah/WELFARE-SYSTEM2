using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services;

namespace WelfareSystem.Api.Controllers;

[ApiController, Route("api/auditlogs"), Authorize(Policy = "AdminOnly")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuditLogsController(AppDbContext db) : ControllerBase
{
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<bool> Admin() => db.Users.AnyAsync(u => u.Id == ActorId && u.IsActive && u.Role.Name == "Administrator");
    private IQueryable<AuditLog> Query(string? action, string? entity, int? actor, DateTime? from, DateTime? to) {
        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(a => a.EntityType == entity);
        if (actor.HasValue) query = query.Where(a => a.UserId == actor.Value);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(a => a.Timestamp < to.Value.ToUniversalTime().Date.AddDays(1));
        return query;
    }
    public static string Redact(string details) {
        try {
            var node = JsonNode.Parse(details);
            void Visit(JsonNode? n) {
                if (n is JsonObject obj) foreach (var key in obj.Select(p => p.Key).ToArray()) {
                    var normalized = key.Replace("_", "").ToLowerInvariant();
                    if (new[] { "password", "token", "secret", "connectionstring", "authorization" }.Any(normalized.Contains)) obj[key] = "[redacted]";
                    else Visit(obj[key]);
                }
                else if (n is JsonArray array) foreach (var item in array) Visit(item);
            }
            Visit(node); return node?.ToJsonString() ?? "{}";
        } catch (JsonException) { return "Details are not available in structured format."; }
    }
    private async Task<List<object>> Rows(IQueryable<AuditLog> query, int skip, int take) {
        var rows = await query.OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Skip(skip).Take(take)
            .Select(a => new { a.Id, a.UserId, actorName = a.User == null ? "System or former user" : a.User.FirstName + " " + a.User.LastName, a.Action, a.EntityType, a.EntityId, a.Timestamp, a.Details }).ToListAsync();
        return rows.Select(a => (object)new { a.Id, a.UserId, a.actorName, a.Action, a.EntityType, a.EntityId, a.Timestamp, details = Redact(a.Details) }).ToList();
    }
    [HttpGet]
    public async Task<IActionResult> List(string? action = null, string? entity = null, int? actor = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 25) {
        if (!await Admin()) return Forbid();
        var query = Query(action, entity, actor, from, to); page = Math.Clamp(page, 1, 100000); pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(new { total = await query.CountAsync(), page, pageSize, items = await Rows(query, (page - 1) * pageSize, pageSize) });
    }
    [HttpGet("export.csv")]
    public async Task<IActionResult> Export(string? action = null, string? entity = null, int? actor = null, DateTime? from = null, DateTime? to = null) {
        if (!await Admin()) return Forbid();
        var query = Query(action, entity, actor, from, to); var count = await query.CountAsync();
        if (count > 10000) return BadRequest(new { message = "Narrow the filters to export at most 10,000 audit events." });
        var rows = await query.OrderByDescending(a => a.Timestamp).Select(a => new { a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.Timestamp, a.Details }).ToListAsync();
        var data = new List<IEnumerable<string?>> { new[] { "Event ID", "Actor ID", "Action", "Entity", "Entity ID", "Time UTC", "Details" } };
        data.AddRange(rows.Select(a => new[] { a.Id.ToString(), a.UserId?.ToString(), a.Action, a.EntityType, a.EntityId, a.Timestamp.ToString("O"), Redact(a.Details) }));
        db.AuditLogs.Add(new AuditLog { UserId = ActorId, Action = "AuditLogExport", EntityType = "Report", EntityId = "export", Details = JsonSerializer.Serialize(new { count }) }); await db.SaveChangesAsync();
        return File(CsvExport.Bytes(data), "text/csv; charset=utf-8", "welfare-audit.csv");
    }
}
