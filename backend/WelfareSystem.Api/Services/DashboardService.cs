using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Dashboard;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDashboardStats> GetEmployeeDashboardStatsAsync(int userId)
    {
        var applications = await _context.WelfareApplications
            .Include(a => a.WelfareProgram)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        var stats = new EmployeeDashboardStats
        {
            TotalApplications = applications.Count,
            PendingApplications = applications.Count(a => a.Status == 2 || a.Status == 3 || a.Status == 4), // Submitted, HR Review, Committee Review
            ApprovedApplications = applications.Count(a => a.Status == 8), // Approved
            RejectedApplications = applications.Count(a => a.Status == 9), // Rejected
            TotalApprovedAmount = applications.Where(a => a.Status == 8).Sum(a => a.RequestedAmount),
            UnreadNotifications = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
            RecentApplications = applications.Take(5).Select(a => new RecentApplication
            {
                Id = a.Id,
                ProgramName = a.WelfareProgram.Name,
                Status = GetStatusName(a.Status),
                RequestedAmount = a.RequestedAmount,
                SubmittedAt = a.SubmittedAt
            }).ToList()
        };

        return stats;
    }

    public async Task<HRDashboardStats> GetHRDashboardStatsAsync()
    {
        var applications = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .Where(a => a.Status == 2 || a.Status == 3) // Submitted or Under HR Review
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();

        var allApplications = await _context.WelfareApplications.ToListAsync();

        var stats = new HRDashboardStats
        {
            TotalApplications = allApplications.Count,
            PendingReview = applications.Count,
            ApprovedByHR = allApplications.Count(a => a.Status >= 4), // Moved past HR
            RejectedByHR = allApplications.Count(a => a.Status == 9), // Rejected
            FlaggedApplications = allApplications.Count(a => !string.IsNullOrEmpty(a.AnomalyFlags) && a.AnomalyFlags != "[]"),
            UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead),
            PendingApplications = applications.Take(10).Select(a => new PendingApplication
            {
                Id = a.Id,
                EmployeeName = $"{a.User.FirstName} {a.User.LastName}",
                ProgramName = a.WelfareProgram.Name,
                RequestedAmount = a.RequestedAmount,
                SubmittedAt = a.SubmittedAt,
                AnomalyFlags = a.AnomalyFlags ?? "[]"
            }).ToList()
        };

        return stats;
    }

    public async Task<CommitteeDashboardStats> GetCommitteeDashboardStatsAsync()
    {
        var applications = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .Where(a => a.Status == 4) // Under Committee Review
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();

        var allApplications = await _context.WelfareApplications.ToListAsync();

        var stats = new CommitteeDashboardStats
        {
            TotalApplications = allApplications.Count,
            PendingReview = applications.Count,
            ApprovedByCommittee = allApplications.Count(a => a.Status == 5), // Approved by Committee
            RejectedByCommittee = allApplications.Count(a => a.Status == 6), // Rejected by Committee
            UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead),
            PendingApplications = applications.Take(10).Select(a => new PendingApplication
            {
                Id = a.Id,
                EmployeeName = $"{a.User.FirstName} {a.User.LastName}",
                ProgramName = a.WelfareProgram.Name,
                RequestedAmount = a.RequestedAmount,
                SubmittedAt = a.SubmittedAt,
                AnomalyFlags = a.AnomalyFlags ?? "[]"
            }).ToList()
        };

        return stats;
    }

    public async Task<ManagementDashboardStats> GetManagementDashboardStatsAsync()
    {
        var applications = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .Where(a => a.Status == 5 || a.Status == 7) // Approved by Committee or Pending Management
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();

        var allApplications = await _context.WelfareApplications.ToListAsync();
        var approvedApplications = allApplications.Where(a => a.Status == 8).ToList();

        var stats = new ManagementDashboardStats
        {
            TotalApplications = allApplications.Count,
            PendingApproval = applications.Count,
            ApprovedByManagement = approvedApplications.Count,
            RejectedByManagement = allApplications.Count(a => a.Status == 9),
            TotalExpenditure = approvedApplications.Sum(a => a.RequestedAmount),
            AverageProcessingDays = approvedApplications.Any() 
                ? (decimal)approvedApplications.Average(a => (a.UpdatedAt - a.SubmittedAt).TotalDays) 
                : 0,
            UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead),
            PendingApplications = applications.Take(10).Select(a => new PendingApplication
            {
                Id = a.Id,
                EmployeeName = $"{a.User.FirstName} {a.User.LastName}",
                ProgramName = a.WelfareProgram.Name,
                RequestedAmount = a.RequestedAmount,
                SubmittedAt = a.SubmittedAt,
                AnomalyFlags = a.AnomalyFlags ?? "[]"
            }).ToList()
        };

        return stats;
    }

    public async Task<AdminDashboardStats> GetAdminDashboardStatsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalApplications = await _context.WelfareApplications.CountAsync();
        var totalPrograms = await _context.WelfarePrograms.CountAsync();
        var activePrograms = await _context.WelfarePrograms.CountAsync(p => p.IsActive);
        var applications = await _context.WelfareApplications.ToListAsync();
        var approvedApplications = applications.Where(a => a.Status == 8).ToList();

        var departmentStats = await _context.Departments
            .Select(d => new DepartmentStats
            {
                DepartmentId = d.Id,
                DepartmentName = d.Name,
                ApplicationCount = _context.WelfareApplications.Count(a => a.User.DepartmentId == d.Id),
                TotalAmount = _context.WelfareApplications
                    .Where(a => a.User.DepartmentId == d.Id && a.Status == 8)
                    .Sum(a => a.RequestedAmount)
            })
            .ToListAsync();

        var stats = new AdminDashboardStats
        {
            TotalUsers = totalUsers,
            TotalApplications = totalApplications,
            TotalPrograms = totalPrograms,
            ActivePrograms = activePrograms,
            TotalExpenditure = approvedApplications.Sum(a => a.RequestedAmount),
            PendingApplications = applications.Count(a => a.Status == 2 || a.Status == 3 || a.Status == 4),
            FlaggedApplications = applications.Count(a => !string.IsNullOrEmpty(a.AnomalyFlags) && a.AnomalyFlags != "[]"),
            DepartmentStats = departmentStats
        };

        return stats;
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
}
