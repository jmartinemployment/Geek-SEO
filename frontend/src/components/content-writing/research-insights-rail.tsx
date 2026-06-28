'use client';

import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  getResearchPack,
  type ContentWriterSerpExport,
} from '@/lib/seo-api';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';

type Props = {
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

export function ResearchInsightsRail({ articleKeyword, serpKeyword }: Props) {
  const { doc } = useWritingWorkspace();
  const { accessToken } = useAuth();
  const [exportData, setExportData] = useState<ContentWriterSerpExport | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!doc.id || !doc.analysisRunId) return;
    let cancelled = false;
    setLoading(true);
    void getResearchPack(doc.id, accessToken).then((data) => {
      if (!cancelled) {
        setExportData(data);
        setLoading(false);
      }
    });
    return () => { cancelled = true; };
  }, [doc.id, doc.analysisRunId, accessToken]);

  const organic = useMemo(() => (exportData ? organicItems(exportData) : []), [exportData]);
  const paa = useMemo(() => (exportData ? paaQuestions(exportData) : []), [exportData]);
  const pasf = useMemo(() => (exportData ? relatedSearches(exportData) : []), [exportData]);

  if (loading) {
    return (
      <div className="border-t px-3 py-4 text-xs text-[var(--color-text-muted)] xl:px-4">
        Loading research…
      </div>
    );
  }

  if (!exportData || organic.length === 0) {
    return (
      <div className="border-t px-3 py-4 text-xs text-amber-800 xl:px-4">
        {exportData
          ? 'No organic SERP results found in research pack.'
          : 'Research pack not available — ensure Site Analyzer research is complete.'}
      </div>
    );
  }

  return (
    <div className="space-y-3 border-t px-3 py-4 xl:px-4">
      <h3 className="text-xs font-semibold xl:text-sm">SERP research</h3>
      <p className="text-xs text-[var(--color-text-muted)]">
        Live from sa2 · {exportData.keyword}
        {exportData.status ? ` · ${exportData.status}` : ''}
      </p>

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

      {exportData.sourceHeadings?.length ? (
        <InsightCard title="Your page structure">
          <ul className="list-disc space-y-1 pl-4">
            {exportData.sourceHeadings.slice(0, 8).map((heading) => (
              <li key={`${heading.sequence}-${heading.text}`}>
                H{heading.level}: {heading.text}
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {exportData.competitors?.length ? (
        <InsightCard title="Competitor seed pages">
          <ul className="space-y-2">
            {exportData.competitors.slice(0, 5).map((competitor) => (
              <li key={competitor.url}>
                <span className="font-medium text-[var(--color-text-primary)]">{competitor.domain}</span>
                {competitor.hasFaqSchema ? (
                  <span className="ml-1 text-[10px] text-[var(--color-text-muted)]">· FAQ schema</span>
                ) : null}
                {competitor.headings?.length ? (
                  <p className="mt-1 text-[10px] text-[var(--color-text-muted)]">
                    Headings: {competitor.headings.slice(0, 4).map((h) => h.text).join(' · ')}
                  </p>
                ) : null}
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {exportData.citationCandidates?.length ? (
        <InsightCard title="Citation candidates">
          <ul className="list-disc space-y-1 pl-4">
            {exportData.citationCandidates.slice(0, 8).map((candidate) => (
              <li key={candidate.url}>
                <span className="text-[10px] uppercase text-[var(--color-text-muted)]">
                  {candidate.source}
                </span>{' '}
                {candidate.title || candidate.url}
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}
    </div>
  );
}
