'use client';

import Link from 'next/link';
import type { FusedSiteUnderstanding } from '@/lib/seo-api';

type Props = {
  fusion: FusedSiteUnderstanding;
  projectId?: string;
};

function topicalMapHref(
  projectId: string | undefined,
  options: { seed?: string; mode?: 'niche'; autogen?: boolean },
): string {
  const params = new URLSearchParams();
  if (projectId) params.set('projectId', projectId);
  if (options.seed) params.set('seed', options.seed);
  if (options.mode) params.set('mode', options.mode);
  if (options.autogen) params.set('autogen', '1');
  const query = params.toString();
  return query ? `/app/strategy/topical-map?${query}` : '/app/strategy/topical-map';
}

function pickMapSeed(fusion: FusedSiteUnderstanding): string | null {
  const action = (fusion.recommendedActions ?? []).find(
    (a) => a.actionType === 'suggest_pillar_page',
  );
  if (action) return action.topicName;

  const gapPillar = fusion.selectedPillars.find(
    (p) => !p.dedicatedPageUrl && p.confidence >= 0.45,
  );
  if (gapPillar) return gapPillar.name;

  return fusion.selectedPillars[0]?.name ?? null;
}

export function FusionPillarMapLauncher({ fusion, projectId }: Readonly<Props>) {
  if (!projectId || fusion.selectedPillars.length === 0) return null;

  const seed = pickMapSeed(fusion);
  if (!seed) return null;

  const gapCount = fusion.selectedPillars.filter((p) => !p.dedicatedPageUrl).length;

  return (
    <section className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-muted)]/30 px-5 py-3">
      <div>
        <p className="text-sm font-medium text-[var(--color-text-primary)]">
          Plan content from this analysis
        </p>
        <p className="text-xs text-[var(--color-text-muted)]">
          Build a topical map from niche pillars
          {gapCount > 0 ? ` — ${gapCount} without a dedicated page` : ''}.
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        <Link
          href={topicalMapHref(projectId, { mode: 'niche', autogen: true })}
          className="rounded-lg bg-[var(--color-accent)] px-3 py-2 text-xs font-medium text-white hover:opacity-90"
        >
          Build from gap pillars
        </Link>
        <Link
          href={topicalMapHref(projectId, { seed })}
          className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-xs font-medium hover:bg-[var(--color-surface-muted)]"
        >
          Single seed: {seed}
        </Link>
      </div>
    </section>
  );
}
