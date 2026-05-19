'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
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
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const load = useCallback(async () => {
    setData(await getCompetitors(documentId, accessToken));
  }, [documentId, accessToken]);

  const stopPolling = useCallback(() => {
    if (pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }, []);

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
            if (refreshed.crawlStatus !== 'complete') {
              pollRef.current = setInterval(() => {
                void load().catch(() => undefined);
              }, 4000);
            }
          } finally {
            setCrawling(false);
          }
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load competitors');
      }
    })();
    return stopPolling;
  }, [documentId, accessToken, load, stopPolling]);

  useEffect(() => {
    if (data?.crawlStatus === 'complete') stopPolling();
  }, [data?.crawlStatus, stopPolling]);

  if (error) return <p className="text-xs text-red-600">{error}</p>;
  if (!data) return <p className="text-xs text-zinc-500">Loading competitors…</p>;

  return (
    <div className="mt-6 border-t pt-4">
      <h3 className="text-sm font-semibold">Top competitors</h3>
      <p className="text-xs text-zinc-500">
        {data.keyword} · {data.location}
      </p>
      {crawling && (
        <p className="mt-2 text-xs text-zinc-600">Crawling competitor pages…</p>
      )}
      {data.benchmarkQuality === 'low_sample_count' && (
        <p className="mt-2 text-xs text-amber-800">Limited crawl data — refresh SERP to improve benchmarks.</p>
      )}
      <ul className="mt-3 max-h-48 space-y-2 overflow-y-auto text-xs">
        {data.pages.map((p) => (
          <li key={p.url} className="rounded border bg-white p-2">
            <div className="font-medium text-zinc-800">
              #{p.position} {p.domain ?? p.url}
            </div>
            <p className="truncate text-zinc-500">{p.metaTitle ?? p.url}</p>
            <p className="text-zinc-600">{p.wordCount > 0 ? `${p.wordCount} words` : 'Not crawled yet'}</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
