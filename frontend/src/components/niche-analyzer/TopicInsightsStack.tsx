'use client';

import type { NicheAnalysisStepLogEntry, SiteTopicProfile } from '@/lib/seo-api';
import { PillarActionPanel } from '@/components/niche-analyzer/PillarActionPanel';
import { EntityCoveragePanel } from '@/components/niche-analyzer/EntityCoveragePanel';
import { InternalLinkGraphPanel } from '@/components/niche-analyzer/InternalLinkGraphPanel';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';
import { GscSilentPillarPanel } from '@/components/niche-analyzer/GscSilentPillarPanel';
import { PillarMapLauncher } from '@/components/niche-analyzer/PillarMapLauncher';
import { LocalGeographyPanel } from '@/components/niche-analyzer/LocalGeographyPanel';
import { CrawlResultsPanel } from '@/components/niche-analyzer/CrawlResultsPanel';

type Props = {
  fusion: SiteTopicProfile;
  projectId?: string;
  profileId?: string;
  accessToken?: string | null;
  showMatrix?: boolean;
  steps?: NicheAnalysisStepLogEntry[];
};

export function TopicInsightsStack({
  fusion,
  projectId,
  profileId,
  accessToken,
  showMatrix = true,
  steps = [],
}: Readonly<Props>) {
  return (
    <div className="space-y-4">
      <PillarMapLauncher fusion={fusion} projectId={projectId} />
      {steps.length > 0 ? <CrawlResultsPanel steps={steps} /> : null}
      <LocalGeographyPanel fusion={fusion} />
      {profileId ? (
        <GscSilentPillarPanel profileId={profileId} fusion={fusion} accessToken={accessToken} />
      ) : null}
      <PillarActionPanel fusion={fusion} projectId={projectId} />
      <EntityCoveragePanel fusion={fusion} />
      <InternalLinkGraphPanel fusion={fusion} />
      {showMatrix ? <TopicCandidateMatrix fusion={fusion} /> : null}
    </div>
  );
}
