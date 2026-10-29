'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/lib/auth/AuthContext';
import { usersApi } from '@/lib/api/users';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import Card from '@/components/ui/Card';
import { TextField } from '@/components/ui/Field';

export default function ProfilePage() {
  const { user, refreshUserData } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [tone, setTone] = useState<'success' | 'error'>('success');
  const [formData, setFormData] = useState({ firstName: '', lastName: '', email: '' });

  useEffect(() => {
    if (user) {
      setFormData({ firstName: user.firstName, lastName: user.lastName, email: user.email });
    }
  }, [user]);

  const handleSave = async () => {
    setIsLoading(true);
    setMessage('');
    try {
      await usersApi.updateCurrentUser({
        email: formData.email,
        firstName: formData.firstName,
        lastName: formData.lastName,
      });
      await refreshUserData();
      setTone('success');
      setMessage('Profile updated.');
      setIsEditing(false);
    } catch (error: unknown) {
      const axiosErr = error as { response?: { data?: { message?: string } } };
      setTone('error');
      setMessage(axiosErr.response?.data?.message || 'Failed to update profile');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AppShell title="Profile" description="Role and department can only be changed by an administrator.">
      {message && (
        <div className="mb-4">
          <Alert tone={tone}>{message}</Alert>
        </div>
      )}
      <Card>
        <div className="mb-6 flex justify-between">
          <h2 className="text-lg font-bold text-gray-900">Personal information</h2>
          {!isEditing && (
            <Button variant="outlined" onClick={() => setIsEditing(true)}>
              Edit
            </Button>
          )}
        </div>
        {isEditing ? (
          <div className="space-y-4">
            <TextField
              id="firstName"
              label="First name"
              value={formData.firstName}
              onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
            />
            <TextField
              id="lastName"
              label="Last name"
              value={formData.lastName}
              onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
            />
            <TextField
              id="email"
              label="Email"
              type="email"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            />
            <div className="flex gap-3">
              <Button variant="primary" loading={isLoading} onClick={handleSave}>
                Save
              </Button>
              <Button variant="ghost" onClick={() => setIsEditing(false)}>
                Cancel
              </Button>
            </div>
          </div>
        ) : (
          <dl className="grid gap-3 text-sm sm:grid-cols-2">
            <div>
              <dt className="text-gray-600">Name</dt>
              <dd className="font-medium text-gray-900">
                {user?.firstName} {user?.lastName}
              </dd>
            </div>
            <div>
              <dt className="text-gray-600">Email</dt>
              <dd className="font-medium text-gray-900">{user?.email}</dd>
            </div>
            <div>
              <dt className="text-gray-600">Employee ID</dt>
              <dd className="font-medium text-gray-900">{user?.employeeId}</dd>
            </div>
            <div>
              <dt className="text-gray-600">Department</dt>
              <dd className="font-medium text-gray-900">{user?.department}</dd>
            </div>
            <div>
              <dt className="text-gray-600">Role</dt>
              <dd className="font-medium text-gray-900">{user?.role}</dd>
            </div>
          </dl>
        )}
      </Card>
    </AppShell>
  );
}
