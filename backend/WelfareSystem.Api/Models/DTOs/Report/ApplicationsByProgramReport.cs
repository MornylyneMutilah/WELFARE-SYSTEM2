namespace WelfareSystem.Api.Models.DTOs.Report;

public class ApplicationsByProgramReport
{
    public List<ProgramCount> ProgramCounts { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public string DateRange { get; set; } = string.Empty;
}

public class ProgramCount
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public int ApprovedCount { get; set; }
}
