'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/auth/AuthContext';
import RegistrationWizard, { type WizardData } from '@/components/common/RegistrationWizard';
import SocialAuthButtons from '@/components/common/SocialAuthButtons';
import Alert from '@/components/ui/Alert';
import { dashboardPathForRole } from '@/lib/auth/roles';

export default function RegisterPage() {
  const router = useRouter();
  const { register, isAuthenticated, user } = useAuth();
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      router.push(dashboardPathForRole(user?.role));
    }
  }, [isAuthenticated, router, user?.role]);

  const handleWizardComplete = async (data: WizardData) => {
    setError('');
    setSuccess('');
    setIsLoading(true);

    try {
      await register({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        employeeId: data.employeeId,
        departmentId: data.departmentId,
        requestedRole: data.requestedRole,
      });
      setSuccess('Account created. Redirecting you to sign in…');
      setTimeout(() => router.push('/login'), 1200);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { errors?: Record<string, string[]>; message?: string } } };
      let errorMessage = 'Registration failed. ';
      const payload = axiosErr.response?.data;
      if (payload?.errors) {
        errorMessage += Object.entries(payload.errors)
          .map(([key, messages]) => `${key}: ${messages.join(', ')}`)
          .join('; ');
      } else if (payload?.message) {
        errorMessage += payload.message;
      } else {
        errorMessage += 'Please check all fields and try again.';
      }
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 px-4 py-10">
      <div className="mx-auto grid max-w-5xl overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm lg:grid-cols-2">
        <div className="hidden professional-gradient p-10 text-white lg:block">
          <h1 className="text-3xl font-bold">Join the welfare portal</h1>
          <p className="mt-4 text-white/90">
            Register with your work email. Privileged roles require administrator approval.
          </p>
        </div>
        <div className="p-6 sm:p-10">
          <h2 className="text-3xl font-bold text-gray-900">Create account</h2>
          <p className="mt-1 text-gray-600">Complete all three steps. Your account is created only after the server confirms.</p>

          <div className="mt-6">
            <p className="mb-3 text-sm font-semibold text-gray-800">Continue with email</p>
            {error && (
              <div className="mb-4">
                <Alert tone="error">{error}</Alert>
              </div>
            )}
            {success && (
              <div className="mb-4">
                <Alert tone="success">{success}</Alert>
              </div>
            )}
            {!success && <RegistrationWizard onComplete={handleWizardComplete} isSubmitting={isLoading} />}
          </div>

          <div className="mt-8">
            <p className="mb-3 text-center text-sm text-gray-500">Or continue with a configured provider</p>
            <SocialAuthButtons isLoading={isLoading} />
          </div>

          <p className="mt-6 text-center text-sm text-gray-600">
            Already have an account?{' '}
            <Link href="/login" className="font-semibold text-red-700 hover:text-red-800">
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
