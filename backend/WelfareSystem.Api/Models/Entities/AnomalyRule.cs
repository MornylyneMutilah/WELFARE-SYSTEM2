using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WelfareSystem.Api.Models.Entities;

public class AnomalyRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string RuleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AnomalyType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    [Required]
    public bool IsActive { get; set; } = true;

    [Column(TypeName = "text")]
    public string? ThresholdConfig { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }

    [ForeignKey(nameof(UpdatedBy))]
    public virtual User? Updater { get; set; }
}
