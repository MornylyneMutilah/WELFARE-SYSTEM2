'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth/AuthContext';
import Link from 'next/link';

export default function Home() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading) {
      if (isAuthenticated) {
        router.push('/employee/dashboard');
      }
    }
  }, [isAuthenticated, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-white">
        <div className="text-gray-600">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Hero Section */}
      <div className="professional-gradient text-white overflow-hidden relative">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
          <div className="text-center fade-in relative z-10">
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 bg-white/20 backdrop-blur-sm rounded-xl flex items-center justify-center shadow-2xl neon-glow float">
                <svg className="w-12 h-12 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                </svg>
              </div>
            </div>
            <h1 className="text-4xl md:text-5xl font-bold mb-4">Welfare Portal</h1>
            <p className="text-2xl font-semibold mb-2 opacity-90">Employee Welfare Management</p>
            <p className="text-lg opacity-80 max-w-2xl mx-auto leading-relaxed">
              A professional platform designed to streamline welfare administration with intelligent automation and efficient user experience.
            </p>
          </div>
        </div>
        {/* Decorative floating elements */}
        <div className="absolute top-10 left-10 w-20 h-20 bg-white/10 rounded-full float-slow"></div>
        <div className="absolute top-20 right-20 w-16 h-16 bg-white/10 rounded-full float"></div>
        <div className="absolute bottom-10 left-1/4 w-12 h-12 bg-white/10 rounded-full float-slow"></div>
      </div>

      {/* Features Section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="text-center mb-16">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">Key Features</h2>
          <p className="text-gray-600 max-w-2xl mx-auto">
            Experience efficient welfare management with our professional platform
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-8">
          <div className="card-3d">
            <div className="professional-card p-8 interactive-hover stagger-1 card-3d-inner relative overflow-hidden morph-hover">
              <div className="shine absolute inset-0 pointer-events-none"></div>
              <div className="w-14 h-14 professional-gradient rounded-lg flex items-center justify-center mb-6 shadow-md icon-3d relative z-10">
                <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <h3 className="text-xl font-bold text-gray-900 mb-3 relative z-10">Smart Eligibility</h3>
              <p className="text-gray-600 leading-relaxed relative z-10">
                AI-powered eligibility checks with intelligent rule-based decision support for instant approval insights.
              </p>
            </div>
          </div>

          <div className="card-3d">
            <div className="professional-card p-8 interactive-hover stagger-2 card-3d-inner relative overflow-hidden morph-hover">
              <div className="shine absolute inset-0 pointer-events-none"></div>
              <div className="w-14 h-14 professional-gradient rounded-lg flex items-center justify-center mb-6 shadow-md icon-3d relative z-10">
                <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              </div>
              <h3 className="text-xl font-bold text-gray-900 mb-3 relative z-10">Fast Processing</h3>
              <p className="text-gray-600 leading-relaxed relative z-10">
                Streamlined workflow with automated routing and real-time status updates for faster approvals.
              </p>
            </div>
          </div>

          <div className="card-3d">
            <div className="professional-card p-8 interactive-hover stagger-3 card-3d-inner relative overflow-hidden morph-hover">
              <div className="shine absolute inset-0 pointer-events-none"></div>
              <div className="w-14 h-14 professional-gradient rounded-lg flex items-center justify-center mb-6 shadow-md icon-3d relative z-10">
                <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                </svg>
              </div>
              <h3 className="text-xl font-bold text-gray-900 mb-3 relative z-10">Real-Time Insights</h3>
              <p className="text-gray-600 leading-relaxed relative z-10">
                Intelligent notifications and anomaly detection for proactive application monitoring and management.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* CTA Section */}
      <div className="professional-gradient text-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
          <div className="text-center">
            <h2 className="text-3xl font-bold mb-4">Ready to Get Started?</h2>
            <p className="text-white/90 mb-8 max-w-2xl mx-auto">
              Join our professional welfare management platform and experience efficient employee benefits administration.
            </p>
            <div className="flex justify-center space-x-4">
              <Link
                href="/login"
                className="px-8 py-4 bg-white text-red-600 rounded-lg hover:bg-gray-50 transition-all duration-300 font-semibold shadow-lg hover:shadow-xl hover:-translate-y-1 button-3d"
              >
                Login
              </Link>
              <Link
                href="/register"
                className="px-8 py-4 bg-white/20 backdrop-blur-sm text-white rounded-lg hover:bg-white/30 transition-all duration-300 font-semibold border border-white/30 hover:border-white/50 hover:-translate-y-1 button-3d"
              >
                Register
              </Link>
            </div>
          </div>
        </div>
      </div>

      {/* Footer */}
      <footer className="bg-gray-900 text-gray-400 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <p className="text-sm">© 2026 Welfare Portal. All rights reserved.</p>
          <p className="text-xs mt-2 text-gray-500">Employee Welfare Management System</p>
        </div>
      </footer>
    </div>
  );
}
