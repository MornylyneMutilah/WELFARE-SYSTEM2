using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.Application;

public class CreateApplicationRequest
{
    [Required(ErrorMessage = "Welfare program ID is required")]
    public int WelfareProgramId { get; set; }

    [Required(ErrorMessage = "Requested amount is required")]
    [Range(0.01, 1000000, ErrorMessage = "Requested amount must be between 0.01 and 1,000,000")]
    public decimal RequestedAmount { get; set; }

    [Required(ErrorMessage = "Reason is required")]
    [MaxLength(2000, ErrorMessage = "Reason cannot exceed 2000 characters")]
    public string Reason { get; set; } = string.Empty;
}
