using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Services.AnomalyDetection;

public class AnomalyDetectionContext
{
    public User User { get; set; } = null!;
    public WelfareProgram Program { get; set; } = null!;
    public WelfareApplication Application { get; set; } = null!;
    public decimal RequestedAmount { get; set; }
    public DateTime ApplicationDate { get; set; }
    public List<WelfareApplication> UserApplications { get; set; } = new();
    public List<ApplicationDocument> Documents { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
