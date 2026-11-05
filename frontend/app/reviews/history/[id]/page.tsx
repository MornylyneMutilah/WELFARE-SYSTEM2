'use client';

import { useCallback } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import EligibilityAssessment from '@/components/common/EligibilityAssessment';
import DecisionAddenda from '@/components/common/DecisionAddenda';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { recordsApi, statusLabel } from '@/lib/api/records';
import { ROLE_LABELS, isAppRole } from '@/lib/auth/roles';

export default function ReceiptPage() {
  const { id } = useParams<{ id: string }>();
  const load = useCallback(() => recordsApi.receipt(Number(id)), [id]);
  const { data, error, loading } = useLiveData(load);
  const record = data?.record; const saved = data?.snapshot;
  const role = saved?.reviewerRole || record?.stage;
  return <AppShell title="Decision confirmation" description="Read-only confirmation of a recorded decision. Committee endorsement still requires management's final decision.">
    {error && <Alert tone="error">{error}</Alert>}{loading ? <p role="status">Loading confirmation…</p> : record && <Card>
      <div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-bold text-gray-900">{data.reference}</h2><Button className="no-print" onClick={() => window.print()}>Print / save as PDF</Button></div>
      {!saved && <div className="my-3"><Alert tone="warning">Legacy record: a decision-time application snapshot was not captured. Employee and program details below reflect current data; the recorded decision and timestamp remain available.</Alert></div>}
      <dl className="mt-5 grid gap-4 text-sm sm:grid-cols-2">{[
        ['Application reference', `#${record.applicationId}`], ['Employee', saved?.employeeName || record.employeeName], ['Program', saved?.programName || record.programName],
        ['Requested amount (not payment confirmation)', (saved?.requestedAmount ?? record.amount).toLocaleString()], ['Decision stage', role && isAppRole(role) ? ROLE_LABELS[role] : role],
        ['Decision', record.decision === 'Approve' && role === 'CommitteeMember' ? 'Endorsed for management review' : record.decision], ['Recorded by', saved?.reviewerName || record.reviewerName],
        ['Decision time', new Date(saved?.reviewedAt || record.reviewedAt).toLocaleString()], ['Application status now', statusLabel(record.currentStatus)],
        ...(saved ? [['Status after this decision', statusLabel(saved.newStatus)], ['Employee ID at decision', saved.employeeId]] : [])
      ].map(([label, value]) => <div key={label}><dt className="text-gray-600">{label}</dt><dd className="font-semibold text-gray-900">{value}</dd></div>)}</dl>
      <h3 className="mt-5 font-bold text-gray-900">Recorded review notes</h3><p className="whitespace-pre-wrap text-gray-800">{saved?.comments || record.comments}</p>
      {saved && <><h3 className="mt-5 font-bold text-gray-900">Application reason at decision</h3><p className="whitespace-pre-wrap text-gray-800">{saved.reason}</p><h3 className="mt-5 font-bold text-gray-900">Eligibility assessment retained with this decision</h3><EligibilityAssessment value={saved.ruleEvaluationResult} /></>}
      <p className="mt-5 text-sm text-gray-600">This reference identifies a system record. It is not a digital signature or proof of disbursement. Recorded decisions and notes cannot be overwritten through the application.</p>
      <DecisionAddenda applicationId={record.applicationId} reviewId={record.id} />
      {saved?.documents && <><h3 className="mt-5 font-bold text-gray-900">Supporting-document manifest at decision</h3>{saved.documents.length ? <ul className="list-inside list-disc text-sm text-gray-800">{saved.documents.map(document => <li key={document.id}>#{document.id} · {document.fileName} · {Math.ceil(document.fileSize / 1024)} KB</li>)}</ul> : <p className="text-sm text-gray-600">No supporting documents were recorded for this decision.</p>}</>}
      <Link className="no-print mt-4 inline-block font-semibold text-red-700 underline" href={`/employee/applications/${record.applicationId}`}>Open application and full timeline</Link>
    </Card>}
  </AppShell>;
}
