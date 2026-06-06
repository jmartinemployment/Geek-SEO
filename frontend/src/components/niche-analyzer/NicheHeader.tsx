'use client';

import type { NicheProfileResult } from '@/lib/seo-api';

type Props = { profile: NicheProfileResult };

const COVERAGE_COLORS: Record<string, string> = {
  covered: 'text-green-600',
  partial: 'text-yellow-600',
  gap: 'text-red-500',
};

export function NicheHeader({ profile }: Props) {
  const score = Math.round(profile.topicalAuthorityScore);
  const scoreColor =
    score >= 70 ? 'text-green-600' : score >= 40 ? 'text-yellow-500' : 'text-red-500';

  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-xs font-medium uppercase tracking-wider text-[var(--color-text-muted)]">
            Core Niche
          </p>
          <h2 className="mt-1 text-xl font-semibold text-[var(--color-text-primary)]">
            {profile.primaryNiche}
          </h2>
          {profile.nicheDescription && (
            <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
              {profile.nicheDescription}
            </p>
          )}
          <div className="mt-2 flex flex-wrap gap-1.5">
            {profile.nicheTags.map((tag) => (
              <span
                key={tag}
                className="rounded-full bg-[var(--color-accent-muted)] px-2 py-0.5 text-xs text-[var(--color-accent)]"
              >
                {tag}
              </span>
            ))}
          </div>
        </div>

        <div className="flex shrink-0 flex-col items-center gap-1 rounded-xl border border-[var(--color-border)] px-6 py-4">
          <p className="text-xs text-[var(--color-text-muted)]">Topical Authority</p>
          <p className={`text-4xl font-bold tabular-nums ${scoreColor}`}>{score}</p>
          <p className="text-xs text-[var(--color-text-muted)]">/ 100</p>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Stat label="Pillars" value={profile.totalPillarsIdentified} />
        <Stat label="Covered" value={profile.pillarsCovered} className="text-green-600" />
        <Stat label="Partial" value={profile.pillarsPartial} className="text-yellow-500" />
        <Stat label="Gaps" value={profile.pillarsGap} className="text-red-500" />
      </div>

      <div className="mt-3 flex gap-4 text-xs text-[var(--color-text-muted)]">
        <span>Competition: <strong>{profile.competitionLevel}</strong></span>
        {profile.analyzedAt && (
          <span>Analyzed: <strong>{new Date(profile.analyzedAt).toLocaleDateString()}</strong></span>
        )}
      </div>
    </div>
  );
}

function Stat({
  label,
  value,
  className = 'text-[var(--color-text-primary)]',
}: {
  label: string;
  value: number;
  className?: string;
}) {
  return (
    <div className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-center">
      <p className={`text-2xl font-bold tabular-nums ${className}`}>{value}</p>
      <p className="text-xs text-[var(--color-text-muted)]">{label}</p>
    </div>
  );
}
