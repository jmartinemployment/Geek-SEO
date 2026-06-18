'use client';

import type { ReactNode } from 'react';
import { AuthProvider } from '@/components/auth/auth-provider';
import { SeoHubProvider } from '@/components/signalr/seo-hub-provider';

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <AuthProvider>
      <SeoHubProvider>{children}</SeoHubProvider>
    </AuthProvider>
  );
}
