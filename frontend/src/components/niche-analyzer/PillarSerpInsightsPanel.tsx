'use client';

import type { NichePillarResult, PaaQuestionItem } from '@/lib/seo-api';

type Props = {
  pillars: NichePillarResult[];
};

export function PillarSerpInsightsPanel({ pillars }: Props) {
  const withData = pillars.filter(
    (p) =>
      p.paaQuestions?.length > 0 ||
      p.relatedSearches?.length > 0 ||
      p.localPaaQuestions?.length > 0 ||
      p.localRelatedSearches?.length > 0,
  );

  if (withData.length === 0) return null;

  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          SERP insights per pillar
        </h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">
          People Also Ask and related searches — national and local.
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
  const hasNational = pillar.paaQuestions?.length > 0 || pillar.relatedSearches?.length > 0;
  const hasLocal = pillar.localPaaQuestions?.length > 0 || pillar.localRelatedSearches?.length > 0;

  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
      {/* Pillar header */}
      <div className="flex items-center gap-2 px-4 py-3 border-b border-[var(--color-border)]">
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

      <div className="grid grid-cols-1 md:grid-cols-2 divide-y md:divide-y-0 md:divide-x divide-[var(--color-border)]">
        {/* National */}
        <SerpColumn
          label="National SERP"
          paaQuestions={pillar.paaQuestions}
          relatedSearches={pillar.relatedSearches}
          empty={!hasNational}
        />
        {/* Local */}
        <SerpColumn
          label="Local SERP"
          paaQuestions={pillar.localPaaQuestions}
          relatedSearches={pillar.localRelatedSearches}
          empty={!hasLocal}
          isLocal
        />
      </div>
    </div>
  );
}

function SerpColumn({
  label,
  paaQuestions,
  relatedSearches,
  empty,
  isLocal,
}: {
  label: string;
  paaQuestions?: PaaQuestionItem[];
  relatedSearches?: string[];
  empty: boolean;
  isLocal?: boolean;
}) {
  return (
    <div className="px-4 py-3 space-y-3">
      <p className={`text-xs font-semibold uppercase tracking-wide ${isLocal ? 'text-[var(--color-success,#22c55e)]' : 'text-[var(--color-text-secondary)]'}`}>
        {label}
      </p>

      {empty ? (
        <p className="text-xs text-[var(--color-text-muted)] italic">No data — re-run analysis to populate.</p>
      ) : (
        <>
          {paaQuestions && paaQuestions.length > 0 && (
            <div>
              <p className="text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
                People Also Ask
              </p>
              <ol className="space-y-2">
                {paaQuestions.map((q, i) => (
                  <li key={i} className="space-y-0.5">
                    <p className="text-xs text-[var(--color-text-primary)] font-medium leading-snug">
                      {q.question}
                    </p>
                    {q.answer && (
                      <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed line-clamp-2">
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

          {relatedSearches && relatedSearches.length > 0 && (
            <div>
              <p className="text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
                Related Searches
              </p>
              <div className="flex flex-wrap gap-1.5">
                {relatedSearches.map((s, i) => (
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
        </>
      )}
    </div>
  );
}
