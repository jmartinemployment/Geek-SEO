'use client';

import type { FusedSiteUnderstanding } from '@/lib/seo-api';

type Props = {
  fusion: FusedSiteUnderstanding;
};

function slugToLabel(slug: string, fusion: FusedSiteUnderstanding): string {
  const match =
    fusion.selectedPillars.find((p) => p.slug === slug) ??
    fusion.allCandidates.find((p) => p.slug === slug);
  return match?.name ?? slug.replaceAll('-', ' ');
}

export function InternalLinkGraphPanel({ fusion }: Readonly<Props>) {
  const graph = fusion.internalLinkGraph;
  if (!graph || (graph.edges.length === 0 && graph.orphanSlugs.length === 0)) return null;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-4">
      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          Internal link graph
        </h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
          Pillar-to-pillar links discovered during the site crawl
        </p>
      </div>

      {graph.edges.length > 0 ? (
        <ul className="mt-3 space-y-1.5">
          {graph.edges.slice(0, 12).map((edge) => (
            <li
              key={`${edge.fromSlug}-${edge.toSlug}`}
              className="flex flex-wrap items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-muted)]/40 px-3 py-2 text-xs"
            >
              <span className="font-medium text-[var(--color-text-primary)]">
                {slugToLabel(edge.fromSlug, fusion)}
              </span>
              <span className="text-[var(--color-text-muted)]">→</span>
              <span className="font-medium text-[var(--color-text-primary)]">
                {slugToLabel(edge.toSlug, fusion)}
              </span>
              <span className="ml-auto rounded bg-[var(--color-surface)] px-1.5 py-0.5 text-[10px] tabular-nums text-[var(--color-text-muted)]">
                {edge.linkCount} link{edge.linkCount === 1 ? '' : 's'}
              </span>
              {edge.sampleAnchors.length > 0 ? (
                <span className="w-full truncate text-[10px] text-[var(--color-text-muted)]">
                  Anchors: {edge.sampleAnchors.join(' · ')}
                </span>
              ) : null}
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-xs text-[var(--color-text-muted)]">
          No cross-pillar internal links found between selected pillars.
        </p>
      )}

      {graph.orphanSlugs.length > 0 ? (
        <div className="mt-3 rounded-lg border border-dashed border-amber-200 bg-amber-50/50 px-3 py-2">
          <p className="text-[10px] font-medium uppercase tracking-wide text-amber-800">
            Orphan pillars (no internal links)
          </p>
          <p className="mt-1 text-xs text-amber-900">
            {graph.orphanSlugs.map((slug) => slugToLabel(slug, fusion)).join(', ')}
          </p>
        </div>
      ) : null}
    </section>
  );
}
