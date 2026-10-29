'use client';

import { useEffect, useState } from 'react';
import { usersApi, type UserResponse } from '@/lib/api/users';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';

export default function AdminUsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    usersApi
      .getAllUsers()
      .then(setUsers)
      .catch(() => setError('Unable to load users. Administrator access is required.'));
  }, []);

  return (
    <AppShell title="User management" requiredRole="Administrator">
      {error && <Alert tone="error">{error}</Alert>}
      <Card>
        <ul className="divide-y divide-gray-100">
          {users.map((u) => (
            <li key={u.id} className="py-3 text-sm">
              <p className="font-semibold text-gray-900">
                {u.firstName} {u.lastName}
              </p>
              <p className="text-gray-600">
                {u.email} · {u.roleName} · {u.departmentName}
              </p>
            </li>
          ))}
        </ul>
      </Card>
    </AppShell>
  );
}
