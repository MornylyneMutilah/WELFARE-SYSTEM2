using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee ID is required")]
    [MaxLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required")]
    public int DepartmentId { get; set; }

    public DateTime? HireDate { get; set; }

    /// <summary>
    /// Optional requested organizational role. Never assigned automatically.
    /// </summary>
    [MaxLength(50)]
    public string? RequestedRole { get; set; }
}
