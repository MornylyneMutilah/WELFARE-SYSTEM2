namespace WelfareSystem.Api.Models.DTOs.Report;

public class ProcessingTimeReport
{
    public decimal AverageProcessingDays { get; set; }
    public decimal MedianProcessingDays { get; set; }
    public decimal MinProcessingDays { get; set; }
    public decimal MaxProcessingDays { get; set; }
    public List<ProcessingTimeByStage> ProcessingTimesByStage { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public string DateRange { get; set; } = string.Empty;
}

public class ProcessingTimeByStage
{
    public string Stage { get; set; } = string.Empty;
    public decimal AverageDays { get; set; }
    public int ApplicationCount { get; set; }
}
