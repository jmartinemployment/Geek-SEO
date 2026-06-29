'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { GoogleSettings } from '@/components/google/google-settings';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  getGoogleIntegrationStatus,
  getGoogleRankings,
  getGa4LandingPages,
  listProjects,
  type Ga4LandingPagesResponse,
  type GoogleRankingsResponse,
  type SeoProject,
} from '@/lib/seo-api';

type GoogleProjectPanelProps = Readonly<{
  title: string;
  description: string;
  mode: 'rankings' | 'analytics';
}>;

export function GoogleProjectPanel({ title, description, mode }: GoogleProjectPanelProps) {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const [rankings, setRankings] = useState<GoogleRankingsResponse | null>(null);
  const [landingPages, setLandingPages] = useState<Ga4LandingPagesResponse | null>(null);

  useEffect(() => {
    if (authLoading) return;
    const timer = setTimeout(() => {
      void (async () => {
        try {
          const list = await listProjects(accessToken);
          setProjects(list);
          if (list.length > 0) setProjectId((current) => current || list[0].id);
        } catch (e) {
          setError(e);
        } finally {
          setLoading(false);
        }
      })();
    }, 0);
    return () => clearTimeout(timer);
  }, [authLoading, accessToken]);

  const loadData = useCallback(async () => {
    if (!projectId) return;
    setError(null);
    try {
      const status = await getGoogleIntegrationStatus(projectId, accessToken);
      setConnected(status.connected);
      if (!status.connected) {
        setRankings(null);
        setLandingPages(null);
        return;
      }
      if (mode === 'rankings') {
        setRankings(await getGoogleRankings(projectId, accessToken));
        setLandingPages(null);
      } else {
        setLandingPages(await getGa4LandingPages(projectId, accessToken));
        setRankings(null);
      }
    } catch (e) {
      setError(e);
    }
  }, [projectId, accessToken, mode]);

  useEffect(() => {
    if (!projectId || authLoading) return;
    const timer = setTimeout(() => {
      void loadData();
    }, 0);
    return () => clearTimeout(timer);
  }, [projectId, authLoading, loadData]);

  if (authLoading || loading) {
    return <main className="p-8 text-sm text-[var(--color-text-secondary)]">Loading…</main>;
  }

  if (projects.length === 0) {
    return (
      <main className="mx-auto max-w-2xl px-6 py-16">
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="mt-2 text-sm text-[var(--color-text-secondary)]">Create a project first, then connect Google.</p>
        <Link href="/projects" className="mt-6 inline-block text-sm underline">
          Go to projects
        </Link>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">{description}</p>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <label className="text-sm text-[var(--color-text-secondary)]" htmlFor="google-project-select">
          Project
        </label>
        <select
          id="google-project-select"
          className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
          value={projectId}
          onChange={(e) => setProjectId(e.target.value)}
        >
          {projects.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <button
          type="button"
          className="rounded-lg border px-3 py-2 text-sm hover:bg-[var(--color-surface-muted)]"
          onClick={() => void loadData()}
        >
          Refresh
        </button>
      </div>

      <div className="mt-6">
        <GoogleSettings
          projectId={projectId}
          accessToken={accessToken}
          projectSiteUrl={projects.find((p) => p.id === projectId)?.url}
        />
      </div>

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      {connected && mode === 'rankings' && rankings && (
        <div className="mt-8 overflow-x-auto rounded-xl border bg-white shadow-sm">
          <p className="border-b px-4 py-3 text-xs text-[var(--color-text-secondary)]">
            {rankings.siteUrl} · {rankings.startDate} → {rankings.endDate}
          </p>
          <table className="min-w-full text-left text-sm">
            <thead className="bg-[var(--color-surface-muted)] text-xs uppercase text-[var(--color-text-secondary)]">
              <tr>
                <th className="px-4 py-2">Query</th>
                <th className="px-4 py-2">Page</th>
                <th className="px-4 py-2">Clicks</th>
                <th className="px-4 py-2">Impr.</th>
                <th className="px-4 py-2">CTR</th>
                <th className="px-4 py-2">Pos.</th>
              </tr>
            </thead>
            <tbody>
              {rankings.rows.map((row) => (
                <tr key={`${row.query}-${row.page}`} className="border-t">
                  <td className="px-4 py-2 font-medium">{row.query}</td>
                  <td className="max-w-xs truncate px-4 py-2 text-[var(--color-text-secondary)]">{row.page}</td>
                  <td className="px-4 py-2">{row.clicks}</td>
                  <td className="px-4 py-2">{row.impressions}</td>
                  <td className="px-4 py-2">{(row.ctr * 100).toFixed(1)}%</td>
                  <td className="px-4 py-2">{row.position.toFixed(1)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {rankings.rows.length === 0 ? (
            <p className="p-6 text-sm text-[var(--color-text-secondary)]">No Search Console rows for this range.</p>
          ) : null}
        </div>
      )}

      {connected && mode === 'analytics' && landingPages && (
        <div className="mt-8 overflow-x-auto rounded-xl border bg-white shadow-sm">
          <p className="border-b px-4 py-3 text-xs text-[var(--color-text-secondary)]">
            Property {landingPages.propertyId} · {landingPages.startDate} → {landingPages.endDate}
          </p>
          <table className="min-w-full text-left text-sm">
            <thead className="bg-[var(--color-surface-muted)] text-xs uppercase text-[var(--color-text-secondary)]">
              <tr>
                <th className="px-4 py-2">Landing page</th>
                <th className="px-4 py-2">Sessions</th>
                <th className="px-4 py-2">Users</th>
                <th className="px-4 py-2">Conversions</th>
              </tr>
            </thead>
            <tbody>
              {landingPages.rows.map((row) => (
                <tr key={row.landingPage} className="border-t">
                  <td className="px-4 py-2 font-medium">{row.landingPage}</td>
                  <td className="px-4 py-2">{row.sessions}</td>
                  <td className="px-4 py-2">{row.users}</td>
                  <td className="px-4 py-2">{row.conversions}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {landingPages.rows.length === 0 ? (
            <p className="p-6 text-sm text-[var(--color-text-secondary)]">No GA4 landing page data for this range.</p>
          ) : null}
        </div>
      )}
    </main>
  );
}
