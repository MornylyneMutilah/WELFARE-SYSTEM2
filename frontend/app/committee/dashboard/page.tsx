'use client';

import { useEffect, useState } from 'react';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import AnomalySummaryCard from '@/components/anomaly/AnomalySummaryCard';
import AnomalyList from '@/components/anomaly/AnomalyList';
import { dashboardsApi } from '@/lib/api/dashboards';

export default function CommitteeDashboard() {
  const [stats, setStats] = useState<Record<string, number>>({});
  useEffect(() => {
    dashboardsApi.committee().then(setStats).catch(() => undefined);
  }, []);

  const statCards = Object.entries(stats)
    .filter(([, v]) => typeof v === 'number')
    .slice(0, 8);

  const getCardColor = (index: number) => {
    const colors = ['border-red-600', 'border-blue-600', 'border-green-600', 'border-amber-600'];
    return colors[index % colors.length];
  };

  return (
    <AppShell title="Committee dashboard" requiredRole="CommitteeMember" description="Welfare committee overview and final approvals.">
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {statCards.map(([key, value], index) => (
          <Card key={key} className={`border-l-4 ${getCardColor(index)}`}>
            <p className="text-sm font-medium text-gray-600 capitalize">{key.replace(/([A-Z])/g, ' $1')}</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">{value.toLocaleString()}</p>
            <p className="mt-1 text-xs text-gray-500">Committee metric</p>
          </Card>
        ))}
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-2">
        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">Committee overview</h2>
            <p className="text-sm text-gray-600">Welfare program governance</p>
          </div>
          <div className="space-y-3">
            <div className="flex items-center justify-between rounded-lg bg-red-50 p-3">
              <span className="text-sm font-medium text-gray-900">High-value approvals pending</span>
              <span className="text-sm font-semibold text-red-700">
                {stats.pendingHighValueApplications ?? 0}
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-blue-50 p-3">
              <span className="text-sm font-medium text-gray-900">Monthly budget utilization</span>
              <span className="text-sm font-semibold text-blue-700">
                {stats.budgetUtilizationPercent ?? 0}%
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-green-50 p-3">
              <span className="text-sm font-medium text-gray-900">Programs active</span>
              <span className="text-sm font-semibold text-green-700">
                {stats.activePrograms ?? 0}
              </span>
            </div>
          </div>
        </Card>

        <AnomalySummaryCard />
      </div>

      <Card>
        <div className="mb-4">
          <h2 className="text-lg font-bold text-gray-900">High-priority anomalies</h2>
          <p className="text-sm text-gray-600">Critical and high severity anomalies requiring immediate attention</p>
        </div>
        <AnomalyList severity="High" limit={5} />
      </Card>
    </AppShell>
  );
}
