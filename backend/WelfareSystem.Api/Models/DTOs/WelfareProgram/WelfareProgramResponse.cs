namespace WelfareSystem.Api.Models.DTOs.WelfareProgram;

public class WelfareProgramResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public int MinServicePeriod { get; set; }
    public string RequiredDocuments { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
