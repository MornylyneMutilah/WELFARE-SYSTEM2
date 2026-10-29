using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IApplicationReviewRepository : IBaseRepository<ApplicationReview>
{
    Task<IEnumerable<ApplicationReview>> GetByApplicationIdAsync(int applicationId);
}
