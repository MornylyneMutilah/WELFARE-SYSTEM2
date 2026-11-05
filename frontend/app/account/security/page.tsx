'use client';
import { useState } from 'react';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import { TextField } from '@/components/ui/Field';
import { accountApi } from '@/lib/api/operations';
import { messageFor } from '@/lib/api/records';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { useAuth } from '@/lib/auth/AuthContext';
export default function SecurityPage() {
  const { data, error } = useLiveData(accountApi.security); const { logout } = useAuth();
  const [current, setCurrent] = useState(''); const [next, setNext] = useState(''); const [repeat, setRepeat] = useState(''); const [failure, setFailure] = useState(''); const [notice, setNotice] = useState(''); const [busy, setBusy] = useState(false);
  return <AppShell title="Account security" description="Manage email verification, account recovery and your password."><Card className="max-w-2xl space-y-4">{(error || failure) && <Alert tone="error">{failure || error}</Alert>}{notice && <Alert tone="success">{notice}</Alert>}{data?.mustChangePassword && <Alert tone="warning">Replace your initial password before using the rest of the system.</Alert>}
    <p className="text-sm text-gray-700">Email: {data?.emailVerified ? 'Verified' : 'Not verified'} · Employment date: {data?.employmentVerifiedAt ? 'Verified by administrator' : 'Awaiting administrator verification'}</p><Button variant="outlined" disabled={busy || data?.emailVerified || data?.mustChangePassword} onClick={async () => { setBusy(true); setFailure(''); try { const result = await accountApi.verify(); setNotice(`${result.message} Delivery: ${result.deliveryMode}. ${result.deliveryMode === 'Development outbox' ? 'Messages are in the API App_Data/MailOutbox folder, not a live inbox.' : ''}`); } catch (err) { setFailure(messageFor(err, 'Could not request verification.')); } finally { setBusy(false); } }}>Request email verification</Button>
    <form className="space-y-4 border-t border-gray-200 pt-4" onSubmit={async event => { event.preventDefault(); if (busy) return; if (next !== repeat) { setFailure('New passwords must match.'); return; } setBusy(true); setFailure(''); try { await accountApi.change(current, next); setCurrent(''); setNext(''); setRepeat(''); logout(); } catch (err) { setFailure(messageFor(err, 'Could not change your password.')); } finally { setBusy(false); } }}><h2 className="text-lg font-bold text-gray-900">Change password</h2><TextField id="security-current" label="Current password" type="password" autoComplete="current-password" required value={current} onChange={e => setCurrent(e.target.value)} /><TextField id="security-new" label="New password" type="password" autoComplete="new-password" minLength={12} maxLength={128} hint="Use 12–128 characters including letters and numbers. Existing sessions will be revoked." required value={next} onChange={e => setNext(e.target.value)} /><TextField id="security-repeat" label="Confirm new password" type="password" required value={repeat} onChange={e => setRepeat(e.target.value)} /><Button type="submit" loading={busy}>Change password and sign out</Button></form>
  </Card></AppShell>;
}
