'use client';

import { useState } from 'react';
import { applicationsApi, type ApplicationResponse } from '@/lib/api/applications';
import { reviewsApi } from '@/lib/api/reviews';

import { useAuth } from '@/lib/auth/AuthContext';
import { useLiveData } from '@/lib/hooks/useLiveData';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import { SelectField, TextAreaField } from '@/components/ui/Field';
import AnomalyList from '@/components/anomaly/AnomalyList';
import ApplicationEvidence from '@/components/common/ApplicationEvidence';
import EligibilityAssessment from '@/components/common/EligibilityAssessment';

const stages: Record<string, number[]> = { HROfficer: [2, 3], CommitteeMember: [4], Management: [5, 7] };
const roleNames: Record<string, string> = { HROfficer: 'HR', CommitteeMember: 'Committee', Management: 'Management', Administrator: 'Administrator' };

function ReviewDetails({ app, eligible, role, onSaved }: { app: ApplicationResponse; eligible: boolean; role: string; onSaved: () => void }) {
  const [recommendation, setRecommendation] = useState('Approve');
  const [comments, setComments] = useState('');
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const approveLabel = role === 'HROfficer' ? 'Approve and forward to committee' : role === 'CommitteeMember' ? 'Endorse and forward to management' : 'Give final approval';

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (busy || !confirmed || !comments.trim() || !eligible) return;
    setBusy(true); setError('');
    try {
      await reviewsApi.createReview(app.id, { recommendation, comments: comments.trim() });
      onSaved();
    } catch (err: unknown) {
      const response = err as { response?: { data?: { message?: string } } };
      setError(response.response?.data?.message || 'Could not save the review. Your notes have been kept.');
    } finally { setBusy(false); }
  };
  return (
    <Card>
      <h2 className="text-lg font-bold text-gray-900">Application #{app.id}: {app.programName}</h2>
      <dl className="mt-3 grid gap-3 text-sm sm:grid-cols-2">
        <div><dt className="text-gray-600">Employee</dt><dd className="font-medium text-gray-900">{app.userName}</dd></div>
        <div><dt className="text-gray-600">Requested amount</dt><dd className="font-medium text-gray-900">{app.requestedAmount.toLocaleString()}</dd></div>
        <div><dt className="text-gray-600">Status</dt><dd className="font-medium text-gray-900">{app.statusName}</dd></div>
        <div><dt className="text-gray-600">Eligibility assessment</dt><dd className="font-medium text-gray-900">{app.isEligible ? 'Eligible' : 'Check eligibility concerns before deciding'}</dd></div>
      </dl>
      <h3 className="mt-4 font-semibold text-gray-900">Reason for application</h3>
      <p className="mt-1 whitespace-pre-wrap text-sm text-gray-700">{app.reason}</p>
      <ApplicationEvidence applicationId={app.id} /><EligibilityAssessment value={app.ruleEvaluationResult} /><h3 className="mt-4 mb-2 font-semibold text-gray-900">Anomaly flags</h3>
      <AnomalyList applicationId={app.id} />
      {eligible ? (
        <form onSubmit={submit} className="mt-6 space-y-4 border-t border-gray-200 pt-4" aria-label="Application decision">
          <h3 className="font-bold text-gray-900">Your decision</h3>
          {error && <Alert tone="error">{error}</Alert>}
          <SelectField id="recommendation" label="Decision" value={recommendation} onChange={e => { setRecommendation(e.target.value); setConfirmed(false); }} disabled={busy}>
            <option value="Approve">{approveLabel}</option><option value="Reject">Reject application</option>
          </SelectField>
          <TextAreaField id="review-comments" label="Review notes / reason" required maxLength={2000} rows={4} value={comments} disabled={busy} onChange={e => setComments(e.target.value)} />
          <label className="flex items-start gap-2 text-sm text-gray-800">
            <input type="checkbox" required checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)} />
            I have checked the application and confirm this {recommendation === 'Approve' ? 'approval' : 'rejection'} decision.
          </label>
          <Button type="submit" loading={busy} disabled={!confirmed || !comments.trim()}>Save decision</Button>
        </form>
      ) : <p className="mt-4 text-sm text-gray-600">This application is not awaiting a decision from your role, or is your own application.</p>}
    </Card>
  );
}

export default function ReviewsPage() {
  const { user } = useAuth();
  const { data, error, loading, refresh } = useLiveData(applicationsApi.getAllApplications);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [showAll, setShowAll] = useState(false);
  const [success, setSuccess] = useState('');
  const role = user?.role || '';
  const items = data ?? [];
  const pending = items.filter(app => stages[role]?.includes(app.status) && app.userId !== user?.id);
  const displayed = showAll ? items.filter(app => app.status !== 1) : pending;
  const selected = items.find(app => app.id === selectedId);
  return (
    <AppShell title={`${roleNames[role] || ''} application reviews`} description="Review the evidence and record a decision at your assigned stage.">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-gray-700">Awaiting your decision: <strong>{pending.length}</strong>. HR → Committee → Management.</p>
        <Button variant="outlined" onClick={() => void refresh()}>Refresh</Button>
      </div>
      {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}
      {success && <div className="mb-4"><Alert tone="success">{success}</Alert></div>}
      <label className="mb-4 flex items-center gap-2 text-sm text-gray-800"><input type="checkbox" checked={showAll} onChange={e => setShowAll(e.target.checked)} /> Show other stages and completed applications</label>
      <div className="grid items-start gap-6 lg:grid-cols-2">
        <Card>
          {loading ? <p role="status">Loading applications…</p> : displayed.length === 0 ? <p className="text-gray-700">No applications awaiting this stage. Employees must submit their drafts; HR approval forwards them to committee, then committee endorsement forwards them to management.</p> : (
            <ul className="divide-y divide-gray-100">{displayed.map(app => <li key={app.id} className="py-3">
              <p className="font-semibold text-gray-900">#{app.id} · {app.userName} · {app.programName}</p>
              <p className="mb-2 text-sm text-gray-600">{app.statusName} · {app.requestedAmount.toLocaleString()}</p>
              <Button variant={selectedId === app.id ? 'primary' : 'outlined'} onClick={() => setSelectedId(app.id)}>View application and reviews</Button>
            </li>)}</ul>
          )}
        </Card>
        {selected && <ReviewDetails key={selected.id} app={selected} role={role} eligible={pending.some(app => app.id === selected.id)} onSaved={() => {
          setSuccess(`Decision saved for application #${selected.id}. The next role's queue and dashboards will refresh automatically.`);
          setSelectedId(null); void refresh();
        }} />}
      </div>
    </AppShell>
  );
}
