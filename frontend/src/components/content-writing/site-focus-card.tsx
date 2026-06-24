'use client';

import type { SiteWritingFocus } from '@/lib/seo-api';

function InsightCard({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/20 p-3">
      <h4 className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
        {title}
      </h4>
      <div className="mt-2 text-xs text-[var(--color-text-secondary)]">{children}</div>
    </section>
  );
}

type Props = {
  siteFocus: SiteWritingFocus;
  capturedAt?: string | null;
  articleKeyword?: string;
  serpKeyword?: string | null;
};

export function SiteFocusCard({
  siteFocus,
  capturedAt,
  articleKeyword,
  serpKeyword,
}: Props) {
  const capturedLabel = capturedAt
    ? new Date(capturedAt).toLocaleString()
    : siteFocus.capturedAt
      ? new Date(siteFocus.capturedAt).toLocaleString()
      : null;

  return (
    <InsightCard title="Site writing focus">
      <p className="font-medium text-[var(--color-text-primary)]">
        {siteFocus.siteName}
        {siteFocus.siteUrl ? (
          <span className="block font-normal text-[var(--color-text-muted)]">{siteFocus.siteUrl}</span>
        ) : null}
      </p>
      {siteFocus.primaryNiche ? <p className="mt-2">Niche: {siteFocus.primaryNiche}</p> : null}
      {siteFocus.businessSummary ? (
        <p className="mt-2">{siteFocus.businessSummary}</p>
      ) : null}
      {siteFocus.matchedPillarTopic ? (
        <p className="mt-2">
          Pillar: {siteFocus.matchedPillarTopic}
          {siteFocus.matchedPillarIntent ? ` · ${siteFocus.matchedPillarIntent}` : ''}
        </p>
      ) : null}
      {siteFocus.geoAnchorNodes?.length ? (
        <p className="mt-2">Geo: {siteFocus.geoAnchorNodes.slice(0, 4).join(' · ')}</p>
      ) : null}
      {siteFocus.gapTopics?.length ? (
        <p className="mt-2">Gaps: {siteFocus.gapTopics.join(', ')}</p>
      ) : null}
      {articleKeyword && serpKeyword && articleKeyword !== serpKeyword ? (
        <p className="mt-2 text-amber-800">
          Article keyword &quot;{articleKeyword}&quot; · SERP from &quot;{serpKeyword}&quot;
        </p>
      ) : null}
      {capturedLabel ? (
        <p className="mt-2 text-[10px] text-[var(--color-text-muted)]">
          Snapshot captured {capturedLabel}
        </p>
      ) : null}
    </InsightCard>
  );
}
