using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.WelfareProgram;

public class CreateWelfareProgramRequest
{
    [Required(ErrorMessage = "Program name is required")]
    [MaxLength(200, ErrorMessage = "Program name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Maximum amount is required")]
    [Range(0.01, 1000000, ErrorMessage = "Maximum amount must be between 0.01 and 1,000,000")]
    public decimal MaxAmount { get; set; }

    [Required(ErrorMessage = "Minimum service period is required")]
    [Range(0, 120, ErrorMessage = "Minimum service period must be between 0 and 120 months")]
    public int MinServicePeriod { get; set; }

    [Required(ErrorMessage = "Required documents is required")]
    public string RequiredDocuments { get; set; } = string.Empty;
}
