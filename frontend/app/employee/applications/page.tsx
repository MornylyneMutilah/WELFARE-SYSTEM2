'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { applicationsApi, type ApplicationResponse } from '@/lib/api/applications';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

export default function ApplicationsPage() {
  const [applications, setApplications] = useState<ApplicationResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [busyId, setBusyId] = useState<number | null>(null);

  const load = async () => {
    try {
      setApplications(await applicationsApi.getApplications());
    } catch {
      setError('Failed to load applications');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const submit = async (id: number) => {
    setBusyId(id);
    try {
      await applicationsApi.submitApplication(id);
      await load();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setError(axiosErr.response?.data?.message || 'Submit failed.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <AppShell title="My applications" description="Only your own applications are listed. Status changes are enforced on the server.">
      {error && (
        <div className="mb-4">
          <Alert tone="error">{error}</Alert>
        </div>
      )}
      <div className="mb-4">
        <Link href="/employee/applications/new" className="font-semibold text-red-700">
          New application
        </Link>
      </div>
      {isLoading ? (
        <p className="text-gray-700">Loading…</p>
      ) : applications.length === 0 ? (
        <Card>
          <p className="text-gray-700">You have not created any applications yet.</p>
        </Card>
      ) : (
        <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-gray-50 text-gray-800">
              <tr>
                <th className="px-4 py-3">ID</th>
                <th className="px-4 py-3">Program</th>
                <th className="px-4 py-3">Amount</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Action</th>
              </tr>
            </thead>
            <tbody>
              {applications.map((app) => (
                <tr key={app.id} className="border-t border-gray-100">
                  <td className="px-4 py-3 font-medium text-gray-900">{app.id}</td>
                  <td className="px-4 py-3 text-gray-900">{app.programName}</td>
                  <td className="px-4 py-3 text-gray-900">{app.requestedAmount.toLocaleString()}</td>
                  <td className="px-4 py-3 text-gray-800">{app.statusName}</td>
                  <td className="px-4 py-3">
                    {app.status === 1 && (
                      <Button
                        variant="primary"
                        className="px-3 py-1 text-xs"
                        loading={busyId === app.id}
                        onClick={() => submit(app.id)}
                      >
                        Submit
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </AppShell>
  );
}
