using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class ApplicationReviewRepository : BaseRepository<ApplicationReview>, IApplicationReviewRepository
{
    public ApplicationReviewRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ApplicationReview>> GetByApplicationIdAsync(int applicationId)
    {
        return await _dbSet
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync();
    }
}
