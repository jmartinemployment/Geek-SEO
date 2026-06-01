'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  getCannibalizationReport,
  getGoogleIntegrationStatus,
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
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [issues, setIssues] = useState<CannibalizationIssue[]>([]);
  const [rangeLabel, setRangeLabel] = useState('');
  const [gscRowCount, setGscRowCount] = useState<number | null>(null);
  const [uniqueQueryCount, setUniqueQueryCount] = useState<number | null>(null);
  const [multiUrlQueryCount, setMultiUrlQueryCount] = useState<number | null>(null);
  const [hasAnalyzed, setHasAnalyzed] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const selectedProject = projects.find((p) => p.id === projectId);

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken, authReady]);

  useEffect(() => {
    setHasAnalyzed(false);
    setIssues([]);
    setRangeLabel('');
    setGscRowCount(null);
    setUniqueQueryCount(null);
    setMultiUrlQueryCount(null);
    setError(null);
  }, [projectId]);

  async function analyze() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    setHasAnalyzed(false);
    try {
      const status = await getGoogleIntegrationStatus(projectId, accessToken);
      if (!status.connected) {
        setError(
          new Error(
            'Google Search Console is not connected for this project. Open Rankings and connect GSC first.',
          ),
        );
        setIssues([]);
        return;
      }

      const report = await getCannibalizationReport(projectId, accessToken);
      setIssues(report.issues);
      setGscRowCount(report.gscRowCount);
      setUniqueQueryCount(report.uniqueQueryCount);
      setMultiUrlQueryCount(report.multiUrlQueryCount);
      setRangeLabel(`${report.startDate} → ${report.endDate}`);
      setHasAnalyzed(true);
    } catch (err) {
      setError(err);
      setIssues([]);
      setGscRowCount(null);
      setUniqueQueryCount(null);
      setMultiUrlQueryCount(null);
      setHasAnalyzed(false);
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
                {p.gscConnected ? '' : ' (GSC not connected)'}
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

      {selectedProject && !selectedProject.gscConnected ? (
        <p className="mt-4 text-sm text-amber-800">
          This project does not show GSC as connected. Connect Search Console under{' '}
          <Link href="/app/rankings" className="font-medium underline">
            Rankings
          </Link>{' '}
          before analyzing.
        </p>
      ) : null}

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      {rangeLabel ? (
        <p className="mt-4 text-xs text-[var(--color-text-muted)]">GSC range: {rangeLabel}</p>
      ) : null}

      {hasAnalyzed && !error ? (
        <p className="mt-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
          Analysis complete — scanned up to {gscRowCount?.toLocaleString() ?? 0} top query/page rows
          across {uniqueQueryCount?.toLocaleString() ?? 0} queries.{' '}
          {multiUrlQueryCount === 0
            ? 'None of those queries had more than one URL in that sample.'
            : `${multiUrlQueryCount?.toLocaleString() ?? 0} ${multiUrlQueryCount === 1 ? 'query had' : 'queries had'} multiple URLs; showing ${issues.length} with merge/canonical guidance.`}
        </p>
      ) : null}

      {!loading && !hasAnalyzed && !error ? (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">
          Click Analyze to load the last 28 days of GSC data and find queries ranking with more than one URL.
        </p>
      ) : null}

      {!loading && hasAnalyzed && issues.length === 0 && !error ? (
        <p className="mt-8 rounded-xl border bg-white p-5 text-sm text-[var(--color-text-secondary)] shadow-sm">
          {multiUrlQueryCount && multiUrlQueryCount > 0
            ? 'Some queries had multiple URLs, but none met the impression threshold in this window — try a longer date range in Rankings or focus on higher-traffic terms.'
            : 'No competing URLs in the top 5,000 query/page rows GSC returned for the last 28 days — each query in that slice maps to a single page. That is common on smaller sites. In Rankings, sort by query: if the same keyword never appears on two different URLs, cannibalization will stay empty.'}
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
