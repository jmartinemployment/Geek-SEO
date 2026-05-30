'use client';

import { AuthStartLink } from '@/components/auth/auth-start-link';
import { useEffect, useState } from 'react';
import { Skeleton } from '@/components/ui/skeleton';

type ScanResultsPanelProps = {
  url: string;
};

type PublicScanResponse = {
  url: string;
  performanceScore?: number | null;
  seoScore?: number | null;
  accessibilityScore?: number | null;
  lcp?: string | null;
  cls?: string | null;
  inp?: string | null;
  title?: string | null;
  metaDescription?: string | null;
  h1?: string | null;
  canonical?: string | null;
  robotsTxtFound?: boolean | null;
  pageSpeedAvailable: boolean;
  nextSteps?: string[];
};

type ScanState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: PublicScanResponse };

function ScoreGauge({ label, score }: { label: string; score: number }) {
  const color =
    score >= 90
      ? 'text-[var(--color-good)]'
      : score >= 50
        ? 'text-[var(--color-warn)]'
        : 'text-[var(--color-bad)]';

  return (
    <div className="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white p-4 shadow-[var(--shadow-card)]">
      <p className="text-xs font-medium uppercase tracking-wide text-[var(--color-text-secondary)]">{label}</p>
      <p className={`mt-2 text-3xl font-bold tabular-nums ${color}`}>{score}</p>
    </div>
  );
}

function LoadingBlocks() {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <Skeleton className="h-28 rounded-[var(--radius-card)]" />
      <Skeleton className="h-28 rounded-[var(--radius-card)]" />
      <Skeleton className="h-28 rounded-[var(--radius-card)]" />
    </div>
  );
}

function formatOptional(value: string | null | undefined, fallback: string) {
  return value && value.length > 0 ? value : fallback;
}

export function ScanResultsPanel({ url }: ScanResultsPanelProps) {
  const [state, setState] = useState<ScanState>({ status: 'loading' });

  useEffect(() => {
    let cancelled = false;

    async function loadScan() {
      setState({ status: 'loading' });
      try {
        const res = await fetch(`/api/public/scan?url=${encodeURIComponent(url)}`);
        const body = (await res.json()) as PublicScanResponse & { error?: string };
        if (!res.ok) {
          if (!cancelled) {
            setState({
              status: 'error',
              message: body.error ?? 'Could not scan that website. Check the URL and try again.',
            });
          }
          return;
        }

        if (!cancelled) {
          setState({ status: 'ready', data: body });
        }
      } catch {
        if (!cancelled) {
          setState({
            status: 'error',
            message: 'Scan service is unavailable. Make sure GeekSeoBackend is running on port 5051.',
          });
        }
      }
    }

    void loadScan();

    return () => {
      cancelled = true;
    };
  }, [url]);

  if (state.status === 'loading') {
    return (
      <div>
        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">Scanning {url}</h2>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          Fetching on-page SEO signals{state.status === 'loading' ? '…' : ''}
        </p>
        <div className="mt-6">
          <LoadingBlocks />
        </div>
      </div>
    );
  }

  if (state.status === 'error') {
    return (
      <div className="rounded-[var(--radius-card)] border border-red-200 bg-red-50 px-5 py-4">
        <h2 className="text-lg font-semibold text-red-900">Scan failed</h2>
        <p className="mt-2 text-sm text-red-800">{state.message}</p>
      </div>
    );
  }

  const { data } = state;
  const hasPageSpeed =
    data.pageSpeedAvailable
    && data.performanceScore != null
    && data.seoScore != null
    && data.accessibilityScore != null;

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">Results for {data.url}</h2>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          Free preview — sign up to run full site audit, topical map, and competitor analysis.
        </p>
      </div>

      {hasPageSpeed ? (
        <div>
          <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">Page speed (Google PageSpeed)</h3>
          <div className="mt-3 grid gap-3 sm:grid-cols-3">
            <ScoreGauge label="Performance" score={data.performanceScore!} />
            <ScoreGauge label="SEO" score={data.seoScore!} />
            <ScoreGauge label="Accessibility" score={data.accessibilityScore!} />
          </div>
          <dl className="mt-4 grid gap-2 text-sm sm:grid-cols-3">
            <div className="rounded-lg border border-[var(--color-border)] bg-white px-3 py-2">
              <dt className="text-xs text-[var(--color-text-secondary)]">LCP</dt>
              <dd className="font-medium tabular-nums text-[var(--color-text-primary)]">
                {formatOptional(data.lcp, '—')}
              </dd>
            </div>
            <div className="rounded-lg border border-[var(--color-border)] bg-white px-3 py-2">
              <dt className="text-xs text-[var(--color-text-secondary)]">CLS</dt>
              <dd className="font-medium tabular-nums text-[var(--color-text-primary)]">
                {formatOptional(data.cls, '—')}
              </dd>
            </div>
            <div className="rounded-lg border border-[var(--color-border)] bg-white px-3 py-2">
              <dt className="text-xs text-[var(--color-text-secondary)]">INP</dt>
              <dd className="font-medium tabular-nums text-[var(--color-text-primary)]">
                {formatOptional(data.inp, '—')}
              </dd>
            </div>
          </dl>
        </div>
      ) : (
        <p className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm text-[var(--color-text-secondary)]">
          PageSpeed scores are not configured yet (set GOOGLE_PSI_API_KEY on GeekSeoBackend). On-page signals below are live.
        </p>
      )}

      <div>
        <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">On-page signals</h3>
        <ul className="mt-3 space-y-2 text-sm">
          {[
            { label: 'Title', value: formatOptional(data.title, 'Not found') },
            { label: 'Meta description', value: formatOptional(data.metaDescription, 'Not found') },
            { label: 'H1', value: formatOptional(data.h1, 'Not found') },
            { label: 'Canonical', value: formatOptional(data.canonical, 'Not found') },
            {
              label: 'robots.txt',
              value:
                data.robotsTxtFound === undefined || data.robotsTxtFound === null
                  ? 'Unknown'
                  : data.robotsTxtFound
                    ? 'Found'
                    : 'Not found',
            },
          ].map((row) => (
            <li
              key={row.label}
              className="flex flex-col gap-1 rounded-lg border border-[var(--color-border)] bg-white px-3 py-2 sm:flex-row sm:items-center sm:justify-between"
            >
              <span className="font-medium text-[var(--color-text-primary)]">{row.label}</span>
              <span className="text-[var(--color-text-secondary)]">{row.value}</span>
            </li>
          ))}
        </ul>
      </div>

      <div className="rounded-[var(--radius-card)] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-bg)] p-4">
        <p className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)]">
          Unlock with a free account
        </p>
        <ul className="mt-3 space-y-2 text-sm text-[var(--color-text-primary)]">
          {(data.nextSteps ?? [
            'Site audit — technical SEO crawl',
            'Topical map — topic clusters for your domain',
            'Competitor analysis — who ranks for your keywords',
          ]).map((step) => (
            <li key={step} className="flex items-start gap-2">
              <span className="mt-1 size-1.5 shrink-0 rounded-full bg-[var(--color-accent)]" />
              <span>{step}</span>
            </li>
          ))}
        </ul>
        <AuthStartLink className="mt-4 inline-flex h-10 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-5 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]">
          Sign up free to unlock full analysis →
        </AuthStartLink>
      </div>
    </div>
  );
}
