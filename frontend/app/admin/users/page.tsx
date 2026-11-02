'use client';

import { useCallback, useState } from 'react';
import { usersApi, type UserResponse, type UserOptions, type UpdateUserRequest } from '@/lib/api/users';
import { ROLE_LABELS, isAppRole } from '@/lib/auth/roles';
import { useAuth } from '@/lib/auth/AuthContext';
import { useLiveData } from '@/lib/hooks/useLiveData';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import { SelectField, TextField, TextAreaField } from '@/components/ui/Field';

const roleLabel = (role: string) => isAppRole(role) ? ROLE_LABELS[role] : role;
const errorMessage = (error: unknown) => {
  const payload = (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } }).response?.data;
  return payload?.message || (payload?.errors ? Object.values(payload.errors).flat().join(' ') : 'The change could not be saved. Your entries have been kept.');
};

function UserEditor({ user, options, self, onSaved, onCancel }: { user?: UserResponse; options: UserOptions; self: boolean; onSaved: (message: string) => void; onCancel: () => void }) {
  const [form, setForm] = useState({ firstName: user?.firstName || '', lastName: user?.lastName || '', email: user?.email || '',
    employeeId: user?.employeeId || '', departmentId: user?.departmentId || options.departments[0]?.id || 0,
    roleId: user?.roleId || options.roles.find(r => r.name === 'Employee')?.id || 0, hireDate: user?.hireDate.slice(0, 10) || '',
    isActive: user?.isActive ?? true, reason: '', password: '' });
  const [confirmed, setConfirmed] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const update = (patch: Partial<typeof form>) => { setForm(value => ({ ...value, ...patch })); setConfirmed(false); };
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (busy || !confirmed) return;
    setBusy(true); setError('');
    try {
      const data: UpdateUserRequest = { firstName: form.firstName.trim(), lastName: form.lastName.trim(), email: form.email.trim(),
        employeeId: form.employeeId.trim(), departmentId: form.departmentId, roleId: form.roleId,
        hireDate: `${form.hireDate}T00:00:00Z`, isActive: form.isActive, reason: form.reason.trim(), expectedUpdatedAt: user?.updatedAt };
      if (user) await usersApi.updateUser(user.id, data);
      else await usersApi.createUser({ email: data.email, firstName: data.firstName, lastName: data.lastName, employeeId: data.employeeId,
        departmentId: data.departmentId, roleId: data.roleId, hireDate: data.hireDate, reason: data.reason, password: form.password });
      onSaved(user ? 'User updated. Changes have been recorded in the audit history.' : 'User created as an active account. Keep the initial password confidential.');
    } catch (err) { setError(errorMessage(err)); }
    finally { setBusy(false); }
  };
  return <Card className="mb-6"><form onSubmit={save} className="space-y-4" aria-label={user ? 'Edit user' : 'Create user'}>
    <h2 className="text-lg font-bold text-gray-900">{user ? `Edit ${user.firstName} ${user.lastName}` : 'Add user'}</h2>
    {error && <Alert tone="error">{error}</Alert>}
    <fieldset disabled={busy} className="grid gap-4 sm:grid-cols-2">
      <TextField id="admin-first-name" label="First name" required maxLength={100} value={form.firstName} onChange={e => update({ firstName: e.target.value })} />
      <TextField id="admin-last-name" label="Last name" required maxLength={100} value={form.lastName} onChange={e => update({ lastName: e.target.value })} />
      <TextField id="admin-email" label="Email address" required type="email" value={form.email} onChange={e => update({ email: e.target.value })} />
      <TextField id="admin-employee-id" label="Employee ID" required maxLength={50} value={form.employeeId} onChange={e => update({ employeeId: e.target.value })} />
      <SelectField id="admin-department" label="Department" required value={form.departmentId} onChange={e => update({ departmentId: Number(e.target.value) })}>
        {options.departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
      </SelectField>
      <SelectField id="admin-role" label="Account role" required disabled={self} value={form.roleId} onChange={e => update({ roleId: Number(e.target.value) })}>
        {options.roles.map(r => <option key={r.id} value={r.id}>{roleLabel(r.name)}</option>)}
      </SelectField>
      <TextField id="admin-hire-date" label="Verified hire date" required type="date" min="1900-01-01" max={new Date().toISOString().slice(0, 10)} value={form.hireDate} onChange={e => update({ hireDate: e.target.value })} hint="Use the employee's verified employment record. This date determines service-period eligibility." />
      {user && <SelectField id="admin-active" label="Account status" disabled={self} value={form.isActive ? 'active' : 'suspended'} onChange={e => update({ isActive: e.target.value === 'active' })}>
        <option value="active">Active</option><option value="suspended">Suspended</option>
      </SelectField>}
      {!user && <div className="space-y-2"><TextField id="admin-initial-password" label="Initial password" type={showPassword ? 'text' : 'password'} autoComplete="new-password" required minLength={8} value={form.password} onChange={e => update({ password: e.target.value })} hint="At least 8 characters. Share privately with the account holder." /><label className="flex items-center gap-2 text-sm text-gray-700"><input type="checkbox" checked={showPassword} onChange={e => setShowPassword(e.target.checked)} />Show password</label></div>}
    </fieldset>
    <TextAreaField id="admin-change-reason" label="Reason for this change" required minLength={3} maxLength={1000} disabled={busy} value={form.reason} onChange={e => update({ reason: e.target.value })} />
    <p className="text-sm text-gray-600">Role, department, or status changes invalidate the existing sessions for this user. Suspension preserves their applications and history. A hire-date correction is used for future eligibility assessments.</p>
    {self && <p className="text-sm text-amber-800">You cannot change your own role or suspend yourself. Ask another administrator. Changing your own department will require signing in again.</p>}
    <label className="flex items-start gap-2 text-sm text-gray-800"><input type="checkbox" required checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)} />I have verified these details and authorize this account change.</label>
    <div className="flex gap-3"><Button type="submit" loading={busy} disabled={!confirmed || form.reason.trim().length < 3}>Save user</Button><Button variant="ghost" disabled={busy} onClick={onCancel}>Cancel</Button></div>
  </form></Card>;
}

