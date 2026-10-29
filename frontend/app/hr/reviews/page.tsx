'use client';

import { useEffect, useState } from 'react';
import { applicationsApi, type ApplicationResponse } from '@/lib/api/applications';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';

export default function ReviewsPage() {
  const [items, setItems] = useState<ApplicationResponse[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    applicationsApi
      .getAllApplications()
      .then(setItems)
      .catch(() => setError('You do not have permission to list all applications, or the request failed.'));
  }, []);

  return (
    <AppShell title="Application reviews" description="Server policies still decide who can change status.">
      {error && <Alert tone="error">{error}</Alert>}
      {!error && (
        <Card>
          {items.length === 0 ? (
            <p className="text-gray-700">No applications to review.</p>
          ) : (
            <ul className="divide-y divide-gray-100">
              {items.map((app) => (
                <li key={app.id} className="py-3">
                  <p className="font-semibold text-gray-900">
                    #{app.id} · {app.userName} · {app.programName}
                  </p>
                  <p className="text-sm text-gray-600">
                    {app.statusName} · {app.requestedAmount.toLocaleString()}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}
    </AppShell>
  );
}
