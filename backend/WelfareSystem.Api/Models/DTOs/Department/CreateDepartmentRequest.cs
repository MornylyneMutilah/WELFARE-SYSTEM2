using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.Department;

public class CreateDepartmentRequest
{
    [Required(ErrorMessage = "Department name is required")]
    [MaxLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department code is required")]
    [MaxLength(20, ErrorMessage = "Department code cannot exceed 20 characters")]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
}
