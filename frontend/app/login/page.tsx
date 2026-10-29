'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/auth/AuthContext';
import SocialAuthButtons from '@/components/common/SocialAuthButtons';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { TextField } from '@/components/ui/Field';
import { dashboardPathForRole } from '@/lib/auth/roles';

export default function LoginPage() {
  const router = useRouter();
  const { login, isAuthenticated, user } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      router.push(dashboardPathForRole(user?.role));
    }
  }, [isAuthenticated, router, user?.role]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      await login(email, password);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      setError(axiosErr.response?.data?.message || 'Login failed. Please check your credentials.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 px-4 py-10">
      <div className="mx-auto grid max-w-5xl overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm lg:grid-cols-2">
        <div className="hidden professional-gradient p-10 text-white lg:flex lg:flex-col lg:justify-end">
          <h1 className="text-3xl font-bold">Care for each other, made simple.</h1>
          <p className="mt-3 text-white/90">Sign in to manage welfare applications for your organization.</p>
        </div>
        <div className="p-6 sm:p-10">
          <h2 className="text-3xl font-bold text-gray-900">Welcome back</h2>
          <p className="mt-1 text-gray-600">Your portal to employee welfare support</p>

          {error && (
            <div className="mt-6">
              <Alert tone="error">{error}</Alert>
            </div>
          )}

          <form className="mt-6 space-y-5" onSubmit={handleSubmit}>
            <TextField
              id="email"
              label="Email address"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
            />
            <div className="relative">
              <TextField
                id="password"
                label="Password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="current-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
              />
              <button
                type="button"
                className="absolute right-3 top-9 text-sm font-medium text-red-700"
                onClick={() => setShowPassword((v) => !v)}
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            <Button type="submit" variant="primary" className="w-full" loading={isLoading} disabled={isLoading}>
              {isLoading ? 'Signing in…' : 'Continue with email'}
            </Button>
          </form>

          <div className="mt-8">
            <p className="mb-3 text-center text-sm text-gray-500">Or continue with a configured provider</p>
            <SocialAuthButtons isLoading={isLoading} />
          </div>

          <p className="mt-6 text-center text-sm text-gray-600">
            Don&apos;t have an account?{' '}
            <Link href="/register" className="font-semibold text-red-700 hover:text-red-800">
              Register
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
