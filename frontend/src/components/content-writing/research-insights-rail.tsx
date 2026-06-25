'use client';

import { useMemo } from 'react';
import {
  parseContentWriterKeywordBundle,
  parseSiteWritingFocus,
  type ContentWriterSerpExport,
  type SiteWritingFocus,
} from '@/lib/seo-api';
import { SiteFocusCard } from '@/components/content-writing/site-focus-card';

type Props = {
  keywordBundleJson?: string | null;
  keywordBundleCapturedAt?: string | null;
  siteFocusJson?: string | null;
  siteFocusCapturedAt?: string | null;
  articleKeyword?: string;
  serpKeyword?: string | null;
};

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

function organicItems(exportData: ContentWriterSerpExport) {
  return exportData.serp.filter((item) => item.type?.toLowerCase() === 'organic');
}

function paaQuestions(exportData: ContentWriterSerpExport) {
  return exportData.serp
    .filter((item) => item.type?.toLowerCase() === 'people_also_ask')
    .flatMap((item) => {
      if (item.relatedQuestions?.length) return item.relatedQuestions;
      return item.title ? [item.title] : [];
    })
    .filter((question) => question.trim().length > 0);
}

function relatedSearches(exportData: ContentWriterSerpExport) {
  return exportData.serp
    .filter((item) => item.type?.toLowerCase() === 'related_searches')
    .flatMap((item) => item.relatedQuestions ?? []);
}

export function ResearchInsightsRail({
  keywordBundleJson,
  keywordBundleCapturedAt,
  siteFocusJson,
  siteFocusCapturedAt,
  articleKeyword,
  serpKeyword,
}: Props) {
  const exportData = useMemo(
    () => parseContentWriterKeywordBundle(keywordBundleJson),
    [keywordBundleJson],
  );

  const organic = useMemo(
    () => (exportData ? organicItems(exportData) : []),
    [exportData],
  );
  const paa = useMemo(() => (exportData ? paaQuestions(exportData) : []), [exportData]);
  const pasf = useMemo(
    () => (exportData ? relatedSearches(exportData) : []),
    [exportData],
  );
  const siteFocus: SiteWritingFocus | null = useMemo(
    () => parseSiteWritingFocus(siteFocusJson),
    [siteFocusJson],
  );

  const bundleCapturedLabel = keywordBundleCapturedAt
    ? new Date(keywordBundleCapturedAt).toLocaleString()
    : null;

  if (!exportData || organic.length === 0) {
    return (
      <div className="border-t px-3 py-4 text-xs text-amber-800 xl:px-4">
        {exportData
          ? 'Frozen keyword bundle has no organic SERP results for insights.'
          : 'Frozen keyword bundle is missing — re-open Content Writing from Site Analyzer with site_profile.'}
      </div>
    );
  }

  return (
    <div className="space-y-3 border-t px-3 py-4 xl:px-4">
      <h3 className="text-xs font-semibold xl:text-sm">SERP research</h3>
      <p className="text-xs text-[var(--color-text-muted)]">
        Frozen at handoff · {exportData.keyword}
        {exportData.status ? ` · ${exportData.status}` : ''}
        {bundleCapturedLabel ? ` · ${bundleCapturedLabel}` : ''}
      </p>

      {siteFocus ? (
        <SiteFocusCard
          siteFocus={siteFocus}
          capturedAt={siteFocusCapturedAt}
          articleKeyword={articleKeyword}
          serpKeyword={serpKeyword ?? exportData.keyword}
        />
      ) : (
        <p className="text-[10px] text-[var(--color-text-muted)]">
          Site focus snapshot not captured — re-handoff from Site Analyzer with site_profile.
        </p>
      )}

      {exportData.writingInstructions ? (
        <InsightCard title="Writing brief">
          <p>{exportData.writingInstructions}</p>
        </InsightCard>
      ) : null}

      {exportData.writingRecommendations?.length ? (
        <InsightCard title="Recommendations">
          <ul className="list-disc space-y-1 pl-4">
            {exportData.writingRecommendations.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      <InsightCard title="SERP overview">
        <ul className="space-y-1">
          <li>Results: {exportData.serpSeResultsCount?.toLocaleString() ?? '—'}</li>
          <li>Organic captured: {organic.length}</li>
          {exportData.benchmarks?.medianWordCountTop5 ? (
            <li>Median words (top 5): {exportData.benchmarks.medianWordCountTop5.toLocaleString()}</li>
          ) : null}
          {exportData.benchmarks?.medianH2CountTop5 ? (
            <li>Median H2s (top 5): {exportData.benchmarks.medianH2CountTop5}</li>
          ) : null}
        </ul>
      </InsightCard>

      {paa.length ? (
        <InsightCard title="People also ask">
          <ul className="list-disc space-y-1 pl-4">
            {paa.map((question) => (
              <li key={question}>{question}</li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {pasf.length ? (
        <InsightCard title="Related searches">
          <ul className="list-disc space-y-1 pl-4">
            {pasf.map((search) => (
              <li key={search}>{search}</li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {organic.length ? (
        <InsightCard title="Top organic results">
          <ul className="space-y-2">
            {organic.slice(0, 5).map((item) => (
              <li key={item.url ?? `${item.position}-${item.title}`}>
                <span className="font-mono text-[10px]">#{item.position}</span>{' '}
                <span className="font-medium text-[var(--color-text-primary)]">
                  {item.title || item.url}
                </span>
                {item.domain ? (
                  <span className="text-[var(--color-text-muted)]"> · {item.domain}</span>
                ) : null}
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}
    </div>
  );
}
