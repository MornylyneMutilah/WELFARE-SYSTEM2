'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { welfareProgramsApi, type WelfareProgramResponse } from '@/lib/api/welfarePrograms';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Card from '@/components/ui/Card';

export default function ProgramsPage() {
  const [programs, setPrograms] = useState<WelfareProgramResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    welfareProgramsApi
      .getActivePrograms()
      .then(setPrograms)
      .catch(() => setError('Failed to load welfare programs'))
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <AppShell title="Welfare programs" description="Browse active programs and start an application.">
      {isLoading ? (
        <p className="text-gray-700">Loading programs…</p>
      ) : error ? (
        <Alert tone="error">{error}</Alert>
      ) : programs.length === 0 ? (
        <Card>No active welfare programs are available.</Card>
      ) : (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          {programs.map((program) => (
            <Card key={program.id}>
              <p className="text-xs font-semibold uppercase tracking-wide text-red-700">{program.category}</p>
              <h3 className="mt-2 text-lg font-bold text-gray-900">{program.name}</h3>
              <p className="mt-2 text-sm text-gray-700">{program.description}</p>
              <p className="mt-3 text-sm text-gray-800">Max amount: {program.maxAmount.toLocaleString()}</p>
              <p className="text-sm text-gray-800">Min service: {program.minServicePeriod} months</p>
              <Link
                href={`/employee/applications/new?programId=${program.id}`}
                className="mt-4 inline-flex rounded-lg bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white"
              >
                Apply
              </Link>
            </Card>
          ))}
        </div>
      )}
    </AppShell>
  );
}
