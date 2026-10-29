namespace WelfareSystem.Api.Services.Rules;

public class RuleEvaluationResult
{
    public bool IsPassed { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}
