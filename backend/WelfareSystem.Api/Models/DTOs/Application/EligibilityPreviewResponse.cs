namespace WelfareSystem.Api.Models.DTOs.Application;

public class EligibilityPreviewResponse
{
    public string RuleEvaluationResult { get; set; } = string.Empty;
    public bool CanSubmit { get; set; }
    public IReadOnlyList<string> BlockingIssues { get; set; } = [];
}
