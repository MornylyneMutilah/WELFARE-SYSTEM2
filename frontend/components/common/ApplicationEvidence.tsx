'use client';

import { useCallback, useRef, useState } from 'react';
import Link from 'next/link';
import { recordsApi, downloadFile, messageFor, statusLabel } from '@/lib/api/records';
import { documentsApi } from '@/lib/api/documents';
import { reviewsApi } from '@/lib/api/reviews';
import { applicationsApi } from '@/lib/api/applications';
import { welfareProgramsApi } from '@/lib/api/welfarePrograms';
import { SelectField } from '@/components/ui/Field';
import { useLiveData } from '@/lib/hooks/useLiveData';
import Button from '@/components/ui/Button';
import ActionFeedback from './ActionFeedback';
import EligibilityAssessment from './EligibilityAssessment';

export default function ApplicationEvidence({ applicationId, canEdit = false, disabled = false, onBusyChange }: { applicationId: number; canEdit?: boolean; disabled?: boolean; onBusyChange?: (busy: boolean) => void }) {
  const load = useCallback(async () => {
    const app = await applicationsApi.getApplicationById(applicationId);
    const program = await welfareProgramsApi.getProgramById(app.welfareProgramId);
    let requirements: string[] = [];
    try { const parsed: unknown = JSON.parse(program.requiredDocuments || '[]'); if (Array.isArray(parsed)) requirements = parsed.filter((value): value is string => typeof value === 'string'); } catch { /* Backend blocks invalid configurations on submission. */ }
    return { requirements, documents: await documentsApi.getDocumentsByApplicationId(applicationId), timeline: await recordsApi.timeline(applicationId), reviews: await reviewsApi.getReviewsByApplicationId(applicationId) };
  }, [applicationId]);
  const { data, error, loading, refresh } = useLiveData(load);
  const [file, setFile] = useState<File | null>(null);
  const [requirement, setRequirement] = useState<string | null>(null);
  const fileInput = useRef<HTMLInputElement>(null);
  const actionInFlight = useRef(false);
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState('');
  const [notice, setNotice] = useState('');
  const [removeId, setRemoveId] = useState<number | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const supplied = (name: string) => data?.documents.some(document => document.contentAvailable && document.requirementName.toLowerCase() === name.toLowerCase());
  const selectedRequirement = requirement ?? data?.requirements.find(name => !supplied(name)) ?? '';
  const action = async (work: () => Promise<unknown>, message = '') => {
    if (actionInFlight.current) return;
    actionInFlight.current = true; setBusy(true); onBusyChange?.(true); setFailure(''); setNotice('');
    try {
      await work(); setNotice(message); setRemoveId(null); setConfirmed(false);
      // Background reads must not keep the native file picker disabled after a completed upload.
      void refresh();
    } catch (err) { setFailure(messageFor(err, err instanceof Error ? err.message : 'Could not complete this action.')); }
    finally { actionInFlight.current = false; setBusy(false); onBusyChange?.(false); }
  };
  return <section className="mt-5 space-y-4" aria-label="Application evidence and timeline">
    <ActionFeedback message={failure || notice || error} tone={failure || (!notice && error) ? 'error' : 'success'} />
    <h2 className="text-lg font-bold text-gray-900">Supporting documents</h2>
    {loading ? <p role="status">Loading evidence…</p> : <ul className="divide-y divide-gray-200">{data?.documents.map(d => <li key={d.id} className="flex flex-wrap items-center justify-between gap-2 py-3"><div><p className="font-medium text-gray-900">{d.fileName}</p><p className="text-sm text-gray-600">{d.requirementName || 'Additional evidence'} · {d.scanStatus} · {Math.ceil(d.fileSize / 1024)} KB · {new Date(d.uploadedAt).toLocaleString()}{!d.contentAvailable && ' · File content unavailable (legacy upload)'}</p></div><div className="no-print flex gap-2"><Button size="sm" variant="outlined" disabled={busy || !d.contentAvailable} onClick={() => void action(() => downloadFile(`/documents/${d.id}/content`, d.fileName))}>Download</Button>{canEdit && <Button size="sm" variant="destructive" disabled={busy} onClick={() => { setRemoveId(d.id); setConfirmed(false); }}>Remove</Button>}</div></li>)}</ul>}
    {data?.requirements.length ? <ul className="space-y-1 text-sm text-gray-700">{data.requirements.map(name => <li key={name}>{supplied(name) ? '✓ Supplied: ' : 'Required: '}{name}</li>)}</ul> : null}
    {!loading && data?.documents.length === 0 && <p className="text-sm text-gray-600">No supporting documents supplied.</p>}
    {canEdit && <form className="no-print space-y-3 rounded-lg border border-gray-200 p-4" onSubmit={event => {
      event.preventDefault();
      if (!file || disabled || actionInFlight.current) return;
      if (file.size === 0 || file.size > 10 * 1024 * 1024) { setNotice(''); setFailure('Choose a non-empty file no larger than 10 MB.'); return; }
      void action(async () => {
        await documentsApi.uploadDocument(applicationId, file, selectedRequirement);
        setFile(null);
        // Keep the same input element mounted; clear its value so even the same file can be selected again.
        if (fileInput.current) fileInput.current.value = '';
        setRequirement(data?.requirements.find(name => name !== selectedRequirement && !supplied(name)) ?? '');
      }, `${file.name} uploaded successfully${selectedRequirement ? ` for ${selectedRequirement}` : ''}. You can choose and upload another document. The application remains a draft until you confirm final submission.`);
    }}>
      <label htmlFor={`evidence-${applicationId}`} className="block text-sm font-semibold text-gray-900">Add a supporting document to this draft</label>
      <SelectField id={`requirement-${applicationId}`} label="Document requirement" disabled={busy || disabled} value={selectedRequirement} onChange={event => setRequirement(event.target.value)}><option value="">Additional evidence</option>{data?.requirements.map(name => <option key={name} value={name}>{name}{supplied(name) ? ' — supplied' : ''}</option>)}</SelectField>
      <input ref={fileInput} id={`evidence-${applicationId}`} className="sr-only" tabIndex={-1} type="file" disabled={busy || disabled} accept=".pdf,.doc,.docx,.png,.jpg,.jpeg" aria-describedby={`evidence-help-${applicationId}`} onChange={event => { setFile(event.target.files?.[0] || null); setFailure(''); setNotice(''); }} />
      <div className="flex flex-wrap items-center gap-3"><Button variant="outlined" disabled={busy || disabled} aria-controls={`evidence-${applicationId}`} onClick={() => fileInput.current?.click()} className="active:bg-red-100">Choose file</Button><span className="break-all text-sm text-gray-700">{file?.name || 'No file selected'}</span></div>
      <p id={`evidence-help-${applicationId}`} className="text-sm text-gray-600">Upload each required document separately. PDF, Word, PNG or JPEG, up to 10 MB per file. Submitted evidence is locked against replacement or removal.</p>
      <Button type="submit" disabled={busy || disabled || !file} loading={busy} aria-busy={busy} className={busy ? 'bg-red-800 text-white disabled:bg-red-800 disabled:text-white' : 'active:bg-red-900 active:shadow-inner'}>{busy ? 'Uploading…' : 'Upload document'}</Button>
    </form>}
    {removeId && <div className="no-print space-y-3 rounded-lg border border-red-200 p-4"><label className="flex gap-2 text-sm text-gray-900"><input type="checkbox" checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)} />Remove this document from my draft?</label><Button variant="destructive" disabled={!confirmed || busy} onClick={() => void action(() => documentsApi.deleteDocument(removeId), 'Draft document removed.')}>Confirm removal</Button><Button variant="ghost" disabled={busy} onClick={() => setRemoveId(null)}>Keep document</Button></div>}
    <h2 className="text-lg font-bold text-gray-900">Application timeline</h2>
    <ol className="space-y-3 border-l-2 border-red-200 pl-4">{data?.timeline.history.map(event => <li key={event.id}><p className="font-semibold text-gray-900">{statusLabel(event.status)}</p><p className="text-sm text-gray-600">{new Date(event.changedAt).toLocaleString()} · {event.actorName || `User #${event.changedBy}`}</p><p className="whitespace-pre-wrap text-sm text-gray-800">{event.notes}</p></li>)}</ol>
    {data?.timeline.history.length === 0 && <p className="text-sm text-gray-600">No submitted status transitions recorded yet.</p>}
    <h2 className="text-lg font-bold text-gray-900">Recorded decisions</h2>
    <ul className="divide-y divide-gray-200">{data?.reviews.map(review => <li key={review.id} className="py-3 text-sm"><p className="font-semibold text-gray-900">WEL-DEC-{String(review.id).padStart(6, '0')} · {review.reviewerRole} · {review.recommendation}</p><p className="text-gray-600">{review.reviewerName} · {new Date(review.reviewedAt).toLocaleString()}</p><p className="whitespace-pre-wrap text-gray-800">{review.comments}</p><Link className="no-print font-semibold text-red-700 underline" href={`/reviews/history/${review.id}`}>Decision confirmation</Link></li>)}</ul>
    {data?.reviews.length === 0 && <p className="text-sm text-gray-600">No decisions recorded yet.</p>}
    <h2 className="text-lg font-bold text-gray-900">Eligibility assessment history</h2>
    {data?.timeline.assessments.map(assessment => <details key={assessment.id} className="rounded-lg border border-gray-200 p-3"><summary className="cursor-pointer font-medium text-gray-900">Assessment · {new Date(assessment.timestamp).toLocaleString()}</summary><EligibilityAssessment value={assessment.details} /></details>)}
  </section>;
}
