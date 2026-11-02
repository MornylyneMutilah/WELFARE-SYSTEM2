using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Dashboard;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class DashboardService(AppDbContext context) : IDashboardService
{
    private static bool Pending(int status) => status is 2 or 3 or 4 or 5 or 7;
    private IQueryable<WelfareApplication> Submitted() => context.WelfareApplications.AsNoTracking().Where(a => a.Status != 1 && a.Status != 10);
    private Task<int> Unread(int? userId) => context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    private static List<PendingApplication> Queue(IEnumerable<WelfareApplication> apps) => apps.OrderBy(a => a.SubmittedAt).Take(10)
        .Select(a => new PendingApplication { Id = a.Id, EmployeeName = $"{a.User.FirstName} {a.User.LastName}",
            ProgramName = a.WelfareProgram.Name, RequestedAmount = a.RequestedAmount, SubmittedAt = a.SubmittedAt,
            AnomalyFlags = a.AnomalyFlags ?? "[]" }).ToList();

    public async Task<EmployeeDashboardStats> GetEmployeeDashboardStatsAsync(int userId)
    {
        var apps = await context.WelfareApplications.AsNoTracking().Include(a => a.WelfareProgram)
            .Where(a => a.UserId == userId).OrderByDescending(a => a.SubmittedAt).ToListAsync();
        return new() {
            TotalApplications = apps.Count, PendingApplications = apps.Count(a => Pending(a.Status)),
            ApprovedApplications = apps.Count(a => a.Status == 8), RejectedApplications = apps.Count(a => a.Status is 6 or 9),
            TotalApprovedAmount = apps.Where(a => a.Status == 8).Sum(a => a.RequestedAmount), UnreadNotifications = await Unread(userId),
            RecentApplications = apps.Take(5).Select(a => new RecentApplication { Id = a.Id, ProgramName = a.WelfareProgram.Name,
                Status = StatusName(a.Status), RequestedAmount = a.RequestedAmount, SubmittedAt = a.SubmittedAt }).ToList()
        };
    }

    public async Task<HRDashboardStats> GetHRDashboardStatsAsync(int? reviewerId = null)
    {
        var actor = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == reviewerId && u.IsActive);
        if (actor?.Role.Name != "HROfficer") return new();
        var apps = await Submitted().Include(a => a.User).Include(a => a.WelfareProgram)
            .Where(a => a.User.DepartmentId == actor.DepartmentId).ToListAsync();
        var ids = apps.Select(a => a.Id).ToList();
        var reviews = await context.ApplicationReviews.Where(r => ids.Contains(r.ApplicationId) && r.ReviewerRole == "HROfficer").ToListAsync();
        return new() {
            TotalApplications = apps.Count, PendingReview = apps.Count(a => (a.Status is 2 or 3) && a.UserId != reviewerId),
            ApprovedByHR = reviews.Where(r => r.Recommendation == "Approve").Select(r => r.ApplicationId).Distinct().Count(),
            RejectedByHR = reviews.Where(r => r.Recommendation == "Reject").Select(r => r.ApplicationId).Distinct().Count(),
            FlaggedApplications = apps.Count(a => !string.IsNullOrWhiteSpace(a.AnomalyFlags) && a.AnomalyFlags != "[]"),
            UnreadNotifications = await Unread(reviewerId), PendingApplications = Queue(apps.Where(a => (a.Status is 2 or 3) && a.UserId != reviewerId))
        };
    }

    public async Task<CommitteeDashboardStats> GetCommitteeDashboardStatsAsync(int? reviewerId = null)
    {
        var apps = await Submitted().Include(a => a.User).Include(a => a.WelfareProgram).ToListAsync();
        var reviews = await context.ApplicationReviews.Where(r => r.ReviewerRole == "CommitteeMember").ToListAsync();
        return new() {
            TotalApplications = apps.Count, PendingReview = apps.Count(a => a.Status == 4 && a.UserId != reviewerId),
            ApprovedByCommittee = reviews.Where(r => r.Recommendation == "Approve").Select(r => r.ApplicationId).Distinct().Count(),
            RejectedByCommittee = reviews.Where(r => r.Recommendation == "Reject").Select(r => r.ApplicationId).Distinct().Count(),
            UnreadNotifications = await Unread(reviewerId), PendingApplications = Queue(apps.Where(a => a.Status == 4 && a.UserId != reviewerId))
        };
    }

    public async Task<ManagementDashboardStats> GetManagementDashboardStatsAsync(int? reviewerId = null)
    {
        var apps = await Submitted().Include(a => a.User).Include(a => a.WelfareProgram).ToListAsync();
        var approved = apps.Where(a => a.Status == 8).ToList();
        var rejected = await context.ApplicationReviews.Where(r => r.ReviewerRole == "Management" && r.Recommendation == "Reject")
            .Select(r => r.ApplicationId).Distinct().CountAsync();
        return new() {
            TotalApplications = apps.Count, PendingApproval = apps.Count(a => (a.Status is 5 or 7) && a.UserId != reviewerId),
            ApprovedByManagement = approved.Count, RejectedByManagement = rejected,
            TotalExpenditure = approved.Sum(a => a.RequestedAmount),
            AverageProcessingDays = approved.Count > 0 ? (decimal)approved.Average(a => (a.UpdatedAt - a.SubmittedAt).TotalDays) : 0,
            UnreadNotifications = await Unread(reviewerId), PendingApplications = Queue(apps.Where(a => (a.Status is 5 or 7) && a.UserId != reviewerId))
        };
    }

    public async Task<AdminDashboardStats> GetAdminDashboardStatsAsync()
    {
        var apps = await context.WelfareApplications.AsNoTracking().ToListAsync();
        return new() {
            TotalUsers = await context.Users.CountAsync(), TotalApplications = apps.Count,
            TotalPrograms = await context.WelfarePrograms.CountAsync(), ActivePrograms = await context.WelfarePrograms.CountAsync(p => p.IsActive),
            TotalExpenditure = apps.Where(a => a.Status == 8).Sum(a => a.RequestedAmount),
            PendingApplications = apps.Count(a => Pending(a.Status)),
            FlaggedApplications = apps.Count(a => !string.IsNullOrEmpty(a.AnomalyFlags) && a.AnomalyFlags != "[]"),
            DepartmentStats = await context.Departments.Select(d => new DepartmentStats { DepartmentId = d.Id, DepartmentName = d.Name,
                ApplicationCount = context.WelfareApplications.Count(a => a.User.DepartmentId == d.Id),
                TotalAmount = context.WelfareApplications.Where(a => a.User.DepartmentId == d.Id && a.Status == 8).Sum(a => a.RequestedAmount)
            }).ToListAsync()
        };
    }

    private static string StatusName(int status) => status switch {
        1 => "Draft", 2 => "Submitted", 3 => "Under HR Review", 4 => "Under Committee Review", 5 => "Approved by Committee",
        6 => "Rejected by Committee", 7 => "Pending Management Approval", 8 => "Approved", 9 => "Rejected", 10 => "Cancelled", _ => "Unknown"
    };
}
