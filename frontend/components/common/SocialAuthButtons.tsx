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
    window.location.href = authApi.externalStartUrl(provider);
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
          Disabled providers are not configured on the server. They will not sign you in until client IDs,
          secrets, and callback URLs are set. Email registration and login remain available.
        </p>
      )}
    </div>
  );
}
