using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WelfareSystem.Api.Models.Entities;

public class AnomalyAlert
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AnomalyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AlertType { get; set; } = string.Empty; // Email, InApp, Dashboard, SMS

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Recipient { get; set; }

    [Column(TypeName = "text")]
    public string? AlertContent { get; set; }

    [Column(TypeName = "text")]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AnomalyId))]
    public virtual Anomaly Anomaly { get; set; } = null!;
}
