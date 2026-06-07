'use client';

import { useEffect, useState } from 'react';
import { getNicheAnalysisDetails, type FusedSiteUnderstanding } from '@/lib/seo-api';
import { FusionInsightsStack } from '@/components/niche-analyzer/FusionInsightsStack';

type Props = {
  profileId: string;
  projectId?: string;
  accessToken?: string | null;
  pollIntervalMs?: number;
  /** Hide the full candidate matrix (shown in analysis steps instead). */
  showMatrix?: boolean;
};

export function FusionSnapshotSection({
  profileId,
  projectId,
  accessToken,
  pollIntervalMs,
  showMatrix = false,
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
      <p className="text-sm text-[var(--color-text-muted)]">Loading discovery signals…</p>
    );
  }

  if (!fusion || fusion.allCandidates.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-[var(--color-border)] px-4 py-6 text-center text-sm text-[var(--color-text-secondary)]">
        <p className="font-medium text-[var(--color-text-primary)]">No fusion snapshot yet</p>
        <p className="mt-1">
          Re-analyze once to save the full candidate pool — every topic the engine considered during
          fusion.
        </p>
      </div>
    );
  }

  return (
    <FusionInsightsStack
      fusion={fusion}
      projectId={projectId}
      profileId={profileId}
      accessToken={accessToken}
      showMatrix={showMatrix}
    />
  );
}
