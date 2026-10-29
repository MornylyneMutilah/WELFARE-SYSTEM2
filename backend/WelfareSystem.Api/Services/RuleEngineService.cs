using WelfareSystem.Api.Services.Interfaces;
using WelfareSystem.Api.Services.Rules;

namespace WelfareSystem.Api.Services;

public class RuleEngineService : IRuleEngineService
{
    private readonly List<IRule> _rules;

    public RuleEngineService()
    {
        // Register all rules
        _rules = new List<IRule>
        {
            new ActiveStatusRule(),
            new ServicePeriodRule(),
            new AmountLimitRule(),
            new FrequencyRule(),
            new DuplicateApplicationRule()
        };
    }

    public RuleEngineResult EvaluateEligibility(RuleEvaluationContext context)
    {
        var result = new RuleEngineResult();
        
        foreach (var rule in _rules)
        {
            try
            {
                var ruleResult = rule.Evaluate(context);
                result.RuleResults.Add(ruleResult);
                
                if (!ruleResult.IsPassed)
                {
                    result.WarningMessages.Add(ruleResult.Message);
                }
            }
            catch (Exception ex)
            {
                result.RuleResults.Add(new RuleEvaluationResult
                {
                    RuleName = rule.RuleName,
                    IsPassed = false,
                    Message = $"Rule evaluation failed: {ex.Message}"
                });
            }
        }

        // Determine overall eligibility
        result.IsEligible = result.RuleResults.All(r => r.IsPassed);
        
        if (!result.IsEligible)
        {
            result.AdditionalData["FailedRuleCount"] = result.RuleResults.Count(r => !r.IsPassed);
            result.AdditionalData["PassedRuleCount"] = result.RuleResults.Count(r => r.IsPassed);
        }

        return result;
    }

    public async Task<RuleEngineResult> EvaluateEligibilityAsync(RuleEvaluationContext context)
    {
        // For now, run synchronously but could be made truly async if needed
        return await Task.Run(() => EvaluateEligibility(context));
    }
}
