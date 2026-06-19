'use client';

import Link from 'next/link';
import type { ScoreSuggestion, ScoreUpdate } from '@/hooks/useContentScoring';
import { selectVisibleInsights } from '@/lib/insight-suggestions';

const GEO_COMPONENT_META: Record<string, { label: string; max: number }> = {
  authority: { label: 'Authority signals', max: 20 },
  readability: { label: 'AI readability', max: 20 },
  structure: { label: 'Answer structure', max: 20 },
  citations: { label: 'Citations', max: 20 },
  depth: { label: 'Topic depth', max: 20 },
};

const COMPONENT_META: Record<string, { label: string; max: number }> = {
  termCoverage: { label: 'Term coverage', max: 35 },
  wordCount: { label: 'Word count', max: 20 },
  headingStructure: { label: 'Heading structure', max: 15 },
  titleTag: { label: 'Title tag', max: 10 },
  metaDescription: { label: 'Meta description', max: 10 },
  readability: { label: 'Readability', max: 10 },
};

export type ScoreSidebarProps = {
  placement?: 'left' | 'right';
  keyword: string;
  scoreUpdate: ScoreUpdate | null;
  pendingReason: string | null;
  benchmarkRefreshing: boolean;
  scoreError: string | null;
  connected: boolean;
  onRefreshSerp: () => void;
  serpRefreshEnabled?: boolean;
  onCopyHtml?: () => void;
  onApplySuggestion?: (suggestion: ScoreSuggestion) => Promise<void>;
  applyingSuggestionId?: string | null;
};

export function ScoreSidebar({
  placement,
  keyword,
  scoreUpdate,
  pendingReason,
  benchmarkRefreshing,
  scoreError,
  connected,
  onRefreshSerp,
  serpRefreshEnabled = true,
  onCopyHtml,
  onApplySuggestion,
  applyingSuggestionId,
}: ScoreSidebarProps) {
  if (!placement) {
    return (
      <div className="flex w-full shrink-0 flex-col border-t bg-[var(--color-bg)] lg:w-96 lg:border-t-0 lg:border-l">
        <ScoreMetricsColumn
          keyword={keyword}
          scoreUpdate={scoreUpdate}
          pendingReason={pendingReason}
          benchmarkRefreshing={benchmarkRefreshing}
          scoreError={scoreError}
          connected={connected}
          onRefreshSerp={onRefreshSerp}
          serpRefreshEnabled={serpRefreshEnabled}
        />
        <ScoreActionsColumn
          keyword={keyword}
          scoreUpdate={scoreUpdate}
          pendingReason={pendingReason}
          benchmarkRefreshing={benchmarkRefreshing}
          onApplySuggestion={onApplySuggestion}
          applyingSuggestionId={applyingSuggestionId}
          onCopyHtml={onCopyHtml}
        />
      </div>
    );
  }

  if (placement === 'left') {
    return (
      <ScoreMetricsColumn
        compact
        keyword={keyword}
        scoreUpdate={scoreUpdate}
        pendingReason={pendingReason}
        benchmarkRefreshing={benchmarkRefreshing}
        scoreError={scoreError}
        connected={connected}
        onRefreshSerp={onRefreshSerp}
        serpRefreshEnabled={serpRefreshEnabled}
      />
    );
  }

  return (
    <ScoreActionsColumn
      compact
      keyword={keyword}
      scoreUpdate={scoreUpdate}
      pendingReason={pendingReason}
      benchmarkRefreshing={benchmarkRefreshing}
      onApplySuggestion={onApplySuggestion}
      applyingSuggestionId={applyingSuggestionId}
      onCopyHtml={onCopyHtml}
    />
  );
}

