'use client';

import { Fragment, useMemo, useState } from 'react';
import type { FusedSiteUnderstanding, TopicCandidate } from '@/lib/seo-api';
import {
  candidateHasSource,
  evidenceTotalWeight,
  formatExclusionReason,
  sourceLabel,
  SOURCE_COLORS,
  uniqueSources,
} from '@/components/niche-analyzer/topic-candidate-matrix-utils';

type Props = {
  fusion: FusedSiteUnderstanding;
};

type StatusFilter = 'all' | 'selected' | 'excluded' | 'multi';

function isSelected(slug: string, fusion: FusedSiteUnderstanding): boolean {
  return fusion.selectedPillars.some((p) => p.slug === slug);
}

function matchesStatusFilter(
  candidate: TopicCandidate,
  fusion: FusedSiteUnderstanding,
  filter: StatusFilter,
): boolean {
  if (filter === 'all') return true;
  if (filter === 'selected') return isSelected(candidate.slug, fusion);
  if (filter === 'excluded') return !isSelected(candidate.slug, fusion);
  return uniqueSources(candidate).length >= 2;
}

function ConfidenceBar({ confidence }: { confidence: number }) {
  const pct = Math.round(confidence * 100);
  const color =
    pct >= 75 ? 'bg-emerald-500' : pct >= 50 ? 'bg-amber-400' : 'bg-stone-300';

  return (
    <div className="flex min-w-[7rem] items-center gap-2">
      <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-[var(--color-border)]">
        <div className={`h-full rounded-full ${color}`} style={{ width: `${pct}%` }} />
      </div>
      <span className="w-8 text-right tabular-nums text-[var(--color-text-secondary)]">
        {confidence.toFixed(2)}
      </span>
    </div>
  );
}

