using System.ComponentModel.DataAnnotations;

namespace WelfareSystem.Api.Models.Entities;

public class AccountActionToken {
    [Key, MaxLength(64)] public string TokenHash { get; set; } = "";
    public int UserId { get; set; }
    [MaxLength(20)] public string Purpose { get; set; } = "";
    [MaxLength(256)] public string Email { get; set; } = "";
    public int TokenVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
public class DecisionAmendment {
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public int RequestedBy { get; set; }
    public int? ResolvedBy { get; set; }
    [MaxLength(20)] public string Kind { get; set; } = "Correction";
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(2000)] public string ProposedNotes { get; set; } = "";
    [MaxLength(2000)] public string Resolution { get; set; } = "";
    public int? ReconsiderationApplicationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
public class ProgramBudget {
    public int Id { get; set; }
    public int WelfareProgramId { get; set; }
    public int Year { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal ProposedAmount { get; set; }
    public int RequestedBy { get; set; }
    public int? ApprovedBy { get; set; }
    [MaxLength(20)] public string ProposalStatus { get; set; } = "Pending";
    [MaxLength(1000)] public string Reason { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class DetectionSettings {
    public int Id { get; set; }
    public int FrequencyDays { get; set; } = 30;
    public int FrequencyLimit { get; set; } = 3;
    public int DuplicateDays { get; set; } = 7;
    public double AmountPercentage { get; set; } = 50;
    public bool EnableExperimentalModel { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
