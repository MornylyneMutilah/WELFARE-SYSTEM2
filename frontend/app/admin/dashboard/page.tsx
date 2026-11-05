'use client';

import { useLiveData } from '@/lib/hooks/useLiveData';
import DashboardMetrics from '@/components/ui/DashboardMetrics';
import Alert from '@/components/ui/Alert';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import AnomalySummaryCard from '@/components/anomaly/AnomalySummaryCard';
import AnomalyList from '@/components/anomaly/AnomalyList';
import { dashboardsApi } from '@/lib/api/dashboards';

export default function AdminDashboard() {
  const { data: stats, error, refresh } = useLiveData(dashboardsApi.admin);

  return (
    <AppShell title="Administrator dashboard" requiredRole="Administrator" description="Platform administration and oversight.">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-gray-600">All pending stages include HR, committee, and management. Updates within 10 seconds.</p><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button></div>
      {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}
      <DashboardMetrics items={[
        { label: 'Users', value: stats?.totalUsers }, { label: 'Applications', value: stats?.totalApplications },
        { label: 'Pending across all stages', value: stats?.pendingApplications }, { label: 'Active programs', value: stats?.activePrograms },
        { label: 'Flagged applications', value: stats?.flaggedApplications }, { label: 'Approved amount', value: stats?.totalExpenditure },
      ]} />
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <AnomalySummaryCard />
        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">Quick actions</h2>
            <p className="text-sm text-gray-600">Common administrative tasks</p>
          </div>
          <div className="space-y-3">
            <Link href="/admin/programs" className="block"><Button variant="primary" className="w-full">Create or manage programs</Button></Link>
            <Link href="/admin/users" className="block">
              <Button variant="outlined" className="w-full">
                Manage users
              </Button>
            </Link>
            <Link href="/admin/role-requests" className="block">
              <Button variant="outlined" className="w-full">
                Review role requests
              </Button>
            </Link>
          </div>
        </Card>

        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">System status</h2>
            <p className="text-sm text-gray-600">Platform health indicators</p>
          </div>
          <div className="space-y-3">
            <div className="flex items-center justify-between rounded-lg bg-green-50 p-3">
              <span className="text-sm font-medium text-gray-900">API Status</span>
              <span className="text-sm font-semibold text-green-700">Operational</span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-green-50 p-3">
              <span className="text-sm font-medium text-gray-900">Database</span>
              <span className="text-sm font-semibold text-green-700">Connected</span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-blue-50 p-3">
              <span className="text-sm font-medium text-gray-900">Authentication</span>
              <span className="text-sm font-semibold text-blue-700">Active</span>
            </div>
          </div>
        </Card>
      </div>
      <Card><h2 className="mb-3 text-lg font-bold text-gray-900">Unresolved anomalies</h2><AnomalyList status="Open,Investigating" limit={20} /></Card>
    </AppShell>
  );
}
