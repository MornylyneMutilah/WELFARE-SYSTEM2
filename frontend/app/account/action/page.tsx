'use client';
import { Suspense, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import { TextField } from '@/components/ui/Field';
import { accountApi } from '@/lib/api/operations';
import { messageFor } from '@/lib/api/records';
function Action() {
  const params = useSearchParams(); const token = params.get('token') || ''; const purpose = params.get('purpose') || '';
  const [password, setPassword] = useState(''); const [repeat, setRepeat] = useState(''); const [confirmed, setConfirmed] = useState(false); const [message, setMessage] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  return <main className="mx-auto max-w-lg px-4 py-16"><Card><h1 className="text-2xl font-bold text-gray-900">{purpose === 'verify' ? 'Verify your email' : purpose === 'invite' ? 'Set up your account' : 'Reset your password'}</h1>{error && <Alert tone="error">{error}</Alert>}{message ? <Alert tone="success">{message}</Alert> : <form className="mt-5 space-y-4" onSubmit={async event => { event.preventDefault(); if (busy || !confirmed) return; if (purpose !== 'verify' && password !== repeat) { setError('Passwords must match.'); return; } setBusy(true); setError(''); try { const result = await accountApi.complete(token, purpose, password); setPassword(''); setRepeat(''); setMessage(result.message); } catch (err) { setError(messageFor(err, 'This account action could not be completed.')); } finally { setBusy(false); } }}>
    {purpose !== 'verify' && <><TextField id="action-password" label="New password" type="password" required minLength={12} maxLength={128} autoComplete="new-password" hint="12–128 characters with letters and numbers." value={password} onChange={e => setPassword(e.target.value)} /><TextField id="action-repeat" label="Confirm new password" type="password" required autoComplete="new-password" value={repeat} onChange={e => setRepeat(e.target.value)} /></>}
    <label className="flex gap-2 text-sm text-gray-800"><input type="checkbox" checked={confirmed} onChange={e => setConfirmed(e.target.checked)} required />I requested this action for my account.</label><Button type="submit" disabled={!token || !confirmed || !['reset', 'verify', 'invite'].includes(purpose)} loading={busy}>Confirm account action</Button></form>}<Link href="/login" className="mt-5 inline-block font-semibold text-red-700 underline">Sign in</Link></Card></main>;
}
export default function ActionPage() { return <Suspense fallback={<p role="status">Loading account action…</p>}><Action /></Suspense>; }
