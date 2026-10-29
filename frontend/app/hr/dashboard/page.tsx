'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import AnomalySummaryCard from '@/components/anomaly/AnomalySummaryCard';
import { dashboardsApi } from '@/lib/api/dashboards';

export default function HRDashboard() {
  const [stats, setStats] = useState<Record<string, number>>({});

  useEffect(() => {
    dashboardsApi.hr().then(setStats).catch(() => undefined);
  }, []);

  const statCards = Object.entries(stats)
    .filter(([, v]) => typeof v === 'number')
    .slice(0, 8);

  const getCardColor = (index: number) => {
    const colors = ['border-red-600', 'border-blue-600', 'border-green-600', 'border-amber-600'];
    return colors[index % colors.length];
  };

  return (
    <AppShell title="HR dashboard" requiredRole="HROfficer" description="Review department welfare applications.">
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {statCards.map(([key, value], index) => (
          <Card key={key} className={`border-l-4 ${getCardColor(index)}`}>
            <p className="text-sm font-medium text-gray-600 capitalize">{key.replace(/([A-Z])/g, ' $1')}</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">{value.toLocaleString()}</p>
            <p className="mt-1 text-xs text-gray-500">Department metric</p>
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
                {stats.pendingApplications ?? 0}
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Approved this month</span>
              <span className="text-sm font-semibold text-green-700">
                {stats.approvedApplications ?? 0}
              </span>
            </div>
          </div>
        </Card>

        <AnomalySummaryCard />
      </div>
    </AppShell>
  );
}
