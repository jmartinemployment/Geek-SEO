import { cn } from '@/lib/utils';

type ClusterStatCardProps = {
  label: string;
  value: number | string;
  hint?: string;
  tone?: 'default' | 'success' | 'warning' | 'muted';
};

const toneClasses = {
  default: 'border-[var(--color-border)] bg-white',
  success: 'border-emerald-200 bg-emerald-50/60',
  warning: 'border-amber-200 bg-amber-50/60',
  muted: 'border-[var(--color-border)] bg-[var(--color-surface-muted)]',
} as const;

export function ClusterStatCard({ label, value, hint, tone = 'default' }: ClusterStatCardProps) {
  return (
    <div className={cn('rounded-xl border px-4 py-3 shadow-sm', toneClasses[tone])}>
      <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--color-text-secondary)]">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">
        {value}
      </p>
      {hint ? <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{hint}</p> : null}
    </div>
  );
}

export type ClusterDashboardTab = 'overview' | 'spokes' | 'links' | 'research';

const TAB_LABELS: Record<ClusterDashboardTab, string> = {
  overview: 'Overview',
  spokes: 'Spokes',
  links: 'Link plan',
  research: 'Research',
};

type ClusterTabBarProps = {
  active: ClusterDashboardTab;
  onChange: (tab: ClusterDashboardTab) => void;
  counts?: Partial<Record<ClusterDashboardTab, number>>;
};

export function ClusterTabBar({ active, onChange, counts }: ClusterTabBarProps) {
  const tabs: ClusterDashboardTab[] = ['overview', 'spokes', 'links', 'research'];

  return (
    <div className="flex flex-wrap gap-1 rounded-xl border bg-[var(--color-surface-muted)] p-1">
      {tabs.map((tab) => {
        const count = counts?.[tab];
        return (
          <button
            key={tab}
            type="button"
            onClick={() => onChange(tab)}
            className={cn(
              'rounded-lg px-3 py-1.5 text-xs font-medium transition-colors',
              active === tab
                ? 'bg-white text-[var(--color-text-primary)] shadow-sm'
                : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]',
            )}
          >
            {TAB_LABELS[tab]}
            {typeof count === 'number' && count > 0 ? (
              <span className="ml-1.5 text-[10px] text-[var(--color-text-muted)]">({count})</span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
