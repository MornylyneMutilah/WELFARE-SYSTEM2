namespace WelfareSystem.Api.Models.DTOs.Dashboard;

public class ManagementDashboardStats
{
    public int TotalApplications { get; set; }
    public int PendingApproval { get; set; }
    public int ApprovedByManagement { get; set; }
    public int RejectedByManagement { get; set; }
    public decimal TotalExpenditure { get; set; }
    public decimal AverageProcessingDays { get; set; }
    public int UnreadNotifications { get; set; }
    public List<PendingApplication> PendingApplications { get; set; } = new();
}
