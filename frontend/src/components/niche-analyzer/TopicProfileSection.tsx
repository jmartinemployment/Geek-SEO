'use client';

import { useEffect, useState } from 'react';
import {
  getAllNicheTopicCandidates,
  getNicheAnalysisDetails,
  type NicheAnalysisStepLogEntry,
  type SiteTopicProfile,
} from '@/lib/seo-api';
import { fusionFromTopicCandidates } from '@/components/niche-analyzer/candidates-to-fusion';
import { TopicInsightsStack } from '@/components/niche-analyzer/TopicInsightsStack';

type Props = {
  profileId: string;
  projectId?: string;
  accessToken?: string | null;
  pollIntervalMs?: number;
  /** Hide the full candidate matrix (shown in analysis steps instead). */
  showMatrix?: boolean;
};

export function TopicProfileSection({
  profileId,
  projectId,
  accessToken,
  pollIntervalMs,
  showMatrix = false,
}: Readonly<Props>) {
  const [fusion, setFusion] = useState<SiteTopicProfile | null>(null);
  const [steps, setSteps] = useState<NicheAnalysisStepLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [inventorySource, setInventorySource] = useState<'fusion' | 'candidates' | null>(null);

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
          setInventorySource('fusion');
          return;
        }

        const rows = await getAllNicheTopicCandidates(profileId, accessToken);
        if (cancelled) return;

        const fromInventory = fusionFromTopicCandidates(rows);
        if (fromInventory) {
          setFusion(fromInventory);
          setInventorySource('candidates');
        } else {
          setFusion(null);
          setInventorySource(null);
        }
      } catch {
        if (!cancelled && showSpinner) {
          setFusion(null);
          setInventorySource(null);
        }
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
      {inventorySource === 'candidates' ? (
        <p className="mb-3 text-xs text-[var(--color-text-muted)]">
          Showing topic inventory from relational candidates (fusion archive not loaded).
        </p>
      ) : null}
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
