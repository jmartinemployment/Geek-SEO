'use client';

import { AppHeader } from '@/components/app/app-header';
import { SidebarLayout } from '@/components/app/app-sidebar';
import { SiteFooter } from '@/components/layout/site-footer';
import { SiteNavbar } from '@/components/layout/site-navbar';
import { cn } from '@/lib/utils';

export function AppShell({
  children,
  mainClassName,
}: {
  children: React.ReactNode;
  mainClassName?: string;
}) {
  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-bg)]">
      <SiteNavbar />
      <SidebarLayout>
        <main className={cn('flex-1 px-4 py-8 md:px-10', mainClassName)}>{children}</main>
      </SidebarLayout>
      <SiteFooter />
    </div>
  );
}
