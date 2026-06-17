'use client';

import { useEffect, useRef, useState, type ComponentType } from 'react';
import Link from 'next/link';
import {
  BarChart3,
  Compass,
  FileText,
  Globe,
  LayoutDashboard,
  Map,
  MoreHorizontal,
  Search,
  Settings,
  ShieldCheck,
  TrendingUp,
} from 'lucide-react';
import { cn } from '@/lib/utils';

type SidebarItem = {
  href: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
};

const primaryItems: SidebarItem[] = [
  { href: '/app/dashboard', label: 'Home', icon: LayoutDashboard },
  { href: '/app/strategy/topical-map', label: 'Topical Map', icon: Map },
  { href: '/url-analyzer', label: 'URL Analyzer', icon: Compass },
  { href: '/content-writing', label: 'Content Writing', icon: Globe },
  { href: '/app/keywords', label: 'Keyword Research', icon: Search },
  { href: '/app/cannibalization', label: 'Cannibalization', icon: TrendingUp },
  { href: '/app/audit', label: 'Site Audit', icon: ShieldCheck },
  { href: '/app/analytics', label: 'Analytics', icon: BarChart3 },
];

const overflowItems: SidebarItem[] = [
  { href: '/app/bulk', label: 'Bulk articles', icon: FileText },
  { href: '/app/calendar', label: 'Content calendar', icon: FileText },
  { href: '/app/serp', label: 'Deep SERP', icon: Search },
  { href: '/app/planner', label: 'Planner', icon: Map },
  { href: '/app/brand-voice', label: 'Brand voice', icon: FileText },
  { href: '/app/geo', label: 'GEO', icon: Globe },
  { href: '/app/content-guard', label: 'Content guard', icon: ShieldCheck },
];

function SidebarLink({
  href,
  label,
  icon: Icon,
  isActive,
}: SidebarItem & { isActive: boolean }) {
  return (
    <Link
      href={href}
      prefetch={false}
      title={label}
      aria-label={label}
      className={cn(
        'relative flex size-10 items-center justify-center rounded-lg text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-sidebar-active)] hover:text-[var(--color-text-primary)]',
        isActive && 'bg-[var(--color-sidebar-active)] text-[var(--color-text-primary)]',
      )}
    >
      {isActive ? (
        <span className="absolute inset-y-1 left-0 w-0.5 rounded-full bg-[var(--color-accent)]" />
      ) : null}
      <Icon className="size-5" />
    </Link>
  );
}

function SidebarOverflowMenu({ pathname }: { pathname: string }) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!open) return;

    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    document.addEventListener('pointerdown', handlePointerDown);
    return () => document.removeEventListener('pointerdown', handlePointerDown);
  }, [open]);

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className="flex size-10 items-center justify-center rounded-lg text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-sidebar-active)] hover:text-[var(--color-text-primary)]"
        title="More tools"
        aria-label="More tools"
        aria-expanded={open}
      >
        <MoreHorizontal className="size-5" />
      </button>
      {open ? (
        <div className="absolute left-full top-0 z-50 ml-2 min-w-44 rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white p-1 shadow-[var(--shadow-card-hover)]">
          {overflowItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              prefetch={false}
              onClick={() => setOpen(false)}
              className="block rounded-md px-3 py-2 text-sm text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)]"
            >
              {item.label}
            </Link>
          ))}
        </div>
      ) : null}
    </div>
  );
}

export function AppSidebar({ pathname }: { pathname: string }) {
  return (
    <aside className="fixed inset-y-0 left-0 z-40 flex w-14 flex-col border-r border-[var(--color-border)] bg-[var(--color-bg)]">
      <div className="flex flex-1 flex-col items-center gap-1 px-2 py-3">
        {primaryItems.map((item) => (
          <SidebarLink
            key={item.href}
            {...item}
            isActive={pathname === item.href || pathname.startsWith(`${item.href}/`)}
          />
        ))}
        <SidebarOverflowMenu pathname={pathname} />
      </div>
      <div className="flex flex-col items-center gap-1 border-t border-[var(--color-border)] px-2 py-3">
        <SidebarLink
          href="/app/settings"
          label="Settings"
          icon={Settings}
          isActive={pathname.startsWith('/app/settings')}
        />
      </div>
    </aside>
  );
}
