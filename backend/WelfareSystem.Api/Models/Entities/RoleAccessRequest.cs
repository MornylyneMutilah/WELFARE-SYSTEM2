namespace WelfareSystem.Api.Models.Entities;

public class RoleAccessRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RequestedRole { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public User User { get; set; } = null!;
}
