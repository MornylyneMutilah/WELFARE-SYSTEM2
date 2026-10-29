'use client';

import { useEffect, useState } from 'react';
import { anomaliesApi, type Anomaly } from '@/lib/api/anomalies';
import AnomalyCard from './AnomalyCard';
import Alert from '@/components/ui/Alert';

interface AnomalyListProps {
  userId?: number;
  applicationId?: number;
  departmentId?: number;
  severity?: string;
  status?: string;
  limit?: number;
}

export default function AnomalyList({ userId, applicationId, departmentId, severity, status, limit }: AnomalyListProps) {
  const [anomalies, setAnomalies] = useState<Anomaly[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchAnomalies = async () => {
      setLoading(true);
      setError('');
      try {
        let data: Anomaly[] = [];
        if (userId) {
          data = await anomaliesApi.getByUserId(userId);
        } else if (applicationId) {
          data = await anomaliesApi.getByApplicationId(applicationId);
        } else if (departmentId) {
          data = await anomaliesApi.getByDepartmentId(departmentId);
        } else {
          data = await anomaliesApi.getAll({ severity, status });
        }

        if (limit) {
          data = data.slice(0, limit);
        }

        setAnomalies(data);
      } catch (err) {
        setError('Failed to load anomalies');
      } finally {
        setLoading(false);
      }
    };

    fetchAnomalies();
  }, [userId, applicationId, departmentId, severity, status, limit]);

  const handleInvestigate = async (id: number) => {
    try {
      await anomaliesApi.investigate(id);
      setAnomalies(anomalies.map(a => a.id === id ? { ...a, status: 'Investigating' } : a));
    } catch (err) {
      setError('Failed to mark as investigating');
    }
  };

  const handleResolve = async (id: number) => {
    try {
      await anomaliesApi.resolve(id, { resolvedBy: 0, notes: 'Resolved by user' });
      setAnomalies(anomalies.map(a => a.id === id ? { ...a, status: 'Resolved', resolvedAt: new Date().toISOString() } : a));
    } catch (err) {
      setError('Failed to resolve anomaly');
    }
  };

  const handleDismiss = async (id: number) => {
    try {
      await anomaliesApi.dismiss(id, { resolvedBy: 0, notes: 'Dismissed by user' });
      setAnomalies(anomalies.map(a => a.id === id ? { ...a, status: 'Dismissed', resolvedAt: new Date().toISOString() } : a));
    } catch (err) {
      setError('Failed to dismiss anomaly');
    }
  };

  if (loading) {
    return <p className="text-sm text-gray-600">Loading anomalies…</p>;
  }

  if (error) {
    return <Alert tone="error">{error}</Alert>;
  }

  if (anomalies.length === 0) {
    return <p className="text-sm text-gray-600">No anomalies detected.</p>;
  }

  return (
    <div className="space-y-3">
      {anomalies.map(anomaly => (
        <AnomalyCard
          key={anomaly.id}
          anomaly={anomaly}
          onInvestigate={handleInvestigate}
          onResolve={handleResolve}
          onDismiss={handleDismiss}
        />
      ))}
    </div>
  );
}
