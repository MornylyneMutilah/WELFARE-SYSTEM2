namespace WelfareSystem.Api.Models.DTOs.Report;

public class FlaggedApplicationsReport
{
    public int TotalFlagged { get; set; }
    public List<FlaggedApplication> FlaggedApplications { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public string DateRange { get; set; } = string.Empty;
}

public class FlaggedApplication
{
    public int ApplicationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public List<string> AnomalyFlags { get; set; } = new();
    public DateTime SubmittedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
}
