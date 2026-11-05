'use client';

import { useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { useLiveData } from '@/lib/hooks/useLiveData';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import { TextField } from '@/components/ui/Field';

const load = async () => ({ providers: await authApi.getExternalProviders(), links: await authApi.getExternalLinks() });

export default function ConnectedSignIns() {
  const { data, error, loading, refresh } = useLiveData(load);
  const [provider, setProvider] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState('');
  const [confirmed, setConfirmed] = useState(false);
  const connect = async (event: React.FormEvent) => {
    event.preventDefault(); if (busy || !password || !confirmed) return;
    setBusy(true); setFailure('');
    try {
      const result = await authApi.linkExternal(provider.toLowerCase(), password);
      setPassword('');
      window.location.assign(result.authorizationUrl);
    } catch (err: unknown) {
      const result = err as { response?: { data?: { message?: string } } };
      setFailure(result.response?.data?.message || 'Could not connect this provider. Please try again.');
      setBusy(false);
    }
  };
  return <Card className="mt-6"><h2 className="text-lg font-bold text-gray-900">Connected sign-in providers</h2>
    <p className="mt-2 text-sm text-gray-600">Connect a provider you own, then use it to sign in to this account. Your assigned role and employee details stay the same.</p>
    {(error || failure) && <div className="mt-3"><Alert tone="error">{failure || error}</Alert></div>}
    {loading ? <p className="mt-3" role="status">Loading sign-in options…</p> : <ul className="mt-4 divide-y divide-gray-200">{data?.providers.map(item => {
      const linked = data.links.some(link => link.provider.toLowerCase() === item.provider.toLowerCase());
      return <li key={item.provider} className="flex flex-wrap items-center justify-between gap-3 py-3"><div><p className="font-medium text-gray-900">{item.provider}</p><p className="text-sm text-gray-600">{linked ? 'Connected' : item.configured ? 'Not connected' : 'Awaiting administrator provider setup'}</p></div><Button variant="outlined" disabled={linked || !item.configured || busy} onClick={() => { setProvider(item.provider); setPassword(''); setConfirmed(false); setFailure(''); }}>Connect {item.provider}</Button></li>;
    })}</ul>}
    {provider && <form onSubmit={connect} className="mt-4 space-y-3 rounded-lg border border-gray-200 p-4" aria-label="Connect sign-in provider">
      <h3 className="font-semibold text-gray-900">Connect {provider}</h3>
      <TextField id="provider-password" label="Confirm your account password" type="password" autoComplete="current-password" required maxLength={1024} disabled={busy} value={password} onChange={e => setPassword(e.target.value)} />
      <label className="flex items-start gap-2 text-sm text-gray-800"><input type="checkbox" required checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)} />I want this provider identity to be able to sign in to my account.</label>
      <div className="flex gap-2"><Button type="submit" loading={busy} disabled={!confirmed || !password}>Continue to {provider}</Button><Button variant="ghost" disabled={busy} onClick={() => { setProvider(''); setPassword(''); }}>Cancel</Button></div>
    </form>}
    {error && <Button className="mt-3" variant="outlined" onClick={() => void refresh()}>Refresh providers</Button>}
  </Card>;
}
