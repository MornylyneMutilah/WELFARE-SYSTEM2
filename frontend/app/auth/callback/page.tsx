'use client';

import { Suspense, useEffect, useState, useRef } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '@/lib/auth/AuthContext';
import { dashboardPathForRole } from '@/lib/auth/roles';
import Alert from '@/components/ui/Alert';

function CallbackContent() {
  const params = useSearchParams();
  const router = useRouter();
  const { completeExternalLogin, user } = useAuth();
  const [message, setMessage] = useState('Completing sign-in…');
  const [error, setError] = useState('');
  const [completed, setCompleted] = useState(false);
  const startedCode = useRef<string | null>(null);
  const code = params.get('code');
  const callbackError = params.get('error') || (!code ? 'This sign-in callback is missing an authorization code.' : '');

  useEffect(() => {
    if (callbackError || !code || startedCode.current === code) return;
    startedCode.current = code;
    completeExternalLogin(code)
      .then(() => {
        setCompleted(true);
        setMessage('Signed in. Redirecting…');
      })
      .catch((err: unknown) => {
        const axiosErr = err as { response?: { data?: { message?: string } } };
        setError(axiosErr.response?.data?.message || 'Social sign-in failed.');
        setMessage('');
      });
  }, [completeExternalLogin, code, callbackError]);

  useEffect(() => {
    if (completed && user) {
      router.replace(dashboardPathForRole(user.role));
    }
  }, [completed, user, router]);

  return (
    <div className="mx-auto max-w-lg px-4 py-16">
      {callbackError || error ? <Alert tone="error">{callbackError || error}</Alert> : <Alert tone="info">{message}</Alert>}
      {(callbackError || error) && <Link href="/login" className="mt-5 inline-block font-semibold text-red-700 underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600">Return to email sign-in</Link>}
    </div>
  );
}

export default function AuthCallbackPage() {
  return (
    <Suspense fallback={<div className="p-10 text-gray-700">Loading…</div>}>
      <CallbackContent />
    </Suspense>
  );
}
