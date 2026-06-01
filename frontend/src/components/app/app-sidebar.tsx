import type { ComponentType } from 'react';
import Link from 'next/link';
import {
  BarChart3,
  FileText,
  Globe,
  LayoutDashboard,
  LineChart,
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
  { href: '/app/content', label: 'Content Documents', icon: FileText },
  { href: '/app/keywords', label: 'Keyword Research', icon: Search },
  { href: '/app/cannibalization', label: 'Cannibalization', icon: TrendingUp },
  { href: '/app/rank-tracker', label: 'Rank Tracker', icon: LineChart },
  { href: '/app/audit', label: 'Site Audit', icon: ShieldCheck },
  { href: '/app/analytics', label: 'Analytics', icon: BarChart3 },
];

const overflowItems: SidebarItem[] = [
  { href: '/app/guided', label: 'Guided flow', icon: Globe },
  { href: '/app/bulk', label: 'Bulk articles', icon: FileText },
  { href: '/app/calendar', label: 'Content calendar', icon: FileText },
  { href: '/app/serp', label: 'Deep SERP', icon: Search },
  { href: '/app/planner', label: 'Planner', icon: Map },
  { href: '/app/brand-voice', label: 'Brand voice', icon: FileText },
  { href: '/app/briefs/new', label: 'Briefs', icon: FileText },
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
        <details className="group relative">
          <summary
            className="flex size-10 cursor-pointer list-none items-center justify-center rounded-lg text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-sidebar-active)] hover:text-[var(--color-text-primary)] [&::-webkit-details-marker]:hidden"
            title="More"
            aria-label="More"
          >
            <MoreHorizontal className="size-5" />
          </summary>
          <div className="absolute left-full top-0 z-50 ml-2 min-w-44 rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white p-1 shadow-[var(--shadow-card-hover)]">
            {overflowItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="block rounded-md px-3 py-2 text-sm text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)]"
              >
                {item.label}
              </Link>
            ))}
          </div>
        </details>
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
