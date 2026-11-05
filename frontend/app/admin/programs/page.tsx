'use client';

import { useState } from 'react';
import AppShell from '@/components/layout/AppShell';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import Alert from '@/components/ui/Alert';
import { TextField, TextAreaField, SelectField } from '@/components/ui/Field';
import { welfareProgramsApi, type WelfareProgramResponse } from '@/lib/api/welfarePrograms';
import { useLiveData } from '@/lib/hooks/useLiveData';

function documentLines(value: string) {
  try { const parsed: unknown = JSON.parse(value); return Array.isArray(parsed) ? parsed.filter(item => typeof item === 'string').join('\n') : value; }
  catch { return value; }
}

function ProgramEditor({ program, onSaved, onCancel }: { program?: WelfareProgramResponse; onSaved: (message: string) => void; onCancel: () => void }) {
  const [form, setForm] = useState({ name: program?.name || '', description: program?.description || '', category: program?.category || '',
    maxAmount: program ? String(program.maxAmount) : '', minServicePeriod: String(program?.minServicePeriod ?? 0),
    documents: documentLines(program?.requiredDocuments || '[]'), isActive: program?.isActive ?? true, reason: '' });
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const update = (patch: Partial<typeof form>) => { setForm(value => ({ ...value, ...patch })); setConfirmed(false); };
  const save = async (event: React.FormEvent) => {
    event.preventDefault(); if (busy || !confirmed || form.reason.trim().length < 3) return;
    setBusy(true); setError('');
    try {
      const docs = [...new Set(form.documents.split('\n').map(value => value.trim()).filter(Boolean))];
      if (docs.length > 100 || docs.some(value => value.length > 200)) throw new Error('Enter at most 100 document names, each at most 200 characters.');
      const payload = { name: form.name.trim(), description: form.description.trim(), category: form.category.trim(),
        maxAmount: Number(form.maxAmount), minServicePeriod: Number(form.minServicePeriod), requiredDocuments: JSON.stringify(docs), reason: form.reason.trim() };
      const result = program ? await welfareProgramsApi.updateProgram(program.id, { ...payload, isActive: form.isActive, expectedUpdatedAt: program.updatedAt })
        : await welfareProgramsApi.createProgram(payload);
      onSaved(`Program #${result.id} ${program ? 'updated' : 'created'}. ${result.isActive ? 'It is available in the employee program list.' : 'It is inactive; existing application records are retained.'}`);
    } catch (err: unknown) {
      const payload = (err as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } }).response?.data;
      setError(payload?.message || (payload?.errors ? Object.values(payload.errors).flat().join(' ') : err instanceof Error ? err.message : 'Could not save the program. Your entries have been kept.'));
    } finally { setBusy(false); }
  };
  return <Card className="mb-6"><form onSubmit={save} aria-label={program ? 'Edit welfare program' : 'Create welfare program'} className="space-y-4">
    <h2 className="text-lg font-bold text-gray-900">{program ? `Edit program #${program.id}` : 'Create welfare program'}</h2>
    {error && <Alert tone="error">{error}</Alert>}
    <fieldset disabled={busy} className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2"><TextField id="program-name" label="Program name" required maxLength={200} value={form.name} onChange={e => update({ name: e.target.value })} />
        <TextField id="program-category" label="Category" required maxLength={100} placeholder="For example Medical or Education" value={form.category} onChange={e => update({ category: e.target.value })} />
      </div>
      <TextAreaField id="program-description" label="Description and application instructions" required maxLength={1000} rows={4} value={form.description} onChange={e => update({ description: e.target.value })} />
      <div className="grid gap-4 sm:grid-cols-2"><TextField id="program-max-amount" label="Maximum allowable amount" type="number" required min="0.01" max="1000000" step="0.01" value={form.maxAmount} onChange={e => update({ maxAmount: e.target.value })} />
        <TextField id="program-service" label="Minimum service period in months" type="number" required min="0" max="120" step="1" value={form.minServicePeriod} onChange={e => update({ minServicePeriod: e.target.value })} />
      </div>
      <TextAreaField id="program-documents" label="Required supporting documents" hint="One document name per line. Leave empty if none are required. This defines instructions; it does not upload files." rows={4} value={form.documents} onChange={e => update({ documents: e.target.value })} />
      {program ? <SelectField id="program-status" label="Program status" value={form.isActive ? 'active' : 'inactive'} onChange={e => update({ isActive: e.target.value === 'active' })}><option value="active">Active</option><option value="inactive">Inactive</option></SelectField>
        : <p className="text-sm text-gray-600">New programs become active immediately after successful creation.</p>}
      <TextAreaField id="program-reason" label="Reason for creating or changing this program" required minLength={3} maxLength={1000} value={form.reason} onChange={e => update({ reason: e.target.value })} />
    </fieldset>
    <div className="rounded-lg bg-gray-50 p-4 text-sm text-gray-800"><strong>Review:</strong> {form.name || 'Unnamed program'} · {form.category || 'No category'} · Maximum {Number(form.maxAmount || 0).toLocaleString()} · Minimum {form.minServicePeriod} months · {form.isActive ? 'Active' : 'Inactive'}</div>
    <label className="flex items-start gap-2 text-sm text-gray-800"><input type="checkbox" required checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)} />I have checked the program details and authorize this change.</label>
    <div className="flex gap-3"><Button type="submit" loading={busy} disabled={!confirmed || form.reason.trim().length < 3}>{program ? 'Save program' : 'Create program'}</Button><Button variant="ghost" disabled={busy} onClick={onCancel}>Cancel</Button></div>
  </form></Card>;
}

