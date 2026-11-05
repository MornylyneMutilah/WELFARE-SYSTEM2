'use client';

import { useCallback, useState } from 'react';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { TextField } from '@/components/ui/Field';
import { applicationsApi } from '@/lib/api/applications';
import { downloadFile } from '@/lib/api/records';
import { useAuth } from '@/lib/auth/AuthContext';
import { useLiveData } from '@/lib/hooks/useLiveData';

export default function ReportsPage() {
  const { user } = useAuth();
  const role = user?.role;
  const load = useCallback(() => role && role !== 'Employee' ? applicationsApi.getAllApplications() : applicationsApi.getApplications(), [role]);
  const { data, error, loading } = useLiveData(load);
  const [from, setFrom] = useState(''); const [to, setTo] = useState(''); const [failure, setFailure] = useState(''); const [busy, setBusy] = useState(false);
  const rows = (data || []).filter(app => (!from || app.submittedAt >= `${from}T00:00:00`) && (!to || app.submittedAt < `${new Date(new Date(`${to}T00:00:00Z`).getTime() + 86400000).toISOString()}`));
  const query = new URLSearchParams({ ...(from ? { from } : {}), ...(to ? { to } : {}) }).toString();
  const exportCsv = async () => { setBusy(true); setFailure(''); try { await downloadFile(`/records/applications.csv?${query}`, 'welfare-applications.csv'); } catch (err) { setFailure(err instanceof Error ? err.message : 'Export failed.'); } finally { setBusy(false); } };
  const approved = rows.filter(a => a.status === 8);
  return <AppShell title="Application reports" description="Reports follow your authorized access: employees see their own records, HR its department, and authorized organization-wide roles the wider register."><Card>
    <div className="no-print mb-4 flex flex-wrap items-end gap-3"><TextField id="report-from" label="From date (UTC)" type="date" value={from} onChange={e => setFrom(e.target.value)} /><TextField id="report-to" label="Through date (UTC)" type="date" value={to} onChange={e => setTo(e.target.value)} /><Button loading={busy} onClick={() => void exportCsv()}>Export matching applications (CSV)</Button><Button variant="outlined" onClick={() => window.print()}>Print / save as PDF</Button></div>
    {(error || failure) && <Alert tone="error">{failure || error}</Alert>}
    <p className="mb-4 text-sm text-gray-700">Applications: <strong>{rows.length}</strong> · Approved: <strong>{approved.length}</strong> · Approved requested amount: <strong>{approved.reduce((sum, app) => sum + app.requestedAmount, 0).toLocaleString()}</strong>. These amounts are not payment or disbursement records. Draft dates represent creation, other dates represent submission.</p>
    {loading ? <p role="status">Loading report…</p> : rows.length === 0 ? <p className="py-5 text-gray-700">No matching applications.</p> : <div className="overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="bg-gray-50 text-gray-900"><tr>{['Application', 'Employee', 'Program', 'Amount requested', 'Status', 'Date'].map(label => <th key={label} className="p-3">{label}</th>)}</tr></thead><tbody>{rows.map(app => <tr key={app.id} className="border-t border-gray-200"><td className="p-3"><Link className="font-semibold text-red-700 underline" href={`/employee/applications/${app.id}`}>#{app.id}</Link></td><td className="p-3">{app.userName}</td><td className="p-3">{app.programName}</td><td className="p-3">{app.requestedAmount.toLocaleString()}</td><td className="p-3">{app.statusName}</td><td className="p-3">{new Date(app.submittedAt).toLocaleString()}</td></tr>)}</tbody></table></div>}
  </Card></AppShell>;
}
