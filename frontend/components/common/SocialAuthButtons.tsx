'use client';

import { useEffect, useState } from 'react';
import { authApi, type ExternalProviderStatus } from '@/lib/api/auth';
import Button from '@/components/ui/Button';

interface SocialAuthButtonsProps {
  isLoading?: boolean;
}

export default function SocialAuthButtons({ isLoading = false }: SocialAuthButtonsProps) {
  const [providers, setProviders] = useState<ExternalProviderStatus[]>([]);
  const [loadError, setLoadError] = useState('');

  useEffect(() => {
    authApi
      .getExternalProviders()
      .then(setProviders)
      .catch(() =>
        setLoadError('Social sign-in status could not be loaded. Email and password still work.')
      );
  }, []);

  const start = (provider: string, configured: boolean) => {
    if (!configured) return;
    window.location.assign(authApi.externalStartUrl(provider));
  };

  const items = providers.length
    ? providers
    : [
        { provider: 'Google', configured: false },
        { provider: 'GitHub', configured: false },
        { provider: 'X', configured: false },
      ];

  return (
    <div className="space-y-3">
      {loadError && <p className="text-sm text-gray-600">{loadError}</p>}
      {items.map((item) => (
        <Button
          key={item.provider}
          variant="outlined"
          className="w-full"
          disabled={isLoading || !item.configured}
          onClick={() => start(item.provider.toLowerCase(), item.configured)}
          aria-label={`Continue with ${item.provider}`}
        >
          Continue with {item.provider}
        </Button>
      ))}
      {items.some((p) => !p.configured) && (
        <p className="text-xs text-gray-600">
          Unavailable providers still need administrator setup. Email registration and login remain available.
        </p>
      )}
      <p className="text-xs text-gray-600">For first-time use, register or sign in with email, then connect your provider from Profile.</p>
    </div>
  );
}
