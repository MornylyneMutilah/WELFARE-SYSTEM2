namespace WelfareSystem.Api.Models.DTOs.Dashboard;

public class CommitteeDashboardStats
{
    public int TotalApplications { get; set; }
    public int PendingReview { get; set; }
    public int ApprovedByCommittee { get; set; }
    public int RejectedByCommittee { get; set; }
    public int UnreadNotifications { get; set; }
    public List<PendingApplication> PendingApplications { get; set; } = new();
}
