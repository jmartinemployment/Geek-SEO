'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import {
  approveContentGuardRun,
  getContentGuardPolicy,
  getPublishedContentAudit,
  listContentGuardRuns,
  listProjects,
  rollbackContentGuardRun,
  scanContentGuard,
  upsertContentGuardPolicy,
  type ContentGuardRun,
  type PerformanceSnapshotPoint,
  type PublishedPageMetrics,
  type SeoProject,
} from '@/lib/seo-api';

function statusClass(status: PublishedPageMetrics['status']): string {
  if (status === 'critical') return 'bg-red-50 text-red-800 border-red-200';
  if (status === 'decaying') return 'bg-amber-50 text-amber-900 border-amber-200';
  return 'bg-green-50 text-green-800 border-green-200';
}

function Sparkline({ points }: { points: PerformanceSnapshotPoint[] }) {
  if (points.length < 2) return null;
  const maxClicks = Math.max(1, ...points.map((p) => p.clicks));
  const width = 120;
  const height = 32;
  const coords = points.map((p, index) => {
    const x = (index / (points.length - 1)) * width;
    const y = height - (p.clicks / maxClicks) * height;
    return `${x},${y}`;
  });

  return (
    <svg width={width} height={height} className="text-[var(--color-accent)]" aria-hidden>
      <polyline fill="none" stroke="currentColor" strokeWidth="2" points={coords.join(' ')} />
    </svg>
  );
}

