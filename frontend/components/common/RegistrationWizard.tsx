'use client';

import { useEffect, useMemo, useState } from 'react';
import { departmentsApi, type DepartmentResponse } from '@/lib/api/departments';
import {
  type FieldErrors,
  type RegistrationFormValues,
  reviewSummary,
  validateStep1,
  validateStep2,
  validateStep3,
} from '@/lib/auth/registrationValidation';
import { PRIVILEGED_ROLES, ROLE_LABELS, type AppRole } from '@/lib/auth/roles';
import Button from '@/components/ui/Button';
import Card from '@/components/ui/Card';
import { TextField, SelectField } from '@/components/ui/Field';

export type WizardData = RegistrationFormValues;

interface RegistrationWizardProps {
  onComplete: (data: WizardData) => Promise<void>;
  isSubmitting: boolean;
}

const STEPS = [
  { id: 1, title: 'Personal and account' },
  { id: 2, title: 'Account type' },
  { id: 3, title: 'Review and confirm' },
];

const ROLE_OPTIONS: { value: AppRole; description: string }[] = [
  { value: 'Employee', description: 'Apply for welfare programs and track your own applications.' },
  { value: 'HROfficer', description: 'Requires administrator approval before HR access is granted.' },
  { value: 'CommitteeMember', description: 'Requires administrator approval before committee access is granted.' },
  { value: 'Management', description: 'Requires administrator approval before management access is granted.' },
  { value: 'Administrator', description: 'Cannot be self-assigned. Your request is queued for a trusted administrator.' },
];