function StatusEditor({ user, onSaved, onCancel }: { user: UserResponse; onSaved: (message: string) => void; onCancel: () => void }) {
  const [reason, setReason] = useState('');
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const verb = user.isActive ? 'Suspend' : 'Reactivate';
  const submit = async (event: React.FormEvent) => {
    event.preventDefault(); if (!confirmed || busy) return;
    setBusy(true); setError('');
    try {
      await usersApi.setStatus(user.id, { isActive: !user.isActive, reason: reason.trim(), expectedUpdatedAt: user.updatedAt });
      onSaved(user.isActive ? 'Account suspended. Existing sessions are now invalid; records have been preserved.' : 'Account reactivated. The user must sign in again.');
    } catch (err) { setError(errorMessage(err)); } finally { setBusy(false); }
  };
  return <Card className="mb-6"><form onSubmit={submit} className="space-y-4" aria-label={`${verb} user`}>
    <h2 className="text-lg font-bold text-gray-900">{verb} {user.firstName} {user.lastName}?</h2>
    <p className="text-sm text-gray-700">{user.email}. {user.isActive ? 'This blocks access on the next API request and prevents new sign-ins. Applications and review history are kept.' : 'This restores access with the existing assigned role. Previous invalidated sessions will not be restored.'}</p>
    {error && <Alert tone="error">{error}</Alert>}
    <TextAreaField id="status-reason" label="Reason" required minLength={3} maxLength={1000} value={reason} disabled={busy} onChange={e => setReason(e.target.value)} />
    <label className="flex items-center gap-2 text-sm text-gray-800"><input type="checkbox" required checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)} />I confirm this account status change.</label>
    <div className="flex gap-3"><Button type="submit" variant={user.isActive ? 'destructive' : 'primary'} loading={busy} disabled={!confirmed || reason.trim().length < 3}>{verb} account</Button><Button variant="ghost" disabled={busy} onClick={onCancel}>Cancel</Button></div>
  </form></Card>;
}

function UserHistory({ user, onClose }: { user: UserResponse; onClose: () => void }) {
  const loader = useCallback(() => usersApi.getAudit(user.id), [user.id]);
  const { data, error, loading } = useLiveData(loader);
  return <Card className="mb-6"><div className="mb-3 flex items-center justify-between"><h2 className="font-bold text-gray-900">Account history: {user.firstName} {user.lastName}</h2><Button variant="ghost" onClick={onClose}>Close</Button></div>
    {error && <Alert tone="error">{error}</Alert>}
    {loading ? <p role="status">Loading account history…</p> : !data?.length ? <p className="text-sm text-gray-600">No administrative changes recorded yet. Older changes may predate account auditing.</p> : <ul className="divide-y divide-gray-200">{data.map(entry => {
      let detail: { reason?: string; before?: Record<string, unknown>; after?: Record<string, unknown> } = {};
      try { detail = JSON.parse(entry.details); } catch { /* Historical records may not contain structured data. */ }
      return <li key={entry.id} className="py-3 text-sm"><p className="font-semibold text-gray-900">{entry.action.replace(/([A-Z])/g, ' $1').trim()} · {entry.actorName}</p><p className="text-gray-600">{new Date(entry.timestamp).toLocaleString()}</p><p className="mt-1 text-gray-800">{detail.reason || 'No reason recorded'}</p>
        {detail.before && detail.after && <ul className="mt-2 space-y-1 text-xs text-gray-600">{Object.keys(detail.after).filter(key => detail.before?.[key] !== detail.after?.[key]).map(key => <li key={key}>{key}: {String(detail.before?.[key] ?? '—')} → {String(detail.after?.[key] ?? '—')}</li>)}</ul>}
      </li>;
    })}</ul>}
  </Card>;
}

type Panel = { mode: 'edit' | 'status' | 'history'; user: UserResponse } | { mode: 'create' } | null;

