using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IAnomalyRuleRepository : IBaseRepository<AnomalyRule>
{
    Task<IEnumerable<AnomalyRule>> GetActiveRulesAsync();
    Task<IEnumerable<AnomalyRule>> GetByTypeAsync(string anomalyType);
}
