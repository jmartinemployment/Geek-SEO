'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import Link from 'next/link';
import { ChevronsLeft, ChevronsRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  SIDEBAR_PRIMARY_WIDTH,
  SIDEBAR_SECONDARY_WIDTH,
  isSidebarItemActive,
  resolveActivePrimaryId,
  sidebarBottomNav,
  sidebarPrimaryNav,
  type SidebarPrimaryNav,
} from '@/components/app/sidebar-navigation';

const SIDEBAR_COLLAPSED_KEY = 'geek-seo-sidebar-collapsed';

type SidebarContextValue = {
  collapsed: boolean;
  totalWidth: number;
};

const SidebarContext = createContext<SidebarContextValue>({
  collapsed: false,
  totalWidth: SIDEBAR_PRIMARY_WIDTH + SIDEBAR_SECONDARY_WIDTH,
});

export function useSidebarLayout() {
  return useContext(SidebarContext);
}

function PrimaryNavButton({
  primary,
  isActive,
  onSelect,
}: {
  primary: SidebarPrimaryNav;
  isActive: boolean;
  onSelect: () => void;
}) {
  const Icon = primary.icon;

  return (
    <Link
      href={primary.sections[0]?.items[0]?.href ?? '/app/dashboard'}
      prefetch={false}
      onClick={onSelect}
      className={cn(
        'group flex w-full flex-col items-center gap-1 px-1 py-2.5 text-[10px] font-medium leading-tight transition-colors',
        isActive
          ? 'bg-white text-[var(--color-text-primary)]'
          : 'text-[var(--color-text-secondary)] hover:bg-white/70 hover:text-[var(--color-text-primary)]',
      )}
      aria-current={isActive ? 'page' : undefined}
    >
      <Icon className="size-5 shrink-0" strokeWidth={1.75} />
      <span className="max-w-full truncate px-0.5">{primary.label}</span>
    </Link>
  );
}

function SecondaryNavLink({
  href,
  label,
  pathname,
}: {
  href: string;
  label: string;
  pathname: string;
}) {
  const isActive = isSidebarItemActive(href, pathname);

  return (
    <Link
      href={href}
      prefetch={false}
      className={cn(
        'relative block rounded-md px-3 py-2 text-sm transition-colors',
        isActive
          ? 'bg-[var(--color-sidebar-active)] font-medium text-[var(--color-text-primary)]'
          : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-sidebar-active)] hover:text-[var(--color-text-primary)]',
      )}
      aria-current={isActive ? 'page' : undefined}
    >
      {isActive ? (
        <span className="absolute inset-y-1.5 left-0 w-0.5 rounded-full bg-[var(--color-metric-blue)]" />
      ) : null}
      {label}
    </Link>
  );
}

