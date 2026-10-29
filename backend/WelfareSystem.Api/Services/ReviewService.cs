using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Review;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class ReviewService : IReviewService
{
    private readonly IApplicationReviewRepository _reviewRepository;
    private readonly IWelfareApplicationRepository _applicationRepository;
    private readonly IStatusHistoryService _statusHistoryService;
    private readonly NotificationTriggerService _notificationTriggerService;
    private readonly AppDbContext _context;

    public ReviewService(
        IApplicationReviewRepository reviewRepository,
        IWelfareApplicationRepository applicationRepository,
        IStatusHistoryService statusHistoryService,
        NotificationTriggerService notificationTriggerService,
        AppDbContext context)
    {
        _reviewRepository = reviewRepository;
        _applicationRepository = applicationRepository;
        _statusHistoryService = statusHistoryService;
        _notificationTriggerService = notificationTriggerService;
        _context = context;
    }

    public async Task<ReviewResponse?> GetReviewByIdAsync(int id)
    {
        var review = await _context.ApplicationReviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (review == null)
        {
            return null;
        }

        return MapToResponse(review);
    }

    public async Task<IEnumerable<ReviewResponse>> GetReviewsByApplicationIdAsync(int applicationId)
    {
        var reviews = await _context.ApplicationReviews
            .Include(r => r.Reviewer)
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync();
        
        return reviews.Select(MapToResponse);
    }

    public async Task<ReviewResponse?> CreateReviewAsync(int applicationId, int reviewerId, string reviewerRole, CreateReviewRequest request)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
        {
            return null;
        }

        // Check if reviewer has already reviewed this application
        var existingReview = await _context.ApplicationReviews
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.ReviewerId == reviewerId);
        
        if (existingReview != null)
        {
            return null; // Already reviewed
        }

        var review = new ApplicationReview
        {
            ApplicationId = applicationId,
            ReviewerId = reviewerId,
            ReviewerRole = reviewerRole,
            Recommendation = request.Recommendation,
            Comments = request.Comments,
            ReviewedAt = DateTime.UtcNow
        };

        var createdReview = await _reviewRepository.AddAsync(review);
        
        // Update application status based on review
        await UpdateApplicationStatusBasedOnReview(applicationId, reviewerRole, request.Recommendation, reviewerId);
        
        // Fetch with navigation properties
        var reviewWithNav = await _context.ApplicationReviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == createdReview.Id);
        
        return MapToResponse(reviewWithNav!);
    }

    public async Task<ReviewResponse?> UpdateReviewAsync(int id, UpdateReviewRequest request)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            return null;
        }

        review.Recommendation = request.Recommendation;
        review.Comments = request.Comments;
        review.ReviewedAt = DateTime.UtcNow;

        await _reviewRepository.UpdateAsync(review);
        
        // Fetch with navigation properties
        var reviewWithNav = await _context.ApplicationReviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        return MapToResponse(reviewWithNav!);
    }

    private async Task UpdateApplicationStatusBasedOnReview(int applicationId, string reviewerRole, string recommendation, int reviewerId)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
        {
            return;
        }

        int newStatus = application.Status;
        string notes = $"Review by {reviewerRole}: {recommendation}";

        switch (reviewerRole.ToLower())
        {
            case "hrofficer":
                if (recommendation.ToLower() == "approve")
                {
                    newStatus = 4; // Under Committee Review
                }
                else if (recommendation.ToLower() == "reject")
                {
                    newStatus = 9; // Rejected
                }
                break;
            
            case "committeemember":
                if (recommendation.ToLower() == "approve")
                {
                    newStatus = 5; // Approved by Committee
                }
                else if (recommendation.ToLower() == "reject")
                {
                    newStatus = 6; // Rejected by Committee
                }
                break;
            
            case "management":
                if (recommendation.ToLower() == "approve")
                {
                    newStatus = 8; // Approved
                }
                else if (recommendation.ToLower() == "reject")
                {
                    newStatus = 9; // Rejected
                }
                break;
        }

        if (newStatus != application.Status)
        {
            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;
            await _applicationRepository.UpdateAsync(application);
            
            await _statusHistoryService.AddStatusHistoryAsync(applicationId, newStatus, reviewerId, notes);
            
            // Trigger notifications based on status change
            var applicationWithNav = await _context.WelfareApplications
                .Include(a => a.User)
                .Include(a => a.WelfareProgram)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            
            if (applicationWithNav != null)
            {
                if (newStatus == 8) // Approved
                {
                    await _notificationTriggerService.TriggerApprovalAsync(
                        applicationWithNav.UserId,
                        applicationWithNav.Id,
                        applicationWithNav.WelfareProgram.Name
                    );
                }
                else if (newStatus == 9) // Rejected
                {
                    await _notificationTriggerService.TriggerRejectionAsync(
                        applicationWithNav.UserId,
                        applicationWithNav.Id,
                        applicationWithNav.WelfareProgram.Name,
                        notes
                    );
                }
                else if (recommendation.ToLower() == "requestinfo") // Additional info requested
                {
                    await _notificationTriggerService.TriggerAdditionalInfoRequestAsync(
                        applicationWithNav.UserId,
                        applicationWithNav.Id,
                        applicationWithNav.WelfareProgram.Name,
                        notes
                    );
                }
            }
        }
    }

    private ReviewResponse MapToResponse(ApplicationReview review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            ApplicationId = review.ApplicationId,
            ReviewerId = review.ReviewerId,
            ReviewerName = $"{review.Reviewer?.FirstName} {review.Reviewer?.LastName}",
            ReviewerRole = review.ReviewerRole,
            Recommendation = review.Recommendation,
            Comments = review.Comments,
            ReviewedAt = review.ReviewedAt
        };
    }
}
