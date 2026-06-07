'use client';

import type { SiteTopicProfile } from '@/lib/seo-api';
import { PillarActionPanel } from '@/components/niche-analyzer/PillarActionPanel';
import { EntityCoveragePanel } from '@/components/niche-analyzer/EntityCoveragePanel';
import { InternalLinkGraphPanel } from '@/components/niche-analyzer/InternalLinkGraphPanel';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';
import { GscSilentPillarPanel } from '@/components/niche-analyzer/GscSilentPillarPanel';
import { PillarMapLauncher } from '@/components/niche-analyzer/PillarMapLauncher';
import { LocalGeographyPanel } from '@/components/niche-analyzer/LocalGeographyPanel';

type Props = {
  fusion: SiteTopicProfile;
  projectId?: string;
  profileId?: string;
  accessToken?: string | null;
  showMatrix?: boolean;
};

export function TopicInsightsStack({
  fusion,
  projectId,
  profileId,
  accessToken,
  showMatrix = true,
}: Readonly<Props>) {
  return (
    <div className="space-y-4">
      <PillarMapLauncher fusion={fusion} projectId={projectId} />
      <LocalGeographyPanel fusion={fusion} />
      {profileId ? (
        <GscSilentPillarPanel profileId={profileId} fusion={fusion} accessToken={accessToken} />
      ) : null}
      <PillarActionPanel fusion={fusion} projectId={projectId} accessToken={accessToken} />
      <EntityCoveragePanel fusion={fusion} />
      <InternalLinkGraphPanel fusion={fusion} />
      {showMatrix ? <TopicCandidateMatrix fusion={fusion} /> : null}
    </div>
  );
}
