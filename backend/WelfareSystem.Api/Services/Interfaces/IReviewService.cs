using WelfareSystem.Api.Models.DTOs.Review;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse?> GetReviewByIdAsync(int id);
    Task<IEnumerable<ReviewResponse>> GetReviewsByApplicationIdAsync(int applicationId);
    Task<ReviewResponse?> CreateReviewAsync(int applicationId, int reviewerId, string reviewerRole, CreateReviewRequest request);
    Task<ReviewResponse?> UpdateReviewAsync(int id, UpdateReviewRequest request);
}
