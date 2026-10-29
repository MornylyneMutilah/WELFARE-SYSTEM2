namespace WelfareSystem.Api.Models.DTOs.Report;

public class ExpenditureReport
{
    public decimal TotalExpenditure { get; set; }
    public List<MonthlyExpenditure> MonthlyExpenditures { get; set; } = new();
    public List<ProgramExpenditure> ProgramExpenditures { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public string DateRange { get; set; } = string.Empty;
}

public class MonthlyExpenditure
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ApplicationCount { get; set; }
}

public class ProgramExpenditure
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ApplicationCount { get; set; }
    public decimal Percentage { get; set; }
}
