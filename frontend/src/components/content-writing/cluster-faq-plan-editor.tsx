'use client';

import {
  type ContentLinkFaqItem,
  type ContentSpokeSummary,
} from '@/lib/seo-api';

function isSpokeGenerated(spoke: ContentSpokeSummary): boolean {
  return spoke.status === 'body_generated' || spoke.wordCount > 80;
}

export type FaqLinkStatus = 'none' | 'pending' | 'linked';

export function resolveFaqLinkStatus(
  item: ContentLinkFaqItem,
  spokes: ContentSpokeSummary[],
): FaqLinkStatus {
  if (!item.targetDocumentId && !item.targetPath) {
    return 'none';
  }

  const spoke = item.targetDocumentId
    ? spokes.find((s) => s.id === item.targetDocumentId)
    : spokes.find((s) => {
        const slug = s.publishSlug;
        return slug && item.targetPath?.includes(slug);
      });

  if (!spoke) {
    return item.targetPath ? 'pending' : 'none';
  }

  return isSpokeGenerated(spoke) ? 'linked' : 'pending';
}

function linkStatusLabel(status: FaqLinkStatus): string {
  switch (status) {
    case 'linked':
      return 'Link ready';
    case 'pending':
      return 'Link pending — generate spoke';
    default:
      return 'No link target';
  }
}

function linkStatusClass(status: FaqLinkStatus): string {
  switch (status) {
    case 'linked':
      return 'bg-emerald-50 text-emerald-800 border-emerald-200';
    case 'pending':
      return 'bg-amber-50 text-amber-900 border-amber-200';
    default:
      return 'bg-slate-50 text-slate-600 border-slate-200';
  }
}

type ClusterFaqPlanEditorProps = {
  items: ContentLinkFaqItem[];
  spokes: ContentSpokeSummary[];
  dirty: boolean;
  saving: boolean;
  onChange: (items: ContentLinkFaqItem[]) => void;
  onSave: () => void;
};

function applySpokeTarget(
  item: ContentLinkFaqItem,
  spokeId: string,
  spokes: ContentSpokeSummary[],
): ContentLinkFaqItem {
  if (!spokeId) {
    return {
      ...item,
      targetDocumentId: null,
      targetPath: null,
    };
  }

  const spoke = spokes.find((s) => s.id === spokeId);
  if (!spoke) {
    return item;
  }

  return {
    ...item,
    targetDocumentId: spoke.id,
    targetPath: spoke.publishSlug ? `/blog/${spoke.publishSlug}` : null,
    anchorText: item.anchorText?.trim()
      ? item.anchorText
      : spoke.spokeSourcePhrase ?? spoke.title,
  };
}

export function ClusterFaqPlanEditor({
  items,
  spokes,
  dirty,
  saving,
  onChange,
  onSave,
}: ClusterFaqPlanEditorProps) {
  if (items.length === 0) {
    return null;
  }

  function updateItem(index: number, patch: Partial<ContentLinkFaqItem>) {
    onChange(items.map((item, i) => (i === index ? { ...item, ...patch } : item)));
  }

  return (
    <section className="mb-4 space-y-3 text-xs">
      <div className="flex items-center justify-between gap-2">
        <h4 className="font-medium text-[var(--color-text-primary)]">
          FAQ plan editor ({items.length})
        </h4>
        <button
          type="button"
          onClick={onSave}
          disabled={!dirty || saving}
          className="rounded border px-2 py-1 font-medium text-[var(--color-text-primary)] disabled:opacity-50"
        >
          {saving ? 'Saving…' : dirty ? 'Save plan' : 'Saved'}
        </button>
      </div>

      <ul className="space-y-3">
        {items.map((item, index) => {
          const linkStatus = resolveFaqLinkStatus(item, spokes);
          const selectedSpokeId = item.targetDocumentId ?? '';

          return (
            <li key={`${item.question}-${index}`} className="space-y-2 rounded-md border px-2 py-2">
              <div className="flex items-start justify-between gap-2">
                <span
                  className={`rounded border px-1.5 py-0.5 text-[10px] font-medium ${linkStatusClass(linkStatus)}`}
                >
                  {linkStatusLabel(linkStatus)}
                </span>
                {item.source ? (
                  <span className="text-[var(--color-text-secondary)]">{item.source}</span>
                ) : null}
              </div>

              <label className="block font-medium">
                Question
                <textarea
                  className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
                  rows={2}
                  value={item.question}
                  onChange={(e) => updateItem(index, { question: e.target.value })}
                />
              </label>

              <label className="block font-medium">
                Link target spoke
                <select
                  className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
                  value={selectedSpokeId}
                  onChange={(e) =>
                    onChange(
                      items.map((row, i) =>
                        i === index ? applySpokeTarget(row, e.target.value, spokes) : row,
                      ),
                    )
                  }
                >
                  <option value="">No spoke link</option>
                  {spokes.map((spoke) => (
                    <option key={spoke.id} value={spoke.id}>
                      {spoke.title}
                      {spoke.publishSlug ? ` · /blog/${spoke.publishSlug}` : ''}
                      {isSpokeGenerated(spoke) ? ' · generated' : ' · shell'}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block font-medium">
                Anchor text
                <input
                  className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
                  value={item.anchorText ?? ''}
                  placeholder="Phrase wrapped by the link in the FAQ answer"
                  onChange={(e) => updateItem(index, { anchorText: e.target.value })}
                />
              </label>

              {item.targetPath ? (
                <p className="text-[var(--color-text-secondary)]">Target path: {item.targetPath}</p>
              ) : null}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
