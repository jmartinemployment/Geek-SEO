'use client';

import type { FusedSiteUnderstanding } from '@/lib/seo-api';
import { FusionActionPanel } from '@/components/niche-analyzer/FusionActionPanel';
import { EntityCoveragePanel } from '@/components/niche-analyzer/EntityCoveragePanel';
import { InternalLinkGraphPanel } from '@/components/niche-analyzer/InternalLinkGraphPanel';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';
import { GscSilentPillarPanel } from '@/components/niche-analyzer/GscSilentPillarPanel';
import { FusionPillarMapLauncher } from '@/components/niche-analyzer/FusionPillarMapLauncher';

type Props = {
  fusion: FusedSiteUnderstanding;
  projectId?: string;
  profileId?: string;
  accessToken?: string | null;
  showMatrix?: boolean;
};

export function FusionInsightsStack({
  fusion,
  projectId,
  profileId,
  accessToken,
  showMatrix = true,
}: Readonly<Props>) {
  return (
    <div className="space-y-4">
      <FusionPillarMapLauncher fusion={fusion} projectId={projectId} />
      {profileId ? (
        <GscSilentPillarPanel profileId={profileId} fusion={fusion} accessToken={accessToken} />
      ) : null}
      <FusionActionPanel fusion={fusion} projectId={projectId} accessToken={accessToken} />
      <EntityCoveragePanel fusion={fusion} />
      <InternalLinkGraphPanel fusion={fusion} />
      {showMatrix ? <TopicCandidateMatrix fusion={fusion} /> : null}
    </div>
  );
}
