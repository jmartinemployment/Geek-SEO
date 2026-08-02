'use client';

import type { SiteAnalysisStepLogEntry, SiteTopicProfile } from '@/lib/seo-api';
import { PillarActionPanel } from '@/components/site-analyzer/PillarActionPanel';
import { EntityCoveragePanel } from '@/components/site-analyzer/EntityCoveragePanel';
import { InternalLinkGraphPanel } from '@/components/site-analyzer/InternalLinkGraphPanel';
import { TopicCandidateMatrix } from '@/components/site-analyzer/TopicCandidateMatrix';
import { GscSilentPillarPanel } from '@/components/site-analyzer/GscSilentPillarPanel';
import { PillarMapLauncher } from '@/components/site-analyzer/PillarMapLauncher';
import { LocalGeographyPanel } from '@/components/site-analyzer/LocalGeographyPanel';
import { CrawlResultsPanel } from '@/components/site-analyzer/CrawlResultsPanel';

type Props = {
  fusion: SiteTopicProfile;
  projectId?: string;
  profileId?: string;
  accessToken?: string | null;
  showMatrix?: boolean;
  steps?: SiteAnalysisStepLogEntry[];
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
