'use client';
import { useCallback } from 'react';
import Link from 'next/link';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { operationsApi } from '@/lib/api/operations';
import Alert from '@/components/ui/Alert';
export default function DecisionAddenda({ applicationId, reviewId }: { applicationId: number; reviewId: number }) {
  const load = useCallback(() => operationsApi.amendments(applicationId), [applicationId]); const { data, error } = useLiveData(load);
  const entries = data?.filter(item => item.entry.reviewId === reviewId);
  return <section className="mt-5 space-y-3"><h3 className="font-bold text-gray-900">Appeals and correction addenda</h3>{error && <Alert tone="error">{error}</Alert>}{!entries?.length && <p className="text-sm text-gray-600">No addenda or appeals recorded for this decision.</p>}{entries?.map(item => <div key={item.entry.id} className="rounded-lg border border-gray-200 p-3 text-sm text-gray-800"><p className="font-semibold">#{item.entry.id} · {item.entry.kind} · {item.entry.status}</p><p className="whitespace-pre-wrap">{item.entry.reason}</p>{item.entry.status === 'Approved' && item.entry.proposedNotes && <p className="whitespace-pre-wrap">Accepted addendum: {item.entry.proposedNotes}</p>}<p className="whitespace-pre-wrap">{item.entry.resolution}</p>{item.entry.resolvedAt && <p>{item.resolver} · {new Date(item.entry.resolvedAt).toLocaleString()}</p>}</div>)}<Link className="no-print inline-block font-semibold text-red-700 underline" href={`/amendments?reviewId=${reviewId}`}>Request an appeal or correction</Link></section>;
}
