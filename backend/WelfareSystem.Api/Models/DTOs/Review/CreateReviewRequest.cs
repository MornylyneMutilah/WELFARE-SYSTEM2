using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.Review;

public class CreateReviewRequest
{
    [Required(ErrorMessage = "Recommendation is required")]
    public string Recommendation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Comments are required")]
    [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters")]
    public string Comments { get; set; } = string.Empty;
}
