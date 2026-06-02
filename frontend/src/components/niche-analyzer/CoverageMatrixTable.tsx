'use client';

import type { NichePillarResult } from '@/lib/seo-api';

type Props = { pillars: NichePillarResult[] };

const PRIORITY_BADGE: Record<string, string> = {
  must_have: 'bg-red-100 text-red-700',
  high_value: 'bg-yellow-100 text-yellow-700',
  expansion: 'bg-blue-100 text-blue-700',
};

const COVERAGE_BADGE: Record<string, string> = {
  covered: 'bg-green-100 text-green-700',
  partial: 'bg-yellow-100 text-yellow-700',
  gap: 'bg-red-100 text-red-700',
};

export function CoverageMatrixTable({ pillars }: Props) {
  if (pillars.length === 0) {
    return (
      <div className="rounded-xl border border-[var(--color-border)] p-8 text-center text-sm text-[var(--color-text-muted)]">
        No pillars found.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-[var(--color-border)]">
      <table className="w-full text-sm">
        <thead className="border-b border-[var(--color-border)] bg-[var(--color-surface-secondary)]">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Pillar</th>
            <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Intent</th>
            <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Score</th>
            <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Subtopics</th>
            <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Coverage</th>
            <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Priority</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-[var(--color-border)]">
          {pillars.map((p) => {
            const quickWins = p.subtopics.filter((s) => s.isQuickWin).length;
            return (
              <tr key={p.id} className="bg-[var(--color-surface)] hover:bg-[var(--color-surface-hover)]">
                <td className="px-4 py-3">
                  <div className="font-medium text-[var(--color-text-primary)]">{p.pillarTopic}</div>
                  {p.primaryKeyword && (
                    <div className="text-xs text-[var(--color-text-muted)]">{p.primaryKeyword}</div>
                  )}
                  {quickWins > 0 && (
                    <span className="mt-0.5 inline-block rounded-full bg-emerald-100 px-1.5 py-0.5 text-xs text-emerald-700">
                      {quickWins} quick win{quickWins > 1 ? 's' : ''}
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-[var(--color-text-secondary)] capitalize">{p.searchIntent}</td>
                <td className="px-4 py-3 text-right">
                  <ScoreBar score={p.coverageScore} />
                </td>
                <td className="px-4 py-3 text-right text-[var(--color-text-secondary)]">
                  {p.coveredSubtopicCount}/{p.requiredSubtopicCount}
                </td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${COVERAGE_BADGE[p.coverageStatus] ?? ''}`}>
                    {p.coverageStatus}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${PRIORITY_BADGE[p.strategicPriority] ?? ''}`}>
                    {p.strategicPriority.replace('_', ' ')}
                  </span>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
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
