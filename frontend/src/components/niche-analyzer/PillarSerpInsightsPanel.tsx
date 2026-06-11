'use client';

import type { NichePillarResult } from '@/lib/seo-api';

type Props = {
  pillars: NichePillarResult[];
};

export function PillarSerpInsightsPanel({ pillars }: Props) {
  const withData = pillars.filter(
    (p) => p.paaQuestions?.length > 0 || p.relatedSearches?.length > 0,
  );

  if (withData.length === 0) return null;

  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          SERP insights per pillar
        </h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">
          People Also Ask and related searches from Google for each pillar keyword.
        </p>
      </div>

      <div className="space-y-3">
        {withData.map((pillar) => (
          <PillarSerpCard key={pillar.id} pillar={pillar} />
        ))}
      </div>
    </div>
  );
}

function PillarSerpCard({ pillar }: { pillar: NichePillarResult }) {
  const hasPaa = pillar.paaQuestions?.length > 0;
  const hasRelated = pillar.relatedSearches?.length > 0;

  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
      {/* Pillar header */}
      <div className="flex items-center gap-2 px-4 py-3 border-b border-[var(--color-border)] bg-[var(--color-surface-raised,var(--color-surface))]">
        <span className="text-xs font-semibold text-[var(--color-accent)] uppercase tracking-wide">
          {pillar.searchIntent}
        </span>
        <span className="text-sm font-semibold text-[var(--color-text-primary)]">
          {pillar.pillarTopic}
        </span>
        {pillar.primaryKeyword && pillar.primaryKeyword !== pillar.pillarTopic.toLowerCase() && (
          <span className="ml-auto text-xs text-[var(--color-text-muted)] font-mono">
            {pillar.primaryKeyword}
          </span>
        )}
      </div>

      <div className="divide-y divide-[var(--color-border)]">
        {/* PAA */}
        {hasPaa && (
          <div className="px-4 py-3">
            <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-2">
              People Also Ask
            </p>
            <ol className="space-y-2">
              {pillar.paaQuestions.map((q, i) => (
                <li key={i} className="space-y-1">
                  <p className="text-sm text-[var(--color-text-primary)] font-medium">
                    {q.question}
                  </p>
                  {q.answer && (
                    <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed line-clamp-3">
                      {q.answer}
                    </p>
                  )}
                  {q.sourceUrl && (
                    <a
                      href={q.sourceUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-xs text-[var(--color-accent)] hover:underline truncate block"
                    >
                      {q.sourceTitle ?? q.sourceUrl}
                    </a>
                  )}
                </li>
              ))}
            </ol>
          </div>
        )}

        {/* Related searches */}
        {hasRelated && (
          <div className="px-4 py-3">
            <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-2">
              Related Searches
            </p>
            <div className="flex flex-wrap gap-1.5">
              {pillar.relatedSearches.map((s, i) => (
                <span
                  key={i}
                  className="px-2 py-0.5 rounded-full text-xs bg-[var(--color-surface-alt,var(--color-border))] text-[var(--color-text-secondary)] border border-[var(--color-border)]"
                >
                  {s}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
