'use client';

import { useAuth } from '@/components/auth/auth-provider';

/** True when auth bootstrap finished and a bearer token is available for SEO API calls. */
export function useAuthReady() {
  const { accessToken, isLoading: authLoading } = useAuth();
  return {
    accessToken,
    authLoading,
    authReady: !authLoading && Boolean(accessToken),
  };
}
