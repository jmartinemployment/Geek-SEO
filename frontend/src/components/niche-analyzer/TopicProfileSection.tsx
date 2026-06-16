'use client';

import { useEffect, useState } from 'react';
import {
  getNicheAnalysisDetails,
  type NicheAnalysisStepLogEntry,
  type SiteTopicProfile,
} from '@/lib/seo-api';
import { TopicInsightsStack } from '@/components/niche-analyzer/TopicInsightsStack';

type Props = {
  profileId: string;
  projectId?: string;
  accessToken?: string | null;
  /** Hide the full candidate matrix (shown in analysis steps instead). */
  showMatrix?: boolean;
};

export function TopicProfileSection({
  profileId,
  projectId,
  accessToken,
  showMatrix = false,
}: Readonly<Props>) {
  const [fusion, setFusion] = useState<SiteTopicProfile | null>(null);
  const [steps, setSteps] = useState<NicheAnalysisStepLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function load(showSpinner: boolean) {
      if (showSpinner) setLoading(true);
      try {
        const data = await getNicheAnalysisDetails(profileId, accessToken);
        if (cancelled) return;

        setSteps(data.steps ?? []);

        if (data.fusionSnapshot && data.fusionSnapshot.allCandidates.length > 0) {
          setFusion(data.fusionSnapshot);
        } else {
          setFusion(null);
        }
      } catch {
        if (!cancelled && showSpinner) {
          setFusion(null);
        }
      } finally {
        if (!cancelled && showSpinner) setLoading(false);
      }
    }

    void load(true);

    return () => {
      cancelled = true;
    };
  }, [profileId, accessToken]);

  async function handleRefreshSignals() {
    setRefreshing(true);
    try {
      const data = await getNicheAnalysisDetails(profileId, accessToken);
      setSteps(data.steps ?? []);
      if (data.fusionSnapshot && data.fusionSnapshot.allCandidates.length > 0) {
        setFusion(data.fusionSnapshot);
      } else {
        setFusion(null);
      }
    } finally {
      setRefreshing(false);
    }
  }

  if (loading) {
    return (
      <p className="text-sm text-[var(--color-text-muted)]">Loading discovery signals…</p>
    );
  }

  if (!fusion || fusion.allCandidates.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-[var(--color-border)] px-4 py-6 text-center text-sm text-[var(--color-text-secondary)]">
        <p className="font-medium text-[var(--color-text-primary)]">No topic inventory yet</p>
        <p className="mt-1">
          Re-analyze to persist the full candidate pool — every topic the engine considered during
          pillar selection.
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="mb-2 flex justify-end">
        <button
          type="button"
          onClick={() => {
            void handleRefreshSignals();
          }}
          disabled={refreshing}
          className="rounded-md border border-[var(--color-border)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] transition-colors hover:text-[var(--color-text-primary)] disabled:cursor-not-allowed disabled:opacity-50"
        >
          {refreshing ? 'Refreshing…' : 'Refresh signals'}
        </button>
      </div>
      <TopicInsightsStack
        fusion={fusion}
        projectId={projectId}
        profileId={profileId}
        accessToken={accessToken}
        showMatrix={showMatrix}
        steps={steps}
      />
    </>
  );
}
