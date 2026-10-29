namespace WelfareSystem.Api.Models.DTOs.Review;

public class ReviewResponse
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerRole { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; }
}
