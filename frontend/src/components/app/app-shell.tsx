'use client';

import { usePathname } from 'next/navigation';
import { AppHeader } from '@/components/app/app-header';
import { AppSidebar } from '@/components/app/app-sidebar';
import { cn } from '@/lib/utils';

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
      <AppSidebar pathname={pathname} />
      <div className="flex min-h-screen flex-col pl-14">
        <AppHeader />
        <main className={cn('flex-1 px-4 py-8 md:px-10', mainClassName)}>{children}</main>
      </div>
    </div>
  );
}
