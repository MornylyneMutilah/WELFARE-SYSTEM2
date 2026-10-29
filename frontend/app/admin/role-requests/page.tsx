'use client';

import { useEffect, useState } from 'react';
import { roleRequestsApi, type RoleAccessRequest } from '@/lib/api/roleRequests';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import Card from '@/components/ui/Card';

export default function RoleRequestsPage() {
  const [items, setItems] = useState<RoleAccessRequest[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState<number | null>(null);

  const load = () =>
    roleRequestsApi
      .list()
      .then(setItems)
      .catch(() => setError('Unable to load role requests.'));

  useEffect(() => {
    load();
  }, []);

  const decide = async (id: number, approve: boolean) => {
    setBusy(id);
    try {
      await roleRequestsApi.decide(id, approve);
      await load();
    } catch {
      setError('Decision failed.');
    } finally {
      setBusy(null);
    }
  };

  return (
    <AppShell title="Role access requests" requiredRole="Administrator">
      {error && (
        <div className="mb-4">
          <Alert tone="error">{error}</Alert>
        </div>
      )}
      <Card>
        {items.length === 0 ? (
          <p className="text-gray-700">No pending or historical requests.</p>
        ) : (
          <ul className="divide-y divide-gray-100">
            {items.map((item) => (
              <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 py-3">
                <div>
                  <p className="font-semibold text-gray-900">
                    {item.userName} · {item.requestedRole}
                  </p>
                  <p className="text-sm text-gray-600">
                    {item.userEmail} · {item.status}
                  </p>
                </div>
                {item.status === 'Pending' && (
                  <div className="flex gap-2">
                    <Button variant="primary" loading={busy === item.id} onClick={() => decide(item.id, true)}>
                      Approve
                    </Button>
                    <Button variant="outlined" disabled={busy === item.id} onClick={() => decide(item.id, false)}>
                      Reject
                    </Button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </Card>
    </AppShell>
  );
}