function EvidencePanel({ candidate }: { candidate: TopicCandidate }) {
  const sorted = [...candidate.evidence].sort((a, b) => b.weight - a.weight);

  return (
    <div className="space-y-2 px-3 py-3">
      {sorted.map((evidence, index) => (
        <div
          key={`${evidence.source}-${index}`}
          className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2"
        >
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${
                SOURCE_COLORS[evidence.source] ?? 'bg-[var(--color-surface-muted)] text-[var(--color-text-muted)]'
              }`}
            >
              {sourceLabel(evidence.source)}
            </span>
            <span className="text-[10px] tabular-nums text-[var(--color-text-muted)]">
              +{evidence.weight.toFixed(2)} weight
            </span>
          </div>
          {evidence.snippet ? (
            <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{evidence.snippet}</p>
          ) : null}
          {evidence.url ? (
            <p className="mt-0.5 truncate text-[10px] text-[var(--color-text-muted)]" title={evidence.url}>
              {evidence.url}
            </p>
          ) : null}
        </div>
      ))}
      <p className="text-[10px] text-[var(--color-text-muted)]">
        Stacked evidence total: {evidenceTotalWeight(candidate.evidence).toFixed(2)} (capped at 1.0 in fusion)
      </p>
    </div>
  );
}

export function TopicCandidateMatrix({ fusion }: Readonly<Props>) {
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [sourceFilter, setSourceFilter] = useState<string>('all');
  const [expandedSlug, setExpandedSlug] = useState<string | null>(null);

  const availableSources = useMemo(() => {
    const set = new Set<string>();
    for (const candidate of fusion.allCandidates) {
      for (const source of uniqueSources(candidate)) set.add(source);
    }
    return [...set].sort();
  }, [fusion.allCandidates]);

  const stats = useMemo(() => {
    const multi = fusion.allCandidates.filter((c) => uniqueSources(c).length >= 2).length;
    return {
      total: fusion.allCandidates.length,
      selected: fusion.selectedPillars.length,
      excluded: fusion.allCandidates.length - fusion.selectedPillars.length,
      multi,
    };
  }, [fusion]);

  const topicalityRows = useMemo(() => {
    const map = fusion.normalizedTopicalityBySlug ?? {};
    return fusion.selectedPillars
      .map((p) => ({
        slug: p.slug,
        name: p.name,
        share: map[p.slug] ?? 0,
      }))
      .filter((r) => r.share > 0)
      .sort((a, b) => b.share - a.share)
      .slice(0, 8);
  }, [fusion]);

  const rows = useMemo(() => {
    return [...fusion.allCandidates]
      .filter((c) => matchesStatusFilter(c, fusion, statusFilter))
      .filter((c) => sourceFilter === 'all' || candidateHasSource(c, sourceFilter))
      .sort((a, b) => b.confidence - a.confidence);
  }, [fusion, statusFilter, sourceFilter]);

  if (fusion.allCandidates.length === 0) return null;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="border-b border-[var(--color-border)] px-5 py-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
              Topic candidate matrix
            </h3>
            <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
              Fusion {fusion.fusionVersion} — {stats.total} candidates considered, {stats.selected}{' '}
              selected
            </p>
          </div>
          <div className="flex flex-wrap gap-2 text-[10px] uppercase tracking-wide text-[var(--color-text-muted)]">
            <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-emerald-800">
              {stats.selected} pillars
            </span>
            <span className="rounded-full bg-stone-100 px-2 py-0.5 text-stone-600">
              {stats.excluded} not selected
            </span>
            <span className="rounded-full bg-blue-50 px-2 py-0.5 text-blue-800">
              {stats.multi} multi-source
            </span>
          </div>
        </div>

        {fusion.signalSourcesPresent.length > 0 ? (
          <p className="mt-2 text-[10px] text-[var(--color-text-muted)]">
            Signals in pool:{' '}
            {fusion.signalSourcesPresent.map((s) => sourceLabel(s)).join(' · ')}
          </p>
        ) : null}

        {topicalityRows.length > 0 ? (
          <div className="mt-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/50 px-3 py-3">
            <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">
              Normalized topicality (crawled pages)
            </p>
            <div className="mt-2 space-y-1.5">
              {topicalityRows.map(({ slug, name, share }) => (
                <div key={slug} className="flex items-center gap-2 text-xs">
                  <span className="w-28 shrink-0 truncate text-[var(--color-text-secondary)]" title={name}>
                    {name}
                  </span>
                  <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-[var(--color-border)]">
                    <div
                      className="h-full rounded-full bg-[var(--color-accent)]"
                      style={{ width: `${Math.min(100, Math.round(share * 100))}%` }}
                    />
                  </div>
                  <span className="w-10 text-right tabular-nums text-[var(--color-text-muted)]">
                    {Math.round(share * 100)}%
                  </span>
                </div>
              ))}
            </div>
          </div>
        ) : null}

        <div className="mt-3 flex flex-wrap gap-1">
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
              onClick={() => setStatusFilter(id)}
              className={`rounded-md px-2.5 py-1 text-xs transition-colors ${
                statusFilter === id
                  ? 'bg-[var(--color-accent)] text-white'
                  : 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
              }`}
            >
              {label}
            </button>
          ))}
        </div>

        {availableSources.length > 0 ? (
          <div className="mt-2 flex flex-wrap items-center gap-1">
            <span className="mr-1 text-[10px] uppercase tracking-wide text-[var(--color-text-muted)]">
              Source:
            </span>
            <button
              type="button"
              onClick={() => setSourceFilter('all')}
              className={`rounded-md px-2 py-0.5 text-[10px] ${
                sourceFilter === 'all'
                  ? 'bg-[var(--color-text-primary)] text-white'
                  : 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]'
              }`}
            >
              Any
            </button>
            {availableSources.map((source) => (
              <button
                key={source}
                type="button"
                onClick={() => setSourceFilter(source)}
                className={`rounded-md px-2 py-0.5 text-[10px] ${
                  sourceFilter === source
                    ? 'bg-[var(--color-text-primary)] text-white'
                    : `${SOURCE_COLORS[source] ?? 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]'}`
                }`}
              >
                {sourceLabel(source)}
              </button>
            ))}
          </div>
        ) : null}
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full text-left text-xs">
          <thead className="bg-[var(--color-surface-muted)] text-[var(--color-text-muted)]">
            <tr>
              <th className="w-8 px-2 py-2" aria-label="Expand" />
              <th className="px-3 py-2 font-medium">Topic</th>
              <th className="px-3 py-2 font-medium">Confidence</th>
              <th className="px-3 py-2 font-medium">Topicality</th>
              <th className="px-3 py-2 font-medium">SERP coverage</th>
              <th className="px-3 py-2 font-medium">Sources</th>
              <th className="px-3 py-2 font-medium">Structure</th>
              <th className="px-3 py-2 font-medium">Outcome</th>
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-5 py-8 text-center text-[var(--color-text-muted)]">
                  No candidates match the current filters.
                </td>
              </tr>
            ) : null}
            {rows.map((row) => {
              const selected = isSelected(row.slug, fusion);
              const reason = formatExclusionReason(fusion.exclusionReasons[row.slug]);
              const expanded = expandedSlug === row.slug;
              const sources = uniqueSources(row);
              const topicality = fusion.normalizedTopicalityBySlug?.[row.slug];
              const coverage = fusion.entityCoverageBySlug?.[row.slug];

              return (
                <Fragment key={row.slug}>
                  <tr
                    key={row.slug}
                    className={`border-t border-[var(--color-border)] ${
                      selected ? 'bg-emerald-50/40' : ''
                    }`}
                  >
                    <td className="px-2 py-2">
                      <button
                        type="button"
                        aria-expanded={expanded}
                        aria-label={expanded ? 'Collapse evidence' : 'Expand evidence'}
                        onClick={() => setExpandedSlug(expanded ? null : row.slug)}
                        className="flex h-6 w-6 items-center justify-center rounded text-[var(--color-text-muted)] hover:bg-[var(--color-surface-muted)]"
                      >
                        {expanded ? '−' : '+'}
                      </button>
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium text-[var(--color-text-primary)]">{row.name}</div>
                      <div className="text-[10px] text-[var(--color-text-muted)]">{row.slug}</div>
                    </td>
                    <td className="px-3 py-2">
                      <ConfidenceBar confidence={row.confidence} />
                    </td>
                    <td className="px-3 py-2 text-[var(--color-text-secondary)]">
                      {topicality !== undefined && topicality > 0 ? (
                        <span className="tabular-nums">{Math.round(topicality * 100)}%</span>
                      ) : (
                        <span className="text-[var(--color-text-muted)]">—</span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-[var(--color-text-secondary)]">
                      {coverage && coverage.expectedEntityCount > 0 ? (
                        <span className={coverage.isEntityThin ? 'text-rose-600' : ''}>
                          {Math.round(coverage.coverageScore * 100)}%
                          {coverage.isEntityThin ? ' · thin' : ''}
                        </span>
                      ) : (
                        <span className="text-[var(--color-text-muted)]">—</span>
                      )}
                    </td>
                    <td className="px-3 py-2">
                      <div className="flex flex-wrap gap-1">
                        {sources.map((source) => (
                          <span
                            key={source}
                            className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${
                              SOURCE_COLORS[source] ??
                              'bg-[var(--color-surface-muted)] text-[var(--color-text-muted)]'
                            }`}
                          >
                            {sourceLabel(source)}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="px-3 py-2 text-[var(--color-text-secondary)]">
                      {row.dedicatedPageUrl ? (
                        <span className="block truncate max-w-[10rem]" title={row.dedicatedPageUrl}>
                          Dedicated URL
                        </span>
                      ) : null}
                      {row.internalLinkCount > 0 ? (
                        <span>{row.internalLinkCount} inbound link{row.internalLinkCount === 1 ? '' : 's'}</span>
                      ) : null}
                      {!row.dedicatedPageUrl && row.internalLinkCount === 0 ? (
                        <span className="text-[var(--color-text-muted)]">—</span>
                      ) : null}
                    </td>
                    <td className="px-3 py-2">
                      {selected ? (
                        <span className="font-medium text-emerald-700">Selected pillar</span>
                      ) : (
                        <div>
                          <span className="text-[var(--color-text-secondary)]">
                            {reason ? 'Held back' : 'Not selected'}
                          </span>
                          {reason ? (
                            <p className="mt-0.5 max-w-xs text-[10px] leading-snug text-[var(--color-text-muted)]">
                              {reason}
                            </p>
                          ) : null}
                        </div>
                      )}
                    </td>
                  </tr>
                  {expanded ? (
                    <tr key={`${row.slug}-evidence`} className="bg-[var(--color-surface-muted)]/40">
                      <td colSpan={7}>
                        <EvidencePanel candidate={row} />
                      </td>
                    </tr>
                  ) : null}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}
