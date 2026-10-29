using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class WelfareApplicationRepository : BaseRepository<WelfareApplication>, IWelfareApplicationRepository
{
    public WelfareApplicationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WelfareApplication>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WelfareApplication>> GetByWelfareProgramIdAsync(int welfareProgramId)
    {
        return await _dbSet
            .Where(a => a.WelfareProgramId == welfareProgramId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WelfareApplication>> GetByStatusAsync(int status)
    {
        return await _dbSet
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }
}
