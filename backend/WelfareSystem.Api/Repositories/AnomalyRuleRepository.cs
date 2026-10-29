using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;

namespace WelfareSystem.Api.Repositories;

public class AnomalyRuleRepository : BaseRepository<AnomalyRule>, IAnomalyRuleRepository
{
    public AnomalyRuleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AnomalyRule>> GetActiveRulesAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .OrderBy(r => r.AnomalyType)
            .ThenBy(r => r.RuleName)
            .ToListAsync();
    }

    public async Task<IEnumerable<AnomalyRule>> GetByTypeAsync(string anomalyType)
    {
        return await _dbSet
            .Where(r => r.AnomalyType == anomalyType && r.IsActive)
            .OrderBy(r => r.RuleName)
            .ToListAsync();
    }
}
