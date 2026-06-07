'use client';

import type { NichePillarResult, PillarCoverageMatrix } from '@/lib/seo-api';
import {
  countPillarCoverage,
  groupPillarsByCoverage,
  PILLAR_COVERAGE_ORDER,
  PILLAR_COVERAGE_ROW,
  PILLAR_COVERAGE_SECTION,
  PILLAR_COVERAGE_SUMMARY,
} from '@/components/niche-analyzer/pillar-coverage-labels';

type Props = {
  pillars: NichePillarResult[];
  coverageFallback?: PillarCoverageMatrix[];
  totalPillarsIdentified?: number;
  pillarsCovered?: number;
  pillarsPartial?: number;
  pillarsGap?: number;
};

const PRIORITY_BADGE: Record<string, string> = {
  must_have: 'bg-red-100 text-red-700',
  high_value: 'bg-yellow-100 text-yellow-700',
  expansion: 'bg-blue-100 text-blue-700',
};

const SECTION_ACCENT: Record<NichePillarResult['coverageStatus'], string> = {
  covered: 'border-green-200 bg-green-50/40',
  partial: 'border-amber-200 bg-amber-50/40',
  gap: 'border-red-200 bg-red-50/40',
};

const SECTION_COUNT_COLOR: Record<NichePillarResult['coverageStatus'], string> = {
  covered: 'text-green-700',
  partial: 'text-yellow-700',
  gap: 'text-red-600',
};

type PillarRowData = {
  id: string;
  pillarTopic: string;
  primaryKeyword?: string;
  pageUrl?: string;
  searchIntent?: string;
  coverageScore: number;
  coveredSubtopics: number;
  totalSubtopics: number;
  coverageStatus: NichePillarResult['coverageStatus'];
  strategicPriority: string;
  quickWinCount?: number;
  hasQuickWins?: boolean;
};

function PillarCoverageSummaryBar({
  covered,
  partial,
  gap,
  total,
}: {
  covered: number;
  partial: number;
  gap: number;
  total: number;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2 border-b border-[var(--color-border)] bg-[var(--color-surface-secondary)] px-4 py-3">
      <span className="mr-1 text-xs text-[var(--color-text-muted)]">
        {total} selected pillar topic{total === 1 ? '' : 's'} =
      </span>
      <SummaryChip
        label={PILLAR_COVERAGE_SUMMARY.gap}
        value={gap}
        className="text-red-600"
      />
      <span className="text-xs text-[var(--color-text-muted)]">+</span>
      <SummaryChip
        label={PILLAR_COVERAGE_SUMMARY.partial}
        value={partial}
        className="text-yellow-700"
      />
      <span className="text-xs text-[var(--color-text-muted)]">+</span>
      <SummaryChip
        label={PILLAR_COVERAGE_SUMMARY.covered}
        value={covered}
        className="text-green-700"
      />
    </div>
  );
}

function SummaryChip({
  label,
  value,
  className,
}: {
  label: string;
  value: number;
  className: string;
}) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-2.5 py-1 text-xs">
      <span className={`font-semibold tabular-nums ${className}`}>{value}</span>
      <span className="text-[var(--color-text-muted)]">{label}</span>
    </span>
  );
}

function toPillarRowsFromMatrix(rows: PillarCoverageMatrix[]): PillarRowData[] {
  return rows.map((row) => ({
    id: row.pillarId,
    pillarTopic: row.pillarTopic,
    primaryKeyword: row.primaryKeyword,
    coverageScore: row.coverageScore,
    coveredSubtopics: row.coveredSubtopics,
    totalSubtopics: row.totalSubtopics,
    coverageStatus: row.coverageStatus as NichePillarResult['coverageStatus'],
    strategicPriority: row.strategicPriority,
    hasQuickWins: row.hasQuickWins,
  }));
}

function toPillarRowsFromResults(pillars: NichePillarResult[]): PillarRowData[] {
  return pillars.map((p) => ({
    id: p.id,
    pillarTopic: p.pillarTopic,
    primaryKeyword: p.primaryKeyword,
    pageUrl: p.pageUrl,
    searchIntent: p.searchIntent,
    coverageScore: p.coverageScore,
    coveredSubtopics: p.coveredSubtopicCount,
    totalSubtopics: p.requiredSubtopicCount,
    coverageStatus: p.coverageStatus,
    strategicPriority: p.strategicPriority,
    quickWinCount: p.subtopics.filter((s) => s.isQuickWin).length,
  }));
}

function GroupedPillarCoverage({
  rows,
  summary,
}: {
  rows: PillarRowData[];
  summary: { covered: number; partial: number; gap: number };
}) {
  const grouped = groupPillarsByCoverage(rows);
  const total = rows.length;

  return (
    <div className="overflow-hidden rounded-xl border border-[var(--color-border)]">
      <div className="border-b border-[var(--color-border)] px-4 py-4">
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          Pillar topic page coverage
        </h3>
        <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
          Your {total} selected pillar topics — not the larger discovery pool. Sections follow the
          summary: gaps first, then partial, then covered.
        </p>
      </div>

      <PillarCoverageSummaryBar {...summary} total={total} />

      <div className="divide-y divide-[var(--color-border)]">
        {PILLAR_COVERAGE_ORDER.map((status) => (
          <CoverageSection
            key={status}
            status={status}
            rows={grouped[status]}
            count={summary[status === 'covered' ? 'covered' : status === 'partial' ? 'partial' : 'gap']}
          />
        ))}
      </div>
    </div>
  );
}

