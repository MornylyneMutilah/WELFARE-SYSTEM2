using WelfareSystem.Api.Models.DTOs.Dashboard;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<EmployeeDashboardStats> GetEmployeeDashboardStatsAsync(int userId);
    Task<HRDashboardStats> GetHRDashboardStatsAsync(int? reviewerId = null);
    Task<CommitteeDashboardStats> GetCommitteeDashboardStatsAsync(int? reviewerId = null);
    Task<ManagementDashboardStats> GetManagementDashboardStatsAsync(int? reviewerId = null);
    Task<AdminDashboardStats> GetAdminDashboardStatsAsync();
}
