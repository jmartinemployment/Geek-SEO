'use client';

import type { NicheAnalysisStepLogEntry } from '@/lib/seo-api';
import { OUTPUT_LABELS } from '@/components/niche-analyzer/pillar-provenance';

type CrawledUrlRow = {
  url: string;
  fetchMethod: string;
  outboundLinkCount: number;
};

type Props = {
  steps: NicheAnalysisStepLogEntry[];
};

function parseCrawledUrls(outputs: Record<string, unknown>): CrawledUrlRow[] {
  const raw = outputs.sampleCrawledUrls;
  if (!Array.isArray(raw)) return [];

  return raw
    .map((item) => {
      if (!item || typeof item !== 'object') return null;
      const row = item as Record<string, unknown>;
      const url = typeof row.url === 'string' ? row.url : '';
      if (!url) return null;
      return {
        url,
        fetchMethod: typeof row.fetchMethod === 'string' ? row.fetchMethod : 'http',
        outboundLinkCount:
          typeof row.outboundLinkCount === 'number' ? row.outboundLinkCount : 0,
      };
    })
    .filter((row): row is CrawledUrlRow => row !== null);
}

function findCrawlStep(steps: NicheAnalysisStepLogEntry[]): NicheAnalysisStepLogEntry | undefined {
  return (
    steps.find((s) => s.slug === 'internal_links')
    ?? steps.find((s) => s.slug === 'site_crawl')
    ?? steps.find((s) => s.slug === 'site_structure')
  );
}

export function CrawlResultsPanel({ steps }: Readonly<Props>) {
  const crawlStep = findCrawlStep(steps);
  if (!crawlStep) return null;

  const rows = parseCrawledUrls(crawlStep.outputs);
  const pagesCrawled =
    typeof crawlStep.outputs.pagesCrawled === 'number'
      ? crawlStep.outputs.pagesCrawled
      : rows.length;
  const crawlStopReason =
    typeof crawlStep.outputs.crawlStopReason === 'string'
      ? crawlStep.outputs.crawlStopReason
      : null;

  if (rows.length === 0 && pagesCrawled <= 1) return null;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-4">
      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
          Crawl transparency
        </h3>
        <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
          Pages fetched during site crawl
        </p>
      </div>

      <dl className="mt-3 grid gap-2 text-xs sm:grid-cols-3">
        <div>
          <dt className="font-medium text-[var(--color-text-muted)]">
            {OUTPUT_LABELS.pagesCrawled}
          </dt>
          <dd className="text-[var(--color-text-secondary)]">{pagesCrawled}</dd>
        </div>
        {crawlStopReason ? (
          <div className="sm:col-span-2">
            <dt className="font-medium text-[var(--color-text-muted)]">Why crawl stopped</dt>
            <dd className="text-[var(--color-text-secondary)]">{crawlStopReason}</dd>
          </div>
        ) : null}
      </dl>

      {rows.length > 0 ? (
        <div className="mt-4 overflow-x-auto">
          <table className="w-full min-w-[28rem] text-left text-xs">
            <thead>
              <tr className="border-b border-[var(--color-border)] text-[var(--color-text-muted)]">
                <th className="py-2 pr-3 font-medium">URL</th>
                <th className="py-2 pr-3 font-medium">Fetch</th>
                <th className="py-2 font-medium text-right">Outbound links</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr
                  key={row.url}
                  className="border-b border-[var(--color-border)]/60 last:border-0"
                >
                  <td className="max-w-[16rem] truncate py-2 pr-3 text-[var(--color-text-secondary)]" title={row.url}>
                    {row.url}
                  </td>
                  <td className="py-2 pr-3 uppercase tracking-wide text-[var(--color-text-muted)]">
                    {row.fetchMethod}
                  </td>
                  <td className="py-2 text-right tabular-nums text-[var(--color-text-secondary)]">
                    {row.outboundLinkCount}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}
