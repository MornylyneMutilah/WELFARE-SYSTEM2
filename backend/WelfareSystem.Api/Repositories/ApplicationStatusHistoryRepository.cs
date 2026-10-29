using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class ApplicationStatusHistoryRepository : BaseRepository<ApplicationStatusHistory>, IApplicationStatusHistoryRepository
{
    public ApplicationStatusHistoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ApplicationStatusHistory>> GetByApplicationIdAsync(int applicationId)
    {
        return await _dbSet
            .Where(h => h.ApplicationId == applicationId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }
}
