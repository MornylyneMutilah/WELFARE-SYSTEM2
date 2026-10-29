namespace WelfareSystem.Api.Models.Entities;

public class WelfareApplication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int WelfareProgramId { get; set; }
    public int Status { get; set; } // Using int instead of enum for simplicity
    public decimal RequestedAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEligible { get; set; } = false;
    public string AnomalyFlags { get; set; } = string.Empty; // JSON array of anomalies
    public string RuleEvaluationResult { get; set; } = string.Empty; // JSON

    // Navigation properties
    public User User { get; set; } = null!;
    public WelfareProgram WelfareProgram { get; set; } = null!;
    public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    public ICollection<ApplicationReview> Reviews { get; set; } = new List<ApplicationReview>();
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
}
