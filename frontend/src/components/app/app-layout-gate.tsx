'use client';

import { usePathname } from 'next/navigation';
import { AppShell } from '@/components/app/app-shell';
import { appShellMainClassName, usesAppShell } from '@/lib/app-shell-routes';

export function AppLayoutGate({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  if (!usesAppShell(pathname)) {
    return children;
  }

  return <AppShell mainClassName={appShellMainClassName(pathname)}>{children}</AppShell>;
}
