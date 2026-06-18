'use client';

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  analyzeUrlResearch,
  getUrlResearch,
  listProjects,
  listUrlResearch,
  type SeoProject,
  type UrlResearchFull,
  type UrlResearchSummary,
} from '@/lib/seo-api';
import { subscribeUrlResearchProgress, subscribeUrlResearchProjectProgress } from '@/lib/url-research-signalr';

const TERMINAL = new Set(['completed', 'failed']);

function formatWhen(iso?: string | null): string {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

function statusBadge(status: string): string {
  if (status === 'completed') return 'bg-emerald-100 text-emerald-800';
  if (status === 'failed') return 'bg-red-100 text-red-800';
  if (status === 'running') return 'bg-amber-100 text-amber-900';
  return 'bg-slate-100 text-slate-700';
}

type UrlAnalyzerWorkspaceProps = {
  accessToken: string | null;
  initialProjectId?: string;
  initialUrlResearchId?: string;
};

export function UrlAnalyzerWorkspace({
  accessToken,
  initialProjectId = '',
  initialUrlResearchId = '',
}: UrlAnalyzerWorkspaceProps) {
  const hub = useSeoHub();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId);
  const [pageUrl, setPageUrl] = useState('');
  const [rows, setRows] = useState<UrlResearchSummary[]>([]);
  const [activeId, setActiveId] = useState<string | null>(initialUrlResearchId || null);
  const [detail, setDetail] = useState<UrlResearchFull | null>(null);
  const [liveStatus, setLiveStatus] = useState<string | null>(null);
  const [liveMessage, setLiveMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [listLoading, setListLoading] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const selectedProject = useMemo(
    () => projects.find((p) => p.id === projectId) ?? null,
    [projectId, projects],
  );

  const refreshList = useCallback(async () => {
    if (!projectId) {
      setRows([]);
      return;
    }
    setListLoading(true);
    try {
      const list = await listUrlResearch(projectId, accessToken);
      setRows(list);
    } catch (listError) {
      setError(listError);
    } finally {
      setListLoading(false);
    }
  }, [accessToken, projectId]);

  useEffect(() => {
    let cancelled = false;
    async function loadProjects() {
      try {
        const list = await listProjects(accessToken);
        if (cancelled) return;
        setProjects(list);
        if (!projectId && list[0]) setProjectId(list[0].id);
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      }
    }
    void loadProjects();
    return () => {
      cancelled = true;
    };
  }, [accessToken, projectId]);

  useEffect(() => {
    if (!projectId) return;
    let cancelled = false;
    async function loadRows() {
      setListLoading(true);
      try {
        const list = await listUrlResearch(projectId, accessToken);
        if (!cancelled) setRows(list);
      } catch (listError) {
        if (!cancelled) setError(listError);
      } finally {
        if (!cancelled) setListLoading(false);
      }
    }
    void loadRows();
    return () => {
      cancelled = true;
    };
  }, [accessToken, projectId]);

  useEffect(() => {
    if (!projectId || !hub.isConnected) return;

    return subscribeUrlResearchProjectProgress(hub, {
      projectId,
      onProgress: ({ urlResearchId, status }) => {
        setRows((prev) =>
          prev.map((row) => (row.id === urlResearchId ? { ...row, status } : row)),
        );
        if (activeId === urlResearchId && TERMINAL.has(status)) {
          void refreshList();
        }
      },
    });
  }, [hub, hub.isConnected, projectId, activeId, refreshList]);

  useEffect(() => {
    if (!activeId || !projectId || !hub.isConnected) return;

    const researchId = activeId;
    const pid = projectId;
    let cancelled = false;
    let unsubscribe: (() => void) | null = null;

    async function trackActiveResearch() {
      try {
        const row = await getUrlResearch(researchId, accessToken);
        if (cancelled) return;
        setDetail(row);
        setLiveStatus(row.status);
        setLiveMessage(null);

        if (TERMINAL.has(row.status)) {
          await refreshList();
          return;
        }

        unsubscribe = subscribeUrlResearchProgress(hub, {
          urlResearchId: researchId,
          projectId: pid,
          onStatus: (status, message) => {
            if (cancelled) return;
            setLiveStatus(status);
            if (message) setLiveMessage(message);
            setDetail((prev) =>
              prev && prev.id === researchId ? { ...prev, status } : prev,
            );
          },
          onTerminal: () => {
            void (async () => {
              try {
                const full = await getUrlResearch(researchId, accessToken);
                if (cancelled) return;
                setDetail(full);
                setLiveStatus(full.status);
                await refreshList();
              } catch (loadError) {
                if (!cancelled) setError(loadError);
              }
            })();
          },
        });
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      }
    }

    void trackActiveResearch();
    return () => {
      cancelled = true;
      unsubscribe?.();
    };
  }, [activeId, projectId, accessToken, refreshList, hub, hub.isConnected]);

  const visibleDetail =
    activeId && detail && detail.id === activeId ? detail : null;
  const progressStatus = liveStatus ?? visibleDetail?.status ?? null;
  const inProgress = Boolean(activeId && progressStatus && !TERMINAL.has(progressStatus));

  async function onAnalyze(e: React.FormEvent) {
    e.preventDefault();
    if (!projectId || !pageUrl.trim()) return;
    setLoading(true);
    setError(null);
    try {
      const queued = await analyzeUrlResearch(
        { projectId, pageUrl: pageUrl.trim() },
        accessToken,
      );
      setActiveId(queued.urlResearchId);
      setLiveStatus(queued.status);
      setPageUrl('');
      await refreshList();
    } catch (analyzeError) {
      setError(analyzeError);
    } finally {
      setLoading(false);
    }
  }

  function openRow(id: string) {
    setActiveId(id);
    setError(null);
  }

  return (
    <div className="mx-auto max-w-5xl">
      <h1 className="text-2xl font-semibold">URL Analyzer</h1>
      <p className="mt-1 max-w-2xl text-sm text-[var(--color-text-secondary)]">
        Analyze one page URL per run. We crawl the page, run SERP research, and save results for
        Content Writing — no JSON export.
      </p>

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      <form onSubmit={onAnalyze} className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Project
          <select
            className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
            value={projectId}
            onChange={(e) => {
              const nextId = e.target.value;
              setProjectId(nextId);
              if (!nextId) setRows([]);
            }}
          >
            <option value="">Select a project</option>
            {projects.map((project) => (
              <option key={project.id} value={project.id}>
                {project.name}
              </option>
            ))}
          </select>
        </label>

        {selectedProject ? (
          <p className="text-xs text-[var(--color-text-secondary)]">
            Page URL must be on the same domain as{' '}
            <span className="font-mono">{selectedProject.url}</span> (subdomains allowed). Search
            location comes from the project.
          </p>
        ) : null}

        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Page URL
          <input
            type="url"
            className="mt-1 w-full rounded-lg border px-3 py-2 font-mono text-sm"
            value={pageUrl}
            onChange={(e) => setPageUrl(e.target.value)}
            placeholder="https://yoursite.com/blog/quickbooks-automation"
            required
            disabled={!projectId}
          />
        </label>

        <button
          type="submit"
          disabled={loading || !projectId}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? 'Enqueueing…' : 'Analyze page'}
        </button>
      </form>

      {inProgress ? (
        <section className="mt-6 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950">
          <p className="font-medium">Analysis in progress</p>
          <p className="mt-1 text-xs">
            Status: {progressStatus}
            {liveMessage ? ` — ${liveMessage}` : null}. Updates arrive live; you can leave this page
            and results stay in the list below.
          </p>
        </section>
      ) : null}

      <section className="mt-10 rounded-xl border bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-lg font-semibold">Page research</h2>
          <button
            type="button"
            onClick={() => void refreshList()}
            disabled={!projectId || listLoading}
            className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] disabled:opacity-50"
          >
            {listLoading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {!projectId ? (
          <p className="mt-4 text-sm text-[var(--color-text-secondary)]">Select a project first.</p>
        ) : rows.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--color-text-secondary)]">
            No page research yet. Analyze a URL above.
          </p>
        ) : (
          <ul className="mt-4 divide-y">
            {rows.map((row) => (
              <li key={row.id}>
                <button
                  type="button"
                  onClick={() => openRow(row.id)}
                  className="flex w-full flex-wrap items-center gap-3 py-3 text-left hover:bg-[var(--color-surface-muted)]"
                >
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadge(row.status)}`}
                  >
                    {row.status}
                  </span>
                  <span className="min-w-0 flex-1 font-mono text-xs">{row.sourceUrl}</span>
                  <span className="text-sm text-[var(--color-text-secondary)]">{row.derivedKeyword}</span>
                  <span className="text-xs text-[var(--color-text-muted)]">
                    {formatWhen(row.researchedAt ?? row.createdAt)}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      {visibleDetail && TERMINAL.has(visibleDetail.status) ? (
        <section className="mt-6 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 className="text-lg font-semibold">Research detail</h2>
              <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                <span className="font-mono text-xs">{visibleDetail.sourceUrl}</span>
                <br />
                Keyword: {visibleDetail.derivedKeyword} · quality: {visibleDetail.dataQuality ?? '—'} · intent:{' '}
                {visibleDetail.intentPrimary}
              </p>
              {visibleDetail.dataQualityNotes ? (
                <p className="mt-2 text-sm text-amber-800">{visibleDetail.dataQualityNotes}</p>
              ) : null}
              {visibleDetail.status === 'failed' && visibleDetail.errorMessage ? (
                <p className="mt-2 text-sm text-red-700">{visibleDetail.errorMessage}</p>
              ) : null}
            </div>
            {visibleDetail.status === 'completed' && projectId ? (
              <Link
                href={`/content-writing?projectId=${encodeURIComponent(projectId)}&urlResearchId=${encodeURIComponent(visibleDetail.id)}`}
                className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-sm text-white hover:bg-[var(--color-accent-hover)]"
              >
                Write from this research
              </Link>
            ) : null}
          </div>

          {visibleDetail.status === 'completed' ? (
            <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">PAA</dt>
                <dd className="text-lg font-medium">{visibleDetail.peopleAlsoAsk?.length ?? 0}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">PASF</dt>
                <dd className="text-lg font-medium">{visibleDetail.relatedSearches?.length ?? 0}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">Organic</dt>
                <dd className="text-lg font-medium">{visibleDetail.organicResults?.length ?? 0}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">Target words</dt>
                <dd className="text-lg font-medium">{visibleDetail.medianWordCountTop5}</dd>
              </div>
            </dl>
          ) : null}
        </section>
      ) : null}
    </div>
  );
}
