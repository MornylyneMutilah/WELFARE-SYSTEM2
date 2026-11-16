'use client';

import { useEffect, useRef } from 'react';
import Alert from '@/components/ui/Alert';

export default function ActionFeedback({ message, tone = 'error' }: {
  message: string; tone?: 'error' | 'success';
}) {
  const target = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!message) return;
    const frame = window.requestAnimationFrame(() => {
      target.current?.focus({ preventScroll: true });
      target.current?.scrollIntoView({
        behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
        block: 'start',
      });
    });
    return () => window.cancelAnimationFrame(frame);
  }, [message, tone]);
  if (!message) return null;
  return <div ref={target} tabIndex={-1} className="mb-4 scroll-mt-6 rounded-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600">
    <Alert tone={tone}>{message}</Alert>
  </div>;
}
