'use client';
import { useState } from 'react';
import Link from 'next/link';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import { TextField } from '@/components/ui/Field';
import { accountApi } from '@/lib/api/operations';
import { messageFor } from '@/lib/api/records';
export default function ForgotPage() {
  const [email, setEmail] = useState(''); const [message, setMessage] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  return <main className="mx-auto max-w-lg px-4 py-16"><Card><h1 className="text-2xl font-bold text-gray-900">Recover your account</h1><p className="mt-2 text-gray-600">Request a private reset link. You will set a new password and sign in again.</p>{message && <Alert tone="success">{message}</Alert>}{error && <Alert tone="error">{error}</Alert>}<form className="mt-5 space-y-4" onSubmit={async event => { event.preventDefault(); if (busy) return; setBusy(true); setError(''); try { const result = await accountApi.forgot(email); setMessage(`${result.message} Delivery: ${result.deliveryMode}. ${result.deliveryMode === 'Development outbox' ? 'Local demonstration messages are saved in the API App_Data/MailOutbox folder; they are not sent to an inbox.' : ''}`); } catch (err) { setError(messageFor(err, 'Could not request an account email.')); } finally { setBusy(false); } }}><TextField id="recovery-email" label="Email address" type="email" required autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} /><Button type="submit" loading={busy}>Request reset link</Button></form><Link href="/login" className="mt-5 inline-block font-semibold text-red-700 underline">Return to sign in</Link></Card></main>;
}
