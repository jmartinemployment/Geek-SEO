'use client';

import type { SiteTopicProfile } from '@/lib/seo-api';

type Props = {
  fusion: SiteTopicProfile;
};

export function EntityCoveragePanel({ fusion }: Readonly<Props>) {
  const rows = fusion.selectedPillars
    .map((pillar) => {
      const coverage = fusion.entityCoverageBySlug?.[pillar.slug];
      return coverage
        ? {
            ...coverage,
            slug: pillar.slug,
            name: pillar.name,
          }
        : null;
    })
    .filter((row): row is NonNullable<typeof row> => row !== null && row.expectedEntityCount > 0);

  if (rows.length === 0) return null;

  const thinCount = rows.filter((r) => r.isEntityThin).length;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
            SERP entity coverage
          </h3>
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
            How many topic entities competitors surface in SERP that your site also covers
          </p>
        </div>
        {thinCount > 0 ? (
          <span className="rounded-full bg-amber-50 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-amber-800">
            {thinCount} entity-thin
          </span>
        ) : null}
      </div>

      <div className="mt-3 space-y-2">
        {rows.map((row) => {
          const pct = Math.round(row.coverageScore * 100);
          const barColor =
            pct >= 75 ? 'bg-emerald-500' : pct >= 60 ? 'bg-amber-400' : 'bg-rose-400';

          return (
            <div
              key={row.slug}
              className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/40 px-3 py-2"
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-xs font-medium text-[var(--color-text-primary)]">
                  {row.name}
                </span>
                <span className="text-[10px] tabular-nums text-[var(--color-text-muted)]">
                  {row.matchedEntityCount}/{row.expectedEntityCount} entities
                </span>
              </div>
              <div className="mt-1.5 flex items-center gap-2">
                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-[var(--color-border)]">
                  <div className={`h-full rounded-full ${barColor}`} style={{ width: `${pct}%` }} />
                </div>
                <span className="w-10 text-right text-xs tabular-nums text-[var(--color-text-secondary)]">
                  {pct}%
                </span>
                {row.isEntityThin ? (
                  <span className="text-[10px] font-medium text-rose-600">thin</span>
                ) : null}
              </div>
              {row.missingEntities.length > 0 ? (
                <p className="mt-1.5 text-[10px] text-[var(--color-text-muted)]">
                  Missing: {row.missingEntities.join(', ')}
                </p>
              ) : null}
            </div>
          );
        })}
      </div>
    </section>
  );
}
