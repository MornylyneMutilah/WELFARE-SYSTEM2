'use client';

import { type Anomaly } from '@/lib/api/anomalies';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';

interface AnomalyCardProps {
  anomaly: Anomaly;
  onResolve?: (id: number) => void;
  onInvestigate?: (id: number) => void;
  onDismiss?: (id: number) => void;
}

const severityColors = {
  Low: 'bg-blue-50 text-blue-800 border-blue-200',
  Medium: 'bg-amber-50 text-amber-800 border-amber-200',
  High: 'bg-orange-50 text-orange-800 border-orange-200',
  Critical: 'bg-red-50 text-red-800 border-red-200',
};

const statusColors = {
  Open: 'bg-gray-100 text-gray-800',
  Investigating: 'bg-blue-100 text-blue-800',
  Resolved: 'bg-green-100 text-green-800',
  Dismissed: 'bg-gray-200 text-gray-600',
};

export default function AnomalyCard({ anomaly, onResolve, onInvestigate, onDismiss }: AnomalyCardProps) {
  return (
    <Card className={`border-l-4 ${severityColors[anomaly.severity as keyof typeof severityColors]?.split(' ')[2] || 'border-gray-400'}`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${severityColors[anomaly.severity as keyof typeof severityColors] || 'bg-gray-100 text-gray-800'}`}>
              {anomaly.severity}
            </span>
            <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${statusColors[anomaly.status as keyof typeof statusColors] || 'bg-gray-100 text-gray-800'}`}>
              {anomaly.status}
            </span>
            <span className="text-xs text-gray-500">
              {new Date(anomaly.detectedAt).toLocaleDateString()}
            </span>
          </div>
          <h4 className="text-sm font-semibold text-gray-900">{anomaly.anomalyType}</h4>
          <p className="mt-1 text-sm text-gray-600">{anomaly.description}</p>
          {anomaly.resolutionNotes && (
            <p className="mt-2 text-xs text-gray-500 italic">
              Resolution: {anomaly.resolutionNotes}
            </p>
          )}
        </div>
        {anomaly.status === 'Open' && (
          <div className="flex flex-col gap-2">
            {onInvestigate && (
              <Button variant="outlined" size="sm" onClick={() => onInvestigate(anomaly.id)}>
                Investigate
              </Button>
            )}
            {onDismiss && (
              <Button variant="outlined" size="sm" onClick={() => onDismiss(anomaly.id)}>
                Dismiss
              </Button>
            )}
          </div>
        )}
        {anomaly.status === 'Investigating' && onResolve && (
          <Button variant="primary" size="sm" onClick={() => onResolve(anomaly.id)}>
            Resolve
          </Button>
        )}
      </div>
    </Card>
  );
}
