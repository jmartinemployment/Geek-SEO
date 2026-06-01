'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  getSiteAudit,
  listProjects,
  listSiteAudits,
  startSiteAudit,
  type SeoProject,
  type SiteAuditDetail,
  type SiteAuditSummary,
} from '@/lib/seo-api';

function severityClass(severity: string): string {
  if (severity === 'critical') return 'text-red-700 bg-red-50 border-red-200';
  if (severity === 'warning') return 'text-amber-800 bg-amber-50 border-amber-200';
  return 'text-slate-700 bg-slate-50 border-slate-200';
}

type SiteAuditViewProps = Readonly<{
  initialProjectId?: string;
}>;

export function SiteAuditView({ initialProjectId }: SiteAuditViewProps) {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId ?? '');
  const [history, setHistory] = useState<SiteAuditSummary[]>([]);
  const [selected, setSelected] = useState<SiteAuditDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const loadHistory = useCallback(async () => {
    if (!projectId) return;
    const audits = await listSiteAudits(projectId, accessToken);
    setHistory(audits);
    return audits;
  }, [projectId, accessToken]);

  useEffect(() => {
    if (authLoading) return;
    void listProjects(accessToken)
      .then((list) => {
        setProjects(list);
        if (initialProjectId && list.some((p) => p.id === initialProjectId)) {
          setProjectId(initialProjectId);
        } else if (list[0]) {
          setProjectId(list[0].id);
        }
      })
      .catch((e) => setError(e))
      .finally(() => setLoading(false));
  }, [accessToken, authLoading, initialProjectId]);

  useEffect(() => {
    if (!projectId || authLoading) return;
    setLoading(true);
    void loadHistory()
      .then((audits) => {
        if (audits?.[0]) {
          return getSiteAudit(audits[0].id, accessToken).then(setSelected);
        }
        setSelected(null);
      })
      .catch((e) => setError(e))
      .finally(() => setLoading(false));
  }, [projectId, authLoading, loadHistory, accessToken]);

  useEffect(() => {
    if (!running || !selected || selected.status !== 'running') return;
    const timer = window.setInterval(() => {
      void getSiteAudit(selected.id, accessToken)
        .then((detail) => {
          setSelected(detail);
          if (detail.status !== 'running') {
            setRunning(false);
            void loadHistory();
          }
        })
        .catch(() => {});
    }, 3000);
    return () => window.clearInterval(timer);
  }, [running, selected, accessToken, loadHistory]);

  async function runAudit() {
    if (!projectId) return;
    setRunning(true);
    setError(null);
    try {
      const started = await startSiteAudit(projectId, accessToken);
      const detail = await getSiteAudit(started.id, accessToken);
      setSelected(detail);
      await loadHistory();
    } catch (e) {
      setError(e);
      setRunning(false);
    }
  }

  async function openAudit(auditId: string) {
    setError(null);
    try {
      const detail = await getSiteAudit(auditId, accessToken);
      setSelected(detail);
      if (detail.status === 'running') setRunning(true);
    } catch (e) {
      setError(e);
    }
  }

  const project = projects.find((p) => p.id === projectId);

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <h1 className="text-2xl font-semibold text-[var(--color-text-primary)]">Site audit</h1>
      <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
        Crawl your site with Playwright and surface on-page SEO issues — titles, meta descriptions, H1s, thin
        content, and structured data.
      </p>

      <div className="mt-8 flex flex-wrap items-end gap-4">
        <label className="block text-sm">
          <span className="font-medium text-[var(--color-text-primary)]">Project</span>
          <select
            className="mt-1 block min-w-[220px] rounded-lg border border-[var(--color-border)] bg-white px-3 py-2"
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
          disabled={!projectId || running}
          onClick={() => void runAudit()}
          className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          {running ? 'Crawl in progress…' : 'Run site audit'}
        </button>
        {project ? (
          <p className="text-xs text-[var(--color-text-secondary)]">
            Target: <span className="font-mono">{project.url}</span>
          </p>
        ) : null}
      </div>

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      {loading ? (
        <p className="mt-10 text-sm text-[var(--color-text-secondary)]">Loading audits…</p>
      ) : (
        <div className="mt-10 grid gap-8 lg:grid-cols-[240px_1fr]">
          <aside>
            <h2 className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)]">
              Recent audits
            </h2>
            <ul className="mt-3 space-y-2">
              {history.length === 0 ? (
                <li className="text-sm text-[var(--color-text-secondary)]">No audits yet.</li>
              ) : (
                history.map((a) => (
                  <li key={a.id}>
                    <button
                      type="button"
                      onClick={() => void openAudit(a.id)}
                      className={`w-full rounded-lg border px-3 py-2 text-left text-sm ${
                        selected?.id === a.id
                          ? 'border-[var(--color-brand)] bg-[var(--color-brand)]/5'
                          : 'border-[var(--color-border)] hover:bg-slate-50'
                      }`}
                    >
                      <span className="block font-medium capitalize">{a.status}</span>
                      <span className="text-xs text-[var(--color-text-secondary)]">
                        {new Date(a.startedAt).toLocaleString()}
                        {a.overallScore != null ? ` · ${a.overallScore}/100` : ''}
                      </span>
                    </button>
                  </li>
                ))
              )}
            </ul>
          </aside>

          <section>
            {!selected ? (
              <div className="rounded-xl border border-dashed border-[var(--color-border)] bg-slate-50/80 px-6 py-10 text-center">
                <p className="text-sm font-medium text-[var(--color-text-primary)]">No audit results yet</p>
                <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                  Choose a project and click <span className="font-medium">Run site audit</span> to crawl your site.
                  The crawl may take a few minutes for larger sites.
                </p>
              </div>
            ) : (
              <>
                <div className="flex flex-wrap items-center gap-4">
                  <div className="rounded-xl border border-[var(--color-border)] px-5 py-4">
                    <p className="text-xs uppercase tracking-wide text-[var(--color-text-secondary)]">Overall</p>
                    <p className="text-3xl font-semibold tabular-nums">
                      {selected.overallScore != null ? selected.overallScore : '—'}
                    </p>
                  </div>
                  <div className="text-sm text-[var(--color-text-secondary)]">
                    <p>
                      Status:{' '}
                      <span className="font-medium capitalize text-[var(--color-text-primary)]">
                        {selected.status}
                      </span>
                    </p>
                    <p className="mt-1">Pages crawled: {selected.pagesCrawled}</p>
                    {selected.errorMessage ? (
                      <p className="mt-2 text-red-700">{selected.errorMessage}</p>
                    ) : null}
                  </div>
                </div>

                {selected.status === 'running' ? (
                  <p className="mt-6 text-sm text-[var(--color-text-secondary)]">
                    Crawling pages… this refreshes automatically.
                  </p>
                ) : null}

                <ul className="mt-8 space-y-4">
                  {selected.pages.map((page) => (
                    <li
                      key={page.id}
                      className="rounded-xl border border-[var(--color-border)] bg-white p-4 shadow-sm"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <a
                          href={page.url}
                          target="_blank"
                          rel="noreferrer"
                          className="text-sm font-medium text-[var(--color-brand)] underline-offset-2 hover:underline"
                        >
                          {page.url}
                        </a>
                        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-sm font-semibold tabular-nums">
                          {page.score}/100
                        </span>
                      </div>
                      {page.issues.length === 0 ? (
                        <p className="mt-3 text-sm text-emerald-700">No issues detected.</p>
                      ) : (
                        <ul className="mt-3 space-y-2">
                          {page.issues.map((issue) => (
                            <li
                              key={`${page.id}-${issue.code}-${issue.message}`}
                              className={`rounded-lg border px-3 py-2 text-sm ${severityClass(issue.severity)}`}
                            >
                              {issue.message}
                            </li>
                          ))}
                        </ul>
                      )}
                    </li>
                  ))}
                </ul>
              </>
            )}
          </section>
        </div>
      )}

      <p className="mt-12 text-sm">
        <Link href="/app/planner" className="text-[var(--color-brand)] underline-offset-2 hover:underline">
          Content planner
        </Link>
        {' · '}
        <Link href="/app/dashboard" className="text-[var(--color-brand)] underline-offset-2 hover:underline">
          Dashboard
        </Link>
      </p>
    </main>
  );
}
