import type { NichePillarResult } from '@/lib/seo-api';

/** Summary stat labels (header + pillar table toolbar). */
export const PILLAR_COVERAGE_SUMMARY = {
  covered: 'Pillar topics covered',
  partial: 'Pillar topics partially covered',
  gap: 'Pillar topic gaps',
} as const;

/** Section headings in the grouped pillar table (include the count in UI). */
export const PILLAR_COVERAGE_SECTION: Record<
  NichePillarResult['coverageStatus'],
  { title: string; empty: string; detail: string }
> = {
  covered: {
    title: 'Covered',
    empty: 'No pillar topics have a dedicated on-site page yet.',
    detail: 'A dedicated page on your site addresses this pillar topic.',
  },
  partial: {
    title: 'Partially covered',
    empty: 'No pillar topics are in partial coverage.',
    detail: 'Some on-site signals exist, but coverage is incomplete.',
  },
  gap: {
    title: 'Gaps',
    empty: 'No pillar topic gaps — every selected pillar has at least partial coverage.',
    detail: 'No dedicated page found — a primary content opportunity.',
  },
};

/** Per-row badge text in the pillar table. */
export const PILLAR_COVERAGE_ROW: Record<NichePillarResult['coverageStatus'], string> = {
  covered: 'Covered',
  partial: 'Partial',
  gap: 'Gap',
};

export const PILLAR_COVERAGE_ORDER: NichePillarResult['coverageStatus'][] = [
  'gap',
  'partial',
  'covered',
];

export function countPillarCoverage(rows: ReadonlyArray<{ coverageStatus: string }>) {
  let covered = 0;
  let partial = 0;
  let gap = 0;
  for (const row of rows) {
    if (row.coverageStatus === 'covered') covered += 1;
    else if (row.coverageStatus === 'partial') partial += 1;
    else gap += 1;
  }
  return { covered, partial, gap };
}

export function groupPillarsByCoverage<T extends { coverageStatus: string }>(
  rows: ReadonlyArray<T>,
): Record<NichePillarResult['coverageStatus'], T[]> {
  const grouped: Record<NichePillarResult['coverageStatus'], T[]> = {
    covered: [],
    partial: [],
    gap: [],
  };

  for (const row of rows) {
    if (row.coverageStatus === 'covered') grouped.covered.push(row);
    else if (row.coverageStatus === 'partial') grouped.partial.push(row);
    else grouped.gap.push(row);
  }

  return grouped;
}
