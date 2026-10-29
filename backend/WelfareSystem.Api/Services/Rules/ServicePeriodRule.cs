namespace WelfareSystem.Api.Services.Rules;

public class ServicePeriodRule : IRule
{
    public string RuleName => "Service Period";
    public string RuleDescription => "Validates that the user has the minimum required service period";

    public RuleEvaluationResult Evaluate(RuleEvaluationContext context)
    {
        var result = new RuleEvaluationResult
        {
            RuleName = RuleName
        };

        // Calculate service period in months
        var servicePeriodMonths = CalculateServicePeriod(context.User.HireDate, context.ApplicationDate);
        
        var minRequiredMonths = context.Program.MinServicePeriod;
        
        result.IsPassed = servicePeriodMonths >= minRequiredMonths;
        result.Message = result.IsPassed 
            ? $"Service period ({servicePeriodMonths} months) meets minimum requirement ({minRequiredMonths} months)"
            : $"Service period ({servicePeriodMonths} months) does not meet minimum requirement ({minRequiredMonths} months)";
        
        result.Details["ServicePeriodMonths"] = servicePeriodMonths;
        result.Details["RequiredMonths"] = minRequiredMonths;
        result.Details["HireDate"] = context.User.HireDate;

        return result;
    }

    private int CalculateServicePeriod(DateTime hireDate, DateTime currentDate)
    {
        var months = (currentDate.Year - hireDate.Year) * 12 + currentDate.Month - hireDate.Month;
        return Math.Max(0, months);
    }
}
