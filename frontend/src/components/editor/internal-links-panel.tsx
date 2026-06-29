'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { contentWritingPath } from '@/lib/content-writing-search-params';
import { autoInsertInternalLink, suggestInternalLinks, type InternalLinkSuggestion } from '@/lib/seo-api';

const LINK_TYPE_META: Record<
  InternalLinkSuggestion['linkType'],
  { label: string; className: string }
> = {
  spoke: { label: 'Spoke', className: 'bg-teal-50 text-teal-800 border-teal-200' },
  pillar: { label: 'Pillar', className: 'bg-indigo-50 text-indigo-800 border-indigo-200' },
  sibling: { label: 'Sibling', className: 'bg-slate-50 text-slate-700 border-slate-200' },
};

type InternalLinksPanelProps = Readonly<{
  projectId: string;
  documentId: string;
  accessToken?: string | null;
  onInsertLink: (href: string, anchorText: string) => void;
  onAutoInsertHtml?: (html: string) => void;
}>;

export function InternalLinksPanel({
  projectId,
  documentId,
  accessToken,
  onInsertLink,
  onAutoInsertHtml,
}: InternalLinksPanelProps) {
  const [suggestions, setSuggestions] = useState<InternalLinkSuggestion[]>([]);
  const [loading, setLoading] = useState(false);
  const [autoBusy, setAutoBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await suggestInternalLinks({ projectId, documentId }, accessToken);
      setSuggestions(items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not load link suggestions');
      setSuggestions([]);
    } finally {
      setLoading(false);
    }
  }, [projectId, documentId, accessToken]);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      setLoading(true);
      setError(null);
      try {
        const items = await suggestInternalLinks({ projectId, documentId }, accessToken);
        if (!cancelled) setSuggestions(items);
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Could not load link suggestions');
          setSuggestions([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void init();
    return () => {
      cancelled = true;
    };
  }, [projectId, documentId, accessToken]);

  async function runAutoInsert() {
    setAutoBusy(true);
    setError(null);
    setNotice(null);
    try {
      const result = await autoInsertInternalLink({ projectId, documentId }, accessToken);
      if (result.inserted && onAutoInsertHtml) {
        onAutoInsertHtml(result.contentHtml);
        setNotice(`Inserted link to "${result.anchorText ?? 'related article'}".`);
      } else {
        setNotice(result.message ?? 'No new link inserted.');
      }
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Auto-insert failed');
    } finally {
      setAutoBusy(false);
    }
  }

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Internal links</h2>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
            Cluster spokes, parent pillar, and related project articles — insert publish paths when available.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            disabled={loading || autoBusy}
            onClick={() => void runAutoInsert()}
            className="rounded-lg bg-[var(--color-accent)] px-2.5 py-1 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {autoBusy ? 'Inserting…' : 'Auto-insert top link'}
          </button>
          <button
            type="button"
            disabled={loading || autoBusy}
            onClick={() => void load()}
            className="rounded-lg border border-[var(--color-border)] px-2.5 py-1 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
          >
            {loading ? 'Loading…' : 'Refresh'}
          </button>
        </div>
      </div>

      {notice ? <p className="mt-3 text-xs text-green-800">{notice}</p> : null}
      {error ? <p className="mt-3 text-sm text-red-700">{error}</p> : null}

      {loading && suggestions.length === 0 ? (
        <p className="mt-3 text-xs text-[var(--color-text-secondary)]">Finding related pages…</p>
      ) : null}

      {!loading && suggestions.length === 0 && !error ? (
        <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
          No related documents yet. Build a cluster or add more articles with overlapping keywords.
        </p>
      ) : null}

      {suggestions.length > 0 ? (
        <ul className="mt-3 max-h-72 space-y-2 overflow-y-auto text-xs">
          {suggestions.map((item) => {
            const typeMeta = LINK_TYPE_META[item.linkType] ?? LINK_TYPE_META.sibling;
            return (
              <li
                key={`${item.targetDocumentId}-${item.targetUrl}`}
                className="rounded-lg border border-slate-200 p-3"
              >
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <p className="font-medium text-[var(--color-text-primary)]">{item.anchorText}</p>
                  <span
                    className={`rounded border px-1.5 py-0.5 text-[10px] font-medium ${typeMeta.className}`}
                  >
                    {typeMeta.label}
                  </span>
                </div>
                <p className="mt-1 text-[var(--color-text-secondary)]">{item.reason}</p>
                <p className="mt-1 font-mono text-[10px] text-[var(--color-text-muted)]">
                  {item.publishPath ?? item.targetUrl}
                </p>
                <div className="mt-2 flex flex-wrap gap-2">
                  <button
                    type="button"
                    className="rounded border bg-white px-2 py-1 font-medium hover:bg-slate-50"
                    onClick={() => onInsertLink(item.targetUrl, item.anchorText)}
                  >
                    Insert link
                  </button>
                  <Link
                    href={contentWritingPath({ documentId: item.targetDocumentId })}
                    className="rounded border px-2 py-1 font-medium text-[var(--color-brand)] hover:bg-slate-50"
                  >
                    Open doc
                  </Link>
                </div>
              </li>
            );
          })}
        </ul>
      ) : null}
    </section>
  );
}
