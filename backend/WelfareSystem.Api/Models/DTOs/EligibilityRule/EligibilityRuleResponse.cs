namespace WelfareSystem.Api.Models.DTOs.EligibilityRule;

public class EligibilityRuleResponse
{
    public int Id { get; set; }
    public int WelfareProgramId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int RuleType { get; set; }
    public string RuleValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
