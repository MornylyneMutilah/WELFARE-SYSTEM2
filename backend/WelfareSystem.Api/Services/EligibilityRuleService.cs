using WelfareSystem.Api.Models.DTOs.EligibilityRule;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class EligibilityRuleService : IEligibilityRuleService
{
    private readonly IEligibilityRuleRepository _eligibilityRuleRepository;
    private readonly IWelfareProgramRepository _welfareProgramRepository;

    public EligibilityRuleService(
        IEligibilityRuleRepository eligibilityRuleRepository,
        IWelfareProgramRepository welfareProgramRepository)
    {
        _eligibilityRuleRepository = eligibilityRuleRepository;
        _welfareProgramRepository = welfareProgramRepository;
    }

    public async Task<EligibilityRuleResponse?> GetRuleByIdAsync(int id)
    {
        var rule = await _eligibilityRuleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            return null;
        }

        return MapToResponse(rule);
    }

    public async Task<IEnumerable<EligibilityRuleResponse>> GetRulesByProgramIdAsync(int welfareProgramId)
    {
        var rules = await _eligibilityRuleRepository.GetByWelfareProgramIdAsync(welfareProgramId);
        return rules.Select(MapToResponse);
    }

    public async Task<EligibilityRuleResponse?> CreateRuleAsync(CreateEligibilityRuleRequest request)
    {
        // Validate that the welfare program exists
        var program = await _welfareProgramRepository.GetByIdAsync(request.WelfareProgramId);
        if (program == null)
        {
            return null;
        }

        var rule = new EligibilityRule
        {
            WelfareProgramId = request.WelfareProgramId,
            RuleName = request.RuleName,
            RuleType = request.RuleType,
            RuleValue = request.RuleValue,
            Description = request.Description,
            IsActive = true
        };

        var createdRule = await _eligibilityRuleRepository.AddAsync(rule);
        return MapToResponse(createdRule);
    }

    public async Task<EligibilityRuleResponse?> UpdateRuleAsync(int id, UpdateEligibilityRuleRequest request)
    {
        var rule = await _eligibilityRuleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            return null;
        }

        rule.RuleName = request.RuleName;
        rule.RuleType = request.RuleType;
        rule.RuleValue = request.RuleValue;
        rule.Description = request.Description;
        rule.IsActive = request.IsActive;

        await _eligibilityRuleRepository.UpdateAsync(rule);
        
        var updatedRule = await _eligibilityRuleRepository.GetByIdAsync(id);
        return MapToResponse(updatedRule!);
    }

    public async Task<bool> DeactivateRuleAsync(int id)
    {
        var rule = await _eligibilityRuleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            return false;
        }

        rule.IsActive = false;

        await _eligibilityRuleRepository.UpdateAsync(rule);
        return true;
    }

    private EligibilityRuleResponse MapToResponse(EligibilityRule rule)
    {
        return new EligibilityRuleResponse
        {
            Id = rule.Id,
            WelfareProgramId = rule.WelfareProgramId,
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            RuleValue = rule.RuleValue,
            Description = rule.Description,
            IsActive = rule.IsActive
        };
    }
}