function ScoreMetricsColumn({
  compact = false,
  keyword,
  scoreUpdate,
  pendingReason,
  benchmarkRefreshing,
  scoreError,
  connected,
  onRefreshSerp,
  serpRefreshEnabled = true,
}: Omit<ScoreSidebarProps, 'placement' | 'onCopyHtml' | 'onApplySuggestion' | 'applyingSuggestionId'> & {
  compact?: boolean;
}) {
  const loading = benchmarkRefreshing || Boolean(pendingReason);
  const score = scoreUpdate?.score ?? 0;
  const ringOffset = 283 - (283 * score) / 100;
  const ringSize = compact ? 'h-14 w-14' : 'h-24 w-24';

  return (
    <aside className={`min-w-0 bg-[var(--color-bg)] ${compact ? 'p-3 xl:p-4' : 'p-5 xl:p-6'}`}>
      <div className="flex flex-col gap-1">
        <div className="flex items-center justify-between gap-2">
          <h2 className={`font-semibold tracking-tight ${compact ? 'text-sm' : 'text-lg'}`}>Content score</h2>
        <span
          className={`rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide ${
            connected ? 'bg-emerald-100 text-emerald-800' : 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]'
          }`}
        >
          {connected ? 'Live' : 'Offline'}
        </span>
        </div>
      </div>

      {serpRefreshEnabled ? (
        <button
          type="button"
          className="mt-2 text-[10px] text-[var(--color-text-secondary)] underline hover:text-[var(--color-text-primary)] xl:text-xs"
          onClick={onRefreshSerp}
        >
          Refresh SERP
        </button>
      ) : (
        <p className="mt-2 text-[10px] text-[var(--color-text-secondary)] xl:text-xs">
          Score uses frozen page research — live SERP refresh is disabled.
        </p>
      )}

      {scoreUpdate?.scoreContextNote ? (
        <p className="mt-2 rounded-lg border border-slate-200 bg-slate-50 px-2 py-2 text-[10px] text-[var(--color-text-secondary)] xl:text-xs">
          {scoreUpdate.researchedAt
            ? `${scoreUpdate.scoreContextNote} Captured ${new Date(scoreUpdate.researchedAt).toLocaleString()}.`
            : scoreUpdate.scoreContextNote}
        </p>
      ) : null}

      {scoreError ? <p className={`mt-2 text-red-600 ${compact ? 'text-xs' : 'text-sm'}`}>{scoreError}</p> : null}

      {loading ? (
        <div className={`mt-3 rounded-lg border border-amber-200 bg-amber-50/80 text-amber-900 ${compact ? 'p-2 text-xs' : 'p-3 text-sm'}`}>
          <p className="font-medium">Building benchmarks…</p>
          {!compact ? (
            <p className="mt-1 text-xs">
              {pendingReason ??
                (keyword
                  ? `Analyzing top results for “${keyword}”. First load can take 20–30 seconds.`
                  : 'Set a target keyword to fetch SERP data.')}
            </p>
          ) : null}
        </div>
      ) : null}

      {scoreUpdate?.benchmarkQuality === 'low_sample_count' ? (
        <p className="mt-2 rounded-lg border border-amber-200 bg-amber-50 p-2 text-[10px] text-amber-900 xl:text-xs">
          Low competitor sample — targets use SERP snippets.
        </p>
      ) : null}

      {scoreUpdate ? (
        <div className={`${compact ? 'mt-4 space-y-4' : 'mt-6 space-y-6'}`}>
          <div className={compact ? 'flex flex-col items-center gap-2' : 'flex items-center gap-5'}>
            <div className={`relative shrink-0 ${ringSize}`}>
              <svg className={`${ringSize} -rotate-90`} viewBox="0 0 100 100" aria-hidden>
                <circle
                  cx="50"
                  cy="50"
                  r="45"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="8"
                  className="text-[var(--color-border)]"
                />
                <circle
                  cx="50"
                  cy="50"
                  r="45"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="8"
                  strokeLinecap="round"
                  strokeDasharray="283"
                  strokeDashoffset={ringOffset}
                  className="text-[var(--color-accent)] transition-[stroke-dashoffset] duration-300 ease-out"
                />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className={`font-bold tabular-nums text-[var(--color-metric-blue)] ${compact ? 'text-lg' : 'text-2xl'}`}>{scoreUpdate.score}</span>
                <span className="text-[10px] text-[var(--color-text-secondary)]">/ 100</span>
              </div>
            </div>
            <div className={compact ? 'text-center' : undefined}>
              <span className={`inline-block rounded-lg bg-[var(--color-accent)] font-semibold text-white ${compact ? 'px-2 py-0.5 text-sm' : 'px-3 py-1 text-lg'}`}>
                {scoreUpdate.grade}
              </span>
              {!compact ? (
                <p className="mt-2 text-xs text-[var(--color-text-secondary)]">Transparent 6-component score</p>
              ) : null}
            </div>
          </div>

          <ul className={`space-y-2 ${compact ? 'text-xs' : 'text-sm'}`}>
            {Object.entries(scoreUpdate.components).map(([key, value]) => {
              const meta = COMPONENT_META[key] ?? { label: key, max: 100 };
              const pct = Math.min(100, Math.round((value / meta.max) * 100));
              return (
                <li key={key}>
                  <div className="mb-1 flex justify-between text-xs">
                    <span className="font-medium text-[var(--color-text-primary)]">{meta.label}</span>
                    <span className="tabular-nums text-[var(--color-text-secondary)]">
                      {value}/{meta.max}
                    </span>
                  </div>
                  <div className="h-1.5 overflow-hidden rounded-full bg-[var(--color-surface-muted)]">
                    <div
                      className="h-full rounded-full bg-[var(--color-accent)] transition-all duration-300"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                </li>
              );
            })}
          </ul>

          {scoreUpdate.geoScore != null && scoreUpdate.geoComponents ? (
            <GeoScoreBlock scoreUpdate={scoreUpdate} compact={compact} />
          ) : null}
        </div>
      ) : (
        !loading && (
          <p className="mt-6 text-sm text-[var(--color-text-secondary)]">
            {keyword
              ? 'Edit content or wait for the first score.'
              : 'Add a target keyword, then edit to see your score.'}
          </p>
        )
      )}
    </aside>
  );
}

