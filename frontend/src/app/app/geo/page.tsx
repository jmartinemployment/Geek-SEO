'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  getGeoPlatforms,
  listProjects,
  probeGeoVisibility,
  type GeoPlatformStatus,
  type GeoProbeResult,
  type SeoProject,
} from '@/lib/seo-api';

export default function GeoPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [query, setQuery] = useState('');
  const [platforms, setPlatforms] = useState<GeoPlatformStatus[]>([]);
  const [result, setResult] = useState<GeoProbeResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
    void getGeoPlatforms(accessToken)
      .then((res) => setPlatforms(res.platforms))
      .catch(() => undefined);
  }, [accessToken]);

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

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">GEO visibility</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        On-demand Google AI Overview and organic visibility checks via DataForSEO. Daily multi-LLM
        tracking requires configured probe workers (not yet scheduled).
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
        <button
          type="submit"
          disabled={loading || !projectId}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? 'Probing…' : 'Probe Google AIO + organic'}
        </button>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

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
          <Link href="/app/serp" className="mt-4 inline-flex text-sm text-[var(--color-brand)] hover:underline">
            Open deep SERP analyzer →
          </Link>
        </section>
      ) : null}
    </main>
  );
}
