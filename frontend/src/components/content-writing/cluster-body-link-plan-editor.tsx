'use client';

import {
  type ContentLinkBodySlot,
  type ContentSpokeSummary,
} from '@/lib/seo-api';

function isSpokeGenerated(spoke: ContentSpokeSummary): boolean {
  return spoke.status === 'body_generated' || spoke.wordCount > 80;
}

export type BodyLinkStatus = 'none' | 'pending' | 'linked';

export function resolveBodyLinkStatus(
  item: ContentLinkBodySlot,
  spokes: ContentSpokeSummary[],
): BodyLinkStatus {
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

function linkStatusLabel(status: BodyLinkStatus): string {
  switch (status) {
    case 'linked':
      return 'Link ready';
    case 'pending':
      return 'Link pending — generate spoke';
    default:
      return 'No link target';
  }
}

function linkStatusClass(status: BodyLinkStatus): string {
  switch (status) {
    case 'linked':
      return 'bg-emerald-50 text-emerald-800 border-emerald-200';
    case 'pending':
      return 'bg-amber-50 text-amber-900 border-amber-200';
    default:
      return 'bg-slate-50 text-slate-600 border-slate-200';
  }
}

type ClusterBodyLinkPlanEditorProps = {
  items: ContentLinkBodySlot[];
  spokes: ContentSpokeSummary[];
  headingHints: string[];
  onChange: (items: ContentLinkBodySlot[]) => void;
};

function applySpokeTarget(
  item: ContentLinkBodySlot,
  spokeId: string,
  spokes: ContentSpokeSummary[],
): ContentLinkBodySlot {
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

export function ClusterBodyLinkPlanEditor({
  items,
  spokes,
  headingHints,
  onChange,
}: ClusterBodyLinkPlanEditorProps) {
  if (items.length === 0) {
    return (
      <p className="text-sm text-[var(--color-text-secondary)]">
        No body link slots yet. Build a cluster plan after your pillar has H2 sections.
      </p>
    );
  }

  function updateItem(index: number, patch: Partial<ContentLinkBodySlot>) {
    onChange(items.map((item, i) => (i === index ? { ...item, ...patch } : item)));
  }

  return (
    <div className="space-y-3 text-xs">
      <ul className="grid gap-3 lg:grid-cols-2">
        {items.map((item, index) => {
          const linkStatus = resolveBodyLinkStatus(item, spokes);
          const selectedSpokeId = item.targetDocumentId ?? '';
          const hintValue = item.insertAfterH2Hint ?? '';

          return (
            <li key={`${hintValue}-${index}`} className="space-y-2 rounded-lg border bg-white px-3 py-3 shadow-sm">
              <div className="flex items-start justify-between gap-2">
                <span
                  className={`rounded border px-1.5 py-0.5 text-[10px] font-medium ${linkStatusClass(linkStatus)}`}
                >
                  {linkStatusLabel(linkStatus)}
                </span>
                <span className="text-[var(--color-text-secondary)]">Priority {item.priority ?? index + 1}</span>
              </div>

              <label className="block font-medium">
                Insert after H2
                <input
                  list={`body-h2-hints-${index}`}
                  className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
                  value={hintValue}
                  placeholder="H2 id or heading text"
                  onChange={(e) => updateItem(index, { insertAfterH2Hint: e.target.value })}
                />
                {headingHints.length > 0 ? (
                  <datalist id={`body-h2-hints-${index}`}>
                    {headingHints.map((hint) => (
                      <option key={hint} value={hint} />
                    ))}
                  </datalist>
                ) : null}
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
                  placeholder="Phrase used in the contextual link"
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
    </div>
  );
}
