'use client';

import { anomaliesApi } from '@/lib/api/anomalies';
import { useLiveData } from '@/lib/hooks/useLiveData';
import Card from '@/components/ui/Card';

interface AnomalySummaryCardProps {
  className?: string;
}

export default function AnomalySummaryCard({ className = '' }: AnomalySummaryCardProps) {
  const { data: summary, loading, error, refresh } = useLiveData(anomaliesApi.getSummary);

  if (loading) {
    return <Card className={className}><p className="text-sm text-gray-600">Loading anomaly summary…</p></Card>;
  }

  if (!summary) {
    return <Card className={className}><p className="text-sm text-gray-600">{error || 'No anomaly data available.'}</p><button type="button" className="mt-2 font-semibold text-red-700" onClick={() => void refresh()}>Refresh</button></Card>;
  }

  const criticalCount = summary.severityCounts['Critical'] || 0;
  const highCount = summary.severityCounts['High'] || 0;
  const openCount = summary.statusCounts['Open'] || 0;

  return (
    <Card className={`border-l-4 ${criticalCount > 0 ? 'border-red-600' : highCount > 0 ? 'border-orange-500' : 'border-green-600'} ${className}`}>
      <p className="text-sm font-medium text-gray-600">Anomaly Detection</p>
      <p className="mt-2 text-3xl font-bold text-gray-900">{summary.totalAnomalies}</p>
      <p className="mt-1 text-xs text-gray-500">Total detected</p>
      <p className="mt-1 text-xs text-gray-500">Includes historical flags. Open and investigating counts show unresolved work.</p>
      <div className="mt-3 space-y-1">
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-600">Critical</span>
          <span className="font-semibold text-red-700">{criticalCount}</span>
        </div>
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-600">High</span>
          <span className="font-semibold text-orange-700">{highCount}</span>
        </div>
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-600">Open</span>
          <span className="font-semibold text-blue-700">{openCount}</span>
        </div>
        <div className="flex items-center justify-between text-xs"><span className="text-gray-600">Investigating</span><span className="font-semibold text-blue-700">{summary.statusCounts.Investigating || 0}</span></div>
      </div>
    </Card>
  );
}
