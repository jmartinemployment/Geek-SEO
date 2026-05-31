'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  getCannibalizationReport,
  listProjects,
  type CannibalizationIssue,
  type SeoProject,
} from '@/lib/seo-api';

function severityClass(severity: string): string {
  if (severity === 'high') return 'bg-red-50 text-red-800 border-red-200';
  if (severity === 'medium') return 'bg-amber-50 text-amber-900 border-amber-200';
  return 'bg-slate-50 text-slate-700 border-slate-200';
}

export default function CannibalizationPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [issues, setIssues] = useState<CannibalizationIssue[]>([]);
  const [rangeLabel, setRangeLabel] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken]);

  async function analyze() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const report = await getCannibalizationReport(projectId, accessToken);
      setIssues(report.issues);
      setRangeLabel(`${report.startDate} → ${report.endDate}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Analysis failed');
      setIssues([]);
    } finally {
      setLoading(false);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Keyword cannibalization</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        GSC queries where multiple URLs compete — requires Search Console connected (Professional tier).
      </p>

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
          {loading ? 'Analyzing…' : 'Analyze'}
        </button>
        <Link href="/app/rankings" className="text-sm text-[var(--color-brand)] hover:underline">
          Rankings →
        </Link>
      </div>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}
      {rangeLabel ? <p className="mt-4 text-xs text-[var(--color-text-muted)]">GSC range: {rangeLabel}</p> : null}

      {!loading && issues.length === 0 && !error ? (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">
          Run analysis to find competing URLs. Connect GSC on your project first if you see auth errors.
        </p>
      ) : null}

      <ul className="mt-8 space-y-4">
        {issues.map((issue) => (
          <li key={issue.query} className="rounded-xl border bg-white p-5 shadow-sm">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-base font-semibold">{issue.query}</h2>
              <span className={`rounded-full border px-2 py-0.5 text-xs font-medium ${severityClass(issue.severity)}`}>
                {issue.severity}
              </span>
              <span className="text-xs text-[var(--color-text-muted)]">
                {issue.totalImpressions.toLocaleString()} impressions
              </span>
            </div>
            <p className="mt-2 text-sm text-[var(--color-text-secondary)]">{issue.recommendation}</p>
            <ul className="mt-3 space-y-1 text-xs">
              {issue.pages.map((page) => (
                <li key={page.url} className="flex flex-wrap gap-3 text-[var(--color-text-secondary)]">
                  <Link href={page.url} target="_blank" rel="noreferrer" className="font-medium text-[var(--color-brand)] hover:underline">
                    {page.url}
                  </Link>
                  <span>{page.impressions} imp · pos {page.position.toFixed(1)}</span>
                </li>
              ))}
            </ul>
          </li>
        ))}
      </ul>
    </main>
  );
}
