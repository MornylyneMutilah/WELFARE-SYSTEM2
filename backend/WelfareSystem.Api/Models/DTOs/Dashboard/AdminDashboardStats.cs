namespace WelfareSystem.Api.Models.DTOs.Dashboard;

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalApplications { get; set; }
    public int TotalPrograms { get; set; }
    public int ActivePrograms { get; set; }
    public decimal TotalExpenditure { get; set; }
    public int PendingApplications { get; set; }
    public int FlaggedApplications { get; set; }
    public List<DepartmentStats> DepartmentStats { get; set; } = new();
}

public class DepartmentStats
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public decimal TotalAmount { get; set; }
}
