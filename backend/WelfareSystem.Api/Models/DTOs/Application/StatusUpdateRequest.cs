using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.Application;

public class StatusUpdateRequest
{
    [Required(ErrorMessage = "Status is required")]
    [Range(1, 10, ErrorMessage = "Invalid status value")]
    public int Status { get; set; }

    [MaxLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters")]
    public string Comments { get; set; } = string.Empty;
}
