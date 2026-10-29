'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import AnomalyList from '@/components/anomaly/AnomalyList';
import { applicationsApi, type ApplicationResponse } from '@/lib/api/applications';
import { dashboardsApi } from '@/lib/api/dashboards';
import { useAuth } from '@/lib/auth/AuthContext';

export default function EmployeeDashboard() {
  const { user } = useAuth();
  const [applications, setApplications] = useState<ApplicationResponse[]>([]);
  const [stats, setStats] = useState<{ totalApplications?: number; pendingApplications?: number; approvedApplications?: number }>({});
  const [error, setError] = useState('');

  useEffect(() => {
    applicationsApi
      .getApplications()
      .then(setApplications)
      .catch(() => setError('Could not load your applications.'));
    dashboardsApi.employee().then(setStats).catch(() => undefined);
  }, []);

  const pendingCount = stats.pendingApplications ?? applications.filter(a => a.statusName === 'Pending').length;
  const approvedCount = stats.approvedApplications ?? applications.filter(a => a.statusName === 'Approved').length;

  return (
    <AppShell title="Employee dashboard" description="Track your welfare applications and open a new request.">
      {error && (
        <div className="mb-4">
          <Alert tone="error">{error}</Alert>
        </div>
      )}

      <div className="mb-6 grid gap-4 sm:grid-cols-3">
        <Card className="border-l-4 border-l-red-600">
          <p className="text-sm font-medium text-gray-600">Total Applications</p>
          <p className="mt-2 text-3xl font-bold text-gray-900">{stats.totalApplications ?? applications.length}</p>
          <p className="mt-1 text-xs text-gray-500">All time</p>
        </Card>
        <Card className="border-l-4 border-l-amber-500">
          <p className="text-sm font-medium text-gray-600">Pending Review</p>
          <p className="mt-2 text-3xl font-bold text-gray-900">{pendingCount}</p>
          <p className="mt-1 text-xs text-gray-500">Awaiting approval</p>
        </Card>
        <Card className="border-l-4 border-l-green-600">
          <p className="text-sm font-medium text-gray-600">Approved</p>
          <p className="mt-2 text-3xl font-bold text-gray-900">{approvedCount}</p>
          <p className="mt-1 text-xs text-gray-500">Successfully processed</p>
        </Card>
      </div>

      <Card className="mb-6">
        <div className="mb-4">
          <h2 className="text-lg font-bold text-gray-900">Application alerts</h2>
          <p className="text-sm text-gray-600">Anomalies detected in your applications</p>
        </div>
        <AnomalyList userId={user?.id} limit={3} />
      </Card>

      <Card>
        <div className="mb-4 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-lg font-bold text-gray-900">Recent applications</h2>
            <p className="text-sm text-gray-600">Track your welfare application status</p>
          </div>
          <Link href="/employee/applications/new">
            <Button variant="primary" className="w-full sm:w-auto">
              New application
            </Button>
          </Link>
        </div>

        {applications.length === 0 ? (
          <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-8 text-center">
            <p className="text-lg font-medium text-gray-900">No applications yet</p>
            <p className="mt-2 text-sm text-gray-600">Start by browsing available welfare programs and submit your first application.</p>
            <Link href="/employee/programs" className="mt-4 inline-block">
              <Button variant="outlined">View programs</Button>
            </Link>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-600">Program</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-600">Amount</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-600">Status</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-600">Reference</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {applications.slice(0, 8).map((app) => (
                  <tr key={app.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <p className="font-semibold text-gray-900">{app.programName}</p>
                      <p className="text-xs text-gray-500">{new Date(app.submittedAt).toLocaleDateString()}</p>
                    </td>
                    <td className="px-4 py-3 font-medium text-gray-900">{app.requestedAmount.toLocaleString()}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${
                        app.statusName === 'Approved' ? 'bg-green-100 text-green-800' :
                        app.statusName === 'Pending' ? 'bg-amber-100 text-amber-800' :
                        app.statusName === 'Rejected' ? 'bg-red-100 text-red-800' :
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {app.statusName}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span className="text-sm font-medium text-red-700">#{app.id}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </AppShell>
  );
}
