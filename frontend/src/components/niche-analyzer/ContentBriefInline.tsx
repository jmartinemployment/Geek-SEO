'use client';

import type { ContentBrief } from '@/lib/seo-api';

type Props = {
  brief: ContentBrief;
};

export function ContentBriefInline({ brief }: Readonly<Props>) {
  return (
    <div className="mt-3 space-y-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-3 text-xs">
      <p className="text-[var(--color-text-muted)]">
        {brief.keyword} · {brief.location} · ~{brief.targetWordCount} words · {brief.benchmarkQuality}{' '}
        benchmarks
      </p>
      {brief.recommendedTerms.length > 0 ? (
        <div>
          <p className="font-medium text-[var(--color-text-primary)]">Recommended terms</p>
          <p className="mt-1 text-[var(--color-text-secondary)]">{brief.recommendedTerms.join(', ')}</p>
        </div>
      ) : null}
      {brief.suggestedHeadings.length > 0 ? (
        <div>
          <p className="font-medium text-[var(--color-text-primary)]">Suggested headings</p>
          <ul className="mt-1 list-inside list-disc text-[var(--color-text-secondary)]">
            {brief.suggestedHeadings.map((heading) => (
              <li key={heading}>{heading}</li>
            ))}
          </ul>
        </div>
      ) : null}
      {brief.topCompetitors.length > 0 ? (
        <div>
          <p className="font-medium text-[var(--color-text-primary)]">Top SERP pages</p>
          <ul className="mt-1 space-y-0.5 text-[var(--color-text-secondary)]">
            {brief.topCompetitors.slice(0, 5).map((c) => (
              <li key={c.url} className="truncate">
                #{c.position} {c.title ?? c.url}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
      {brief.peopleAlsoAsk.length > 0 ? (
        <div>
          <p className="font-medium text-[var(--color-text-primary)]">People also ask</p>
          <ul className="mt-1 list-inside list-disc text-[var(--color-text-secondary)]">
            {brief.peopleAlsoAsk.slice(0, 4).map((q) => (
              <li key={q}>{q}</li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
