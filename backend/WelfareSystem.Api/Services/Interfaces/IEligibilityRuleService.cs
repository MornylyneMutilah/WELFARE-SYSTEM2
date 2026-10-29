using WelfareSystem.Api.Models.DTOs.EligibilityRule;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IEligibilityRuleService
{
    Task<EligibilityRuleResponse?> GetRuleByIdAsync(int id);
    Task<IEnumerable<EligibilityRuleResponse>> GetRulesByProgramIdAsync(int welfareProgramId);
    Task<EligibilityRuleResponse?> CreateRuleAsync(CreateEligibilityRuleRequest request);
    Task<EligibilityRuleResponse?> UpdateRuleAsync(int id, UpdateEligibilityRuleRequest request);
    Task<bool> DeactivateRuleAsync(int id);
}
