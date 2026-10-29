using WelfareSystem.Api.Models.DTOs.Dashboard;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<EmployeeDashboardStats> GetEmployeeDashboardStatsAsync(int userId);
    Task<HRDashboardStats> GetHRDashboardStatsAsync();
    Task<CommitteeDashboardStats> GetCommitteeDashboardStatsAsync();
    Task<ManagementDashboardStats> GetManagementDashboardStatsAsync();
    Task<AdminDashboardStats> GetAdminDashboardStatsAsync();
}
