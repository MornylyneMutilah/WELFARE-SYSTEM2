using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class EligibilityRuleRepository : BaseRepository<EligibilityRule>, IEligibilityRuleRepository
{
    public EligibilityRuleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<EligibilityRule>> GetByWelfareProgramIdAsync(int welfareProgramId)
    {
        return await _dbSet
            .Where(r => r.WelfareProgramId == welfareProgramId)
            .ToListAsync();
    }
}
