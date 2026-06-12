'use client';

import type { CompetitorSiteInsight, NichePillarResult, PaaQuestionItem } from '@/lib/seo-api';

type Props = {
  pillars: NichePillarResult[];
};

export function PillarSerpInsightsPanel({ pillars }: Props) {
  const withData = pillars.filter(
    (p) =>
      p.paaQuestions?.length > 0 ||
      p.relatedSearches?.length > 0 ||
      p.localPaaQuestions?.length > 0 ||
      p.localRelatedSearches?.length > 0 ||
      p.competitorInsights?.length > 0,
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
        <SerpColumn
          label="National SERP"
          paaQuestions={pillar.paaQuestions}
          relatedSearches={pillar.relatedSearches}
          empty={!hasNational}
        />
        <SerpColumn
          label="Local SERP"
          paaQuestions={pillar.localPaaQuestions}
          relatedSearches={pillar.localRelatedSearches}
          empty={!hasLocal}
          isLocal
        />
      </div>

      {/* Competitor crawl results */}
      {pillar.competitorInsights?.length > 0 && (
        <div className="border-t border-[var(--color-border)] px-4 py-3">
          <p className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-3">
            Competitor sites crawled
          </p>
          <div className="space-y-3">
            {pillar.competitorInsights.map((c, i) => (
              <CompetitorInsightCard key={i} insight={c} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function CompetitorInsightCard({ insight }: { insight: CompetitorSiteInsight }) {
  const scopeColor = insight.scope === 'local' ? 'text-green-600'
    : insight.scope === 'both' ? 'text-[var(--color-accent)]'
    : 'text-[var(--color-text-muted)]';

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3 space-y-2">
      {/* Header */}
      <div className="flex items-center gap-2 flex-wrap">
        <span className="text-sm font-semibold text-[var(--color-text-primary)]">{insight.domain}</span>
        <span className={`text-xs font-medium ${scopeColor}`}>{insight.scope}</span>
        {insight.hasFaqSchema && (
          <span className="ml-auto text-xs bg-green-100 text-green-700 px-1.5 py-0.5 rounded">FAQ schema</span>
        )}
      </div>

      {/* Stats */}
      <div className="flex gap-4 text-xs text-[var(--color-text-secondary)]">
        <span>{insight.pagesCrawled} pages crawled</span>
        <span>~{insight.avgWordCount.toLocaleString()} words avg</span>
      </div>

      {/* Description */}
      {insight.description && (
        <p className="text-xs text-[var(--color-text-secondary)] italic line-clamp-2">{insight.description}</p>
      )}

      {/* Schema — services declared */}
      {insight.services && insight.services.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-1">Services (schema)</p>
          <div className="flex flex-wrap gap-1">
            {insight.services.slice(0, 8).map((s, i) => (
              <span key={i} className="px-1.5 py-0.5 rounded text-xs bg-blue-50 text-blue-700 border border-blue-200">{s}</span>
            ))}
          </div>
        </div>
      )}

      {/* Schema — knowsAbout */}
      {insight.knowsAbout && insight.knowsAbout.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-1">Knows About (schema)</p>
          <div className="flex flex-wrap gap-1">
            {insight.knowsAbout.slice(0, 8).map((k, i) => (
              <span key={i} className="px-1.5 py-0.5 rounded text-xs bg-purple-50 text-purple-700 border border-purple-200">{k}</span>
            ))}
          </div>
        </div>
      )}

      {/* Schema — areaServed */}
      {insight.areaServed && insight.areaServed.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-1">Area Served (schema)</p>
          <div className="flex flex-wrap gap-1">
            {insight.areaServed.slice(0, 6).map((a, i) => (
              <span key={i} className="px-1.5 py-0.5 rounded text-xs bg-green-50 text-green-700 border border-green-200">{a}</span>
            ))}
          </div>
        </div>
      )}

      {/* Top headings */}
      {insight.topHeadings.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-1">Top headings</p>
          <ul className="space-y-0.5">
            {insight.topHeadings.slice(0, 6).map((h, i) => (
              <li key={i} className="text-xs text-[var(--color-text-secondary)] truncate">— {h}</li>
            ))}
          </ul>
        </div>
      )}
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
