interface AlertProps {
  tone?: 'error' | 'success' | 'warning' | 'info';
  children: React.ReactNode;
}

const tones = {
  error: 'bg-red-50 border-red-600 text-red-800',
  success: 'bg-green-50 border-green-600 text-green-800',
  warning: 'bg-amber-50 border-amber-600 text-amber-900',
  info: 'bg-gray-50 border-gray-400 text-gray-800',
};

const ariaLive = {
  error: 'assertive',
  success: 'polite',
  warning: 'polite',
  info: 'polite',
} as const;

export default function Alert({ tone = 'info', children }: AlertProps) {
  return (
    <div
      className={`border-l-4 px-4 py-3 rounded-r-md ${tones[tone]}`}
      role={tone === 'error' ? 'alert' : 'status'}
      aria-live={ariaLive[tone]}
    >
      {children}
    </div>
  );
}
