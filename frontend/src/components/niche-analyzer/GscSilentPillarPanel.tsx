'use client';

import { useEffect, useState } from 'react';
import { getNicheAnalysisDetails, type SiteTopicProfile } from '@/lib/seo-api';
import { resolveGscSilentPillars } from '@/components/niche-analyzer/pillar-provenance';

type Props = {
  profileId: string;
  fusion?: SiteTopicProfile | null;
  accessToken?: string | null;
};

export function GscSilentPillarPanel({
  profileId,
  fusion,
  accessToken,
}: Readonly<Props>) {
  const [silent, setSilent] = useState<{ slug: string; name: string }[]>([]);

  useEffect(() => {
    let cancelled = false;

    void getNicheAnalysisDetails(profileId, accessToken)
      .then((data) => {
        if (cancelled) return;
        setSilent(resolveGscSilentPillars(data.steps, data.fusionSnapshot ?? fusion));
      })
      .catch(() => {
        if (!cancelled) setSilent([]);
      });

    return () => {
      cancelled = true;
    };
  }, [profileId, accessToken, fusion]);

  if (silent.length === 0) return null;

  return (
    <section className="rounded-xl border border-amber-200 bg-amber-50/80 px-5 py-4">
      <h3 className="text-sm font-semibold text-amber-950">GSC silent pillars</h3>
      <p className="mt-0.5 text-xs text-amber-900/90">
        These selected pillars have no matching Search Console query cluster yet — the site may not
        earn impressions for them, or content is too thin for Google to associate queries.
      </p>
      <ul className="mt-3 flex flex-wrap gap-2">
        {silent.map((pillar) => (
          <li
            key={pillar.slug}
            className="rounded-full border border-amber-300 bg-white px-2.5 py-1 text-xs font-medium text-amber-950"
          >
            {pillar.name}
          </li>
        ))}
      </ul>
    </section>
  );
}
