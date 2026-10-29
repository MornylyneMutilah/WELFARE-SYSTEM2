namespace WelfareSystem.Api.Services.Rules;

public class DuplicateApplicationRule : IRule
{
    public string RuleName => "Duplicate Application";
    public string RuleDescription => "Validates that there are no duplicate applications with the same details";

    public RuleEvaluationResult Evaluate(RuleEvaluationContext context)
    {
        var result = new RuleEvaluationResult
        {
            RuleName = RuleName
        };

        if (context.UserApplications == null || context.UserApplications.Count == 0)
        {
            result.IsPassed = true;
            result.Message = "No previous applications found";
            return result;
        }

        // Check for duplicate applications with same program and similar amount
        var duplicates = context.UserApplications
            .Where(a => a.WelfareProgramId == context.Program.Id &&
                       Math.Abs(a.RequestedAmount - context.RequestedAmount) < 10 &&
                       a.SubmittedAt >= DateTime.UtcNow.AddDays(-30))
            .ToList();

        if (duplicates.Any())
        {
            result.IsPassed = false;
            result.Message = $"Found {duplicates.Count} similar application(s) within the last 30 days";
            result.Details["DuplicateCount"] = duplicates.Count;
            result.Details["SimilarApplications"] = duplicates.Select(d => d.Id).ToList();
            return result;
        }

        result.IsPassed = true;
        result.Message = "No duplicate applications found";
        result.Details["TotalApplications"] = context.UserApplications.Count;

        return result;
    }
}
