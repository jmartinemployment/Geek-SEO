'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  getCompetitors,
  refreshCompetitorCrawl,
  type CompetitorInsights,
} from '@/lib/seo-api';

type CompetitorPanelProps = {
  documentId: string;
  accessToken: string | null;
};

export function CompetitorPanel({ documentId, accessToken }: CompetitorPanelProps) {
  const [data, setData] = useState<CompetitorInsights | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [crawling, setCrawling] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    setData(await getCompetitors(documentId, accessToken));
  }, [documentId, accessToken]);

  useEffect(() => {
    void (async () => {
      try {
        const initial = await getCompetitors(documentId, accessToken);
        setData(initial);
        const needsCrawl =
          initial.pages.length > 0 &&
          initial.pages.some((p) => p.wordCount <= 0) &&
          initial.crawlStatus !== 'complete';
        if (needsCrawl) {
          setCrawling(true);
          try {
            const refreshed = await refreshCompetitorCrawl(documentId, accessToken);
            setData(refreshed);
          } finally {
            setCrawling(false);
          }
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load competitors');
      }
    })();
  }, [documentId, accessToken, load]);

  async function refreshCompetitorsNow() {
    setRefreshing(true);
    setError(null);
    try {
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to refresh competitors');
    } finally {
      setRefreshing(false);
    }
  }

  if (error) return <p className="text-xs text-red-600">{error}</p>;
  if (!data) return <p className="text-xs text-[var(--color-text-secondary)]">Loading competitors…</p>;

  return (
    <div className="mt-6 border-t pt-4">
      <h3 className="text-sm font-semibold">Top competitors</h3>
      <p className="text-xs text-[var(--color-text-secondary)]">
        {data.keyword} · {data.location}
      </p>
      {crawling && (
        <p className="mt-2 text-xs text-[var(--color-text-secondary)]">Crawling competitor pages…</p>
      )}
      {!crawling && data.crawlStatus !== 'complete' ? (
        <div className="mt-2 flex items-center gap-2">
          <button
            type="button"
            onClick={() => {
              void refreshCompetitorsNow();
            }}
            disabled={refreshing}
            className="rounded-md border px-2.5 py-1 text-[11px] font-medium hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {refreshing ? 'Refreshing…' : 'Refresh crawl status'}
          </button>
          <span className="text-[11px] text-[var(--color-text-secondary)]">
            Crawl updates are manual on this panel.
          </span>
        </div>
      ) : null}
      {data.benchmarkQuality === 'low_sample_count' && (
        <p className="mt-2 text-xs text-amber-800">Limited crawl data — refresh SERP to improve benchmarks.</p>
      )}
      <ul className="mt-3 max-h-48 space-y-2 overflow-y-auto text-xs">
        {data.pages.map((p) => (
          <li key={p.url} className="rounded border bg-white p-2">
            <div className="font-medium text-[var(--color-text-primary)]">
              #{p.position} {p.domain ?? p.url}
            </div>
            <p className="truncate text-[var(--color-text-secondary)]">{p.metaTitle ?? p.url}</p>
            <p className="text-[var(--color-text-secondary)]">{p.wordCount > 0 ? `${p.wordCount} words` : 'Not crawled yet'}</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
