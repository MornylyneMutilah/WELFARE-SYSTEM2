using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Repositories.Interfaces;

public interface IEligibilityRuleRepository : IBaseRepository<EligibilityRule>
{
    Task<IEnumerable<EligibilityRule>> GetByWelfareProgramIdAsync(int welfareProgramId);
}
