'use client';

import { useState } from 'react';
import Link from 'next/link';
import { generateBrief, type ContentBrief, type TopicalGapSummary } from '@/lib/seo-api';
import { ContentBriefInline } from '@/components/niche-analyzer/ContentBriefInline';

type Props = {
  gaps: TopicalGapSummary[];
  projectId?: string;
  accessToken?: string | null;
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

function topicalMapHref(projectId: string | undefined, seed: string): string {
  const params = new URLSearchParams();
  if (projectId) params.set('projectId', projectId);
  params.set('seed', seed);
  const query = params.toString();
  return query ? `/app/strategy/topical-map?${query}` : '/app/strategy/topical-map';
}

function GapRow({
  gap,
  projectId,
  accessToken,
}: {
  gap: TopicalGapSummary;
  projectId?: string;
  accessToken?: string | null;
}) {
  const [loading, setLoading] = useState(false);
  const [brief, setBrief] = useState<ContentBrief | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleGenerateBrief() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      setBrief(await generateBrief({ projectId, keyword: gap.targetKeyword }, accessToken));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Brief generation failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
      <div className="flex items-start gap-3">
        <span className="mt-0.5 text-base" title={gap.recommendedFormat}>
          {FORMAT_ICONS[gap.recommendedFormat] ?? '📄'}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <p className="truncate font-medium text-[var(--color-text-primary)]">
              {gap.subtopicTitle}
            </p>
            {gap.isQuickWin ? (
              <span className="shrink-0 rounded-full bg-emerald-100 px-1.5 py-0.5 text-xs font-medium text-emerald-700">
                Quick win
              </span>
            ) : null}
          </div>
          <p className="mt-0.5 truncate text-xs text-[var(--color-text-muted)]">
            {gap.targetKeyword} · {gap.pillarTopic}
          </p>
          {error ? <p className="mt-1 text-xs text-red-600">{error}</p> : null}
          {projectId ? (
            <div className="mt-2 flex flex-wrap gap-2">
              <Link
                href={topicalMapHref(projectId, gap.targetKeyword)}
                className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-muted)]/50 px-2 py-1 text-[10px] font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)]"
              >
                Plan in topical map
              </Link>
              <button
                type="button"
                disabled={loading}
                onClick={() => void handleGenerateBrief()}
                className="rounded-md bg-[var(--color-accent)] px-2 py-1 text-[10px] font-medium text-white disabled:opacity-50"
              >
                {loading ? 'Generating…' : brief ? 'Refresh brief' : 'Generate brief'}
              </button>
            </div>
          ) : null}
          {brief ? <ContentBriefInline brief={brief} /> : null}
        </div>
        <div className="shrink-0 text-right text-xs text-[var(--color-text-muted)]">
          <div className="font-medium text-[var(--color-text-secondary)]">
            KD {gap.keywordDifficulty.toFixed(0)}
          </div>
          <div className="capitalize">{gap.fixEffort}</div>
        </div>
      </div>
    </div>
  );
}

export function TopicalGapsPanel({
  gaps,
  projectId,
  accessToken,
  onQuickWinsToggle,
}: Readonly<Props>) {
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
          {quickWinsOnly ? (
            <span className="ml-2 text-xs text-[var(--color-text-muted)]">
              (KD &lt; 35, volume &gt; 100)
            </span>
          ) : null}
        </div>
        <button
          type="button"
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

      {displayed.length === 0 ? (
        <div className="rounded-xl border border-[var(--color-border)] p-8 text-center text-sm text-[var(--color-text-muted)]">
          No gaps found.
        </div>
      ) : (
        <div className="space-y-2">
          {displayed.map((gap) => (
            <GapRow
              key={gap.subtopicId}
              gap={gap}
              projectId={projectId}
              accessToken={accessToken}
            />
          ))}
        </div>
      )}
    </div>
  );
}
