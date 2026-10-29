namespace WelfareSystem.Api.Models.DTOs.Report;

public class ApplicationsByDepartmentReport
{
    public List<DepartmentCount> DepartmentCounts { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public string DateRange { get; set; } = string.Empty;
}

public class DepartmentCount
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int ApprovedCount { get; set; }
}
