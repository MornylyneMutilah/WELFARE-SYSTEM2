namespace WelfareSystem.Api.Services.Rules;

public interface IRule
{
    string RuleName { get; }
    string RuleDescription { get; }
    RuleEvaluationResult Evaluate(RuleEvaluationContext context);
}
