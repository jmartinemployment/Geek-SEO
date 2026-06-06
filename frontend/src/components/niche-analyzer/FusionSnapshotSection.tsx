'use client';

import { useEffect, useState } from 'react';
import { getNicheAnalysisDetails, type FusedSiteUnderstanding } from '@/lib/seo-api';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';

type Props = {
  profileId: string;
  accessToken?: string | null;
  pollIntervalMs?: number;
};

export function FusionSnapshotSection({
  profileId,
  accessToken,
  pollIntervalMs,
}: Readonly<Props>) {
  const [fusion, setFusion] = useState<FusedSiteUnderstanding | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function load(showSpinner: boolean) {
      if (showSpinner) setLoading(true);
      try {
        const data = await getNicheAnalysisDetails(profileId, accessToken);
        if (!cancelled) setFusion(data.fusionSnapshot ?? null);
      } catch {
        if (!cancelled && showSpinner) setFusion(null);
      } finally {
        if (!cancelled && showSpinner) setLoading(false);
      }
    }

    void load(true);

    if (!pollIntervalMs || pollIntervalMs <= 0) {
      return () => {
        cancelled = true;
      };
    }

    const id = window.setInterval(() => {
      void load(false);
    }, pollIntervalMs);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [profileId, accessToken, pollIntervalMs]);

  if (loading) {
    return (
      <p className="text-sm text-[var(--color-text-muted)]">Loading topic candidate matrix…</p>
    );
  }

  if (!fusion || fusion.allCandidates.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-[var(--color-border)] px-4 py-6 text-center text-sm text-[var(--color-text-secondary)]">
        <p className="font-medium text-[var(--color-text-primary)]">No fusion snapshot yet</p>
        <p className="mt-1">
          Re-analyze once to save the full candidate pool — every topic the engine considered before
          applying the pillar cap.
        </p>
      </div>
    );
  }

  return <TopicCandidateMatrix fusion={fusion} />;
}
