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

export default function CommitteeDashboard() {
  const { data: stats, error, refresh } = useLiveData(dashboardsApi.committee);
  return <AppShell title="Committee dashboard" requiredRole="CommitteeMember" description="Review applications forwarded by HR and endorse them for management.">
    <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-gray-600">Updates automatically within 10 seconds and on tab focus.</p><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button></div>
    {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}
    <DashboardMetrics items={[
      { label: 'Submitted applications', value: stats?.totalApplications },
      { label: 'Awaiting committee review', value: stats?.pendingReview },
      { label: 'Endorsed by committee', value: stats?.approvedByCommittee },
      { label: 'Rejected by committee', value: stats?.rejectedByCommittee },
    ]} />
    <div className="mb-6 grid gap-6 lg:grid-cols-2">
      <Card><h2 className="text-lg font-bold text-gray-900">Committee overview</h2>
        <ul className="mt-3 space-y-3 text-sm text-gray-700">
          <li><strong>Awaiting committee review:</strong> HR has approved these applications. They need your decision.</li>
          <li><strong>Endorsed by committee:</strong> recorded committee approvals, including applications that management has since decided.</li>
          <li><strong>Rejected by committee:</strong> recorded committee rejections; they do not move to management.</li>
        </ul>
        <Link href="/hr/reviews" className="mt-4 inline-block font-semibold text-red-700">Review committee queue →</Link>
      </Card><AnomalySummaryCard />
    </div>
    <Card><h2 className="mb-2 text-lg font-bold text-gray-900">High-priority anomalies</h2><p className="mb-3 text-sm text-gray-600">Open or investigating high and critical flags. A flag requires investigation; it does not automatically reject an application.</p><AnomalyList severity="High,Critical" status="Open,Investigating" limit={5} /></Card>
  </AppShell>;
}
