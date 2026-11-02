'use client';

import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import AnomalySummaryCard from '@/components/anomaly/AnomalySummaryCard';
import AnomalyList from '@/components/anomaly/AnomalyList';
import { dashboardsApi } from '@/lib/api/dashboards';
import { useLiveData } from '@/lib/hooks/useLiveData';
import Alert from '@/components/ui/Alert';

export default function HRDashboard() {
  const { data: stats, error, refresh } = useLiveData(dashboardsApi.hr);

  const statCards = [
    { label: 'Total applications', value: stats?.totalApplications },
    { label: 'Pending review', value: stats?.pendingReview },
    { label: 'Approved by HR', value: stats?.approvedByHR },
    { label: 'Rejected by HR', value: stats?.rejectedByHR },
    { label: 'Flagged applications', value: stats?.flaggedApplications },
    { label: 'Unread notifications', value: stats?.unreadNotifications },
  ];

  const getCardColor = (index: number) => {
    const colors = ['border-red-600', 'border-blue-600', 'border-green-600', 'border-amber-600'];
    return colors[index % colors.length];
  };

  return (
    <AppShell title="HR dashboard" requiredRole="HROfficer" description="Review department welfare applications.">
      <div className="mb-4 flex items-center justify-between gap-3"><p className="text-sm text-gray-600">Updates automatically within 10 seconds and when you return to this tab.</p><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button></div>
      {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {statCards.map(({ label, value }, index) => (
          <Card key={label} className={`border-l-4 ${getCardColor(index)}`}>
            <p className="text-sm font-medium text-gray-600">{label}</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">{value?.toLocaleString() ?? '—'}</p>
            <p className="mt-1 text-xs text-gray-500">HR metric</p>
          </Card>
        ))}
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">Quick actions</h2>
            <p className="text-sm text-gray-600">HR review tasks</p>
          </div>
          <div className="space-y-3">
            <Link href="/hr/reviews" className="block">
              <Button variant="primary" className="w-full">
                Review applications
              </Button>
            </Link>
          </div>
        </Card>

        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">Department overview</h2>
            <p className="text-sm text-gray-600">HR welfare activity</p>
          </div>
          <div className="space-y-3">
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Applications pending review</span>
              <span className="text-sm font-semibold text-red-700">
                {stats?.pendingReview?.toLocaleString() ?? '—'}
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Approved by HR</span>
              <span className="text-sm font-semibold text-green-700">
                {stats?.approvedByHR?.toLocaleString() ?? '—'}
              </span>
            </div>
          </div>
        </Card>

        <AnomalySummaryCard />
      </div>
      <Card><h2 className="mb-3 text-lg font-bold text-gray-900">Unresolved department anomalies</h2><AnomalyList status="Open,Investigating" limit={10} /></Card>
    </AppShell>
  );
}
