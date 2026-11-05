import Alert from '@/components/ui/Alert';

export default function EligibilityAssessment({ value }: { value: string }) {
  type Assessment = { AssessedAt?: string; Stage?: string; RuleEngine?: { IsEligible: boolean; RuleResults?: { RuleName: string; IsPassed: boolean; Message: string }[] } };
  let parsed: Assessment | null = null;
  try { parsed = JSON.parse(value || '{}') as Assessment; } catch { /* Older records may not contain JSON. */ }
  if (!parsed) return <p className="text-sm text-gray-600">The saved assessment cannot be displayed.</p>;
    if (!parsed.RuleEngine) return <p className="text-sm text-gray-600">No structured assessment recorded.</p>;
    return <div className="space-y-2 text-sm"><p className="text-gray-600">{parsed.AssessedAt ? `${parsed.Stage}: ${new Date(parsed.AssessedAt).toLocaleString()}` : 'Assessment recorded before submission-time snapshots were introduced.'}</p>
      <Alert tone={parsed.RuleEngine.IsEligible ? 'success' : 'warning'}>{parsed.RuleEngine.IsEligible ? 'All recorded eligibility checks passed.' : 'Review the concerns below. Active account/program, verified employment, service, amount and required evidence are mandatory for submission. Duplicate and frequency concerns support human review.'}</Alert>
      <ul className="divide-y divide-gray-200">{parsed.RuleEngine.RuleResults?.map((rule, index) => <li key={`${rule.RuleName}-${index}`} className="py-2"><strong>{rule.RuleName}: {rule.IsPassed ? 'Passed' : 'Concern'}</strong><p>{rule.Message}</p></li>)}</ul></div>;
}
