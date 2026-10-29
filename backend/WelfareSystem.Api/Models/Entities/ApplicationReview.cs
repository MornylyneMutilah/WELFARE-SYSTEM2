namespace WelfareSystem.Api.Models.Entities;

public class ApplicationReview
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerRole { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty; // Approve/Reject/RequestInfo
    public string Comments { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public WelfareApplication Application { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
