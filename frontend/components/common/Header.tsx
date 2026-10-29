'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/lib/auth/AuthContext';

export default function Header() {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const hide =
    pathname?.startsWith('/login') ||
    pathname?.startsWith('/register') ||
    pathname?.startsWith('/auth/') ||
    pathname?.startsWith('/employee') ||
    pathname?.startsWith('/hr') ||
    pathname?.startsWith('/admin') ||
    pathname?.startsWith('/committee') ||
    pathname?.startsWith('/management');

  if (hide) {
    return null;
  }

  return (
    <header className="bg-white shadow-sm border-b border-gray-200 sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo and Brand */}
          <div className="flex items-center space-x-3">
            <div className="flex items-center space-x-2">
              {/* Professional Logo */}
              <div className="w-10 h-10 professional-gradient rounded-lg flex items-center justify-center shadow-md icon-3d">
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                </svg>
              </div>
              <div>
                <h1 className="text-xl font-bold text-gray-900">Welfare Portal</h1>
                <p className="text-xs text-red-600 font-semibold tracking-wide">Employee Management</p>
              </div>
            </div>
          </div>

          {/* Navigation */}
          <nav className="hidden md:flex items-center space-x-8">
            <Link href="/" className="text-gray-600 hover:text-red-600 transition-colors font-medium">
              Home
            </Link>
            {user && (
              <>
                <Link href="/employee/programs" className="text-gray-600 hover:text-red-600 transition-colors font-medium">
                  Programs
                </Link>
                <Link href="/employee/applications" className="text-gray-600 hover:text-red-600 transition-colors font-medium">
                  Applications
                </Link>
                <Link href="/employee/dashboard" className="text-gray-600 hover:text-red-600 transition-colors font-medium">
                  Dashboard
                </Link>
              </>
            )}
          </nav>

          {/* User Menu */}
          <div className="flex items-center space-x-4">
            {user ? (
              <div className="flex items-center space-x-3">
                <div className="text-right hidden sm:block">
                  <p className="text-sm font-semibold text-gray-900">{user.firstName} {user.lastName}</p>
                  <p className="text-xs text-gray-500 capitalize">{user.role}</p>
                </div>
                <div className="w-10 h-10 professional-gradient rounded-full flex items-center justify-center text-white font-bold shadow-md icon-3d bounce-hover">
                  {user.firstName[0]}{user.lastName[0]}
                </div>
                <button
                  onClick={logout}
                  className="px-4 py-2 professional-button text-white text-sm button-3d"
                >
                  Logout
                </button>
              </div>
            ) : (
              <div className="flex items-center space-x-3">
                <Link
                  href="/login"
                  className="px-4 py-2 text-red-600 border-2 border-red-600 rounded-lg hover:bg-red-50 transition-colors font-medium"
                >
                  Login
                </Link>
                <Link
                  href="/register"
                  className="px-4 py-2 professional-button text-white text-sm button-3d"
                >
                  Register
                </Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
