namespace WelfareSystem.Api.Models.Entities;

public class ApplicationStatusHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int Status { get; set; } // Using int instead of enum for simplicity
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public WelfareApplication Application { get; set; } = null!;
}
