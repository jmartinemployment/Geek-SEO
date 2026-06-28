'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { ChevronsLeft, ChevronsRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  SIDEBAR_PRIMARY_WIDTH,
  SIDEBAR_SECONDARY_WIDTH,
  defaultActivePrimaryId,
  defaultActiveSecondaryLabel,
  isPlaceholderHref,
  sidebarPrimaryNav,
  type SidebarPrimaryNav,
} from '@/components/app/sidebar-navigation';

const SIDEBAR_COLLAPSED_KEY = 'geek-seo-sidebar-collapsed';

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
    <button
      type="button"
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
    </button>
  );
}

function SecondaryNavLink({
  href,
  label,
  isActive,
  onSelect,
}: {
  href: string;
  label: string;
  isActive: boolean;
  onSelect: () => void;
}) {
  const className = cn(
    'relative block w-full rounded-md px-3 py-2 text-left text-sm transition-colors',
    isActive
      ? 'bg-[var(--color-sidebar-active)] font-medium text-[var(--color-text-primary)]'
      : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-sidebar-active)] hover:text-[var(--color-text-primary)]',
  );

  const activeBar = isActive ? (
    <span className="absolute inset-y-1.5 left-0 w-0.5 rounded-full bg-[var(--color-metric-blue)]" />
  ) : null;

  if (isPlaceholderHref(href)) {
    return (
      <button type="button" onClick={onSelect} className={className} aria-current={isActive ? 'page' : undefined}>
        {activeBar}
        {label}
      </button>
    );
  }

  return (
    <Link
      href={href}
      prefetch={false}
      onClick={onSelect}
      className={className}
      aria-current={isActive ? 'page' : undefined}
    >
      {activeBar}
      {label}
    </Link>
  );
}

function SidebarRail({
  activePrimaryId,
  activeSecondaryLabel,
  onPrimarySelect,
  onSecondarySelect,
  collapsed,
  onToggleCollapsed,
}: {
  activePrimaryId: string;
  activeSecondaryLabel: string | null;
  onPrimarySelect: (id: string) => void;
  onSecondarySelect: (label: string) => void;
  collapsed: boolean;
  onToggleCollapsed: () => void;
}) {
  const activePrimary =
    sidebarPrimaryNav.find((item) => item.id === activePrimaryId) ?? sidebarPrimaryNav[1];

  return (
    <>
      <div
        className="flex h-full min-h-0 shrink-0 flex-col bg-[var(--color-sidebar-rail)]"
        style={{ width: SIDEBAR_PRIMARY_WIDTH }}
      >
        <div className="flex h-14 shrink-0 items-center justify-center border-b border-[var(--color-border)]">
          <span
            className="flex size-9 items-center justify-center rounded-lg bg-[var(--color-accent)] text-xs font-bold text-white"
            title="Geek SEO"
          >
            G
          </span>
        </div>

        <nav className="flex min-h-0 flex-1 flex-col overflow-y-auto">
          {sidebarPrimaryNav.map((primary) => (
            <PrimaryNavButton
              key={primary.id}
              primary={primary}
              isActive={primary.id === activePrimaryId}
              onSelect={() => onPrimarySelect(primary.id)}
            />
          ))}
        </nav>
      </div>

      {!collapsed ? (
        <div
          className="flex h-full min-h-0 shrink-0 flex-col bg-white"
          style={{ width: SIDEBAR_SECONDARY_WIDTH }}
        >
          <div className="flex h-14 shrink-0 items-center justify-between border-b border-[var(--color-border)] px-4">
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

          <div className="min-h-0 flex-1 overflow-y-auto px-2 py-3">
            {activePrimary.sections.length === 0 ? null : (
              activePrimary.sections.map((section, sectionIndex) => (
                <div
                  key={section.title ?? `section-${sectionIndex}`}
                  className="mb-5 last:mb-0"
                >
                  {section.title ? (
                    <p className="mb-1 px-3 text-[11px] font-semibold uppercase tracking-[0.04em] text-[var(--color-text-muted)]">
                      {section.title}
                    </p>
                  ) : null}
                  <div className="space-y-0.5">
                    {section.items.map((item) => (
                      <SecondaryNavLink
                        key={item.label}
                        href={item.href}
                        label={item.label}
                        isActive={activeSecondaryLabel === item.label}
                        onSelect={() => onSecondarySelect(item.label)}
                      />
                    ))}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      ) : (
        <button
          type="button"
          onClick={onToggleCollapsed}
          className="absolute top-14 right-0 z-10 flex size-7 translate-x-1/2 items-center justify-center rounded-md border border-[var(--color-border)] bg-white text-[var(--color-text-muted)] shadow-sm transition-colors hover:text-[var(--color-text-primary)]"
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
  children,
}: {
  children: React.ReactNode;
}) {
  const [collapsed, setCollapsed] = useState(false);
  const [activePrimaryId, setActivePrimaryId] = useState(defaultActivePrimaryId);
  const [activeSecondaryLabel, setActiveSecondaryLabel] = useState<string | null>(() =>
    defaultActiveSecondaryLabel(defaultActivePrimaryId()),
  );

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

  function handlePrimarySelect(id: string) {
    setActivePrimaryId(id);
    setActiveSecondaryLabel(defaultActiveSecondaryLabel(id));
  }

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
    <div className="flex min-h-0 w-full flex-1">
      <aside
        className="app-sidebar-viewport relative flex min-h-0 shrink-0 border-r border-[var(--color-border)]"
        style={{ width: totalWidth }}
      >
        <SidebarRail
          activePrimaryId={activePrimaryId}
          activeSecondaryLabel={activeSecondaryLabel}
          onPrimarySelect={handlePrimarySelect}
          onSecondarySelect={setActiveSecondaryLabel}
          collapsed={collapsed}
          onToggleCollapsed={toggleCollapsed}
        />
      </aside>
      <div className="flex min-h-0 min-w-0 flex-1 flex-col">{children}</div>
    </div>
  );
}
