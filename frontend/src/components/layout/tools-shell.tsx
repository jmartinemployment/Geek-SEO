'use client';

import { usePathname } from 'next/navigation';
import { SiteFooter } from '@/components/layout/site-footer';
import { SiteNavbar } from '@/components/layout/site-navbar';
import { usesAppShell } from '@/lib/app-shell-routes';

export function ToolsShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  if (usesAppShell(pathname)) {
    return <>{children}</>;
  }

  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-bg)]">
      <SiteNavbar />
      <div className="flex flex-1 flex-col">{children}</div>
      <SiteFooter />
    </div>
  );
}
