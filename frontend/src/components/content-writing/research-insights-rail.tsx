'use client';

import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  getClusterPlan,
  getResearchPack,
  listContentSpokes,
  type ContentLinkPlan,
  type ContentWriterSerpExport,
} from '@/lib/seo-api';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';

type Props = {
  articleKeyword?: string;
  serpKeyword?: string | null;
};

function normalizePhrase(value: string): string {
  return value.toLowerCase().replace(/\s+/g, ' ').trim();
}

function buildClusterPhraseMarkers(plan: ContentLinkPlan, spokePhrases: string[]): Set<string> {
  const markers = new Set<string>();
  for (const item of plan.faqItems) {
    if (item.question) markers.add(normalizePhrase(item.question));
    if (item.anchorText) markers.add(normalizePhrase(item.anchorText));
  }
  for (const item of plan.bodyLinks) {
    if (item.anchorText) markers.add(normalizePhrase(item.anchorText));
  }
  for (const phrase of spokePhrases) {
    if (phrase) markers.add(normalizePhrase(phrase));
  }
  return markers;
}

function phraseInClusterPlan(phrase: string, markers: Set<string>): boolean {
  const normalized = normalizePhrase(phrase);
  if (!normalized) return false;
  if (markers.has(normalized)) return true;
  for (const marker of markers) {
    if (normalized.includes(marker) || marker.includes(normalized)) return true;
  }
  return false;
}

function ClusterPhraseRow({ phrase, inPlan }: { phrase: string; inPlan: boolean }) {
  return (
    <li className="flex items-start gap-2">
      <span
        aria-hidden
        className={`mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded border text-[10px] ${
          inPlan
            ? 'border-emerald-500 bg-emerald-50 text-emerald-700'
            : 'border-[var(--color-border)] bg-white text-transparent'
        }`}
      >
        ✓
      </span>
      <span className={inPlan ? 'text-[var(--color-text-primary)]' : undefined}>{phrase}</span>
    </li>
  );
}

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
  const [clusterPlan, setClusterPlan] = useState<ContentLinkPlan>({ faqItems: [], bodyLinks: [] });
  const [spokePhrases, setSpokePhrases] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const isClusterPillar = Boolean(doc.analysisRunId) && doc.documentKind !== 'spoke';

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

  useEffect(() => {
    if (!doc.id || !isClusterPillar) {
      setClusterPlan({ faqItems: [], bodyLinks: [] });
      setSpokePhrases([]);
      return;
    }
    let cancelled = false;
    void Promise.all([getClusterPlan(doc.id, accessToken), listContentSpokes(doc.id, accessToken)])
      .then(([plan, spokes]) => {
        if (cancelled) return;
        setClusterPlan(plan);
        setSpokePhrases(
          spokes
            .map((spoke) => spoke.spokeSourcePhrase?.trim())
            .filter((phrase): phrase is string => Boolean(phrase)),
        );
      })
      .catch(() => {
        if (!cancelled) {
          setClusterPlan({ faqItems: [], bodyLinks: [] });
          setSpokePhrases([]);
        }
      });
    return () => { cancelled = true; };
  }, [doc.id, isClusterPillar, accessToken]);

  const clusterPhraseMarkers = useMemo(
    () => buildClusterPhraseMarkers(clusterPlan, spokePhrases),
    [clusterPlan, spokePhrases],
  );

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
          {isClusterPillar ? (
            <p className="mb-2 text-[10px] text-[var(--color-text-muted)]">
              Checked items appear in your saved cluster link plan or spokes.
            </p>
          ) : null}
          <ul className={isClusterPillar ? 'space-y-1' : 'list-disc space-y-1 pl-4'}>
            {paa.map((question) =>
              isClusterPillar ? (
                <ClusterPhraseRow
                  key={question}
                  phrase={question}
                  inPlan={phraseInClusterPlan(question, clusterPhraseMarkers)}
                />
              ) : (
                <li key={question}>{question}</li>
              ),
            )}
          </ul>
        </InsightCard>
      ) : null}

      {pasf.length ? (
        <InsightCard title="Related searches">
          {isClusterPillar ? (
            <p className="mb-2 text-[10px] text-[var(--color-text-muted)]">
              Checked items appear in your saved cluster link plan or spokes.
            </p>
          ) : null}
          <ul className={isClusterPillar ? 'space-y-1' : 'list-disc space-y-1 pl-4'}>
            {pasf.map((search) =>
              isClusterPillar ? (
                <ClusterPhraseRow
                  key={search}
                  phrase={search}
                  inPlan={phraseInClusterPlan(search, clusterPhraseMarkers)}
                />
              ) : (
                <li key={search}>{search}</li>
              ),
            )}
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
        <InsightCard title="Authoritative sources">
          <ul className="list-disc space-y-1 pl-4">
            {exportData.citationCandidates.slice(0, 8).map((candidate) => (
              <li key={candidate.url}>
                <span className="text-[10px] uppercase text-[var(--color-text-muted)]">
                  {candidate.source}
                </span>{' '}
                <a
                  href={candidate.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-[var(--color-text-primary)] underline-offset-2 hover:underline"
                >
                  {candidate.title || candidate.url}
                </a>
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {exportData.operatorQueries?.length ? (
        <InsightCard title="Operator searches (Google)">
          <ul className="space-y-2 pl-0">
            {exportData.operatorQueries.map((item) => (
              <li key={`${item.bucket}-${item.query}`} className="list-none">
                <p className="text-[10px] font-semibold uppercase text-[var(--color-text-muted)]">
                  {item.label}
                </p>
                <code className="mt-1 block whitespace-pre-wrap break-all rounded bg-white px-2 py-1 text-[10px] text-[var(--color-text-primary)]">
                  {item.query}
                </code>
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {exportData.featuredSnippetCandidate ? (
        <InsightCard title="Featured snippet target">
          <p>{exportData.featuredSnippetCandidate}</p>
        </InsightCard>
      ) : null}

      {exportData.newsHooks?.length ? (
        <InsightCard title="Timely angles">
          <ul className="list-disc space-y-1 pl-4">
            {exportData.newsHooks.slice(0, 4).map((hook) => (
              <li key={hook}>{hook}</li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {exportData.localAngleHint ? (
        <InsightCard title="Local SMB angle">
          <p>{exportData.localAngleHint}</p>
        </InsightCard>
      ) : null}

      {exportData.supplementalPaaQuestions?.length ? (
        <InsightCard title="Extra question ideas">
          <ul className="list-disc space-y-1 pl-4">
            {exportData.supplementalPaaQuestions.slice(0, 6).map((question) => (
              <li key={question}>{question}</li>
            ))}
          </ul>
        </InsightCard>
      ) : null}
    </div>
  );
}
