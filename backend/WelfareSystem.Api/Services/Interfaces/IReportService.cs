using WelfareSystem.Api.Models.DTOs.Report;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IReportService
{
    Task<ApplicationsByStatusReport> GetApplicationsByStatusReportAsync(DateTime? startDate, DateTime? endDate);
    Task<ApplicationsByDepartmentReport> GetApplicationsByDepartmentReportAsync(DateTime? startDate, DateTime? endDate);
    Task<ApplicationsByProgramReport> GetApplicationsByProgramReportAsync(DateTime? startDate, DateTime? endDate);
    Task<ExpenditureReport> GetExpenditureReportAsync(DateTime? startDate, DateTime? endDate);
    Task<ProcessingTimeReport> GetProcessingTimeReportAsync(DateTime? startDate, DateTime? endDate);
    Task<FlaggedApplicationsReport> GetFlaggedApplicationsReportAsync(DateTime? startDate, DateTime? endDate);
}
