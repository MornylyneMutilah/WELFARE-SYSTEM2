'use client';

import type { InputHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react';

interface FieldWrapProps {
  id: string;
  label: string;
  required?: boolean;
  error?: string;
  hint?: string;
  children: React.ReactNode;
}

export function FieldWrap({ id, label, required, error, hint, children }: FieldWrapProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;

  return (
    <div>
      <label htmlFor={id} className="ds-label">
        {label}
        {required && (
          <span className="ml-1 text-red-600" aria-hidden>
            *
          </span>
        )}
      </label>
      {children}
      {hint && !error && (
        <p id={hintId} className="mt-1 text-sm text-gray-600">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="mt-1 text-sm text-red-700" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  error?: string;
  hint?: string;
};

export function TextField({ id, label, error, hint, required, className = '', ...props }: InputProps) {
  const describedBy = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean).join(' ') || undefined;
  return (
    <FieldWrap id={id!} label={label} required={required} error={error} hint={hint}>
      <input
        id={id}
        required={required}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
        aria-required={required}
        className={`ds-input ${error ? 'border-red-600' : ''} ${className}`}
        {...props}
      />
    </FieldWrap>
  );
}

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  error?: string;
  hint?: string;
};

export function SelectField({
  id,
  label,
  error,
  hint,
  required,
  className = '',
  children,
  ...props
}: SelectProps) {
  const describedBy = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean).join(' ') || undefined;
  return (
    <FieldWrap id={id!} label={label} required={required} error={error} hint={hint}>
      <select
        id={id}
        required={required}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
        aria-required={required}
        className={`ds-input ${error ? 'border-red-600' : ''} ${className}`}
        {...props}
      >
        {children}
      </select>
    </FieldWrap>
  );
}

type AreaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label: string;
  error?: string;
  hint?: string;
};

export function TextAreaField({
  id,
  label,
  error,
  hint,
  required,
  className = '',
  ...props
}: AreaProps) {
  const describedBy = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean).join(' ') || undefined;
  return (
    <FieldWrap id={id!} label={label} required={required} error={error} hint={hint}>
      <textarea
        id={id}
        required={required}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
        aria-required={required}
        className={`ds-input ${error ? 'border-red-600' : ''} ${className}`}
        {...props}
      />
    </FieldWrap>
  );
}
