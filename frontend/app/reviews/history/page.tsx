'use client';

import { useCallback, useState } from 'react';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { SelectField, TextField } from '@/components/ui/Field';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { useAuth } from '@/lib/auth/AuthContext';
import { ROLE_LABELS, isAppRole } from '@/lib/auth/roles';
import { recordsApi, downloadFile, statusLabel } from '@/lib/api/records';

export default function DecisionHistoryPage() {
  const { user } = useAuth();
  const [filters, setFilters] = useState({ scope: 'team', search: '', stage: '', decision: '', from: '', to: '' });
  const [page, setPage] = useState(1);
  const [failure, setFailure] = useState('');
  const [busy, setBusy] = useState(false);
  const query = new URLSearchParams(Object.entries(filters).filter(([, value]) => value)).toString();
  const load = useCallback(() => recordsApi.decisions(`${query}&page=${page}`), [query, page]);
  const { data, error, loading, refresh } = useLiveData(load);
  const update = (patch: Partial<typeof filters>) => { setFilters(value => ({ ...value, ...patch })); setPage(1); };
  const exportCsv = async () => { setBusy(true); setFailure(''); try { await downloadFile(`/records/decisions.csv?${query}`, 'welfare-decisions.csv'); } catch (err) { setFailure(err instanceof Error ? err.message : 'Export failed.'); } finally { setBusy(false); } };
  return <AppShell title="Decision register" description="Find completed HR reviews, committee endorsements, management approvals, and rejections. Recorded decisions remain available after the application moves to the next stage.">
    <Card><div className="no-print mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3"><TextField id="decision-search" label="Application ID, decision ID, employee or program" value={filters.search} onChange={e => update({ search: e.target.value })} />
      <SelectField id="decision-scope" label="Recorded by" value={filters.scope} onChange={e => update({ scope: e.target.value })}><option value="team">Authorized stage records</option><option value="mine">My decisions</option></SelectField>
      <SelectField id="decision-filter" label="Decision" value={filters.decision} onChange={e => update({ decision: e.target.value })}><option value="">All decisions</option><option value="Approve">Approved / endorsed</option><option value="Reject">Rejected</option></SelectField>
      {user?.role === 'Administrator' && <SelectField id="decision-stage" label="Stage" value={filters.stage} onChange={e => update({ stage: e.target.value })}><option value="">All stages</option><option value="HROfficer">HR</option><option value="CommitteeMember">Committee</option><option value="Management">Management</option></SelectField>}
      <TextField id="decision-from" type="date" label="From date (UTC)" value={filters.from} onChange={e => update({ from: e.target.value })} /><TextField id="decision-to" type="date" label="Through date (UTC)" value={filters.to} onChange={e => update({ to: e.target.value })} />
    </div><div className="no-print mb-4 flex flex-wrap gap-2"><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button><Button loading={busy} onClick={() => void exportCsv()}>Export matching decisions (CSV)</Button><Button variant="outlined" onClick={() => window.print()}>Print current page</Button></div>
    {(error || failure) && <Alert tone="error">{failure || error}</Alert>}<p className="mb-3 text-sm text-gray-600">{data?.total ?? 0} decisions. Names and application status in this list reflect current records. Open a confirmation to see the saved decision-time snapshot where available.</p>
    {loading ? <p role="status">Loading decisions…</p> : !data?.items.length ? <p className="py-6 text-gray-700">No decisions match these filters. Completed decisions appear here after they are saved.</p> : <div className="overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="bg-gray-50 text-gray-900"><tr>{['Reference / application', 'Employee / program', 'Stage / decision', 'Reviewer / date', 'Current status', 'Confirmation'].map(label => <th key={label} className="p-3">{label}</th>)}</tr></thead><tbody>{data.items.map(record => <tr key={record.id} className="border-t border-gray-200"><td className="p-3 font-medium text-gray-900">WEL-DEC-{String(record.id).padStart(6, '0')}<p>Application #{record.applicationId}</p></td><td className="p-3">{record.employeeName}<p>{record.programName}</p></td><td className="p-3">{isAppRole(record.stage) ? ROLE_LABELS[record.stage] : record.stage}<p>{record.decision === 'Approve' && record.stage === 'CommitteeMember' ? 'Endorsed' : record.decision}</p></td><td className="p-3">{record.reviewerName}<p>{new Date(record.reviewedAt).toLocaleString()}</p></td><td className="p-3">{statusLabel(record.currentStatus)}</td><td className="p-3"><Link className="font-semibold text-red-700 underline" href={`/reviews/history/${record.id}`}>Open confirmation</Link></td></tr>)}</tbody></table></div>}
    <div className="no-print mt-4 flex items-center gap-3"><Button variant="outlined" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</Button><p className="text-sm">Page {page}</p><Button variant="outlined" disabled={!data || page * data.pageSize >= data.total} onClick={() => setPage(page + 1)}>Next</Button></div></Card>
  </AppShell>;
}
