'use client';

import { useCallback, useState } from 'react';
import { useLiveData } from '@/lib/hooks/useLiveData';
import { anomaliesApi, type Anomaly } from '@/lib/api/anomalies';
import { useAuth } from '@/lib/auth/AuthContext';
import { PRIVILEGED_ROLES } from '@/lib/auth/roles';
import AnomalyCard from './AnomalyCard';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { TextAreaField } from '@/components/ui/Field';

interface AnomalyListProps {
  userId?: number;
  applicationId?: number;
  departmentId?: number;
  severity?: string;
  status?: string;
  limit?: number;
}

export default function AnomalyList({ userId, applicationId, departmentId, severity, status, limit }: AnomalyListProps) {
  const { user, isLoading: authLoading } = useAuth();
  const canManage = PRIVILEGED_ROLES.some(role => role === user?.role);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState<number | null>(null);
  const [decision, setDecision] = useState<{ id: number; action: 'resolve' | 'dismiss' } | null>(null);
  const [notes, setNotes] = useState('');
  const loadAnomalies = useCallback(async () => {
    let data: Anomaly[];
    if (userId) data = await anomaliesApi.getByUserId(userId);
    else if (applicationId) data = await anomaliesApi.getByApplicationId(applicationId);
    else if (departmentId) data = await anomaliesApi.getByDepartmentId(departmentId);
    else data = await anomaliesApi.getAll({ severity, status });
    if (severity) data = data.filter(a => severity.toLowerCase().split(',').includes(a.severity.toLowerCase()));
    if (status) data = data.filter(a => status.toLowerCase().split(',').includes(a.status.toLowerCase()));
    return limit ? data.slice(0, limit) : data;
  }, [userId, applicationId, departmentId, severity, status, limit]);
  const { data, error: loadError, loading, refresh } = useLiveData(loadAnomalies);
  const anomalies = data ?? [];
  const investigate = async (id: number) => {
    if (busy !== null) return;
    setBusy(id);
    try {
      await anomaliesApi.investigate(id);
      await refresh();
      setError('');
    } catch { setError('Failed to start investigation. Please try again.'); }
    finally { setBusy(null); }
  };

  const confirmDecision = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!decision || !notes.trim() || busy !== null) return;
    setBusy(decision.id);
    try {
      await anomaliesApi[decision.action](decision.id, { notes: notes.trim() });
      setDecision(null);
      setNotes('');
      void refresh();
    } catch { setError('The decision could not be saved. Your notes have been kept.'); }
    finally { setBusy(null); }
  };

  if (loading || authLoading) return <p role="status" className="text-sm text-gray-600">Loading anomalies…</p>;

  return (
    <div className="space-y-3">
      {(error || loadError) && <Alert tone="error">{error || loadError}</Alert>}
      {(error || loadError) && <Button variant="outlined" onClick={() => void refresh()}>Retry</Button>}
      {decision && (
        <form onSubmit={confirmDecision} className="space-y-3 rounded-lg border border-gray-200 bg-white p-4" aria-label="Confirm anomaly decision">
          <h3 className="font-semibold text-gray-900">{decision.action === 'resolve' ? 'Resolve' : 'Dismiss'} anomaly #{decision.id}</h3>
          <TextAreaField id="anomaly-notes" label="Investigation notes" required maxLength={2000} value={notes} onChange={e => setNotes(e.target.value)} />
          <div className="flex gap-2">
            <Button type="submit" loading={busy !== null} disabled={!notes.trim()}>Confirm decision</Button>
            <Button variant="ghost" disabled={busy !== null} onClick={() => setDecision(null)}>Cancel</Button>
          </div>
        </form>
      )}
      {!error && !loadError && anomalies.length === 0 && <p className="text-sm text-gray-600">No anomalies detected.</p>}
      {anomalies.map(anomaly => (
        <AnomalyCard key={anomaly.id} anomaly={anomaly} busy={busy !== null}
          onInvestigate={canManage ? investigate : undefined}
          onResolve={canManage ? id => { setNotes(''); setDecision({id, action: 'resolve'}); } : undefined}
          onDismiss={canManage ? id => { setNotes(''); setDecision({id, action: 'dismiss'}); } : undefined} />
      ))}
    </div>
  );
}
