using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Review;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class ReviewService(AppDbContext context, NotificationTriggerService notifications) : IReviewService
{
    public async Task<ReviewResponse?> GetReviewByIdAsync(int id)
    {
        var review = await context.ApplicationReviews.Include(r => r.Reviewer).FirstOrDefaultAsync(r => r.Id == id);
        return review == null ? null : Map(review);
    }

    public async Task<IEnumerable<ReviewResponse>> GetReviewsByApplicationIdAsync(int applicationId) =>
        (await context.ApplicationReviews.Include(r => r.Reviewer).Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.ReviewedAt).ToListAsync()).Select(Map);

    public async Task<ReviewResponse?> CreateReviewAsync(int applicationId, int reviewerId, string reviewerRole, CreateReviewRequest request)
    {
        var recommendation = request.Recommendation.Trim().ToLowerInvariant();
        if (recommendation is not ("approve" or "reject") || string.IsNullOrWhiteSpace(request.Comments)) return null;
        var actor = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == reviewerId && u.IsActive);
        if (actor == null) return null;
        // The current database role is authoritative; the caller's role string is not trusted.
        reviewerRole = actor.Role.Name;
        await using var transaction = context.Database.CurrentTransaction == null ? await context.Database.BeginTransactionAsync() : null;
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"WelfareApplications\" WHERE \"Id\" = {applicationId} FOR UPDATE");
        var application = await context.WelfareApplications.Include(a => a.User).Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application == null || application.UserId == reviewerId) return null;
        await context.Entry(application).ReloadAsync();
        var allowed = reviewerRole switch {
            "HROfficer" => (application.Status is 2 or 3) && application.User.DepartmentId == actor.DepartmentId,
            "CommitteeMember" => application.Status == 4,
            "Management" => application.Status is 5 or 7,
            _ => false
        };
        if (!allowed || await context.ApplicationReviews.AnyAsync(r => r.ApplicationId == applicationId && r.ReviewerRole == reviewerRole)) return null;
        var newStatus = (reviewerRole, recommendation) switch {
            ("HROfficer", "approve") => 4,
            ("HROfficer", "reject") => 9,
            ("CommitteeMember", "approve") => 5,
            ("CommitteeMember", "reject") => 6,
            ("Management", "approve") => 8,
            ("Management", "reject") => 9,
            _ => application.Status
        };
        var review = new ApplicationReview {
            ApplicationId = applicationId, ReviewerId = reviewerId, ReviewerRole = reviewerRole,
            Recommendation = recommendation == "approve" ? "Approve" : "Reject",
            Comments = request.Comments.Trim(), ReviewedAt = DateTime.UtcNow, Reviewer = actor
        };
        context.ApplicationReviews.Add(review);
        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;
        context.ApplicationStatusHistories.Add(new ApplicationStatusHistory {
            ApplicationId = applicationId, Status = newStatus, ChangedBy = reviewerId,
            Notes = ($"{reviewerRole}: {review.Recommendation}. {review.Comments}").Length > 1000
                ? ($"{reviewerRole}: {review.Recommendation}. {review.Comments}")[..997] + "..."
                : $"{reviewerRole}: {review.Recommendation}. {review.Comments}"
        });
        await context.SaveChangesAsync();
        var documents = await context.ApplicationDocuments.AsNoTracking().Where(d => d.ApplicationId == applicationId)
            .Select(d => new { d.Id, d.FileName, d.FileType, d.FileSize, d.UploadedAt, d.ContentHash, d.RequirementName, d.ScanStatus }).ToListAsync();
        context.AuditLogs.Add(new AuditLog { UserId = reviewerId, Action = "ApplicationReview",
            EntityType = "WelfareApplication", EntityId = applicationId.ToString(),
            Details = System.Text.Json.JsonSerializer.Serialize(new { reviewId = review.Id, reviewerRole, review.Recommendation, newStatus,
                snapshot = new { reference = $"WEL-DEC-{review.Id:D6}", applicationId, employeeName = $"{application.User.FirstName} {application.User.LastName}",
                    application.User.EmployeeId, application.User.DepartmentId, programName = application.WelfareProgram.Name, application.RequestedAmount,
                    application.Reason, application.IsEligible, application.RuleEvaluationResult, application.SubmittedAt,
                    reviewerName = $"{actor.FirstName} {actor.LastName}", reviewerRole, review.Recommendation, review.Comments, review.ReviewedAt, newStatus, documents }
            }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)) });
        await context.SaveChangesAsync();
        if (newStatus == 8) await notifications.TriggerApprovalAsync(application.UserId, applicationId, application.WelfareProgram.Name);
        else if (newStatus is 6 or 9) await notifications.TriggerRejectionAsync(application.UserId, applicationId, application.WelfareProgram.Name, review.Comments);
        else await notifications.TriggerStatusChangeAsync(application.UserId, applicationId, application.WelfareProgram.Name,
            newStatus == 4 ? "Under Committee Review" : "Approved by Committee — awaiting management");
        if (transaction != null) await transaction.CommitAsync();
        return Map(review);
    }

    public async Task<ReviewResponse?> UpdateReviewAsync(int id, UpdateReviewRequest request)
    {
        var review = await context.ApplicationReviews.Include(r => r.Reviewer).FirstOrDefaultAsync(r => r.Id == id);
        // A completed decision cannot be changed independently of its application/history.
        if (review == null || !string.Equals(review.Recommendation, request.Recommendation.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(request.Comments)) return null;
        // Finalized decisions and their notes are immutable. Corrections require a separate, attributed event.
        if (review.Comments != request.Comments.Trim()) return null;
        return Map(review);
    }

    private static ReviewResponse Map(ApplicationReview r) => new() {
        Id = r.Id, ApplicationId = r.ApplicationId, ReviewerId = r.ReviewerId,
        ReviewerName = $"{r.Reviewer?.FirstName} {r.Reviewer?.LastName}", ReviewerRole = r.ReviewerRole,
        Recommendation = r.Recommendation, Comments = r.Comments, ReviewedAt = r.ReviewedAt
    };
}