function SidebarRail({
  pathname,
  activePrimaryId,
  onPrimarySelect,
  collapsed,
  onToggleCollapsed,
}: {
  pathname: string;
  activePrimaryId: string;
  onPrimarySelect: (id: string) => void;
  collapsed: boolean;
  onToggleCollapsed: () => void;
}) {
  const activePrimary =
    sidebarPrimaryNav.find((item) => item.id === activePrimaryId) ?? sidebarPrimaryNav[0];
  const BottomIcon = sidebarBottomNav.icon;

  return (
    <>
      <div
        className="flex shrink-0 flex-col bg-[var(--color-sidebar-rail)]"
        style={{ width: SIDEBAR_PRIMARY_WIDTH }}
      >
        <div className="flex h-14 items-center justify-center border-b border-[var(--color-border)]">
          <Link
            href="/app/dashboard"
            prefetch={false}
            className="flex size-9 items-center justify-center rounded-lg bg-[var(--color-accent)] text-xs font-bold text-white"
            title="Geek SEO"
          >
            G
          </Link>
        </div>

        <nav className="flex flex-1 flex-col">
          {sidebarPrimaryNav.map((primary) => (
            <PrimaryNavButton
              key={primary.id}
              primary={primary}
              isActive={primary.id === activePrimaryId}
              onSelect={() => onPrimarySelect(primary.id)}
            />
          ))}
        </nav>

        <div className="border-t border-[var(--color-border)]">
          <Link
            href={sidebarBottomNav.href}
            prefetch={false}
            className={cn(
              'flex w-full flex-col items-center gap-1 px-1 py-2.5 text-[10px] font-medium leading-tight transition-colors',
              pathname.startsWith(sidebarBottomNav.href)
                ? 'bg-white text-[var(--color-text-primary)]'
                : 'text-[var(--color-text-secondary)] hover:bg-white/70 hover:text-[var(--color-text-primary)]',
            )}
            title={sidebarBottomNav.label}
          >
            <BottomIcon className="size-5 shrink-0" strokeWidth={1.75} />
            <span className="max-w-full truncate px-0.5">{sidebarBottomNav.label}</span>
          </Link>
        </div>
      </div>

      {!collapsed ? (
        <div
          className="flex shrink-0 flex-col bg-white"
          style={{ width: SIDEBAR_SECONDARY_WIDTH }}
        >
          <div className="flex h-14 items-center justify-between border-b border-[var(--color-border)] px-4">
            <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">
              {activePrimary.label}
            </h2>
            <button
              type="button"
              onClick={onToggleCollapsed}
              className="flex size-7 items-center justify-center rounded-md text-[var(--color-text-muted)] transition-colors hover:bg-[var(--color-sidebar-active)] hover:text-[var(--color-text-primary)]"
              title="Collapse menu"
              aria-label="Collapse menu"
            >
              <ChevronsLeft className="size-4" />
            </button>
          </div>

          <div className="flex-1 overflow-y-auto px-2 py-3">
            {activePrimary.sections.map((section) => (
              <div key={section.title} className="mb-5 last:mb-0">
                <p className="mb-1 px-3 text-[11px] font-semibold uppercase tracking-[0.04em] text-[var(--color-text-muted)]">
                  {section.title}
                </p>
                <div className="space-y-0.5">
                  {section.items.map((item) => (
                    <SecondaryNavLink
                      key={item.href}
                      href={item.href}
                      label={item.label}
                      pathname={pathname}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <button
          type="button"
          onClick={onToggleCollapsed}
          className="absolute left-[calc(100%-1px)] top-14 z-50 flex size-7 -translate-x-1/2 items-center justify-center rounded-md border border-[var(--color-border)] bg-white text-[var(--color-text-muted)] shadow-sm transition-colors hover:text-[var(--color-text-primary)]"
          title="Expand menu"
          aria-label="Expand menu"
        >
          <ChevronsRight className="size-4" />
        </button>
      )}
    </>
  );
}

export function SidebarLayout({
  pathname,
  children,
}: {
  pathname: string;
  children: React.ReactNode;
}) {
  const [collapsed, setCollapsed] = useState(false);
  const [activePrimaryId, setActivePrimaryId] = useState(() => resolveActivePrimaryId(pathname));

  useEffect(() => {
    setActivePrimaryId(resolveActivePrimaryId(pathname));
  }, [pathname]);

  useEffect(() => {
    try {
      const stored = window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY);
      if (stored === 'true') {
        setCollapsed(true);
      }
    } catch {
      // ignore storage errors
    }
  }, []);

  function toggleCollapsed() {
    setCollapsed((prev) => {
      const next = !prev;
      try {
        window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(next));
      } catch {
        // ignore storage errors
      }
      return next;
    });
  }

  const totalWidth = collapsed
    ? SIDEBAR_PRIMARY_WIDTH
    : SIDEBAR_PRIMARY_WIDTH + SIDEBAR_SECONDARY_WIDTH;

  return (
    <SidebarContext.Provider value={{ collapsed, totalWidth }}>
      <aside
        className="fixed inset-y-0 left-0 z-40 flex border-r border-[var(--color-border)]"
        style={{ width: totalWidth }}
      >
        <SidebarRail
          pathname={pathname}
          activePrimaryId={activePrimaryId}
          onPrimarySelect={setActivePrimaryId}
          collapsed={collapsed}
          onToggleCollapsed={toggleCollapsed}
        />
      </aside>
      {children}
    </SidebarContext.Provider>
  );
}
