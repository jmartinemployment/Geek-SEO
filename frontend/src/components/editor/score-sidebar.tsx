'use client';

import Link from 'next/link';
import type { ScoreUpdate } from '@/hooks/useContentScoring';

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

type ScoreSidebarProps = {
  keyword: string;
  scoreUpdate: ScoreUpdate | null;
  pendingReason: string | null;
  benchmarkRefreshing: boolean;
  scoreError: string | null;
  connected: boolean;
  onRefreshSerp: () => void;
  onCopyHtml?: () => void;
  onApplySuggestion?: (suggestionId: string) => Promise<void>;
  applyingSuggestionId?: string | null;
};

export function ScoreSidebar({
  keyword,
  scoreUpdate,
  pendingReason,
  benchmarkRefreshing,
  scoreError,
  connected,
  onRefreshSerp,
  onCopyHtml,
  onApplySuggestion,
  applyingSuggestionId,
}: ScoreSidebarProps) {
  const loading = benchmarkRefreshing || Boolean(pendingReason);
  const score = scoreUpdate?.score ?? 0;
  const ringOffset = 283 - (283 * score) / 100;

  return (
    <aside className="w-full shrink-0 border-t bg-[var(--color-bg)] p-6 lg:w-96 lg:border-t-0 lg:border-l">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-lg font-semibold tracking-tight">Content score</h2>
        <span
          className={`rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide ${
            connected ? 'bg-emerald-100 text-emerald-800' : 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]'
          }`}
        >
          {connected ? 'Live' : 'Offline'}
        </span>
      </div>

      <button
        type="button"
        className="mt-3 text-xs text-[var(--color-text-secondary)] underline hover:text-[var(--color-text-primary)]"
        onClick={onRefreshSerp}
      >
        Refresh SERP benchmarks
      </button>

      {scoreError && <p className="mt-2 text-sm text-red-600">{scoreError}</p>}

      {loading && (
        <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50/80 p-3 text-sm text-amber-900">
          <p className="font-medium">Building benchmarks…</p>
          <p className="mt-1 text-xs">
            {pendingReason ??
              (keyword
                ? `Analyzing top results for “${keyword}”. First load can take 20–30 seconds.`
                : 'Set a target keyword to fetch SERP data.')}
          </p>
        </div>
      )}

      {scoreUpdate?.benchmarkQuality === 'low_sample_count' && (
        <p className="mt-2 rounded-lg border border-amber-200 bg-amber-50 p-2 text-xs text-amber-900">
          Fewer than 3 competitor pages crawled — word-count targets use SERP snippets.
        </p>
      )}

      {scoreUpdate ? (
        <div className="mt-6 space-y-5">
          <div className="flex items-center gap-5">
            <div className="relative h-24 w-24 shrink-0">
              <svg className="h-24 w-24 -rotate-90" viewBox="0 0 100 100" aria-hidden>
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
                <span className="text-2xl font-bold tabular-nums text-[var(--color-metric-blue)]">{scoreUpdate.score}</span>
                <span className="text-[10px] text-[var(--color-text-secondary)]">/ 100</span>
              </div>
            </div>
            <div>
              <span className="inline-block rounded-lg bg-[var(--color-accent)] px-3 py-1 text-lg font-semibold text-white">
                {scoreUpdate.grade}
              </span>
              <p className="mt-2 text-xs text-[var(--color-text-secondary)]">Transparent 6-component score</p>
            </div>
          </div>

          <ul className="space-y-2 text-sm">
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

          {scoreUpdate.suggestions.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold">Proposed changes</h3>
              <ul className="mt-2 space-y-2 text-sm">
                {[...scoreUpdate.suggestions]
                  .sort((a, b) => b.pointValue - a.pointValue)
                  .slice(0, 5)
                  .map((s) => (
                    <li
                      key={s.id}
                      className="rounded-lg border border-[var(--color-border)] bg-white p-3 shadow-sm"
                    >
                      <div className="flex items-start justify-between gap-2">
                        <span className="shrink-0 text-xs font-medium text-emerald-700">+{s.pointValue} pts</span>
                        {s.applyMode !== 'none' && onApplySuggestion ? (
                          <button
                            type="button"
                            disabled={applyingSuggestionId === s.id}
                            className="shrink-0 rounded-md border border-[var(--color-border-strong)] bg-white px-2 py-0.5 text-[11px] font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
                            onClick={() => void onApplySuggestion(s.id)}
                          >
                            {applyingSuggestionId === s.id
                              ? 'Applying…'
                              : s.applyMode === 'ai'
                                ? 'Apply with AI'
                                : 'Apply'}
                          </button>
                        ) : null}
                      </div>
                      <p className="mt-1 font-medium text-[var(--color-text-primary)]">{s.proposedChange}</p>
                      <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{s.actionText}</p>
                    </li>
                  ))}
              </ul>
            </div>
          )}

          {scoreUpdate.eeatAdvisories.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold">E-E-A-T advisories</h3>
              <ul className="mt-2 space-y-2 text-sm">
                {scoreUpdate.eeatAdvisories.map((a) => (
                  <li key={a.code} className="rounded-lg border border-amber-100 bg-amber-50/90 p-2 text-[var(--color-text-primary)]">
                    {a.actionText}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {scoreUpdate.serpFeatures.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold">SERP features</h3>
              <ul className="mt-2 space-y-1.5 text-sm text-[var(--color-text-primary)]">
                {scoreUpdate.serpFeatures.map((f) => (
                  <li key={f.feature} className="rounded-lg border bg-white px-2 py-1.5">
                    {f.actionText}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {scoreUpdate.geoScore != null && scoreUpdate.geoComponents ? (
            <div className="rounded-xl border border-purple-100 bg-purple-50/40 p-4">
              <div className="flex items-center gap-4">
                <div className="relative h-16 w-16 shrink-0">
                  <svg className="h-16 w-16 -rotate-90" viewBox="0 0 100 100" aria-hidden>
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
                    <span className="text-lg font-bold tabular-nums text-purple-900">{scoreUpdate.geoScore}</span>
                  </div>
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-purple-950">GEO score</h3>
                  <span className="mt-1 inline-block rounded-lg bg-purple-600 px-2 py-0.5 text-sm font-semibold text-white">
                    {scoreUpdate.geoGrade ?? '—'}
                  </span>
                </div>
              </div>
              <ul className="mt-4 space-y-2 text-sm">
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

      <div className="mt-8 hidden border-t pt-4 lg:block">
        <h3 className="text-sm font-semibold">Export</h3>
        <p className="mt-1 text-xs text-[var(--color-text-secondary)]">Copy HTML for any CMS.</p>
        {onCopyHtml && (
          <button
            type="button"
            className="mt-3 w-full rounded-lg border bg-white px-3 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)]"
            onClick={onCopyHtml}
          >
            Copy HTML
          </button>
        )}
        <Link
          href="/pricing"
          className="mt-3 block text-center text-xs text-[var(--color-text-secondary)] underline hover:text-[var(--color-text-primary)]"
        >
          Plans &amp; limits
        </Link>
      </div>
    </aside>
  );
}