function CoverageSection({
  status,
  rows,
  count,
}: {
  status: NichePillarResult['coverageStatus'];
  rows: PillarRowData[];
  count: number;
}) {
  const meta = PILLAR_COVERAGE_SECTION[status];

  return (
    <section className={`border-l-4 ${SECTION_ACCENT[status]}`}>
      <div className="flex flex-wrap items-baseline justify-between gap-2 px-4 py-3">
        <div>
          <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">
            {meta.title}{' '}
            <span className={`tabular-nums ${SECTION_COUNT_COLOR[status]}`}>({count})</span>
          </h4>
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">{meta.detail}</p>
        </div>
      </div>

      {rows.length === 0 ? (
        <p className="px-4 pb-4 text-sm text-[var(--color-text-muted)]">{meta.empty}</p>
      ) : (
        <div className="overflow-x-auto px-4 pb-4">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] text-left">
                <th className="py-2 pr-3 font-medium text-[var(--color-text-secondary)]">Pillar</th>
                <th className="py-2 pr-3 font-medium text-[var(--color-text-secondary)]">On-site page</th>
                <th className="py-2 pr-3 text-right font-medium text-[var(--color-text-secondary)]">Score</th>
                <th className="py-2 pr-3 text-right font-medium text-[var(--color-text-secondary)]">Subtopics</th>
                <th className="py-2 font-medium text-[var(--color-text-secondary)]">Priority</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {rows.map((row) => (
                <PillarRow key={row.id} row={row} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function PillarRow({ row }: { row: PillarRowData }) {
  return (
    <tr className="bg-[var(--color-surface)]">
      <td className="py-3 pr-3 align-top">
        <div className="font-medium text-[var(--color-text-primary)]">{row.pillarTopic}</div>
        {row.primaryKeyword ? (
          <div className="text-xs text-[var(--color-text-muted)]">{row.primaryKeyword}</div>
        ) : null}
        {row.searchIntent ? (
          <div className="mt-0.5 text-xs capitalize text-[var(--color-text-muted)]">{row.searchIntent}</div>
        ) : null}
        {(row.quickWinCount ?? 0) > 0 ? (
          <span className="mt-1 inline-block rounded-full bg-emerald-100 px-1.5 py-0.5 text-xs text-emerald-700">
            {row.quickWinCount} quick win{row.quickWinCount === 1 ? '' : 's'}
          </span>
        ) : null}
        {row.hasQuickWins ? (
          <span className="mt-1 inline-block rounded-full bg-emerald-100 px-1.5 py-0.5 text-xs text-emerald-700">
            Quick wins
          </span>
        ) : null}
      </td>
      <td className="py-3 pr-3 align-top">
        {row.pageUrl ? (
          <a
            href={row.pageUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="block max-w-xs truncate text-xs text-[var(--color-accent)] hover:underline"
            title={row.pageUrl}
          >
            {row.pageUrl}
          </a>
        ) : (
          <span className="text-xs text-[var(--color-text-muted)]">No dedicated page matched</span>
        )}
        <span className="mt-1 block text-[10px] text-[var(--color-text-muted)]">
          {PILLAR_COVERAGE_ROW[row.coverageStatus]}
        </span>
      </td>
      <td className="py-3 pr-3 align-top text-right">
        <ScoreBar score={row.coverageScore} />
      </td>
      <td className="py-3 pr-3 align-top text-right tabular-nums text-[var(--color-text-secondary)]">
        {row.coveredSubtopics}/{row.totalSubtopics}
      </td>
      <td className="py-3 align-top">
        <span
          className={`rounded-full px-2 py-0.5 text-xs font-medium ${PRIORITY_BADGE[row.strategicPriority] ?? ''}`}
        >
          {row.strategicPriority.replace('_', ' ')}
        </span>
      </td>
    </tr>
  );
}

export function CoverageMatrixTable({
  pillars,
  coverageFallback = [],
  totalPillarsIdentified = 0,
  pillarsCovered,
  pillarsPartial,
  pillarsGap,
}: Props) {
  if (pillars.length === 0 && coverageFallback.length > 0) {
    const counts = countPillarCoverage(coverageFallback);
    return <GroupedPillarCoverage rows={toPillarRowsFromMatrix(coverageFallback)} summary={counts} />;
  }

  if (pillars.length === 0) {
    const savedCountsOnly = totalPillarsIdentified > 0;
    return (
      <div className="rounded-xl border border-[var(--color-border)] p-8 text-center text-sm text-[var(--color-text-muted)]">
        <p className="font-medium text-[var(--color-text-primary)]">
          {savedCountsOnly ? 'Pillar details missing' : 'No pillars found'}
        </p>
        <p className="mt-2">
          {savedCountsOnly
            ? `The summary shows ${totalPillarsIdentified} pillar(s), but rows were not saved. Run Re-analyze once.`
            : 'If the sitemap lists only the homepage, pillars are inferred from schema.org on that page (knowsAbout, services). v1 does not crawl every internal link yet.'}
        </p>
      </div>
    );
  }

  const summary = {
    covered: pillarsCovered ?? countPillarCoverage(pillars).covered,
    partial: pillarsPartial ?? countPillarCoverage(pillars).partial,
    gap: pillarsGap ?? countPillarCoverage(pillars).gap,
  };

  return <GroupedPillarCoverage rows={toPillarRowsFromResults(pillars)} summary={summary} />;
}

function ScoreBar({ score }: { score: number }) {
  const pct = Math.round(score);
  const color = pct >= 70 ? 'bg-green-500' : pct >= 40 ? 'bg-yellow-400' : 'bg-red-400';
  return (
    <div className="flex items-center justify-end gap-2">
      <div className="h-1.5 w-20 overflow-hidden rounded-full bg-[var(--color-border)]">
        <div className={`h-full rounded-full ${color}`} style={{ width: `${pct}%` }} />
      </div>
      <span className="w-7 text-xs tabular-nums text-[var(--color-text-secondary)]">{pct}</span>
    </div>
  );
}
