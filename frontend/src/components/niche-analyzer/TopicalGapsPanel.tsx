'use client';

import { useState } from 'react';
import type { TopicalGapSummary } from '@/lib/seo-api';

type Props = {
  gaps: TopicalGapSummary[];
  onQuickWinsToggle?: (quickWinsOnly: boolean) => void;
};

const FORMAT_ICONS: Record<string, string> = {
  how_to: '🔧',
  listicle: '📋',
  comparison: '⚖️',
  definition: '📖',
  faq: '❓',
  case_study: '📊',
  local_page: '📍',
  tool_review: '🛠️',
};

export function TopicalGapsPanel({ gaps, onQuickWinsToggle }: Props) {
  const [quickWinsOnly, setQuickWinsOnly] = useState(false);

  const displayed = quickWinsOnly ? gaps.filter((g) => g.isQuickWin) : gaps;

  function toggle() {
    const next = !quickWinsOnly;
    setQuickWinsOnly(next);
    onQuickWinsToggle?.(next);
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <span className="text-sm font-medium text-[var(--color-text-primary)]">
            {displayed.length} gap{displayed.length !== 1 ? 's' : ''}
          </span>
          {quickWinsOnly && (
            <span className="ml-2 text-xs text-[var(--color-text-muted)]">
              (KD &lt; 35, volume &gt; 100)
            </span>
          )}
        </div>
        <button
          onClick={toggle}
          className={`rounded-lg border px-3 py-1.5 text-xs font-medium transition-colors ${
            quickWinsOnly
              ? 'border-emerald-300 bg-emerald-50 text-emerald-700'
              : 'border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-hover)]'
          }`}
        >
          {quickWinsOnly ? '✓ Quick wins only' : 'Show quick wins'}
        </button>
      </div>

      {displayed.length === 0 && (
        <div className="rounded-xl border border-[var(--color-border)] p-8 text-center text-sm text-[var(--color-text-muted)]">
          No gaps found.
        </div>
      )}

      <div className="space-y-2">
        {displayed.map((gap) => (
          <div
            key={gap.subtopicId}
            className="flex items-start gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3"
          >
            <span className="mt-0.5 text-base" title={gap.recommendedFormat}>
              {FORMAT_ICONS[gap.recommendedFormat] ?? '📄'}
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <p className="truncate font-medium text-[var(--color-text-primary)]">
                  {gap.subtopicTitle}
                </p>
                {gap.isQuickWin && (
                  <span className="shrink-0 rounded-full bg-emerald-100 px-1.5 py-0.5 text-xs font-medium text-emerald-700">
                    Quick win
                  </span>
                )}
              </div>
              <p className="mt-0.5 truncate text-xs text-[var(--color-text-muted)]">
                {gap.targetKeyword} · {gap.pillarTopic}
              </p>
            </div>
            <div className="shrink-0 text-right text-xs text-[var(--color-text-muted)]">
              <div className="font-medium text-[var(--color-text-secondary)]">
                KD {gap.keywordDifficulty.toFixed(0)}
              </div>
              <div className="capitalize">{gap.fixEffort}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
