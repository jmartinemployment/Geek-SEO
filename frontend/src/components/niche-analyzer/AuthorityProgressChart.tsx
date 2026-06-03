'use client';

import type { AuthorityProgressPoint } from '@/lib/seo-api';

type Props = { points: AuthorityProgressPoint[] };

export function AuthorityProgressChart({ points }: Props) {
  if (points.length === 0) {
    return (
      <div className="flex h-32 items-center justify-center rounded-xl border border-[var(--color-border)] text-sm text-[var(--color-text-muted)]">
        No completed analyses yet — run Analyze, then re-analyze monthly to track authority over time.
      </div>
    );
  }

  if (points.length === 1) {
    const p = points[0]!;
    const date = new Date(p.snapshotDate).toLocaleDateString('en-US', {
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    });
    return (
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
        <p className="text-sm font-medium text-[var(--color-text-primary)]">Authority snapshot</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          First data point from {date}. Re-analyze monthly to unlock the trend chart.
        </p>
        <div className="mt-4 grid gap-4 sm:grid-cols-4">
          <Metric label="Topical authority" value={`${Math.round(p.topicalAuthorityScore)}`} />
          <Metric label="Pillars covered" value={`${p.pillarsCovered}`} />
          <Metric label="Subtopics covered" value={`${p.totalSubtopicsCovered}`} />
          <Metric label="Gaps" value={`${p.totalGaps}`} />
        </div>
      </div>
    );
  }

  const max = Math.max(...points.map((p) => p.topicalAuthorityScore), 100);
  const width = 600;
  const height = 140;
  const padX = 40;
  const padY = 16;
  const chartW = width - padX * 2;
  const chartH = height - padY * 2;

  const pts = points.map((p, i) => ({
    x: padX + (i / (points.length - 1)) * chartW,
    y: padY + (1 - p.topicalAuthorityScore / max) * chartH,
    label: new Date(p.snapshotDate).toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
    score: p.topicalAuthorityScore,
  }));

  const pathD = pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
  const areaD = `${pathD} L ${pts.at(-1)!.x} ${padY + chartH} L ${pts[0].x} ${padY + chartH} Z`;

  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <p className="mb-3 text-sm font-medium text-[var(--color-text-primary)]">Authority progression</p>
      <svg viewBox={`0 0 ${width} ${height}`} className="w-full" aria-label="Authority progression chart">
        {/* Grid lines */}
        {[0, 25, 50, 75, 100].map((v) => {
          const y = padY + (1 - v / max) * chartH;
          return (
            <g key={v}>
              <line x1={padX} x2={padX + chartW} y1={y} y2={y} stroke="var(--color-border)" strokeWidth="0.5" />
              <text x={padX - 4} y={y + 4} textAnchor="end" fontSize="10" fill="var(--color-text-muted)">{v}</text>
            </g>
          );
        })}

        {/* Area fill */}
        <path d={areaD} fill="var(--color-accent)" fillOpacity="0.08" />

        {/* Line */}
        <path d={pathD} fill="none" stroke="var(--color-accent)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />

        {/* Points + labels */}
        {pts.map((p) => (
          <g key={p.x}>
            <circle cx={p.x} cy={p.y} r="3.5" fill="var(--color-accent)" />
            <text x={p.x} y={height - 2} textAnchor="middle" fontSize="9" fill="var(--color-text-muted)">{p.label}</text>
          </g>
        ))}
      </svg>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-secondary)] px-4 py-3">
      <p className="text-xs text-[var(--color-text-muted)]">{label}</p>
      <p className="mt-1 text-xl font-semibold tabular-nums text-[var(--color-text-primary)]">{value}</p>
    </div>
  );
}
