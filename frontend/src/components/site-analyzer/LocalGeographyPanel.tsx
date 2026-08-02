'use client';

import type { SiteTopicProfile, LocalGeographyAnalysis } from '@/lib/seo-api';

type Props = {
  fusion: SiteTopicProfile;
};

function LocalGeographyContent({ local }: { local: LocalGeographyAnalysis }) {
  if (!local.isLocalBusiness) return null;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-4">
      <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">Service areas on your site</h3>
      <p className="mt-1 text-xs text-[var(--color-text-muted)]">
        Checks whether counties or cities listed on your site also have their own landing page
        (for example <span className="font-medium">/locations/broward-county</span>). Uses only
        what we can read from your website — no extra accounts to connect.
      </p>

      {local.areasServed.length > 0 ? (
        <div className="mt-3">
          <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">
            Counties &amp; areas listed on your site
          </p>
          <ul className="mt-1 flex flex-wrap gap-1.5">
            {local.areasServed.map((area) => (
              <li
                key={area}
                className="rounded-full border border-[var(--color-border)] bg-[var(--color-surface-muted)]/50 px-2 py-0.5 text-xs text-[var(--color-text-secondary)]"
              >
                {area}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {local.locationPagesFound.length > 0 ? (
        <div className="mt-3">
          <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">
            Location pages we found on your site ({local.locationPagesFound.length})
          </p>
          <ul className="mt-1 space-y-1 text-xs text-[var(--color-text-secondary)]">
            {local.locationPagesFound.slice(0, 8).map((page) => (
              <li key={page.slug}>
                <span className="font-medium text-[var(--color-text-primary)]">{page.name}</span>
                {page.url ? (
                  <span className="text-[var(--color-text-muted)]"> — {page.url}</span>
                ) : null}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {local.gaps.length > 0 ? (
        <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50/60 px-3 py-2">
          <p className="text-xs font-medium text-amber-900">
            {local.gaps.length} area{local.gaps.length === 1 ? '' : 's'} you claim to serve but
            don&apos;t have a dedicated page for yet
          </p>
          <ul className="mt-2 space-y-1.5 text-xs text-amber-950">
            {local.gaps.map((gap) => (
              <li key={gap.areaName}>
                <span className="font-medium">{gap.areaName}</span>
                <span className="text-amber-800"> — suggested: {gap.suggestedTitle}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : local.areasServed.length > 0 ? (
        <p className="mt-3 text-xs text-green-800">
          All declared service areas have a matching location page.
        </p>
      ) : null}
    </section>
  );
}

export function LocalGeographyPanel({ fusion }: Readonly<Props>) {
  const local = fusion.localGeography;
  if (!local?.isLocalBusiness) return null;

  return <LocalGeographyContent local={local} />;
}