export default function AdminProgramsPage() {
  const { data, error, loading, refresh } = useLiveData(welfareProgramsApi.getAllPrograms);
  const [selected, setSelected] = useState<WelfareProgramResponse | 'new' | null>(null);
  const [success, setSuccess] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');
  const programs = (data || []).filter(program => `${program.name} ${program.category}`.toLowerCase().includes(search.toLowerCase()) &&
    (status === 'all' || program.isActive === (status === 'active')));
  return <AppShell title="Welfare program management" requiredRole="Administrator" description="Create and maintain programs, amounts, service requirements, and document instructions.">
    <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-gray-600">{data?.length ?? 0} programs. Changes are audited and lists refresh automatically.</p><div className="flex gap-2"><Button variant="outlined" onClick={() => void refresh()}>Refresh</Button><Button onClick={() => { setSuccess(''); setSelected('new'); }}>New program</Button></div></div>
    {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}{success && <div className="mb-4"><Alert tone="success">{success}</Alert></div>}
    {selected && <ProgramEditor key={selected === 'new' ? 'new' : `${selected.id}-${selected.isActive}`} program={selected === 'new' ? undefined : selected} onSaved={message => { setSuccess(message); setSelected(null); void refresh(); }} onCancel={() => setSelected(null)} />}
    <Card><div className="mb-4 grid gap-3 sm:grid-cols-2"><TextField id="program-search" label="Search programs" value={search} onChange={e => setSearch(e.target.value)} />
      <SelectField id="program-filter" label="Status" value={status} onChange={e => setStatus(e.target.value)}><option value="all">All programs</option><option value="active">Active</option><option value="inactive">Inactive</option></SelectField></div>
      {loading ? <p role="status">Loading programs…</p> : programs.length === 0 ? <p className="py-5 text-gray-600">No matching programs. Use New program to create one.</p> : <ul className="divide-y divide-gray-200">{programs.map(program => <li key={program.id} className="flex flex-wrap items-start justify-between gap-3 py-4"><div className="max-w-2xl"><h2 className="font-semibold text-gray-900">{program.name} <span className={`ml-2 rounded-full px-2 py-1 text-xs ${program.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-700'}`}>{program.isActive ? 'Active' : 'Inactive'}</span></h2><p className="mt-2 whitespace-pre-wrap text-sm text-gray-700">{program.description}</p><p className="mt-2 text-sm text-gray-600">{program.category} · Maximum {program.maxAmount.toLocaleString()} · Minimum {program.minServicePeriod} months</p></div><div className="flex gap-2"><Button variant="outlined" size="sm" onClick={() => setSelected(program)}>Edit</Button><Button variant={program.isActive ? 'destructive' : 'primary'} size="sm" onClick={() => setSelected({ ...program, isActive: !program.isActive })}>{program.isActive ? 'Deactivate' : 'Reactivate'}</Button></div></li>)}</ul>}
      <p className="mt-4 text-xs text-gray-500">Deactivation hides a program from active listings without deleting its application history. Role access is enforced by the API.</p>
    </Card>
  </AppShell>;
}
