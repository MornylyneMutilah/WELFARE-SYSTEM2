interface CardProps {
  children: React.ReactNode;
  className?: string;
  role?: string;
  ariaLabel?: string;
}

export default function Card({ children, className = '', role, ariaLabel }: CardProps) {
  return (
    <div
      className={`rounded-xl border border-gray-200 bg-white p-6 shadow-sm ${className}`}
      role={role}
      aria-label={ariaLabel}
    >
      {children}
    </div>
  );
}
