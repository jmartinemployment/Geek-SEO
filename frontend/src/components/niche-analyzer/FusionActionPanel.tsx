'use client';

import { useState } from 'react';
import Link from 'next/link';
import { generateBrief, type FusedSiteUnderstanding, type FusionRecommendedAction } from '@/lib/seo-api';

type Props = {
  fusion: FusedSiteUnderstanding;
  projectId?: string;
  accessToken?: string | null;
};

const ACTION_LABELS: Record<string, string> = {
  suggest_pillar_page: 'Create pillar page',
  schema_sync: 'Add to schema',
  entity_thin_content: 'Expand content cluster',
  link_orphan_pillar: 'Add internal links',
};

function actionBadgeClass(actionType: string): string {
  switch (actionType) {
    case 'entity_thin_content':
      return 'bg-rose-50 text-rose-800';
    case 'suggest_pillar_page':
      return 'bg-blue-50 text-blue-800';
    case 'schema_sync':
      return 'bg-violet-50 text-violet-800';
    default:
      return 'bg-amber-50 text-amber-800';
  }
}

function ActionRow({
  action,
  projectId,
  accessToken,
}: {
  action: FusionRecommendedAction;
  projectId?: string;
  accessToken?: string | null;
}) {
  const [loading, setLoading] = useState(false);
  const [briefReady, setBriefReady] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleGenerateBrief() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      await generateBrief({ projectId, keyword: action.topicName }, accessToken);
      setBriefReady(true);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Brief generation failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <li className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/40 px-3 py-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${actionBadgeClass(action.actionType)}`}
            >
              {ACTION_LABELS[action.actionType] ?? action.actionType}
            </span>
            <span className="text-sm font-medium text-[var(--color-text-primary)]">
              {action.topicName}
            </span>
          </div>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{action.summary}</p>
          {error ? <p className="mt-1 text-xs text-red-600">{error}</p> : null}
          {briefReady ? (
            <p className="mt-1 text-xs text-emerald-700">Content brief generated — check Content tools.</p>
          ) : null}
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          {action.actionType === 'entity_thin_content' && projectId ? (
            <button
              type="button"
              disabled={loading || briefReady}
              onClick={() => void handleGenerateBrief()}
              className="rounded-md bg-[var(--color-accent)] px-2.5 py-1 text-xs font-medium text-white disabled:opacity-50"
            >
              {loading ? 'Generating…' : briefReady ? 'Brief ready' : 'Generate brief'}
            </button>
          ) : null}
          {action.actionType === 'suggest_pillar_page' ? (
            <Link
              href="/app/strategy/topical-map"
              className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] px-2.5 py-1 text-xs font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)]"
            >
              Open topical map
            </Link>
          ) : null}
        </div>
      </div>
    </li>
  );
}

export function FusionActionPanel({ fusion, projectId, accessToken }: Readonly<Props>) {
  const actions = fusion.recommendedActions ?? [];
  if (actions.length === 0) return null;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-4">
      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          Recommended actions
        </h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
          Draft suggestions from your fusion snapshot — review before applying (Phase E)
        </p>
      </div>
      <ul className="mt-3 space-y-2">
        {actions.slice(0, 8).map((action) => (
          <ActionRow
            key={`${action.actionType}-${action.topicSlug}`}
            action={action}
            projectId={projectId}
            accessToken={accessToken}
          />
        ))}
      </ul>
      {actions.length > 8 ? (
        <p className="mt-2 text-[10px] text-[var(--color-text-muted)]">
          +{actions.length - 8} more actions in snapshot
        </p>
      ) : null}
    </section>
  );
}