export default function RegistrationWizard({ onComplete, isSubmitting }: RegistrationWizardProps) {
  const [currentStep, setCurrentStep] = useState(1);
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [departments, setDepartments] = useState<DepartmentResponse[]>([]);
  const [deptLoadError, setDeptLoadError] = useState('');
  const [values, setValues] = useState<RegistrationFormValues>({
    firstName: '',
    lastName: '',
    email: '',
    employeeId: '',
    departmentId: 0,
    password: '',
    confirmPassword: '',
    requestedRole: 'Employee',
    confirmed: false,
  });

  useEffect(() => {
    let cancelled = false;
    departmentsApi
      .getAllDepartments()
      .then((data) => {
        if (!cancelled) setDepartments(data);
      })
      .catch(() => {
        if (!cancelled) setDeptLoadError('Could not load departments. Refresh the page or try again shortly.');
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const departmentName = useMemo(
    () => departments.find((d) => d.id === values.departmentId)?.name,
    [departments, values.departmentId]
  );

  const update = (patch: Partial<RegistrationFormValues>) => {
    setValues((prev) => ({ ...prev, ...patch }));
  };

  const goNext = () => {
    const stepErrors =
      currentStep === 1 ? validateStep1(values) : currentStep === 2 ? validateStep2(values) : validateStep3(values);
    setErrors(stepErrors);
    if (Object.keys(stepErrors).length === 0) {
      setCurrentStep((s) => Math.min(3, s + 1));
    }
  };

  const goBack = () => {
    setErrors({});
    setCurrentStep((s) => Math.max(1, s - 1));
  };

  const handleSubmit = async () => {
    const all = { ...validateStep1(values), ...validateStep2(values), ...validateStep3(values) };
    setErrors(all);
    if (Object.keys(all).length > 0 || isSubmitting) return;
    await onComplete(values);
  };

  const summary = reviewSummary(values, departmentName);

  return (
    <div className="w-full">
      <ol className="mb-6 flex items-center gap-2" aria-label="Registration progress">
        {STEPS.map((step, index) => {
          const status = currentStep > step.id ? 'completed' : currentStep === step.id ? 'current' : 'upcoming';
          return (
            <li key={step.id} className="flex flex-1 items-center gap-2">
              <div className="flex min-w-0 flex-1 flex-col items-center">
                <span
                  className={`flex h-9 w-9 items-center justify-center rounded-full text-sm font-bold ${
                    status === 'upcoming' ? 'bg-gray-200 text-gray-600' : 'bg-[var(--primary)] text-white'
                  }`}
                  aria-current={status === 'current' ? 'step' : undefined}
                >
                  {status === 'completed' ? '✓' : step.id}
                </span>
                <span className="mt-1 text-center text-xs font-medium text-gray-700">{step.title}</span>
              </div>
              {index < STEPS.length - 1 && (
                <div className={`mb-5 h-0.5 flex-1 ${currentStep > step.id ? 'bg-red-600' : 'bg-gray-200'}`} />
              )}
            </li>
          );
        })}
      </ol>

      {currentStep === 1 && (
        <Card className="space-y-4" >
          <h3 className="text-lg font-bold text-gray-900">Personal and account information</h3>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextField
              id="firstName"
              label="First name"
              required
              value={values.firstName}
              error={errors.firstName}
              onChange={(e) => update({ firstName: e.target.value })}
              placeholder="Jane"
              autoComplete="given-name"
            />
            <TextField
              id="lastName"
              label="Last name"
              required
              value={values.lastName}
              error={errors.lastName}
              onChange={(e) => update({ lastName: e.target.value })}
              placeholder="Mwangi"
              autoComplete="family-name"
            />
          </div>
          <TextField
            id="email"
            label="Email address"
            type="email"
            required
            value={values.email}
            error={errors.email}
            onChange={(e) => update({ email: e.target.value })}
            placeholder="you@example.com"
            autoComplete="email"
          />
          <TextField
            id="employeeId"
            label="Employee ID"
            required
            value={values.employeeId}
            error={errors.employeeId}
            onChange={(e) => update({ employeeId: e.target.value })}
            placeholder="EMP001"
          />
          <SelectField
            id="departmentId"
            label="Department"
            required
            value={values.departmentId || ''}
            error={errors.departmentId || deptLoadError}
            onChange={(e) => update({ departmentId: Number(e.target.value) })}
          >
            <option value="">Select a department</option>
            {departments.map((dept) => (
              <option key={dept.id} value={dept.id}>
                {dept.name}
              </option>
            ))}
          </SelectField>
          <div className="relative">
            <TextField
              id="password"
              label="Password"
              type={showPassword ? 'text' : 'password'}
              required
              value={values.password}
              error={errors.password}
              hint="At least 8 characters."
              onChange={(e) => update({ password: e.target.value })}
              autoComplete="new-password"
              placeholder="Create a password"
            />
            <button
              type="button"
              className="absolute right-3 top-9 text-sm font-medium text-red-700"
              onClick={() => setShowPassword((v) => !v)}
              aria-pressed={showPassword}
            >
              {showPassword ? 'Hide' : 'Show'}
            </button>
          </div>
          <TextField
            id="confirmPassword"
            label="Confirm password"
            type={showPassword ? 'text' : 'password'}
            required
            value={values.confirmPassword}
            error={errors.confirmPassword}
            onChange={(e) => update({ confirmPassword: e.target.value })}
            autoComplete="new-password"
            placeholder="Re-enter password"
          />
        </Card>
      )}

      {currentStep === 2 && (
        <Card className="space-y-4">
          <h3 className="text-lg font-bold text-gray-900">Account type</h3>
          <p className="text-sm text-gray-700">
            Public registration always creates an Employee account. Privileged roles are requests only; an
            administrator must approve them. Role names sent from this browser are not trusted for access.
          </p>
          <fieldset className="space-y-3">
            <legend className="ds-label">Requested account type *</legend>
            {ROLE_OPTIONS.map((role) => (
              <label
                key={role.value}
                className={`flex cursor-pointer gap-3 rounded-lg border p-4 ${
                  values.requestedRole === role.value ? 'border-red-600 bg-red-50' : 'border-gray-200 bg-white'
                }`}
              >
                <input
                  type="radio"
                  name="requestedRole"
                  value={role.value}
                  checked={values.requestedRole === role.value}
                  onChange={() => update({ requestedRole: role.value })}
                  className="mt-1"
                />
                <span>
                  <span className="block font-semibold text-gray-900">{ROLE_LABELS[role.value]}</span>
                  <span className="block text-sm text-gray-600">{role.description}</span>
                </span>
              </label>
            ))}
          </fieldset>
          {errors.requestedRole && (
            <p className="text-sm text-red-700" role="alert">
              {errors.requestedRole}
            </p>
          )}
          {PRIVILEGED_ROLES.includes(values.requestedRole as AppRole) && (
            <p className="rounded-md bg-amber-50 p-3 text-sm text-amber-900">
              You will still register as an Employee. A role-access request will be sent for administrator review.
            </p>
          )}
        </Card>
      )}

      {currentStep === 3 && (
        <Card className="space-y-4">
          <h3 className="text-lg font-bold text-gray-900">Review and confirmation</h3>
          <dl className="divide-y divide-gray-100 rounded-lg border border-gray-200">
            {[
              ['Full name', summary.fullName],
              ['Email address', summary.email],
              ['Employee ID', summary.employeeId],
              ['Department', summary.department],
              [
                'Account role',
                values.requestedRole === 'Employee'
                  ? ROLE_LABELS.Employee
                  : `${ROLE_LABELS.Employee} (requesting ${ROLE_LABELS[values.requestedRole as AppRole]})`,
              ],
            ].map(([label, value]) => (
              <div key={label} className="flex justify-between gap-4 px-4 py-3 text-sm">
                <dt className="text-gray-600">{label}</dt>
                <dd className="font-medium text-gray-900">{value}</dd>
              </div>
            ))}
          </dl>
          <p className="text-xs text-gray-500">Passwords are never shown in this summary.</p>
          <label className="flex items-start gap-3 text-sm text-gray-800">
            <input
              type="checkbox"
              checked={values.confirmed}
              onChange={(e) => update({ confirmed: e.target.checked })}
              className="mt-1 h-4 w-4"
              required
            />
            <span>
              I confirm that the information provided is accurate and that I agree to the applicable terms and
              privacy policy.
            </span>
          </label>
          {errors.confirmed && (
            <p className="text-sm text-red-700" role="alert">
              {errors.confirmed}
            </p>
          )}
        </Card>
      )}

      <div className="mt-6 flex flex-col gap-3 sm:flex-row">
        {currentStep > 1 && (
          <Button variant="outlined" onClick={goBack} disabled={isSubmitting} className="sm:flex-1">
            Back
          </Button>
        )}
        {currentStep < 3 ? (
          <Button variant="primary" onClick={goNext} className="sm:flex-1">
            Continue
          </Button>
        ) : (
          <Button
            variant="primary"
            onClick={handleSubmit}
            loading={isSubmitting}
            disabled={isSubmitting}
            className="sm:flex-1"
          >
            {isSubmitting ? 'Creating account…' : 'Create account'}
          </Button>
        )}
      </div>
    </div>
  );
}
