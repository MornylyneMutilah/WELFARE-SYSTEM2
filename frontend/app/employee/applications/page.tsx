'use client';


import { useLiveData } from '@/lib/hooks/useLiveData';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { applicationsApi } from '@/lib/api/applications';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

export default function ApplicationsPage() {
  const { data, error: loadError, loading: isLoading } = useLiveData(applicationsApi.getApplications);
  const router = useRouter();
  const applications = data ?? [];
  return (
    <AppShell title="My applications" description="Only your own applications are listed. Status changes are enforced on the server.">
      {loadError && (
        <div className="mb-4">
          <Alert tone="error">{loadError}</Alert>
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
                  <td className="px-4 py-3 font-medium text-gray-900"><Link className="text-red-700 underline" href={`/employee/applications/${app.id}`}>#{app.id} · View timeline</Link></td>
                  <td className="px-4 py-3 text-gray-900">{app.programName}</td>
                  <td className="px-4 py-3 text-gray-900">{app.requestedAmount.toLocaleString()}</td>
                  <td className="px-4 py-3 text-gray-800">{app.statusName}</td>
                  <td className="px-4 py-3">
                    {app.status === 1 && (
                      <Button
                        variant="primary"
                        className="px-3 py-1 text-xs"
                        onClick={() => router.push(`/employee/applications/${app.id}`)}
                      >
                        Review and submit
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
