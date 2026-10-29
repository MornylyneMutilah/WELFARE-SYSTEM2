'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/lib/auth/AuthContext';
import { navigationForRole, ROLE_LABELS, isAppRole } from '@/lib/auth/roles';
import ProtectedRoute from '@/components/common/ProtectedRoute';

interface AppShellProps {
  title: string;
  description?: string;
  requiredRole?: string;
  children: React.ReactNode;
}

export default function AppShell({ title, description, requiredRole, children }: AppShellProps) {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const items = navigationForRole(user?.role);
  const roleLabel = isAppRole(user?.role) ? ROLE_LABELS[user.role] : user?.role;

  return (
    <ProtectedRoute requiredRole={requiredRole}>
      <div className="min-h-screen bg-gray-50">
        <header className="border-b border-gray-200 bg-white shadow-sm">
          <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4 py-4 sm:px-6 lg:px-8">
            <div className="flex-1">
              <p className="text-xs font-semibold uppercase tracking-wider text-red-700">Welfare Portal</p>
              <h1 className="mt-1 text-xl font-bold text-gray-900">{title}</h1>
              {description && <p className="mt-1 text-sm text-gray-600">{description}</p>}
            </div>
            <div className="flex items-center gap-4">
              <div className="hidden text-right sm:block">
                <p className="text-sm font-semibold text-gray-900">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className="text-xs text-gray-500">{roleLabel}</p>
              </div>
              <button
                type="button"
                onClick={logout}
                className="rounded-lg bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-[var(--primary-dark)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600"
              >
                Logout
              </button>
            </div>
          </div>
          <nav className="mx-auto flex max-w-7xl gap-1 overflow-x-auto px-4 pb-3 sm:px-6 lg:px-8" aria-label="Section navigation">
            {items.map((item) => {
              const active = pathname === item.href;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`whitespace-nowrap rounded-lg px-4 py-2 text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 ${
                    active
                      ? 'bg-red-600 text-white shadow-sm'
                      : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900'
                  }`}
                  aria-current={active ? 'page' : undefined}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </header>
        <main className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">{children}</main>
      </div>
    </ProtectedRoute>
  );
}
