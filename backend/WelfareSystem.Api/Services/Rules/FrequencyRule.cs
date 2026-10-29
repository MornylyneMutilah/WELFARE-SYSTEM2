namespace WelfareSystem.Api.Services.Rules;

public class FrequencyRule : IRule
{
    public string RuleName => "Application Frequency";
    public string RuleDescription => "Validates that the user hasn't exceeded the allowed application frequency";

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

        // Check for pending applications for the same program
        var pendingApplications = context.UserApplications
            .Where(a => a.WelfareProgramId == context.Program.Id && 
                       (a.Status == 1 || a.Status == 2 || a.Status == 3 || a.Status == 4)) // Draft, Submitted, HR Review, Committee Review
            .ToList();

        if (pendingApplications.Any())
        {
            result.IsPassed = false;
            result.Message = $"User has {pendingApplications.Count} pending application(s) for this program";
            result.Details["PendingApplications"] = pendingApplications.Count;
            return result;
        }

        // Check if user has applied for this program in the last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var recentApplications = context.UserApplications
            .Where(a => a.WelfareProgramId == context.Program.Id && 
                       a.SubmittedAt >= sixMonthsAgo &&
                       (a.Status == 8 || a.Status == 9)) // Approved or Rejected
            .ToList();

        if (recentApplications.Any())
        {
            result.IsPassed = false;
            result.Message = $"User has {recentApplications.Count} recent application(s) for this program within the last 6 months";
            result.Details["RecentApplications"] = recentApplications.Count;
            return result;
        }

        result.IsPassed = true;
        result.Message = "Application frequency is within acceptable limits";
        result.Details["TotalApplications"] = context.UserApplications.Count;

        return result;
    }
}
