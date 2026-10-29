using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.DTOs.EligibilityRule;

public class CreateEligibilityRuleRequest
{
    [Required(ErrorMessage = "Welfare program ID is required")]
    public int WelfareProgramId { get; set; }

    [Required(ErrorMessage = "Rule name is required")]
    [MaxLength(200, ErrorMessage = "Rule name cannot exceed 200 characters")]
    public string RuleName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rule type is required")]
    [Range(1, 5, ErrorMessage = "Invalid rule type")]
    public int RuleType { get; set; }

    [Required(ErrorMessage = "Rule value is required")]
    public string RuleValue { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
}
