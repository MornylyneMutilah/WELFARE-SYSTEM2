namespace WelfareSystem.Api.Models.DTOs.Application;

public class ApplicationResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int WelfareProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public string AnomalyFlags { get; set; } = string.Empty;
    public string RuleEvaluationResult { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
