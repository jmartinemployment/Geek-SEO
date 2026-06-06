'use client';

import { useState } from 'react';
import type { FusedSiteUnderstanding, TopicCandidate } from '@/lib/seo-api';

type Props = {
  fusion: FusedSiteUnderstanding;
};

type Filter = 'all' | 'selected' | 'excluded' | 'multi';

function sourceBadges(candidate: TopicCandidate): string[] {
  const sources = candidate.evidence.map((e) => e.source);
  return [...new Set(sources)];
}

function isSelected(slug: string, fusion: FusedSiteUnderstanding): boolean {
  return fusion.selectedPillars.some((p) => p.slug === slug);
}

function matchesFilter(candidate: TopicCandidate, fusion: FusedSiteUnderstanding, filter: Filter): boolean {
  if (filter === 'all') return true;
  if (filter === 'selected') return isSelected(candidate.slug, fusion);
  if (filter === 'excluded') return !isSelected(candidate.slug, fusion);
  return sourceBadges(candidate).length >= 2;
}

export function TopicCandidateMatrix({ fusion }: Readonly<Props>) {
  const [filter, setFilter] = useState<Filter>('all');

  const rows = [...fusion.allCandidates]
    .filter((c) => matchesFilter(c, fusion, filter))
    .sort((a, b) => b.confidence - a.confidence);

  if (fusion.allCandidates.length === 0) return null;

  return (
    <div className="mt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
            Topic candidate matrix
          </h3>
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
            Fusion {fusion.fusionVersion} — {fusion.allCandidates.length} peer candidates, cap{' '}
            {fusion.pillarCap}
          </p>
        </div>
        <div className="flex flex-wrap gap-1">
          {(
            [
              ['all', 'All'],
              ['selected', 'Selected'],
              ['excluded', 'Held back'],
              ['multi', 'Multi-source'],
            ] as const
          ).map(([id, label]) => (
            <button
              key={id}
              type="button"
              onClick={() => setFilter(id)}
              className={`rounded-md px-2.5 py-1 text-xs ${
                filter === id
                  ? 'bg-[var(--color-accent)] text-white'
                  : 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-3 overflow-x-auto rounded-lg border border-[var(--color-border)]">
        <table className="min-w-full text-left text-xs">
          <thead className="bg-[var(--color-surface-muted)] text-[var(--color-text-muted)]">
            <tr>
              <th className="px-3 py-2 font-medium">Topic</th>
              <th className="px-3 py-2 font-medium">Confidence</th>
              <th className="px-3 py-2 font-medium">Sources</th>
              <th className="px-3 py-2 font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const selected = isSelected(row.slug, fusion);
              const reason = fusion.exclusionReasons[row.slug];
              return (
                <tr key={row.slug} className="border-t border-[var(--color-border)]">
                  <td className="px-3 py-2 font-medium text-[var(--color-text-primary)]">
                    {row.name}
                  </td>
                  <td className="px-3 py-2 text-[var(--color-text-secondary)]">
                    {row.confidence.toFixed(2)}
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex flex-wrap gap-1">
                      {sourceBadges(row).map((source) => (
                        <span
                          key={source}
                          className="rounded bg-[var(--color-surface-muted)] px-1.5 py-0.5 text-[10px] uppercase tracking-wide text-[var(--color-text-muted)]"
                        >
                          {source}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="px-3 py-2 text-[var(--color-text-secondary)]">
                    {selected ? (
                      <span className="text-emerald-700">Selected</span>
                    ) : (
                      <span title={reason ?? undefined}>
                        {reason ? 'Excluded' : 'Not selected'}
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
