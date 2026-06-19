'use client';

import { useEffect, useState } from 'react';
import { getUrlResearch, type UrlResearchFull } from '@/lib/seo-api';

type Props = {
  urlResearchId: string;
  accessToken?: string | null;
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

export function ResearchInsightsRail({ urlResearchId, accessToken }: Props) {
  const [pack, setPack] = useState<UrlResearchFull | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    void getUrlResearch(urlResearchId, accessToken)
      .then((data) => {
        if (!cancelled) setPack(data);
      })
      .catch(() => {
        if (!cancelled) {
          setPack(null);
          setError('Research pack could not be loaded.');
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [urlResearchId, accessToken]);

  if (loading) {
    return (
      <div className="border-t px-3 py-4 text-xs text-[var(--color-text-muted)] xl:px-4">
        Loading research insights…
      </div>
    );
  }

  if (error || !pack || pack.dataQuality !== 'full') {
    return (
      <div className="border-t px-3 py-4 text-xs text-amber-800 xl:px-4">
        {error ?? 'Complete Site Analyzer pack required for insights.'}
      </div>
    );
  }

  return (
    <div className="space-y-3 border-t px-3 py-4 xl:px-4">
      <h3 className="text-xs font-semibold xl:text-sm">Research insights</h3>
      <p className="text-xs text-[var(--color-text-muted)]">
        From Site Analyzer · {pack.derivedKeyword}
      </p>

      <InsightCard title="Intent">
        <p className="font-medium text-[var(--color-text-primary)]">{pack.intentPrimary || '—'}</p>
        {pack.intentJustification ? <p className="mt-1">{pack.intentJustification}</p> : null}
      </InsightCard>

      <InsightCard title="Benchmarks">
        <ul className="space-y-1">
          <li>Target words (top 5 median): {pack.medianWordCountTop5 || '—'}</li>
          <li>Title length (top 10 median): {pack.medianTitleLengthTop10 || '—'}</li>
          <li>H2 count (top 5 median): {pack.medianH2CountTop5 || '—'}</li>
          <li>Format: {pack.dominantContentFormat || '—'}</li>
        </ul>
      </InsightCard>

      {pack.sectionHints?.length ? (
        <InsightCard title="Section checklist">
          <ul className="list-disc space-y-1 pl-4">
            {pack.sectionHints.map((hint) => (
              <li key={hint.suggestedH2}>
                <span className="font-medium text-[var(--color-text-primary)]">
                  {hint.suggestedH2}
                </span>
                {hint.subtopicsFromSerp?.length ? (
                  <span className="text-[var(--color-text-muted)]">
                    {' '}
                    — {hint.subtopicsFromSerp.join(', ')}
                  </span>
                ) : null}
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {pack.recommendedTerms?.length ? (
        <InsightCard title="Terms to include">
          <p>{pack.recommendedTerms.map((t) => t.term).join(', ')}</p>
        </InsightCard>
      ) : null}

      {pack.peopleAlsoAsk?.length ? (
        <InsightCard title="People also ask">
          <ul className="list-disc space-y-1 pl-4">
            {pack.peopleAlsoAsk.slice(0, 8).map((item) => (
              <li key={item.question}>{item.question}</li>
            ))}
          </ul>
        </InsightCard>
      ) : null}

      {pack.directAnswerInstruction || pack.pafText ? (
        <InsightCard title="Direct answer">
          {pack.directAnswerInstruction ? <p>{pack.directAnswerInstruction}</p> : null}
          {pack.pafText ? (
            <p className="mt-1 italic text-[var(--color-text-muted)]">PAF: {pack.pafText}</p>
          ) : null}
        </InsightCard>
      ) : null}

      {pack.competitors?.length ? (
        <InsightCard title="Competitors">
          <ul className="space-y-2">
            {pack.competitors.slice(0, 5).map((c) => (
              <li key={c.url}>
                <span className="font-mono text-[10px]">#{c.position}</span>{' '}
                <span className="font-medium text-[var(--color-text-primary)]">{c.h1 || c.url}</span>
                <span className="text-[var(--color-text-muted)]"> · ~{c.estimatedWordCount} words</span>
              </li>
            ))}
          </ul>
        </InsightCard>
      ) : null}
    </div>
  );
}
