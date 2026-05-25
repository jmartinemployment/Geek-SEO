'use client';

import Link from 'next/link';
import type { ScoreUpdate } from '@/hooks/useContentScoring';

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
}: ScoreSidebarProps) {
  const loading = benchmarkRefreshing || Boolean(pendingReason);
  const score = scoreUpdate?.score ?? 0;
  const ringOffset = 283 - (283 * score) / 100;

  return (
    <aside className="w-full shrink-0 border-t bg-zinc-50 p-6 lg:w-96 lg:border-t-0 lg:border-l">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-lg font-semibold tracking-tight">Content score</h2>
        <span
          className={`rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide ${
            connected ? 'bg-emerald-100 text-emerald-800' : 'bg-zinc-200 text-zinc-600'
          }`}
        >
          {connected ? 'Live' : 'Offline'}
        </span>
      </div>

      <button
        type="button"
        className="mt-3 text-xs text-zinc-500 underline hover:text-zinc-800"
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
                  className="text-zinc-200"
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
                  className="text-zinc-900 transition-[stroke-dashoffset] duration-300 ease-out"
                />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-2xl font-bold tabular-nums">{scoreUpdate.score}</span>
                <span className="text-[10px] text-zinc-500">/ 100</span>
              </div>
            </div>
            <div>
              <span className="inline-block rounded-lg bg-zinc-900 px-3 py-1 text-lg font-semibold text-white">
                {scoreUpdate.grade}
              </span>
              <p className="mt-2 text-xs text-zinc-500">Transparent 6-component score</p>
            </div>
          </div>

          <ul className="space-y-2 text-sm">
            {Object.entries(scoreUpdate.components).map(([key, value]) => {
              const meta = COMPONENT_META[key] ?? { label: key, max: 100 };
              const pct = Math.min(100, Math.round((value / meta.max) * 100));
              return (
                <li key={key}>
                  <div className="mb-1 flex justify-between text-xs">
                    <span className="font-medium text-zinc-700">{meta.label}</span>
                    <span className="tabular-nums text-zinc-600">
                      {value}/{meta.max}
                    </span>
                  </div>
                  <div className="h-1.5 overflow-hidden rounded-full bg-zinc-200">
                    <div
                      className="h-full rounded-full bg-zinc-800 transition-all duration-300"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                </li>
              );
            })}
          </ul>

          {scoreUpdate.suggestions.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold">Top suggestions</h3>
              <ul className="mt-2 space-y-2 text-sm">
                {[...scoreUpdate.suggestions]
                  .sort((a, b) => b.pointValue - a.pointValue)
                  .slice(0, 5)
                  .map((s, index) => (
                    <li
                      key={`${s.component}-${index}`}
                      className="rounded-lg border border-zinc-200 bg-white p-3 shadow-sm"
                    >
                      <span className="text-xs font-medium text-emerald-700">+{s.pointValue} pts</span>
                      <p className="mt-1 text-zinc-700">{s.actionText}</p>
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
                  <li key={a.code} className="rounded-lg border border-amber-100 bg-amber-50/90 p-2 text-zinc-800">
                    {a.actionText}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {scoreUpdate.serpFeatures.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold">SERP features</h3>
              <ul className="mt-2 space-y-1.5 text-sm text-zinc-700">
                {scoreUpdate.serpFeatures.map((f) => (
                  <li key={f.feature} className="rounded-lg border bg-white px-2 py-1.5">
                    {f.actionText}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      ) : (
        !loading && (
          <p className="mt-6 text-sm text-zinc-500">
            {keyword
              ? 'Edit content or wait for the first score.'
              : 'Add a target keyword, then edit to see your score.'}
          </p>
        )
      )}

      <div className="mt-8 hidden border-t pt-4 lg:block">
        <h3 className="text-sm font-semibold">Export</h3>
        <p className="mt-1 text-xs text-zinc-500">Copy HTML for any CMS.</p>
        {onCopyHtml && (
          <button
            type="button"
            className="mt-3 w-full rounded-lg border bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50"
            onClick={onCopyHtml}
          >
            Copy HTML
          </button>
        )}
        <Link
          href="/pricing"
          className="mt-3 block text-center text-xs text-zinc-500 underline hover:text-zinc-800"
        >
          Plans &amp; limits
        </Link>
      </div>
    </aside>
  );
}
