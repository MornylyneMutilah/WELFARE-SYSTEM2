namespace WelfareSystem.Api.Models.DTOs.Dashboard;

public class EmployeeDashboardStats
{
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public decimal TotalApprovedAmount { get; set; }
    public int UnreadNotifications { get; set; }
    public List<RecentApplication> RecentApplications { get; set; } = new();
}

public class RecentApplication
{
    public int Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public DateTime SubmittedAt { get; set; }
}
