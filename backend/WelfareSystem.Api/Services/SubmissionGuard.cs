using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Services;

public class SubmissionValidationException(IReadOnlyList<string> issues) : Exception("Submission blocked: " + string.Join(" ", issues)) { public IReadOnlyList<string> Issues { get; } = issues; }
public static class SubmissionGuard {
    public static async Task<List<string>> Check(AppDbContext db, WelfareApplication app) {
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == app.UserId);
        var program = await db.WelfarePrograms.AsNoTracking().FirstAsync(p => p.Id == app.WelfareProgramId);
        var issues = new List<string>();
        if (!user.IsActive || !program.IsActive) issues.Add("Your account and the program must be active.");
        if (user.MustChangePassword) issues.Add("Replace your initial account password before submitting.");
        if (user.EmploymentVerifiedAt == null) issues.Add("Ask your administrator to verify your employment start date.");
        var service = new Rules.ServicePeriodRule().Evaluate(new Rules.RuleEvaluationContext { User = user, Program = program, ApplicationDate = DateTime.UtcNow });
        if (!service.IsPassed) issues.Add(service.Message);
        if (app.RequestedAmount <= 0 || app.RequestedAmount > program.MaxAmount) issues.Add($"The amount must be greater than zero and no more than {program.MaxAmount:N2}.");
        string[] required;
        try { required = JsonSerializer.Deserialize<string[]>(program.RequiredDocuments) ?? []; }
        catch (JsonException) { issues.Add("This program has invalid document requirements; contact an administrator."); return issues; }
        var evidence = await db.ApplicationDocuments.AsNoTracking().Where(d => d.ApplicationId == app.Id && d.ContentHash != "").Select(d => d.RequirementName).ToListAsync();
        foreach (var name in required) if (!evidence.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase))) issues.Add($"Upload the required document: {name}.");
        return issues;
    }
}
