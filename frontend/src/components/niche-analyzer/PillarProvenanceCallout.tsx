'use client';

import { useEffect, useState } from 'react';
import { getNicheAnalysisDetails } from '@/lib/seo-api';
import { buildPillarProvenanceSummary } from '@/components/niche-analyzer/pillar-provenance';

type Props = {
  profileId: string;
  accessToken?: string | null;
  pillarCount: number;
};

export function PillarProvenanceCallout({
  profileId,
  accessToken,
  pillarCount,
}: Readonly<Props>) {
  const [summary, setSummary] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void getNicheAnalysisDetails(profileId, accessToken)
      .then((data) => {
        if (cancelled) return;
        setSummary(buildPillarProvenanceSummary(data.steps, pillarCount));
      })
      .catch(() => {
        // Optional context — ignore if step log unavailable
      });

    return () => {
      cancelled = true;
    };
  }, [profileId, accessToken, pillarCount]);

  if (!summary) return null;

  return (
    <div className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900">
      <p className="font-medium">Where these pillars came from</p>
      <p className="mt-1 text-blue-800">{summary}</p>
    </div>
  );
}
