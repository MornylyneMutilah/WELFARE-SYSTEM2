'use client';

import { useCallback, useState } from 'react';
import Link from 'next/link';
import { recordsApi, downloadFile, messageFor, statusLabel } from '@/lib/api/records';
import { documentsApi } from '@/lib/api/documents';
import { reviewsApi } from '@/lib/api/reviews';
import { applicationsApi } from '@/lib/api/applications';
import { welfareProgramsApi } from '@/lib/api/welfarePrograms';
import { SelectField } from '@/components/ui/Field';
import { useLiveData } from '@/lib/hooks/useLiveData';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import EligibilityAssessment from './EligibilityAssessment';

export default function ApplicationEvidence({ applicationId, canEdit = false }: { applicationId: number; canEdit?: boolean }) {
  const load = useCallback(async () => {
    const app = await applicationsApi.getApplicationById(applicationId);
    const program = await welfareProgramsApi.getProgramById(app.welfareProgramId);
    let requirements: string[] = [];
    try { const parsed: unknown = JSON.parse(program.requiredDocuments || '[]'); if (Array.isArray(parsed)) requirements = parsed.filter((value): value is string => typeof value === 'string'); } catch { /* Backend blocks invalid configurations on submission. */ }
    return { requirements, documents: await documentsApi.getDocumentsByApplicationId(applicationId), timeline: await recordsApi.timeline(applicationId), reviews: await reviewsApi.getReviewsByApplicationId(applicationId) };
  }, [applicationId]);
  const { data, error, loading, refresh } = useLiveData(load);
  const [file, setFile] = useState<File | null>(null);
  const [requirement, setRequirement] = useState('');
  const [inputVersion, setInputVersion] = useState(0);
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState('');
  const [notice, setNotice] = useState('');
  const [removeId, setRemoveId] = useState<number | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const action = async (work: () => Promise<unknown>, message = '') => { if (busy) return; setBusy(true); setFailure(''); setNotice(''); try { await work(); setNotice(message); setRemoveId(null); setConfirmed(false); await refresh(); } catch (err) { setFailure(messageFor(err, err instanceof Error ? err.message : 'Could not complete this action.')); } finally { setBusy(false); } };
  return <section className="mt-5 space-y-4" aria-label="Application evidence and timeline">
    {(error || failure) && <Alert tone="error">{failure || error}</Alert>}{notice && <Alert tone="success">{notice}</Alert>}
    <h2 className="text-lg font-bold text-gray-900">Supporting documents</h2>
    {loading ? <p role="status">Loading evidence…</p> : <ul className="divide-y divide-gray-200">{data?.documents.map(d => <li key={d.id} className="flex flex-wrap items-center justify-between gap-2 py-3"><div><p className="font-medium text-gray-900">{d.fileName}</p><p className="text-sm text-gray-600">{d.requirementName || 'Additional evidence'} · {d.scanStatus} · {Math.ceil(d.fileSize / 1024)} KB · {new Date(d.uploadedAt).toLocaleString()}{!d.contentAvailable && ' · File content unavailable (legacy upload)'}</p></div><div className="no-print flex gap-2"><Button size="sm" variant="outlined" disabled={busy || !d.contentAvailable} onClick={() => void action(() => downloadFile(`/documents/${d.id}/content`, d.fileName))}>Download</Button>{canEdit && <Button size="sm" variant="destructive" disabled={busy} onClick={() => { setRemoveId(d.id); setConfirmed(false); }}>Remove</Button>}</div></li>)}</ul>}
    {data?.requirements.length ? <ul className="space-y-1 text-sm text-gray-700">{data.requirements.map(name => <li key={name}>{data.documents.some(document => document.contentAvailable && document.requirementName === name) ? '✓ Supplied: ' : 'Required: '}{name}</li>)}</ul> : null}
    {!loading && data?.documents.length === 0 && <p className="text-sm text-gray-600">No supporting documents supplied.</p>}
    {canEdit && <form className="no-print space-y-3 rounded-lg border border-gray-200 p-4" onSubmit={event => { event.preventDefault(); if (file) void action(async () => { await documentsApi.uploadDocument(applicationId, file, requirement); setFile(null); setInputVersion(value => value + 1); }, 'Document stored successfully.'); }}><label htmlFor={`evidence-${applicationId}`} className="block text-sm font-semibold text-gray-900">Add a supporting document to this draft</label><SelectField id={`requirement-${applicationId}`} label="Document requirement" value={requirement} onChange={event => setRequirement(event.target.value)}><option value="">Additional evidence</option>{data?.requirements.map(name => <option key={name} value={name}>{name}</option>)}</SelectField><input key={inputVersion} id={`evidence-${applicationId}`} type="file" required disabled={busy} accept=".pdf,.doc,.docx,.png,.jpg,.jpeg" onChange={event => setFile(event.target.files?.[0] || null)} /><p className="text-sm text-gray-600">PDF, Word, PNG or JPEG, up to 10 MB. Submitted evidence is locked against replacement or removal.</p><Button type="submit" disabled={busy || !file} loading={busy}>Upload document</Button></form>}
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
