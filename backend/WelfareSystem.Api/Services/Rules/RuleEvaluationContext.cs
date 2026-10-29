using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Services.Rules;

public class RuleEvaluationContext
{
    public User User { get; set; } = null!;
    public WelfareProgram Program { get; set; } = null!;
    public decimal RequestedAmount { get; set; }
    public DateTime ApplicationDate { get; set; }
    public List<WelfareApplication>? UserApplications { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
