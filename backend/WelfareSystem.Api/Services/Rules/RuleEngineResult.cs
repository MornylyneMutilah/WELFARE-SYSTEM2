namespace WelfareSystem.Api.Services.Rules;

public class RuleEngineResult
{
    public bool IsEligible { get; set; }
    public List<RuleEvaluationResult> RuleResults { get; set; } = new();
    public List<string> WarningMessages { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
