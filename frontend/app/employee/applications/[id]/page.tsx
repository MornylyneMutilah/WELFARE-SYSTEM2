'use client';

import { useCallback, useState } from 'react';
import { useParams } from 'next/navigation';
import { applicationsApi } from '@/lib/api/applications';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { useAuth } from '@/lib/auth/AuthContext';
import { messageFor } from '@/lib/api/records';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { TextField, TextAreaField } from '@/components/ui/Field';
import ApplicationEvidence from '@/components/common/ApplicationEvidence';
import EligibilityAssessment from '@/components/common/EligibilityAssessment';
import ApplicationJourney from '@/components/common/ApplicationJourney';

export default function ApplicationPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const load = useCallback(() => applicationsApi.getApplicationById(Number(id)), [id]);
  const { data: app, error, loading, refresh } = useLiveData(load);
  const [edit, setEdit] = useState<{ amount: string; reason: string } | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState('');
  const [success, setSuccess] = useState('');
  const canEdit = app?.status === 1 && app.userId === user?.id;
  const action = async (work: () => Promise<unknown>, message: string) => { if (busy) return; setBusy(true); setFailure(''); setSuccess(''); try { await work(); setSuccess(message); setEdit(null); setConfirmed(false); await refresh(); } catch (err) { setFailure(messageFor(err, 'Could not save the application. Your entries have been kept.')); } finally { setBusy(false); } };
  return <AppShell title={`Application #${id}`} description="Review your application, documents, eligibility checks, and recorded decisions.">
    {(error || failure) && <Alert tone="error">{failure || error}</Alert>}{success && <Alert tone="success">{success}</Alert>}
    {loading ? <p role="status">Loading application…</p> : app && <Card><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-bold text-gray-900">{app.programName}</h2><Button variant="outlined" className="no-print" onClick={() => window.print()}>Print application</Button></div>
      <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2"><div><dt>Employee</dt><dd className="font-semibold text-gray-900">{app.userName}</dd></div><div><dt>Current status</dt><dd className="font-semibold text-gray-900">{app.statusName}</dd></div><div><dt>Requested amount</dt><dd className="font-semibold text-gray-900">{app.requestedAmount.toLocaleString()}</dd></div><div><dt>{app.status === 1 ? 'Draft created' : 'Submitted'}</dt><dd>{new Date(app.submittedAt).toLocaleString()}</dd></div></dl>
      <h3 className="mt-4 font-bold text-gray-900">Application reason</h3><p className="whitespace-pre-wrap text-gray-800">{app.reason}</p>
      <ApplicationJourney status={app.status} />
      <h3 className="mt-4 font-bold text-gray-900">Latest recorded eligibility assessment</h3><EligibilityAssessment value={app.ruleEvaluationResult} />
      {canEdit && <div className="no-print mt-4 space-y-4 border-t border-gray-200 pt-4">{edit ? <form className="space-y-3" onSubmit={event => { event.preventDefault(); void action(() => applicationsApi.updateApplication(app.id, { requestedAmount: Number(edit.amount), reason: edit.reason }), 'Draft updated and eligibility reassessed.'); }}><TextField id="draft-amount" label="Requested amount" type="number" min="0.01" step="0.01" required disabled={busy} value={edit.amount} onChange={e => setEdit({ ...edit, amount: e.target.value })} /><TextAreaField id="draft-reason" label="Application reason" required maxLength={2000} disabled={busy} value={edit.reason} onChange={e => setEdit({ ...edit, reason: e.target.value })} /><Button type="submit" loading={busy}>Save draft changes</Button><Button variant="ghost" disabled={busy} onClick={() => setEdit(null)}>Cancel edit</Button></form> : <Button variant="outlined" disabled={busy} onClick={() => { setEdit({ amount: String(app.requestedAmount), reason: app.reason }); setConfirmed(false); }}>Edit draft</Button>}
        <p className="text-sm text-gray-700">Check the amount, reason, document instructions, and supporting evidence below before submitting. Eligibility is reassessed using current rules; HR → Committee → Management review follows. Submission locks the draft and evidence.</p>
        <label className="flex items-start gap-2 text-sm text-gray-900"><input type="checkbox" checked={confirmed} disabled={busy || !!edit} onChange={e => setConfirmed(e.target.checked)} />I have reviewed this application and its documents and confirm submission.</label>
        <Button disabled={!confirmed || busy || !!edit} loading={busy} onClick={() => void action(() => applicationsApi.submitApplication(app.id), `Application #${app.id} submitted. HR will review it first; track progress here.`)}>Confirm and submit application</Button></div>}
      <ApplicationEvidence applicationId={app.id} canEdit={canEdit} />
    </Card>}
  </AppShell>;
}
