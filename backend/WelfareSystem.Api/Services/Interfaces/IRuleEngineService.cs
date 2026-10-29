using WelfareSystem.Api.Services.Rules;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IRuleEngineService
{
    RuleEngineResult EvaluateEligibility(RuleEvaluationContext context);
    Task<RuleEngineResult> EvaluateEligibilityAsync(RuleEvaluationContext context);
}
