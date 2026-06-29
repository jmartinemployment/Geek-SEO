'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import {
  createGeoQuery,
  deleteGeoQuery,
  getGeoPlatforms,
  getGeoTrends,
  listGeoQueries,
  listProjects,
  probeGeoVisibility,
  type GeoPlatformStatus,
  type GeoProbeResult,
  type GeoTrackingQuery,
  type GeoTrendsResponse,
  type SeoProject,
} from '@/lib/seo-api';

export default function GeoPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [query, setQuery] = useState('');
  const [platforms, setPlatforms] = useState<GeoPlatformStatus[]>([]);
  const [trackedQueries, setTrackedQueries] = useState<GeoTrackingQuery[]>([]);
  const [selectedQueryId, setSelectedQueryId] = useState('');
  const [trends, setTrends] = useState<GeoTrendsResponse | null>(null);
  const [result, setResult] = useState<GeoProbeResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
    void getGeoPlatforms(accessToken)
      .then((res) => setPlatforms(res.platforms))
      .catch(() => undefined);
  }, [accessToken, authReady]);

  useEffect(() => {
    if (!authReady || !projectId) return;
    void listGeoQueries(projectId, accessToken)
      .then((queries) => {
        setTrackedQueries(queries);
        if (queries[0]) setSelectedQueryId(queries[0].id);
      })
      .catch(() => setTrackedQueries([]));
  }, [projectId, accessToken, authReady]);

  useEffect(() => {
    if (!authReady || !selectedQueryId) {
      setTrends(null);
      return;
    }
    void getGeoTrends(selectedQueryId, accessToken)
      .then(setTrends)
      .catch(() => setTrends(null));
  }, [selectedQueryId, accessToken, authReady]);

  async function onProbe(e: React.FormEvent) {
    e.preventDefault();
    if (!projectId || !query.trim()) return;

    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const probe = await probeGeoVisibility({ projectId, query: query.trim() }, accessToken);
      setResult(probe);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'GEO probe failed');
    } finally {
      setLoading(false);
    }
  }

  async function onTrackQuery() {
    if (!projectId || !query.trim()) return;
    setError(null);
    try {
      const created = await createGeoQuery({ projectId, queryText: query.trim() }, accessToken);
      setTrackedQueries((prev) => [...prev, created]);
      setSelectedQueryId(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to track query');
    }
  }

  async function onDeleteQuery(id: string) {
    setError(null);
    try {
      await deleteGeoQuery(id, accessToken);
      setTrackedQueries((prev) => prev.filter((q) => q.id !== id));
      if (selectedQueryId === id) setSelectedQueryId('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete query');
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">GEO visibility</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        Track queries for daily Google AIO probes and review 30-day mention trends. On-demand probes run immediately.
      </p>

      <section className="mt-6 rounded-xl border bg-white p-5 shadow-sm">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
          Platform status
        </h2>
        <ul className="mt-3 grid gap-2 sm:grid-cols-2">
          {platforms.map((platform) => (
            <li
              key={platform.id}
              className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="font-medium">{platform.name}</span>
                <span
                  className={
                    platform.configured
                      ? 'rounded-full bg-green-50 px-2 py-0.5 text-xs text-green-800'
                      : 'rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600'
                  }
                >
                  {platform.configured ? 'Ready' : 'Not configured'}
                </span>
              </div>
              {platform.note ? (
                <p className="mt-1 text-xs text-[var(--color-text-muted)]">{platform.note}</p>
              ) : null}
            </li>
          ))}
        </ul>
      </section>

      <form onSubmit={onProbe} className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
        <label className="block text-sm font-medium">
          Project
          <select
            className="mt-1 w-full rounded-lg border px-3 py-2"
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
        <label className="block text-sm font-medium">
          Query to probe
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="best local seo agency"
            required
          />
        </label>
        <div className="flex flex-wrap gap-2">
          <button
            type="submit"
            disabled={loading || !projectId}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {loading ? 'Probing…' : 'Probe Google AIO + organic'}
          </button>
          <button
            type="button"
            disabled={!projectId || !query.trim()}
            onClick={() => void onTrackQuery()}
            className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
          >
            Track daily
          </button>
        </div>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

      {trackedQueries.length > 0 ? (
        <section className="mt-8 rounded-xl border bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold">Tracked queries</h2>
          <ul className="mt-4 space-y-2">
            {trackedQueries.map((q) => (
              <li key={q.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border px-3 py-2 text-sm">
                <button
                  type="button"
                  className={`text-left font-medium ${selectedQueryId === q.id ? 'text-[var(--color-brand)]' : ''}`}
                  onClick={() => setSelectedQueryId(q.id)}
                >
                  {q.queryText}
                </button>
                <button
                  type="button"
                  className="text-xs text-red-600 hover:underline"
                  onClick={() => void onDeleteQuery(q.id)}
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
          {trends ? (
            <div className="mt-6">
              <h3 className="text-sm font-semibold">
                30-day trends — {trends.queryText}
              </h3>
              <p className="mt-1 text-xs text-[var(--color-text-muted)]">
                Mention rate: {trends.mentionRate30d}%
              </p>
              <div className="mt-3 flex flex-wrap gap-1">
                {trends.points.map((point) => (
                  <span
                    key={`${point.date}-${point.platform}`}
                    title={`${point.date}: ${point.mentioned ? 'mentioned' : 'not mentioned'}`}
                    className={`h-6 w-6 rounded-sm border ${
                      point.mentioned ? 'bg-emerald-500 border-emerald-600' : 'bg-slate-100 border-slate-200'
                    }`}
                  />
                ))}
              </div>
            </div>
          ) : null}
        </section>
      ) : null}

      {result ? (
        <section className="mt-8 rounded-xl border bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold">Probe result</h2>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Query: <strong>{result.query}</strong> · checked {new Date(result.checkedAt).toLocaleString()}
          </p>
          <div className="mt-4 flex flex-wrap gap-2">
            <span
              className={
                result.hasAiOverview
                  ? 'rounded-full border border-purple-200 bg-purple-50 px-3 py-1 text-xs font-medium text-purple-900'
                  : 'rounded-full border px-3 py-1 text-xs text-[var(--color-text-muted)]'
              }
            >
              AI Overview {result.hasAiOverview ? 'present' : 'not detected'}
            </span>
            <span
              className={
                result.organicPosition
                  ? 'rounded-full border border-green-200 bg-green-50 px-3 py-1 text-xs font-medium text-green-900'
                  : 'rounded-full border px-3 py-1 text-xs text-[var(--color-text-muted)]'
              }
            >
              {result.organicPosition
                ? `Organic #${result.organicPosition}`
                : 'Not in top organic results'}
            </span>
          </div>
          {result.note ? (
            <p className="mt-3 text-sm text-[var(--color-text-secondary)]">{result.note}</p>
          ) : null}
          {result.snippet ? (
            <p className="mt-2 rounded-lg bg-[var(--color-surface)] p-3 text-xs text-[var(--color-text-secondary)]">
              {result.snippet}
            </p>
          ) : null}
          <Link href="/serp" className="mt-4 inline-flex text-sm text-[var(--color-brand)] hover:underline">
            Open deep SERP analyzer →
          </Link>
        </section>
      ) : null}
    </main>
  );
}
