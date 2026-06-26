'use client';

import { usePathname } from 'next/navigation';
import { AppHeader } from '@/components/app/app-header';
import { SidebarLayout, useSidebarLayout } from '@/components/app/app-sidebar';
import { cn } from '@/lib/utils';

function AppMain({
  children,
  mainClassName,
}: {
  children: React.ReactNode;
  mainClassName?: string;
}) {
  const { totalWidth } = useSidebarLayout();

  return (
    <div className="flex min-h-screen flex-col" style={{ paddingLeft: totalWidth }}>
      <AppHeader />
      <main className={cn('flex-1 px-4 py-8 md:px-10', mainClassName)}>{children}</main>
    </div>
  );
}

export function AppShell({
  children,
  mainClassName,
}: {
  children: React.ReactNode;
  mainClassName?: string;
}) {
  const pathname = usePathname();

  return (
    <div className="min-h-screen bg-[var(--color-bg)]">
      <SidebarLayout pathname={pathname}>
        <AppMain mainClassName={mainClassName}>{children}</AppMain>
      </SidebarLayout>
    </div>
  );
}
