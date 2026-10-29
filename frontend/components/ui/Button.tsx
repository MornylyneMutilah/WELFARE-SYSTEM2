'use client';

import type { ButtonHTMLAttributes } from 'react';

type Variant = 'primary' | 'secondary' | 'outlined' | 'destructive' | 'ghost';

const variants: Record<Variant, string> = {
  primary:
    'bg-[var(--primary)] text-white hover:bg-[var(--primary-dark)] disabled:bg-gray-300 disabled:text-gray-500',
  secondary:
    'bg-gray-800 text-white hover:bg-gray-900 disabled:bg-gray-300 disabled:text-gray-500',
  outlined:
    'border-2 border-[var(--primary)] text-[var(--primary)] bg-white hover:bg-red-50 disabled:border-gray-300 disabled:text-gray-400',
  destructive: 'bg-red-800 text-white hover:bg-red-900 disabled:bg-gray-300 disabled:text-gray-500',
  ghost: 'bg-transparent text-gray-800 hover:bg-gray-100 disabled:text-gray-400',
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  loading?: boolean;
}

export default function Button({
  variant = 'primary',
  loading = false,
  className = '',
  children,
  disabled,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={`inline-flex items-center justify-center gap-2 rounded-lg px-5 py-3 text-sm font-semibold focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 ${variants[variant]} ${className}`}
      {...props}
    >
      {loading && (
        <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden>
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
          />
        </svg>
      )}
      {children}
    </button>
  );
}
