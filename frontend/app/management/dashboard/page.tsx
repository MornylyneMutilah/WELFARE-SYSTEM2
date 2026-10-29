'use client';

import { useEffect, useState } from 'react';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import AnomalySummaryCard from '@/components/anomaly/AnomalySummaryCard';
import AnomalyList from '@/components/anomaly/AnomalyList';
import { dashboardsApi } from '@/lib/api/dashboards';

export default function ManagementDashboard() {
  const [stats, setStats] = useState<Record<string, number>>({});
  useEffect(() => {
    dashboardsApi.management().then(setStats).catch(() => undefined);
  }, []);

  const statCards = Object.entries(stats)
    .filter(([, v]) => typeof v === 'number')
    .slice(0, 8);

  const getCardColor = (index: number) => {
    const colors = ['border-red-600', 'border-blue-600', 'border-green-600', 'border-amber-600'];
    return colors[index % colors.length];
  };

  return (
    <AppShell title="Management dashboard" requiredRole="Management" description="Institutional welfare overview and executive reports.">
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {statCards.map(([key, value], index) => (
          <Card key={key} className={`border-l-4 ${getCardColor(index)}`}>
            <p className="text-sm font-medium text-gray-600 capitalize">{key.replace(/([A-Z])/g, ' $1')}</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">{value.toLocaleString()}</p>
            <p className="mt-1 text-xs text-gray-500">Executive metric</p>
          </Card>
        ))}
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">Financial overview</h2>
            <p className="text-sm text-gray-600">Welfare fund utilization</p>
          </div>
          <div className="space-y-3">
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Total disbursed</span>
              <span className="text-sm font-semibold text-gray-900">
                {stats.totalDisbursed?.toLocaleString() ?? 0}
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Budget remaining</span>
              <span className="text-sm font-semibold text-green-700">
                {stats.budgetRemaining?.toLocaleString() ?? 0}
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Pending approvals</span>
              <span className="text-sm font-semibold text-amber-700">
                {stats.pendingApprovals ?? 0}
              </span>
            </div>
          </div>
        </Card>

        <Card>
          <div className="mb-4">
            <h2 className="text-lg font-bold text-gray-900">Department performance</h2>
            <p className="text-sm text-gray-600">Welfare program engagement</p>
          </div>
          <div className="space-y-3">
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Active departments</span>
              <span className="text-sm font-semibold text-blue-700">
                {stats.activeDepartments ?? 0}
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Employee participation rate</span>
              <span className="text-sm font-semibold text-green-700">
                {stats.participationRate ?? 0}%
              </span>
            </div>
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span className="text-sm font-medium text-gray-900">Programs active</span>
              <span className="text-sm font-semibold text-red-700">
                {stats.activePrograms ?? 0}
              </span>
            </div>
          </div>
        </Card>

        <AnomalySummaryCard />
      </div>

      <Card>
        <div className="mb-4">
          <h2 className="text-lg font-bold text-gray-900">Critical anomalies</h2>
          <p className="text-sm text-gray-600">High-severity anomalies requiring executive attention</p>
        </div>
        <AnomalyList severity="Critical" limit={5} />
      </Card>
    </AppShell>
  );
}
