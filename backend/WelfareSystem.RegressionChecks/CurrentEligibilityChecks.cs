using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Controllers;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services;

internal static class CurrentEligibilityChecks
{
    public static async Task Run(AppDbContext db, WelfareApplicationService service, Action<bool, string> check)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var user = new User { Email = $"rules-{suffix}@example.invalid", EmployeeId = suffix, FirstName = "Rule", LastName = "Refresh",
            PasswordHash = "unused", DepartmentId = await db.Departments.Select(d => d.Id).FirstAsync(),
            RoleId = await db.Roles.Where(r => r.Name == "Employee").Select(r => r.Id).FirstAsync(),
            HireDate = DateTime.UtcNow.AddYears(-3), EmploymentVerifiedAt = DateTime.UtcNow, IsActive = true };
        var program = new WelfareProgram { Name = "Live rules " + suffix, Category = suffix, MaxAmount = 2000, MinServicePeriod = 6, RequiredDocuments = "[]" };
        db.Users.Add(user); db.WelfarePrograms.Add(program); await db.SaveChangesAsync();
        var draft = (await service.CreateApplicationAsync(user.Id, new() { WelfareProgramId = program.Id, RequestedAmount = 3000, Reason = "Current rule checks" }))!;
        var recorded = draft.RuleEvaluationResult;
        var audits = await db.AuditLogs.CountAsync();
        var low = (await service.GetCurrentEligibilityAsync(draft.Id))!;
        check(!low.CanSubmit && low.BlockingIssues.Any(i => i.Contains("2,000")), "Live preview blocks a 3000 request under the 2000 maximum");
        var controller = new ApplicationsController(service, null!, db) { ControllerContext = new() { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Regression")) } } };
        check(await controller.GetEligibilityPreview(draft.Id) is OkObjectResult, "The owner can read a draft's live eligibility preview");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "0") }, "Regression"));
        check(await controller.GetEligibilityPreview(draft.Id) is NotFoundResult, "Live eligibility does not expose another employee's draft");
        program.MaxAmount = 5000; await db.SaveChangesAsync();
        var raised = (await service.GetCurrentEligibilityAsync(draft.Id))!;
        check(raised.CanSubmit && raised.BlockingIssues.Count == 0, "Raising the maximum to 5000 clears the old 2000 amount failure without editing the draft");
        using (var preview = JsonDocument.Parse(raised.RuleEvaluationResult))
            check(preview.RootElement.GetProperty("Inputs").GetProperty("MaxAmount").GetDecimal() == 5000, "Live preview reports the current program maximum");
        check((await service.GetApplicationByIdAsync(draft.Id))!.RuleEvaluationResult == recorded && await db.AuditLogs.CountAsync() == audits,
            "Polling current eligibility does not overwrite historical assessments or add audit rows");
        program.MinServicePeriod = 120; await db.SaveChangesAsync();
        check(!(await service.GetCurrentEligibilityAsync(draft.Id))!.CanSubmit, "Live preview also applies updated minimum service requirements");
        program.MinServicePeriod = 6; program.RequiredDocuments = "[\"Proof\"]"; await db.SaveChangesAsync();
        check((await service.GetCurrentEligibilityAsync(draft.Id))!.BlockingIssues.Any(i => i.Contains("Proof")), "Live preview applies newly required evidence categories");
        program.RequiredDocuments = "[]"; program.IsActive = false; await db.SaveChangesAsync();
        check(!(await service.GetCurrentEligibilityAsync(draft.Id))!.CanSubmit, "Live preview reflects program deactivation");
        program.IsActive = true; program.MaxAmount = 2000; await db.SaveChangesAsync();
        var blocked = false;
        try { await service.SubmitApplicationAsync(draft.Id); } catch (SubmissionValidationException e) { blocked = e.Issues.Any(i => i.Contains("2,000")); }
        check(blocked && (await service.GetApplicationByIdAsync(draft.Id))!.Status == 1, "The backend blocks final submission at 2000 and preserves the draft");
        var failedAssessment = (await service.GetApplicationByIdAsync(draft.Id))!.RuleEvaluationResult;
        program.MaxAmount = 5000; await db.SaveChangesAsync();
        check((await service.GetCurrentEligibilityAsync(draft.Id))!.CanSubmit, "A draft blocked at submission becomes eligible when its limit returns to 5000");
        var submitted = (await service.SubmitApplicationAsync(draft.Id))!;
        check(submitted.Status == 2, "The same 3000 draft submits successfully after raising its maximum to 5000");
        using (var snapshot = JsonDocument.Parse(submitted.RuleEvaluationResult))
            check(snapshot.RootElement.GetProperty("Inputs").GetProperty("MaxAmount").GetDecimal() == 5000, "Successful submission snapshots the current 5000 limit");
        check(await db.AuditLogs.AnyAsync(a => a.EntityType == "WelfareApplication" && a.EntityId == draft.Id.ToString() && a.Details == failedAssessment),
            "The failed 2000 assessment is preserved in audit history");
        var submittedSnapshot = submitted.RuleEvaluationResult;
        program.MaxAmount = 1000; await db.SaveChangesAsync();
        check(await service.GetCurrentEligibilityAsync(draft.Id) == null && (await service.GetApplicationByIdAsync(draft.Id))!.RuleEvaluationResult == submittedSnapshot,
            "Later policy changes never rewrite submitted assessment snapshots");
    }
}
