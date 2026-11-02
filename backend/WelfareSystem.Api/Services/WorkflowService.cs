using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class WorkflowService(AppDbContext context, IReviewService reviews) : IWorkflowService
{
    public Task<bool> CanTransitionStatusAsync(int currentStatus, int newStatus, string userRole) => Task.FromResult(
        (userRole, currentStatus, newStatus) switch {
            ("HROfficer", 2, 3) => true,
            ("HROfficer", 2 or 3, 4 or 9) => true,
            ("CommitteeMember", 4, 5 or 6) => true,
            ("Management", 5, 7) => true,
            ("Management", 5 or 7, 8 or 9) => true,
            _ => false
        });

    public async Task<bool> UpdateApplicationStatusAsync(int applicationId, int newStatus, string comments, int userId, string userRole)
    {
        var actor = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        var application = await context.WelfareApplications.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == applicationId);
        if (actor == null || application == null || application.UserId == userId || string.IsNullOrWhiteSpace(comments)) return false;
        userRole = actor.Role.Name;
        if (userRole == "HROfficer" && application.User.DepartmentId != actor.DepartmentId) return false;
        if (!await CanTransitionStatusAsync(application.Status, newStatus, userRole)) return false;
        if (newStatus is 4 or 5 or 6 or 8 or 9)
        {
            var recommendation = newStatus is 6 or 9 ? "Reject" : "Approve";
            return await reviews.CreateReviewAsync(applicationId, userId, userRole, new() { Recommendation = recommendation, Comments = comments }) != null;
        }
        // Starting review changes only an intermediate state, with a conditional update to reject races.
        await using var tx = context.Database.CurrentTransaction == null ? await context.Database.BeginTransactionAsync() : null;
        var before = application.Status;
        var changed = await context.WelfareApplications.Where(a => a.Id == applicationId && a.Status == before)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, newStatus).SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        if (changed == 0) return false;
        context.ApplicationStatusHistories.Add(new() { ApplicationId = applicationId, Status = newStatus, ChangedBy = userId, Notes = comments });
        await context.SaveChangesAsync();
        await context.Entry(application).ReloadAsync();
        if (tx != null) await tx.CommitAsync();
        return true;
    }
}