function GeoScoreBlock({ scoreUpdate, compact = false }: { scoreUpdate: ScoreUpdate; compact?: boolean }) {
  if (scoreUpdate.geoScore == null || !scoreUpdate.geoComponents) return null;
  const ringSize = compact ? 'h-12 w-12' : 'h-16 w-16';

  return (
    <div className={`rounded-xl border border-purple-100 bg-purple-50/40 ${compact ? 'p-3' : 'p-4'}`}>
      <div className={compact ? 'flex flex-col items-center gap-2' : 'flex items-center gap-4'}>
        <div className={`relative shrink-0 ${ringSize}`}>
          <svg className={`${ringSize} -rotate-90`} viewBox="0 0 100 100" aria-hidden>
            <circle cx="50" cy="50" r="45" fill="none" stroke="currentColor" strokeWidth="8" className="text-purple-100" />
            <circle
              cx="50"
              cy="50"
              r="45"
              fill="none"
              stroke="currentColor"
              strokeWidth="8"
              strokeLinecap="round"
              strokeDasharray="283"
              strokeDashoffset={283 - (283 * scoreUpdate.geoScore) / 100}
              className="text-purple-600 transition-[stroke-dashoffset] duration-300 ease-out"
            />
          </svg>
          <div className="absolute inset-0 flex flex-col items-center justify-center">
            <span className={`font-bold tabular-nums text-purple-900 ${compact ? 'text-sm' : 'text-lg'}`}>{scoreUpdate.geoScore}</span>
          </div>
        </div>
        <div className={compact ? 'text-center' : undefined}>
          <h3 className={`font-semibold text-purple-950 ${compact ? 'text-xs' : 'text-sm'}`}>GEO score</h3>
          <span className={`mt-1 inline-block rounded-lg bg-purple-600 font-semibold text-white ${compact ? 'px-1.5 py-0.5 text-xs' : 'px-2 py-0.5 text-sm'}`}>
            {scoreUpdate.geoGrade ?? '—'}
          </span>
        </div>
      </div>
      <ul className={`mt-3 space-y-2 ${compact ? 'text-xs' : 'text-sm'}`}>
        {Object.entries(scoreUpdate.geoComponents).map(([key, value]) => {
          const meta = GEO_COMPONENT_META[key] ?? { label: key, max: 20 };
          const pct = Math.min(100, Math.round((value / meta.max) * 100));
          return (
            <li key={key}>
              <div className="mb-1 flex justify-between text-xs">
                <span className="font-medium text-purple-950">{meta.label}</span>
                <span className="tabular-nums text-purple-800">
                  {value}/{meta.max}
                </span>
              </div>
              <div className="h-1.5 overflow-hidden rounded-full bg-purple-100">
                <div className="h-full rounded-full bg-purple-600 transition-all duration-300" style={{ width: `${pct}%` }} />
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

function ScoreActionsColumn({
  compact = false,
  keyword,
  scoreUpdate,
  pendingReason,
  benchmarkRefreshing,
  onApplySuggestion,
  applyingSuggestionId,
  onCopyHtml,
}: Pick<
  ScoreSidebarProps,
  | 'keyword'
  | 'scoreUpdate'
  | 'pendingReason'
  | 'benchmarkRefreshing'
  | 'onApplySuggestion'
  | 'applyingSuggestionId'
  | 'onCopyHtml'
> & { compact?: boolean }) {
  const loading = benchmarkRefreshing || Boolean(pendingReason);

  return (
    <aside className={`min-w-0 bg-[var(--color-bg)] ${compact ? 'p-3 xl:p-4' : 'p-5 xl:p-6'}`}>
      <h2 className={`font-semibold tracking-tight ${compact ? 'text-sm' : 'text-lg'}`}>Insights</h2>
      {!compact ? (
        <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
          Proposed fixes, advisories, and export tools.
        </p>
      ) : null}

      {scoreUpdate ? (
        <div className={`${compact ? 'mt-4 space-y-4' : 'mt-6 space-y-5'}`}>
          {scoreUpdate.suggestions.length > 0 ? (
            <div>
              <h3 className={`font-semibold ${compact ? 'text-xs' : 'text-sm'}`}>Proposed changes</h3>
              <ul className={`mt-2 space-y-2 ${compact ? 'text-xs' : 'text-sm'}`}>
                {selectVisibleInsights(scoreUpdate.suggestions).map((s) => (
                    <li
                      key={s.id}
                      className={`rounded-lg border border-[var(--color-border)] bg-white shadow-sm ${compact ? 'p-2' : 'p-3'}`}
                    >
                      <div className="flex flex-col gap-2">
                        <span className="text-xs font-medium text-emerald-700">+{s.pointValue} pts</span>
                        {s.applyMode !== 'none' && onApplySuggestion ? (
                          <button
                            type="button"
                            disabled={applyingSuggestionId === s.id}
                            className="w-full rounded-md border border-[var(--color-border-strong)] bg-white px-2 py-1 text-[10px] font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50 xl:text-[11px]"
                            onClick={() => void onApplySuggestion(s)}
                          >
                            {applyingSuggestionId === s.id
                              ? s.id === 'geo_citations' && s.applyMode === 'ai'
                                ? 'Finding sources…'
                                : 'Applying…'
                              : s.id === 'geo_citations' && s.applyMode === 'ai'
                                ? 'Find Sources with AI'
                                : s.applyMode === 'ai'
                                  ? 'Apply with AI'
                                  : 'Apply'}
                          </button>
                        ) : null}
                      </div>
                      <p className={`mt-1 font-medium text-[var(--color-text-primary)] ${compact ? 'text-xs leading-snug' : ''}`}>{s.proposedChange}</p>
                      {!compact ? (
                        <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{s.actionText}</p>
                      ) : null}
                    </li>
                  ))}
              </ul>
            </div>
          ) : null}

          {scoreUpdate.eeatAdvisories.length > 0 ? (
            <div>
              <h3 className={`font-semibold ${compact ? 'text-xs' : 'text-sm'}`}>E-E-A-T</h3>
              <ul className={`mt-2 space-y-2 ${compact ? 'text-[11px]' : 'text-sm'}`}>
                {scoreUpdate.eeatAdvisories.map((a) => (
                  <li key={a.code} className="rounded-lg border border-amber-100 bg-amber-50/90 p-2 text-[var(--color-text-primary)]">
                    {a.actionText}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          {scoreUpdate.serpFeatures.length > 0 ? (
            <div>
              <h3 className={`font-semibold ${compact ? 'text-xs' : 'text-sm'}`}>SERP</h3>
              <ul className={`mt-2 space-y-1.5 text-[var(--color-text-primary)] ${compact ? 'text-[11px]' : 'text-sm'}`}>
                {scoreUpdate.serpFeatures.map((f) => (
                  <li
                    key={f.feature}
                    className={`rounded-lg border bg-white ${compact ? 'px-2 py-1.5' : 'p-3'}`}
                  >
                    {f.applyMode && f.applyMode !== 'none' && f.suggestionId && onApplySuggestion ? (
                      <button
                        type="button"
                        disabled={applyingSuggestionId === f.suggestionId}
                        className={`mb-2 w-full rounded-md border border-[var(--color-border-strong)] bg-white font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50 ${compact ? 'px-2 py-1 text-[10px]' : 'px-2 py-1.5 text-xs'}`}
                        onClick={() =>
                          void onApplySuggestion({
                            id: f.suggestionId!,
                            component: 'serp',
                            pointValue: 0,
                            actionText: f.actionText,
                            proposedChange: 'Add a direct answer paragraph after the first H2',
                            applyMode: f.applyMode as ScoreSuggestion['applyMode'],
                          })
                        }
                      >
                        {applyingSuggestionId === f.suggestionId ? 'Applying…' : 'Apply'}
                      </button>
                    ) : null}
                    <span className={compact ? 'text-[11px]' : 'text-sm'}>{f.actionText}</span>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>
      ) : (
        !loading && (
          <p className="mt-6 text-sm text-[var(--color-text-secondary)]">
            {keyword ? 'Suggestions appear after the first score.' : 'Add a target keyword to unlock suggestions.'}
          </p>
        )
      )}

      <div className={`border-t pt-4 ${compact ? 'mt-4' : 'mt-8'}`}>
        <h3 className={`font-semibold ${compact ? 'text-xs' : 'text-sm'}`}>Export</h3>
        {!compact ? (
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">Copy HTML for any CMS.</p>
        ) : null}
        {onCopyHtml ? (
          <button
            type="button"
            className={`mt-2 w-full rounded-lg border bg-white font-medium hover:bg-[var(--color-surface-muted)] ${compact ? 'px-2 py-1.5 text-xs' : 'mt-3 px-3 py-2 text-sm'}`}
            onClick={onCopyHtml}
          >
            Copy HTML
          </button>
        ) : null}
        {!compact ? (
          <Link
            href="/pricing"
            className="mt-3 block text-center text-xs text-[var(--color-text-secondary)] underline hover:text-[var(--color-text-primary)]"
          >
            Plans &amp; limits
          </Link>
        ) : null}
      </div>
    </aside>
  );
}
