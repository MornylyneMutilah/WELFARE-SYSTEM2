namespace WelfareSystem.Api.Services.Rules;

public class AmountLimitRule : IRule
{
    public string RuleName => "Amount Limit";
    public string RuleDescription => "Validates that the requested amount does not exceed the program maximum";

    public RuleEvaluationResult Evaluate(RuleEvaluationContext context)
    {
        var result = new RuleEvaluationResult
        {
            RuleName = RuleName
        };

        var maxAmount = context.Program.MaxAmount;
        var requestedAmount = context.RequestedAmount;
        
        result.IsPassed = requestedAmount <= maxAmount;
        result.Message = result.IsPassed 
            ? $"Requested amount (${requestedAmount:F2}) is within program limit (${maxAmount:F2})"
            : $"Requested amount (${requestedAmount:F2}) exceeds program limit (${maxAmount:F2})";
        
        result.Details["RequestedAmount"] = requestedAmount;
        result.Details["MaxAmount"] = maxAmount;
        result.Details["Difference"] = maxAmount - requestedAmount;

        return result;
    }
}
