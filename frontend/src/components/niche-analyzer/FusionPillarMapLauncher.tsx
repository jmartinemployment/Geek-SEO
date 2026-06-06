'use client';

import Link from 'next/link';
import type { FusedSiteUnderstanding } from '@/lib/seo-api';

type Props = {
  fusion: FusedSiteUnderstanding;
  projectId?: string;
};

function topicalMapHref(projectId: string | undefined, seed: string): string {
  const params = new URLSearchParams();
  if (projectId) params.set('projectId', projectId);
  params.set('seed', seed);
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

  const gapCount = (fusion.recommendedActions ?? []).filter(
    (a) => a.actionType === 'suggest_pillar_page',
  ).length;

  return (
    <section className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-muted)]/30 px-5 py-3">
      <div>
        <p className="text-sm font-medium text-[var(--color-text-primary)]">
          Plan content from this analysis
        </p>
        <p className="text-xs text-[var(--color-text-muted)]">
          Open topical map with {gapCount > 0 ? `${gapCount} pillar gap(s)` : 'your top pillar'} as
          the seed keyword.
        </p>
      </div>
      <Link
        href={topicalMapHref(projectId, seed)}
        className="rounded-lg bg-[var(--color-accent)] px-3 py-2 text-xs font-medium text-white hover:opacity-90"
      >
        Open topical map
      </Link>
    </section>
  );
}
