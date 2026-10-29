namespace WelfareSystem.Api.Services.Rules;

public class ActiveStatusRule : IRule
{
    public string RuleName => "Active Status";
    public string RuleDescription => "Validates that the user is active and the program is active";

    public RuleEvaluationResult Evaluate(RuleEvaluationContext context)
    {
        var result = new RuleEvaluationResult
        {
            RuleName = RuleName
        };

        var userActive = context.User.IsActive;
        var programActive = context.Program.IsActive;

        if (!userActive)
        {
            result.IsPassed = false;
            result.Message = "User account is not active";
            result.Details["UserActive"] = false;
            return result;
        }

        if (!programActive)
        {
            result.IsPassed = false;
            result.Message = "Welfare program is not active";
            result.Details["ProgramActive"] = false;
            return result;
        }

        result.IsPassed = true;
        result.Message = "User and program are both active";
        result.Details["UserActive"] = true;
        result.Details["ProgramActive"] = true;

        return result;
    }
}