export default function AdminUsersPage() {
  const { user: actor } = useAuth();
  const usersState = useLiveData(usersApi.getAllUsers);
  const optionsState = useLiveData(usersApi.getOptions);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');
  const [role, setRole] = useState('all');
  const [panel, setPanel] = useState<Panel>(null);
  const [success, setSuccess] = useState('');
  const users = usersState.data || [];
  const options = optionsState.data;
  const shown = users.filter(u => (status === 'all' || u.isActive === (status === 'active')) && (role === 'all' || u.roleName === role) &&
    `${u.firstName} ${u.lastName} ${u.email} ${u.employeeId} ${u.departmentName}`.toLowerCase().includes(search.trim().toLowerCase()));
  const saved = (message: string) => { setPanel(null); setSuccess(message); void usersState.refresh(); };
  return <AppShell title="User management" requiredRole="Administrator" description="Manage account access, employment details, departments, and roles without deleting employee records.">
    <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-gray-600">{users.length} users · {users.filter(u => u.isActive).length} active · {users.filter(u => !u.isActive).length} suspended</p><div className="flex gap-2"><Button variant="outlined" onClick={() => { void usersState.refresh(); void optionsState.refresh(); }}>Refresh</Button><Button disabled={!options} onClick={() => { setSuccess(''); setPanel({ mode: 'create' }); }}>Add user</Button></div></div>
    {(usersState.error || optionsState.error) && <div className="mb-4"><Alert tone="error">{usersState.error || optionsState.error}</Alert></div>}
    {success && <div className="mb-4"><Alert tone="success">{success}</Alert></div>}
    {panel && options && (panel.mode === 'edit' || panel.mode === 'create') && <UserEditor key={panel.mode === 'create' ? 'new' : `edit-${panel.user.id}`} user={panel.mode === 'edit' ? panel.user : undefined} self={panel.mode === 'edit' && panel.user.id === actor?.id} options={options} onSaved={saved} onCancel={() => setPanel(null)} />}
    {panel?.mode === 'status' && <StatusEditor key={`status-${panel.user.id}`} user={panel.user} onSaved={saved} onCancel={() => setPanel(null)} />}
    {panel?.mode === 'history' && <UserHistory key={`history-${panel.user.id}`} user={panel.user} onClose={() => setPanel(null)} />}
    <Card><div className="mb-4 grid gap-3 md:grid-cols-3">
      <TextField id="user-search" label="Search users" placeholder="Name, email, employee ID, department" value={search} onChange={e => setSearch(e.target.value)} />
      <SelectField id="user-status-filter" label="Account status" value={status} onChange={e => setStatus(e.target.value)}><option value="all">All statuses</option><option value="active">Active</option><option value="suspended">Suspended</option></SelectField>
      <SelectField id="user-role-filter" label="Role" value={role} onChange={e => setRole(e.target.value)}><option value="all">All roles</option>{options?.roles.map(r => <option key={r.id} value={r.name}>{roleLabel(r.name)}</option>)}</SelectField>
    </div>
    {usersState.loading ? <p role="status">Loading users…</p> : shown.length === 0 ? <p className="py-6 text-gray-600">No users match these filters.</p> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">User accounts and administration actions</caption>
      <thead><tr className="border-b border-gray-200 text-gray-600">{['User', 'Role / department', 'Hire date', 'Status', 'Actions'].map(label => <th scope="col" key={label} className="px-3 py-3 font-semibold">{label}</th>)}</tr></thead>
      <tbody className="divide-y divide-gray-100">{shown.map(u => <tr key={u.id}><td className="px-3 py-3"><p className="font-semibold text-gray-900">{u.firstName} {u.lastName}{u.id === actor?.id ? ' (you)' : ''}</p><p className="text-gray-600">{u.email}</p><p className="text-xs text-gray-500">{u.employeeId}</p></td><td className="px-3 py-3 text-gray-700">{roleLabel(u.roleName)}<br />{u.departmentName}</td><td className="px-3 py-3 whitespace-nowrap text-gray-700">{u.hireDate?.slice(0, 10) || 'Not recorded'}</td><td className="px-3 py-3"><span className={`rounded-full px-2 py-1 text-xs font-semibold ${u.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>{u.isActive ? 'Active' : 'Suspended'}</span></td><td className="px-3 py-3"><div className="flex flex-wrap gap-2"><Button size="sm" variant="outlined" disabled={!options} onClick={() => { setSuccess(''); setPanel({ mode: 'edit', user: u }); }}>Edit</Button><Button size="sm" variant={u.isActive ? 'destructive' : 'primary'} disabled={u.id === actor?.id} onClick={() => { setSuccess(''); setPanel({ mode: 'status', user: u }); }}>{u.isActive ? 'Suspend' : 'Reactivate'}</Button><Button size="sm" variant="ghost" onClick={() => setPanel({ mode: 'history', user: u })}>History</Button></div></td></tr>)}</tbody>
    </table></div>}
    <p className="mt-4 text-xs text-gray-500">Existing records are retained. Changes need a reason and are audited. This page refreshes automatically; an open edit keeps its original values so it cannot silently overwrite changes made by another administrator.</p>
    </Card>
  </AppShell>;
}
