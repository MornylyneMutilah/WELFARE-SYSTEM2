namespace WelfareSystem.Api.Models.Entities;

public class EligibilityRule
{
    public int Id { get; set; }
    public int WelfareProgramId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int RuleType { get; set; } // Using int instead of enum for simplicity
    public string RuleValue { get; set; } = string.Empty; // JSON configuration
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public WelfareProgram WelfareProgram { get; set; } = null!;
}
