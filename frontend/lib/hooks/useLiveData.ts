'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { useAuth } from '@/lib/auth/AuthContext';

// Refresh active views after writes, on tab focus, and every 10 seconds.
// The storage event reaches other tabs; polling also covers different browser profiles/devices.
export function useLiveData<T>(loader: () => Promise<T>) {
  const { user, isLoading: authLoading } = useAuth();
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const sequence = useRef(0);
  const active = useRef(false);
  const refresh = useCallback(() => {
    const request = ++sequence.current;
    return loader().then(value => {
      if (active.current && request === sequence.current) { setData(value); setError(''); }
      return value;
    }).catch(() => {
      if (active.current && request === sequence.current) setError('Could not refresh data. Use Refresh to try again.');
      return null;
    }).finally(() => {
      if (active.current && request === sequence.current) setLoading(false);
    });
  }, [loader]);

  useEffect(() => {
    if (authLoading || !user) return;
    active.current = true;
    const update = () => { if (document.visibilityState === 'visible') void refresh(); };
    const storage = (event: StorageEvent) => { if (event.key === 'welfare:last-change') update(); };
    void refresh();
    const timer = window.setInterval(update, 10000);
    window.addEventListener('focus', update);
    window.addEventListener('welfare:data-changed', update);
    window.addEventListener('storage', storage);
    document.addEventListener('visibilitychange', update);
    return () => {
      active.current = false;
      window.clearInterval(timer);
      window.removeEventListener('focus', update);
      window.removeEventListener('welfare:data-changed', update);
      window.removeEventListener('storage', storage);
      document.removeEventListener('visibilitychange', update);
    };
  }, [authLoading, user, refresh]);
  return { data, error, loading, refresh };
}