export default function ContentGuardPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [pages, setPages] = useState<PublishedPageMetrics[]>([]);
  const [runs, setRuns] = useState<ContentGuardRun[]>([]);
  const [enabled, setEnabled] = useState(false);
  const [autoPatch, setAutoPatch] = useState(false);
  const [decayingCount, setDecayingCount] = useState(0);
  const [rangeLabel, setRangeLabel] = useState('');
  const [loading, setLoading] = useState(false);
  const [savingPolicy, setSavingPolicy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showAll, setShowAll] = useState(false);

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken, authReady]);

  useEffect(() => {
    if (!authReady || !projectId) return;
    void getContentGuardPolicy(projectId, accessToken)
      .then((policy) => {
        setEnabled(policy?.enabled ?? false);
        setAutoPatch(policy?.autoPatch ?? false);
      })
      .catch(() => undefined);
    void listContentGuardRuns(projectId, accessToken)
      .then(setRuns)
      .catch(() => setRuns([]));
  }, [projectId, accessToken, authReady]);

  async function savePolicy() {
    if (!projectId) return;
    setSavingPolicy(true);
    setError(null);
    try {
      await upsertContentGuardPolicy(projectId, { enabled, autoPatch }, accessToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save policy');
    } finally {
      setSavingPolicy(false);
    }
  }

  async function analyze() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const report = await getPublishedContentAudit(projectId, accessToken);
      setPages(report.pages);
      setDecayingCount(report.decayingCount);
      setRangeLabel(
        `Recent ${report.recentStartDate} → ${report.recentEndDate} vs baseline ${report.baselineStartDate} → ${report.baselineEndDate}`,
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Content audit failed');
      setPages([]);
      setLoading(false);
      return;
    }

    try {
      await scanContentGuard(projectId, accessToken);
      setRuns(await listContentGuardRuns(projectId, accessToken));
    } catch (err) {
      const scanMessage = err instanceof Error ? err.message : 'Guard scan failed';
      setError(
        `Decay report loaded, but guard scan failed: ${scanMessage}. Runs from earlier scans are shown below.`,
      );
      try {
        setRuns(await listContentGuardRuns(projectId, accessToken));
      } catch {
        /* keep prior runs */
      }
    } finally {
      setLoading(false);
    }
  }

  async function onApprove(runId: string) {
    setError(null);
    try {
      await approveContentGuardRun(runId, accessToken);
      setRuns(await listContentGuardRuns(projectId, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Approve failed');
    }
  }

  async function onRollback(runId: string) {
    setError(null);
    try {
      await rollbackContentGuardRun(runId, accessToken);
      setRuns(await listContentGuardRuns(projectId, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Rollback failed');
    }
  }

  const visiblePages = showAll
    ? pages
    : pages.filter((p) => p.status === 'decaying' || p.status === 'critical');

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Content Guard</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        GSC-backed decay detection with optional AI refresh drafts in WordPress. Enable auto-patch to
        queue draft posts when decay is detected.
      </p>

      <section className="mt-6 rounded-xl border bg-white p-5 shadow-sm">
        <h2 className="text-sm font-semibold">Automation policy</h2>
        <div className="mt-3 flex flex-wrap gap-6 text-sm">
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={enabled} onChange={(e) => setEnabled(e.target.checked)} />
            Daily scan enabled
          </label>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={autoPatch} onChange={(e) => setAutoPatch(e.target.checked)} />
            Auto-patch decaying pages (WP draft)
          </label>
          <button
            type="button"
            disabled={savingPolicy || !projectId}
            onClick={() => void savePolicy()}
            className="rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
          >
            {savingPolicy ? 'Saving…' : 'Save policy'}
          </button>
        </div>
      </section>

      <div className="mt-6 flex flex-wrap items-end gap-3">
        <label className="text-sm font-medium">
          Project
          <select
            className="ml-2 rounded-lg border px-3 py-2"
            value={projectId}
            onChange={(e) => setProjectId(e.target.value)}
          >
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
        <button
          type="button"
          disabled={loading || !projectId}
          onClick={() => void analyze()}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? 'Scanning…' : 'Scan for decay'}
        </button>
        <Link href="/app/rankings" className="text-sm text-[var(--color-brand)] hover:underline">
          Rankings →
        </Link>
      </div>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}
      {rangeLabel ? <p className="mt-4 text-xs text-[var(--color-text-muted)]">{rangeLabel}</p> : null}

      {runs.length > 0 ? (
        <section className="mt-8 rounded-xl border bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold">Guard runs</h2>
          <ul className="mt-4 space-y-3">
            {runs.map((run) => (
              <li key={run.id} className="rounded-lg border px-4 py-3 text-sm">
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div>
                    <p className="font-medium break-all">{run.url}</p>
                    <p className="mt-1 text-xs text-[var(--color-text-muted)]">
                      Status: {run.status}
                      {run.wordPressDraftPostId ? ` · WP draft #${run.wordPressDraftPostId}` : ''}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    {run.status === 'draft_ready' ? (
                      <button
                        type="button"
                        className="rounded-lg border border-green-200 bg-green-50 px-3 py-1 text-xs font-medium text-green-900"
                        onClick={() => void onApprove(run.id)}
                      >
                        Approve
                      </button>
                    ) : null}
                    {run.status === 'approved' || run.status === 'draft_ready' ? (
                      <button
                        type="button"
                        className="rounded-lg border px-3 py-1 text-xs font-medium hover:bg-slate-50"
                        onClick={() => void onRollback(run.id)}
                      >
                        Rollback
                      </button>
                    ) : null}
                  </div>
                </div>
                {run.recommendation ? (
                  <p className="mt-2 text-xs text-[var(--color-text-secondary)]">{run.recommendation}</p>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {pages.length > 0 ? (
        <div className="mt-4 flex flex-wrap items-center gap-3 text-sm">
          <span className="rounded-full border border-red-200 bg-red-50 px-3 py-1">
            {decayingCount} pages need attention
          </span>
          <label className="flex items-center gap-2 text-[var(--color-text-secondary)]">
            <input
              type="checkbox"
              checked={showAll}
              onChange={(e) => setShowAll(e.target.checked)}
            />
            Show all pages ({pages.length})
          </label>
        </div>
      ) : null}

      {!loading && pages.length === 0 && !error ? (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">
          Run a scan to compare recent vs baseline GSC performance. Connect Search Console on your
          project first.
        </p>
      ) : null}

      {!loading && pages.length > 0 && visiblePages.length === 0 ? (
        <p className="mt-8 rounded-xl border bg-green-50 p-4 text-sm text-green-900">
          No decaying pages detected in this period — toggle &quot;Show all pages&quot; to review stable URLs.
        </p>
      ) : null}

      <ul className="mt-8 space-y-4">
        {visiblePages.map((page) => (
          <li key={page.url} className="rounded-xl border bg-white p-5 shadow-sm">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                <Link
                  href={page.url}
                  target="_blank"
                  rel="noreferrer"
                  className="break-all text-sm font-medium text-[var(--color-brand)] hover:underline"
                >
                  {page.url}
                </Link>
                <p className="mt-2 text-xs text-[var(--color-text-secondary)]">{page.recommendation}</p>
              </div>
              <div className="flex shrink-0 flex-col items-end gap-2">
                <span className={`rounded-full border px-2 py-0.5 text-xs font-medium ${statusClass(page.status)}`}>
                  {page.status}
                </span>
                {page.sparkline && page.sparkline.length > 1 ? (
                  <Sparkline points={page.sparkline} />
                ) : null}
              </div>
            </div>
            <dl className="mt-4 grid grid-cols-2 gap-3 text-xs sm:grid-cols-4">
              <div>
                <dt className="text-[var(--color-text-muted)]">Clicks change</dt>
                <dd className="font-medium">{page.clicksChangePercent}%</dd>
              </div>
              <div>
                <dt className="text-[var(--color-text-muted)]">Position change</dt>
                <dd className="font-medium">{page.positionChange > 0 ? '+' : ''}{page.positionChange}</dd>
              </div>
              <div>
                <dt className="text-[var(--color-text-muted)]">Recent clicks</dt>
                <dd className="font-medium">{page.recentClicks}</dd>
              </div>
              <div>
                <dt className="text-[var(--color-text-muted)]">Baseline clicks</dt>
                <dd className="font-medium">{page.baselineClicks}</dd>
              </div>
            </dl>
          </li>
        ))}
      </ul>
    </main>
  );
}
