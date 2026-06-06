'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { getLatestNicheProfile, type NicheProfileResult } from '@/lib/seo-api';

type Props = {
  projectId: string;
  accessToken?: string | null;
};

export function NicheStrategyContextBanner({ projectId, accessToken }: Readonly<Props>) {
  const [profile, setProfile] = useState<NicheProfileResult | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void getLatestNicheProfile(projectId, accessToken)
      .then((data) => {
        if (!cancelled) setProfile(data);
      })
      .catch(() => {
        if (!cancelled) setProfile(null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, accessToken]);

  if (loading) return null;

  if (!profile || profile.status !== 'complete') {
    return (
      <section className="mt-6 rounded-xl border border-dashed border-[var(--color-border)] bg-[var(--color-surface-muted)]/20 px-5 py-4">
        <p className="text-sm font-medium text-[var(--color-text-primary)]">
          No niche analysis yet
        </p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          Run a site scan first so decaying pages can be grouped by topic — that makes it easier to
          choose whether to refresh an existing page or write something new.
        </p>
        <Link
          href="/app/strategy/niche-analyzer"
          className="mt-3 inline-block text-sm font-medium text-[var(--color-accent)] hover:underline"
        >
          Open niche analyzer
        </Link>
      </section>
    );
  }

  const topicalMapHref = `/app/strategy/topical-map?projectId=${projectId}&mode=niche&autogen=1`;

  return (
    <section className="mt-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-muted)]/30 px-5 py-4">
      <div>
        <p className="text-sm font-medium text-[var(--color-text-primary)]">
          Your site&apos;s main topics
        </p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
          {profile.primaryNiche}. You have strong pages for {profile.pillarsCovered} topic
          {profile.pillarsCovered === 1 ? '' : 's'}, partial coverage on {profile.pillarsPartial},
          and {profile.pillarsGap} topic{profile.pillarsGap === 1 ? '' : 's'} with little or no
          dedicated content. When a URL below is losing clicks, check its topic label — a &quot;gap&quot;
          topic may need a new page, not just an edit.
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        <Link
          href="/app/strategy/niche-analyzer"
          className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-xs font-medium hover:bg-[var(--color-surface-muted)]"
        >
          View analysis
        </Link>
        {profile.pillarsGap > 0 ? (
          <Link
            href={topicalMapHref}
            className="rounded-lg bg-[var(--color-accent)] px-3 py-2 text-xs font-medium text-white hover:opacity-90"
          >
            Plan missing topics
          </Link>
        ) : null}
      </div>
    </section>
  );
}
