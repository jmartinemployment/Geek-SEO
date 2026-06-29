'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
  generateBrief,
  type ContentBrief,
  type SiteTopicProfile,
  type PillarRecommendedAction,
} from '@/lib/seo-api';
import { ContentBriefInline } from '@/components/niche-analyzer/ContentBriefInline';
import {
  buildKnowsAboutSyncSnippet,
  orphanLinkSuggestions,
} from '@/components/niche-analyzer/fusion-action-helpers';

type Props = {
  fusion: SiteTopicProfile;
  projectId?: string;
  accessToken?: string | null;
};

const ACTION_LABELS: Record<string, string> = {
  suggest_pillar_page: 'Create pillar page',
  suggest_local_page: 'Create location page',
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
    case 'suggest_local_page':
      return 'bg-teal-50 text-teal-800';
    case 'schema_sync':
      return 'bg-violet-50 text-violet-800';
    default:
      return 'bg-amber-50 text-amber-800';
  }
}

function topicalMapHref(projectId: string | undefined, seed: string): string {
  const params = new URLSearchParams();
  if (projectId) params.set('projectId', projectId);
  params.set('seed', seed);
  const query = params.toString();
  return query ? `/strategy/topical-map?${query}` : '/strategy/topical-map';
}

function ActionRow({
  action,
  fusion,
  projectId,
  accessToken,
}: {
  action: PillarRecommendedAction;
  fusion: SiteTopicProfile;
  projectId?: string;
  accessToken?: string | null;
}) {
  const [loading, setLoading] = useState(false);
  const [brief, setBrief] = useState<ContentBrief | null>(null);
  const [schemaCopied, setSchemaCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleGenerateBrief() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      setBrief(await generateBrief({ projectId, keyword: action.topicName }, accessToken));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Brief generation failed');
    } finally {
      setLoading(false);
    }
  }

  async function handleCopySchemaSnippet() {
    const { snippet } = buildKnowsAboutSyncSnippet(fusion, action.topicName);
    try {
      await navigator.clipboard.writeText(snippet);
      setSchemaCopied(true);
      window.setTimeout(() => setSchemaCopied(false), 2000);
    } catch {
      setError('Could not copy to clipboard');
    }
  }

  const schemaSync =
    action.actionType === 'schema_sync'
      ? buildKnowsAboutSyncSnippet(fusion, action.topicName)
      : null;
  const linkTargets =
    action.actionType === 'link_orphan_pillar'
      ? orphanLinkSuggestions(fusion, action.topicSlug)
      : [];

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
        </div>
        <div className="flex shrink-0 flex-wrap gap-2">
          {action.actionType === 'entity_thin_content' && projectId ? (
            <button
              type="button"
              disabled={loading}
              onClick={() => void handleGenerateBrief()}
              className="rounded-md bg-[var(--color-accent)] px-2.5 py-1 text-xs font-medium text-white disabled:opacity-50"
            >
              {loading ? 'Generating…' : brief ? 'Refresh brief' : 'Generate brief'}
            </button>
          ) : null}
          {action.actionType === 'schema_sync' ? (
            <button
              type="button"
              onClick={() => void handleCopySchemaSnippet()}
              className="rounded-md border border-violet-200 bg-violet-50 px-2.5 py-1 text-xs font-medium text-violet-900 hover:bg-violet-100"
            >
              {schemaCopied ? 'Copied!' : 'Copy knowsAbout'}
            </button>
          ) : null}
          {action.actionType === 'suggest_pillar_page' || action.actionType === 'suggest_local_page' ? (
            <Link
              href={topicalMapHref(projectId, action.topicName)}
              className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] px-2.5 py-1 text-xs font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)]"
            >
              Plan in topical map
            </Link>
          ) : null}
        </div>
      </div>

      {schemaSync ? (
        <div className="mt-3 rounded-lg border border-violet-100 bg-violet-50/50 px-3 py-2">
          <p className="text-[10px] font-medium uppercase tracking-wide text-violet-800">
            Paste into homepage JSON-LD
          </p>
          <pre className="mt-1 overflow-x-auto whitespace-pre-wrap break-words font-mono text-[10px] text-violet-950">
            {schemaSync.snippet}
          </pre>
          <p className="mt-1 text-[10px] text-violet-700">
            Merges {schemaSync.knowsAbout.length} topics — paste into your homepage LocalBusiness /
            ProfessionalService JSON-LD wherever you manage site schema.
          </p>
        </div>
      ) : null}

      {linkTargets.length > 0 ? (
        <p className="mt-2 text-[10px] text-[var(--color-text-muted)]">
          Link <strong>{action.topicName}</strong> from: {linkTargets.join(', ')}
        </p>
      ) : null}

      {brief ? <ContentBriefInline brief={brief} /> : null}
    </li>
  );
}

export function PillarActionPanel({ fusion, projectId, accessToken }: Readonly<Props>) {
  const actions = fusion.recommendedActions ?? [];
  if (actions.length === 0) return null;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-4">
      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          Recommended actions
        </h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
          Draft suggestions from your fusion snapshot — review before applying
        </p>
      </div>
      <ul className="mt-3 space-y-2">
        {actions.slice(0, 8).map((action) => (
          <ActionRow
            key={`${action.actionType}-${action.topicSlug}`}
            action={action}
            fusion={fusion}
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
