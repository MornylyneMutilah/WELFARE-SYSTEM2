'use client';

import { Suspense, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { welfareProgramsApi, type WelfareProgramResponse } from '@/lib/api/welfarePrograms';
import { applicationsApi } from '@/lib/api/applications';
import AppShell from '@/components/layout/AppShell';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import Card from '@/components/ui/Card';
import { SelectField, TextAreaField, TextField } from '@/components/ui/Field';

function NewApplicationContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [programs, setPrograms] = useState<WelfareProgramResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [step, setStep] = useState<'form' | 'review'>('form');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const initialProgramId = searchParams.get('programId') ? parseInt(searchParams.get('programId')!) : 0;
  const [formData, setFormData] = useState({
    welfareProgramId: initialProgramId,
    requestedAmount: 0,
    reason: '',
  });

  useEffect(() => {
    welfareProgramsApi
      .getActivePrograms()
      .then(setPrograms)
      .catch(() => setError('Failed to load welfare programs'))
      .finally(() => setIsLoading(false));
  }, []);

  const selectedProgram = programs.find((p) => p.id === Number(formData.welfareProgramId));

  const validate = () => {
    const next: Record<string, string> = {};
    if (!formData.welfareProgramId) next.welfareProgramId = 'Select a welfare program.';
    if (!formData.requestedAmount || formData.requestedAmount <= 0) {
      next.requestedAmount = 'Enter a requested amount greater than zero.';
    } else if (selectedProgram && formData.requestedAmount > selectedProgram.maxAmount) {
      next.requestedAmount = `Amount cannot exceed ${selectedProgram.maxAmount.toLocaleString()}.`;
    }
    if (!formData.reason.trim()) next.reason = 'Explain why you need this support.';
    setFieldErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) {
      setStep('form');
      return;
    }
    setError('');
    setSuccess('');
    setIsSubmitting(true);
    try {
      const application = await applicationsApi.createApplication({
        welfareProgramId: Number(formData.welfareProgramId),
        requestedAmount: formData.requestedAmount,
        reason: formData.reason.trim(),
      });
      setSuccess(
        `Application ${application.id} was created as a draft. Next, submit it from My Applications when your documents are ready.`
      );
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setError(axiosErr.response?.data?.message || 'Failed to create application');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AppShell title="New welfare application" description="Apply for an active program. Eligibility is enforced on the server.">
      {success && (
        <div className="mb-6 space-y-3">
          <Alert tone="success">{success}</Alert>
          <Button variant="primary" onClick={() => router.push('/employee/applications')}>
            View my applications
          </Button>
        </div>
      )}
      {error && (
        <div className="mb-6">
          <Alert tone="error">{error}</Alert>
        </div>
      )}

      {isLoading ? (
        <Card>
          <p className="text-center text-gray-700">Loading programs…</p>
        </Card>
      ) : !success && step === 'form' ? (
        <Card className="space-y-6">
          <div>
            <h3 className="text-lg font-bold text-gray-900">Select welfare program</h3>
            <p className="text-sm text-gray-600">Choose the program you wish to apply for</p>
          </div>

          <SelectField
            id="welfareProgramId"
            label="Welfare program"
            required
            value={formData.welfareProgramId || ''}
            error={fieldErrors.welfareProgramId}
            onChange={(e) => setFormData({ ...formData, welfareProgramId: Number(e.target.value) })}
          >
            <option value="">Select a program</option>
            {programs.map((program) => (
              <option key={program.id} value={program.id}>
                {program.name} — max {program.maxAmount.toLocaleString()}
              </option>
            ))}
          </SelectField>

          {selectedProgram && (
            <div className="rounded-lg border-l-4 border-l-red-600 bg-red-50 p-5">
              <h4 className="text-lg font-bold text-gray-900">{selectedProgram.name}</h4>
              <p className="mt-2 text-sm text-gray-700">{selectedProgram.description}</p>
              <div className="mt-4 space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-gray-900">Eligibility:</span>
                  <span className="text-gray-700">Minimum service period {selectedProgram.minServicePeriod} months</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-gray-900">Maximum amount:</span>
                  <span className="text-gray-700">{selectedProgram.maxAmount.toLocaleString()}</span>
                </div>
                <div className="flex items-start gap-2">
                  <span className="font-semibold text-gray-900">Required documents:</span>
                  <span className="text-gray-700">
                    {(() => {
                      try {
                        return JSON.parse(selectedProgram.requiredDocuments || '[]').join(', ') || 'None specified';
                      } catch {
                        return selectedProgram.requiredDocuments || 'None specified';
                      }
                    })()}
                  </span>
                </div>
              </div>
              <p className="mt-4 text-xs text-gray-600">
                Final eligibility and authorization are checked by the API during submission.
              </p>
            </div>
          )}

          <div>
            <h3 className="text-lg font-bold text-gray-900">Application details</h3>
            <p className="text-sm text-gray-600">Provide the requested amount and reason for your application</p>
          </div>

          <TextField
            id="requestedAmount"
            label="Requested amount"
            type="number"
            required
            min="0.01"
            step="0.01"
            value={formData.requestedAmount || ''}
            error={fieldErrors.requestedAmount}
            onChange={(e) => setFormData({ ...formData, requestedAmount: parseFloat(e.target.value) || 0 })}
            placeholder="0.00"
            hint="Enter the amount you are requesting (must not exceed program maximum)"
          />
          <TextAreaField
            id="reason"
            label="Reason for application"
            required
            rows={5}
            maxLength={2000}
            value={formData.reason}
            error={fieldErrors.reason}
            onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
            placeholder="Describe your circumstances and how this program would help you."
            hint="Provide a clear explanation of why you need this support"
          />

          <div className="flex flex-col gap-3 sm:flex-row">
            <Button variant="outlined" onClick={() => router.back()} className="sm:flex-1">
              Cancel
            </Button>
            <Button
              variant="primary"
              className="sm:flex-1"
              onClick={() => {
                if (validate()) setStep('review');
              }}
            >
              Review application
            </Button>
          </div>
        </Card>
      ) : !success ? (
        <Card className="space-y-6">
          <div>
            <h3 className="text-lg font-bold text-gray-900">Review your application</h3>
            <p className="text-sm text-gray-600">Confirm all details before submitting</p>
          </div>

          <div className="rounded-lg border border-gray-200 bg-gray-50 p-5">
            <dl className="space-y-4">
              <div className="flex justify-between gap-4 border-b border-gray-200 pb-3">
                <dt className="text-sm font-medium text-gray-600">Program</dt>
                <dd className="text-sm font-semibold text-gray-900">{selectedProgram?.name}</dd>
              </div>
              <div className="flex justify-between gap-4 border-b border-gray-200 pb-3">
                <dt className="text-sm font-medium text-gray-600">Requested amount</dt>
                <dd className="text-sm font-semibold text-gray-900">{formData.requestedAmount.toLocaleString()}</dd>
              </div>
              <div>
                <dt className="mb-2 text-sm font-medium text-gray-600">Reason</dt>
                <dd className="rounded-lg bg-white p-3 text-sm text-gray-900">{formData.reason}</dd>
              </div>
            </dl>
          </div>

          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
            <p className="text-sm text-amber-900">
              <strong>Note:</strong> This application will be created as a draft. You will need to submit it with supporting documents from your applications list.
            </p>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row">
            <Button variant="outlined" onClick={() => setStep('form')} disabled={isSubmitting} className="sm:flex-1">
              Edit
            </Button>
            <Button
              variant="primary"
              loading={isSubmitting}
              disabled={isSubmitting}
              onClick={handleSubmit}
              className="sm:flex-1"
            >
              {isSubmitting ? 'Creating…' : 'Create application'}
            </Button>
          </div>
        </Card>
      ) : null}
    </AppShell>
  );
}

export default function NewApplicationPage() {
  return (
    <Suspense fallback={<div className="p-10 text-gray-700">Loading…</div>}>
      <NewApplicationContent />
    </Suspense>
  );
}
