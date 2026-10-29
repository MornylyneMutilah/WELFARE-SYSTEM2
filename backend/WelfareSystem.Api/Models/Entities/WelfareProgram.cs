namespace WelfareSystem.Api.Models.Entities;

public class WelfareProgram
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public int MinServicePeriod { get; set; } // in months
    public string RequiredDocuments { get; set; } = string.Empty; // JSON array
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<EligibilityRule> EligibilityRules { get; set; } = new List<EligibilityRule>();
    public ICollection<WelfareApplication> Applications { get; set; } = new List<WelfareApplication>();
}
