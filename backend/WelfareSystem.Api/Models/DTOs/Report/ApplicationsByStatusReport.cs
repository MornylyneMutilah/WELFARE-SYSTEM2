namespace WelfareSystem.Api.Models.DTOs.Report;

public class ApplicationsByStatusReport
{
    public List<StatusCount> StatusCounts { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public string DateRange { get; set; } = string.Empty;
}

public class StatusCount
{
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}
