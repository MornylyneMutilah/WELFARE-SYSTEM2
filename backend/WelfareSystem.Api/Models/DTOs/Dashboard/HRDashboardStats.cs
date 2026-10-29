namespace WelfareSystem.Api.Models.DTOs.Dashboard;

public class HRDashboardStats
{
    public int TotalApplications { get; set; }
    public int PendingReview { get; set; }
    public int ApprovedByHR { get; set; }
    public int RejectedByHR { get; set; }
    public int FlaggedApplications { get; set; }
    public int UnreadNotifications { get; set; }
    public List<PendingApplication> PendingApplications { get; set; } = new();
}

public class PendingApplication
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string AnomalyFlags { get; set; } = string.Empty;
}
