'use client';

import { useCallback, useState } from 'react';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { TextField } from '@/components/ui/Field';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { recordsApi, downloadFile } from '@/lib/api/records';

export default function AuditPage() {
  const [filters, setFilters] = useState({ action: '', entity: '', actor: '', from: '', to: '' });
  const [page, setPage] = useState(1); const [failure, setFailure] = useState(''); const [busy, setBusy] = useState(false);
  const query = new URLSearchParams(Object.entries(filters).filter(([, value]) => value)).toString();
  const load = useCallback(() => recordsApi.audit(`${query}&page=${page}`), [query, page]);
  const { data, error, loading, refresh } = useLiveData(load);
  const update = (patch: Partial<typeof filters>) => { setFilters(value => ({ ...value, ...patch })); setPage(1); };
  const exportCsv = async () => { setBusy(true); setFailure(''); try { await downloadFile(`/auditlogs/export.csv?${query}`, 'welfare-audit.csv'); } catch (err) { setFailure(err instanceof Error ? err.message : 'Export failed.'); } finally { setBusy(false); } };
  return <AppShell title="Audit log" requiredRole="Administrator" description="Read-only records of account, program, application, document, and export actions. No edit or delete controls are provided."><Card>
    <div className="no-print mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3"><TextField id="audit-action" label="Exact action" placeholder="ApplicationReview, UserSuspended, ProgramCreated" value={filters.action} onChange={e => update({ action: e.target.value })} /><TextField id="audit-entity" label="Exact entity type" placeholder="User, WelfareApplication, WelfareProgram, Report" value={filters.entity} onChange={e => update({ entity: e.target.value })} /><TextField id="audit-actor" label="Actor user ID" type="number" min="1" value={filters.actor} onChange={e => update({ actor: e.target.value })} /><TextField id="audit-from" label="From date (UTC)" type="date" value={filters.from} onChange={e => update({ from: e.target.value })} /><TextField id="audit-to" label="Through date (UTC)" type="date" value={filters.to} onChange={e => update({ to: e.target.value })} /></div>
    <div className="no-print mb-4 flex flex-wrap gap-2"><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button><Button loading={busy} onClick={() => void exportCsv()}>Export matching events (CSV)</Button><Button variant="outlined" onClick={() => window.print()}>Print current page</Button></div>
    {(error || failure) && <Alert tone="error">{failure || error}</Alert>}<p className="mb-3 text-sm text-gray-600">{data?.total ?? 0} matching events. Dates are displayed in your browser timezone. Secret fields are redacted. Logs cover recorded actions; they are not a complete network access log.</p>
    {loading ? <p role="status">Loading audit events…</p> : !data?.items.length ? <p className="py-6 text-gray-700">No matching audit events.</p> : <ul className="divide-y divide-gray-200">{data.items.map(event => <li key={event.id} className="py-4"><h2 className="font-bold text-gray-900">#{event.id} · {event.action}</h2><p className="text-sm text-gray-600">{new Date(event.timestamp).toLocaleString()} · {event.actorName} (user {event.userId ?? 'system'}) · {event.entityType} #{event.entityId}</p><details className="mt-2"><summary className="cursor-pointer text-sm font-semibold text-red-700">Recorded details</summary><pre className="mt-2 max-h-96 overflow-auto whitespace-pre-wrap break-words rounded-lg bg-gray-50 p-3 text-xs text-gray-800">{event.details}</pre></details></li>)}</ul>}
    <div className="no-print mt-4 flex items-center gap-3"><Button variant="outlined" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</Button><p className="text-sm">Page {page}</p><Button variant="outlined" disabled={!data || page * data.pageSize >= data.total} onClick={() => setPage(page + 1)}>Next</Button></div>
  </Card></AppShell>;
}
