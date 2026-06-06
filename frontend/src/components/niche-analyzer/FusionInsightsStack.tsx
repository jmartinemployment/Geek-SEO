'use client';

import type { FusedSiteUnderstanding } from '@/lib/seo-api';
import { FusionActionPanel } from '@/components/niche-analyzer/FusionActionPanel';
import { EntityCoveragePanel } from '@/components/niche-analyzer/EntityCoveragePanel';
import { InternalLinkGraphPanel } from '@/components/niche-analyzer/InternalLinkGraphPanel';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';

type Props = {
  fusion: FusedSiteUnderstanding;
  projectId?: string;
  accessToken?: string | null;
  showMatrix?: boolean;
};

export function FusionInsightsStack({
  fusion,
  projectId,
  accessToken,
  showMatrix = true,
}: Readonly<Props>) {
  return (
    <div className="space-y-4">
      <FusionActionPanel fusion={fusion} projectId={projectId} accessToken={accessToken} />
      <EntityCoveragePanel fusion={fusion} />
      <InternalLinkGraphPanel fusion={fusion} />
      {showMatrix ? <TopicCandidateMatrix fusion={fusion} /> : null}
    </div>
  );
}
