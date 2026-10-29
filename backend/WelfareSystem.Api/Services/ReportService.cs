using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Report;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationsByStatusReport> GetApplicationsByStatusReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.WelfareApplications.Include(a => a.WelfareProgram).AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        }

        var applications = await query.ToListAsync();

        var statusCounts = applications
            .GroupBy(a => a.Status)
            .Select(g => new StatusCount
            {
                Status = g.Key,
                StatusName = GetStatusName(g.Key),
                Count = g.Count(),
                TotalAmount = g.Sum(a => a.RequestedAmount)
            })
            .OrderBy(s => s.Status)
            .ToList();

        return new ApplicationsByStatusReport
        {
            StatusCounts = statusCounts,
            ReportDate = DateTime.UtcNow,
            DateRange = GetDateRange(startDate, endDate)
        };
    }

    public async Task<ApplicationsByDepartmentReport> GetApplicationsByDepartmentReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.WelfareApplications.Include(a => a.User).Include(a => a.WelfareProgram).AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        }

        var applications = await query.ToListAsync();

        var departmentCounts = applications
            .GroupBy(a => a.User.DepartmentId)
            .Select(g => new DepartmentCount
            {
                DepartmentId = g.Key,
                DepartmentName = g.First().User.Department.Name,
                ApplicationCount = g.Count(),
                TotalAmount = g.Sum(a => a.RequestedAmount),
                ApprovedCount = g.Count(a => a.Status == 8)
            })
            .OrderByDescending(d => d.ApplicationCount)
            .ToList();

        return new ApplicationsByDepartmentReport
        {
            DepartmentCounts = departmentCounts,
            ReportDate = DateTime.UtcNow,
            DateRange = GetDateRange(startDate, endDate)
        };
    }

    public async Task<ApplicationsByProgramReport> GetApplicationsByProgramReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.WelfareApplications.Include(a => a.WelfareProgram).AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        }

        var applications = await query.ToListAsync();

        var programCounts = applications
            .GroupBy(a => a.WelfareProgramId)
            .Select(g => new ProgramCount
            {
                ProgramId = g.Key,
                ProgramName = g.First().WelfareProgram.Name,
                ApplicationCount = g.Count(),
                TotalAmount = g.Sum(a => a.RequestedAmount),
                AverageAmount = g.Average(a => a.RequestedAmount),
                ApprovedCount = g.Count(a => a.Status == 8)
            })
            .OrderByDescending(p => p.ApplicationCount)
            .ToList();

        return new ApplicationsByProgramReport
        {
            ProgramCounts = programCounts,
            ReportDate = DateTime.UtcNow,
            DateRange = GetDateRange(startDate, endDate)
        };
    }

    public async Task<ExpenditureReport> GetExpenditureReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.WelfareApplications.Include(a => a.WelfareProgram).AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        }

        var applications = await query.Where(a => a.Status == 8).ToListAsync(); // Only approved

        var totalExpenditure = applications.Sum(a => a.RequestedAmount);

        var monthlyExpenditures = applications
            .GroupBy(a => new { a.SubmittedAt.Year, a.SubmittedAt.Month })
            .Select(g => new MonthlyExpenditure
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                Amount = g.Sum(a => a.RequestedAmount),
                ApplicationCount = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var programExpenditures = applications
            .GroupBy(a => a.WelfareProgramId)
            .Select(g => new ProgramExpenditure
            {
                ProgramId = g.Key,
                ProgramName = g.First().WelfareProgram.Name,
                TotalAmount = g.Sum(a => a.RequestedAmount),
                ApplicationCount = g.Count(),
                Percentage = totalExpenditure > 0 ? (g.Sum(a => a.RequestedAmount) / totalExpenditure) * 100 : 0
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        return new ExpenditureReport
        {
            TotalExpenditure = totalExpenditure,
            MonthlyExpenditures = monthlyExpenditures,
            ProgramExpenditures = programExpenditures,
            ReportDate = DateTime.UtcNow,
            DateRange = GetDateRange(startDate, endDate)
        };
    }

    public async Task<ProcessingTimeReport> GetProcessingTimeReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.WelfareApplications.AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        }

        var applications = await query.Where(a => a.Status == 8 || a.Status == 9).ToListAsync(); // Completed

        var processingTimes = applications
            .Select(a => (a.UpdatedAt - a.SubmittedAt).TotalDays)
            .ToList();

        var averageProcessingDays = processingTimes.Any() ? processingTimes.Average() : 0;
        var medianProcessingDays = processingTimes.Any() ? processingTimes.OrderBy(t => t).Skip(processingTimes.Count / 2).First() : 0;
        var minProcessingDays = processingTimes.Any() ? processingTimes.Min() : 0;
        var maxProcessingDays = processingTimes.Any() ? processingTimes.Max() : 0;

        var processingTimesByStage = new List<ProcessingTimeByStage>
        {
            new ProcessingTimeByStage { Stage = "HR Review", AverageDays = 3, ApplicationCount = applications.Count },
            new ProcessingTimeByStage { Stage = "Committee Review", AverageDays = 5, ApplicationCount = applications.Count },
            new ProcessingTimeByStage { Stage = "Management Approval", AverageDays = 2, ApplicationCount = applications.Count }
        };

        return new ProcessingTimeReport
        {
            AverageProcessingDays = (decimal)averageProcessingDays,
            MedianProcessingDays = (decimal)medianProcessingDays,
            MinProcessingDays = (decimal)minProcessingDays,
            MaxProcessingDays = (decimal)maxProcessingDays,
            ProcessingTimesByStage = processingTimesByStage,
            ReportDate = DateTime.UtcNow,
            DateRange = GetDateRange(startDate, endDate)
        };
    }

    public async Task<FlaggedApplicationsReport> GetFlaggedApplicationsReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.WelfareApplications.Include(a => a.User).Include(a => a.WelfareProgram).AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(a => a.SubmittedAt <= endDate.Value);
        }

        var flaggedApplications = await query
            .Where(a => !string.IsNullOrEmpty(a.AnomalyFlags) && a.AnomalyFlags != "[]")
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        var flaggedList = flaggedApplications.Select(a => new FlaggedApplication
        {
            ApplicationId = a.Id,
            EmployeeName = $"{a.User.FirstName} {a.User.LastName}",
            ProgramName = a.WelfareProgram.Name,
            RequestedAmount = a.RequestedAmount,
            AnomalyFlags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(a.AnomalyFlags) ?? new List<string>(),
            SubmittedAt = a.SubmittedAt,
            Severity = GetSeverityFromAnomalies(a.AnomalyFlags)
        }).ToList();

        return new FlaggedApplicationsReport
        {
            TotalFlagged = flaggedList.Count,
            FlaggedApplications = flaggedList,
            ReportDate = DateTime.UtcNow,
            DateRange = GetDateRange(startDate, endDate)
        };
    }

    private string GetStatusName(int status)
    {
        return status switch
        {
            1 => "Draft",
            2 => "Submitted",
            3 => "Under HR Review",
            4 => "Under Committee Review",
            5 => "Approved by Committee",
            6 => "Rejected by Committee",
            7 => "Pending Management Approval",
            8 => "Approved",
            9 => "Rejected",
            10 => "Cancelled",
            _ => "Unknown"
        };
    }

    private string GetDateRange(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}";
        }
        else if (startDate.HasValue)
        {
            return $"{startDate.Value:yyyy-MM-dd} to present";
        }
        else if (endDate.HasValue)
        {
            return $"All time to {endDate.Value:yyyy-MM-dd}";
        }
        else
        {
            return "All time";
        }
    }

    private string GetSeverityFromAnomalies(string anomalyFlags)
    {
        try
        {
            var flags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(anomalyFlags);
            if (flags != null && flags.Any(f => f.Contains("Critical", StringComparison.OrdinalIgnoreCase)))
            {
                return "Critical";
            }
            else if (flags != null && flags.Any(f => f.Contains("High", StringComparison.OrdinalIgnoreCase)))
            {
                return "High";
            }
            else if (flags != null && flags.Any(f => f.Contains("Medium", StringComparison.OrdinalIgnoreCase)))
            {
                return "Medium";
            }
            else
            {
                return "Low";
            }
        }
        catch
        {
            return "Unknown";
        }
    }
}
