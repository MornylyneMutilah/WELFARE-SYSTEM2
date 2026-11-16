'use client';

import { useCallback, useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import { applicationsApi } from '@/lib/api/applications';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { useAuth } from '@/lib/auth/AuthContext';
import { messageFor } from '@/lib/api/records';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import ActionFeedback from '@/components/common/ActionFeedback';
import Button from '@/components/ui/Button';
import { TextField, TextAreaField } from '@/components/ui/Field';
import ApplicationEvidence from '@/components/common/ApplicationEvidence';
import EligibilityAssessment from '@/components/common/EligibilityAssessment';
import ApplicationJourney from '@/components/common/ApplicationJourney';

export default function ApplicationPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const load = useCallback(async () => {
    const application = await applicationsApi.getApplicationById(Number(id));
    const eligibilityPreview = application.status === 1 ? await applicationsApi.getEligibilityPreview(application.id) : null;
    return { ...application, eligibilityPreview };
  }, [id]);
  const { data: app, error, loading, refresh } = useLiveData(load);
  const [edit, setEdit] = useState<{ amount: string; reason: string } | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [evidenceBusy, setEvidenceBusy] = useState(false);
  const actionInFlight = useRef(false);
  const [failure, setFailure] = useState('');
  const [submissionBlocked, setSubmissionBlocked] = useState(false);
  const [success, setSuccess] = useState('');
  const canEdit = app?.status === 1 && app.userId === user?.id;
  const action = async (work: () => Promise<unknown>, message: string, submitting = false) => { if (actionInFlight.current || evidenceBusy) return; actionInFlight.current = true; setBusy(true); setFailure(''); setSubmissionBlocked(false); setSuccess(''); try { await work(); setSuccess(message); setEdit(null); setConfirmed(false); await refresh(); } catch (err) { setFailure(messageFor(err, 'Could not save the application. Your entries have been kept.')); const issues = (err as { response?: { data?: { errors?: unknown } } }).response?.data?.errors; setSubmissionBlocked(submitting && Array.isArray(issues)); void refresh(); } finally { actionInFlight.current = false; setBusy(false); } };
  const currentFailure = submissionBlocked && app?.eligibilityPreview
    ? app.eligibilityPreview.blockingIssues.length ? `Submission blocked: ${app.eligibilityPreview.blockingIssues.join(' ')}` : ''
    : failure;
  return <AppShell title={`Application #${id}`} description="Review your application, documents, eligibility checks, and recorded decisions.">
    <ActionFeedback message={currentFailure || success || error} tone={currentFailure || (!success && error) ? 'error' : 'success'} />
    {loading ? <p role="status">Loading application…</p> : app && <Card><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-bold text-gray-900">{app.programName}</h2><Button variant="outlined" className="no-print" onClick={() => window.print()}>Print application</Button></div>
      <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2"><div><dt>Employee</dt><dd className="font-semibold text-gray-900">{app.userName}</dd></div><div><dt>Current status</dt><dd className="font-semibold text-gray-900">{app.statusName}</dd></div><div><dt>Requested amount</dt><dd className="font-semibold text-gray-900">{app.requestedAmount.toLocaleString()}</dd></div><div><dt>{app.status === 1 ? 'Draft created' : 'Submitted'}</dt><dd>{new Date(app.submittedAt).toLocaleString()}</dd></div></dl>
      <h3 className="mt-4 font-bold text-gray-900">Application reason</h3><p className="whitespace-pre-wrap text-gray-800">{app.reason}</p>
      <ApplicationJourney status={app.status} />
      {app.eligibilityPreview ? <section className="mt-4 space-y-2"><div className="flex flex-wrap items-center justify-between gap-2"><h3 className="font-bold text-gray-900">Current eligibility checks</h3><Button variant="outlined" size="sm" disabled={busy || evidenceBusy} onClick={() => { setConfirmed(false); void refresh(); }}>Refresh checks</Button></div><p className="text-sm text-gray-600">Uses the latest program rules and uploaded evidence. Updates when you return to this tab and within 10 seconds of a change.</p><EligibilityAssessment value={app.eligibilityPreview.ruleEvaluationResult} /><details><summary className="cursor-pointer text-sm font-semibold text-gray-700">Previous recorded assessment (historical)</summary><EligibilityAssessment value={app.ruleEvaluationResult} /></details></section> : <><h3 className="mt-4 font-bold text-gray-900">Recorded eligibility assessment</h3><EligibilityAssessment value={app.ruleEvaluationResult} /></>}
      {canEdit && <div className="no-print mt-4 space-y-4 border-t border-gray-200 pt-4">{edit ? <form className="space-y-3" onSubmit={event => { event.preventDefault(); void action(() => applicationsApi.updateApplication(app.id, { requestedAmount: Number(edit.amount), reason: edit.reason }), 'Draft updated and eligibility reassessed.'); }}><TextField id="draft-amount" label="Requested amount" type="number" min="0.01" step="0.01" required disabled={busy} value={edit.amount} onChange={e => setEdit({ ...edit, amount: e.target.value })} /><TextAreaField id="draft-reason" label="Application reason" required maxLength={2000} disabled={busy} value={edit.reason} onChange={e => setEdit({ ...edit, reason: e.target.value })} /><Button type="submit" loading={busy}>Save draft changes</Button><Button variant="ghost" disabled={busy} onClick={() => setEdit(null)}>Cancel edit</Button></form> : <Button variant="outlined" disabled={busy} onClick={() => { setEdit({ amount: String(app.requestedAmount), reason: app.reason }); setConfirmed(false); }}>Edit draft</Button>}
        <p className="text-sm text-gray-700">Check the amount, reason, document instructions, and supporting evidence below before submitting. Eligibility is reassessed using current rules; HR → Committee → Management review follows. Submission locks the draft and evidence.</p>
        <label className="flex items-start gap-2 text-sm text-gray-900"><input type="checkbox" checked={confirmed} disabled={busy || !!edit} onChange={e => setConfirmed(e.target.checked)} />I have reviewed this application and its documents and confirm submission.</label>
        {evidenceBusy && <p role="status" className="text-sm text-red-800">Wait for the document upload to finish before submitting the application.</p>}
        <Button disabled={!confirmed || busy || evidenceBusy || !!edit} loading={busy} onClick={() => void action(() => applicationsApi.submitApplication(app.id), `Application #${app.id} submitted. HR will review it first; track progress here.`, true)}>Confirm and submit application</Button></div>}
      <ApplicationEvidence applicationId={app.id} canEdit={canEdit} disabled={busy} onBusyChange={setEvidenceBusy} />
    </Card>}
  </AppShell>;
}
