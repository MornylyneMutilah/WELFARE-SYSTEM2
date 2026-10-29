using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WelfareSystem.Api.Models.Entities;

public class Anomaly
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnomalyType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical

    [Required]
    [Column(TypeName = "text")]
    public string Description { get; set; } = string.Empty;

    public int? ApplicationId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open"; // Open, Investigating, Resolved, Dismissed

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public int? ResolvedBy { get; set; }

    [Column(TypeName = "text")]
    public string? ResolutionNotes { get; set; }

    [Column(TypeName = "text")]
    public string? DetectionDetails { get; set; }

    public double? AnomalyScore { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ApplicationId))]
    public virtual WelfareApplication? Application { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(ResolvedBy))]
    public virtual User? Resolver { get; set; }

    public virtual ICollection<AnomalyAlert> Alerts { get; set; } = new List<AnomalyAlert>();
}
