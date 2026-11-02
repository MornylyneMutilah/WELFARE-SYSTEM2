'use client';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import DashboardMetrics from '@/components/ui/DashboardMetrics';
import AnomalySummaryCard from '@/components/anomaly/AnomalySummaryCard';
import AnomalyList from '@/components/anomaly/AnomalyList';
import { dashboardsApi } from '@/lib/api/dashboards';
import { useLiveData } from '@/lib/hooks/useLiveData';

export default function ManagementDashboard() {
  const { data: stats, error, refresh } = useLiveData(dashboardsApi.management);
  return <AppShell title="Management dashboard" requiredRole="Management" description="Give final decisions on committee-endorsed applications.">
    <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-gray-600">Updates automatically within 10 seconds and on tab focus.</p><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button></div>
    {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}
    <DashboardMetrics items={[
      { label: 'Submitted applications', value: stats?.totalApplications },
      { label: 'Pending final approval', value: stats?.pendingApproval },
      { label: 'Finally approved', value: stats?.approvedByManagement },
      { label: 'Rejected by management', value: stats?.rejectedByManagement },
    ]} />
    <div className="mb-6 grid gap-6 lg:grid-cols-2"><Card><h2 className="text-lg font-bold text-gray-900">Management overview</h2>
      <p className="mt-2 text-sm text-gray-700">Pending final approval includes committee-endorsed applications and applications already marked pending management approval.</p>
      <dl className="mt-4 space-y-3 text-sm text-gray-700"><div><dt>Approved amount</dt><dd className="font-bold text-gray-900">{stats?.totalExpenditure.toLocaleString() ?? '—'}</dd></div><div><dt>Average processing time for final approvals</dt><dd className="font-bold text-gray-900">{stats?.averageProcessingDays.toFixed(1) ?? '—'} days</dd></div></dl>
      <p className="mt-2 text-xs text-gray-500">Approved amount totals approved requests; it is not a record of payments or disbursements.</p>
      <Link href="/hr/reviews" className="mt-4 inline-block font-semibold text-red-700">Review final approval queue →</Link>
    </Card><AnomalySummaryCard /></div>
    <Card><h2 className="mb-3 text-lg font-bold text-gray-900">Unresolved critical anomalies</h2><AnomalyList severity="Critical" status="Open,Investigating" limit={5} /></Card>
  </AppShell>;
}
