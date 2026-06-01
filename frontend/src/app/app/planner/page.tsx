'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import {
  clusterKeywords,
  createContent,
  listProjects,
  researchKeywords,
  startFullArticle,
  type KeywordCluster,
  type KeywordResult,
  type SeoProject,
} from '@/lib/seo-api';

type PlannerMode = 'full' | 'quick';

export default function ContentPlannerPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [seed, setSeed] = useState('');
  const [location, setLocation] = useState('United States');
  const [mode, setMode] = useState<PlannerMode>('full');
  const [keywords, setKeywords] = useState<KeywordResult[]>([]);
  const [clusters, setClusters] = useState<KeywordCluster[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionMsg, setActionMsg] = useState<string | null>(null);

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) {
        setProjectId(list[0].id);
        setLocation(list[0].defaultLocation || 'United States');
      }
    });
  }, [accessToken, authReady]);

  async function runPlanner(e: React.FormEvent) {
    e.preventDefault();
    if (!projectId || !seed.trim()) return;
    setLoading(true);
    setError(null);
    setActionMsg(null);
    try {
      const count = mode === 'quick' ? 15 : 40;
      const researched = await researchKeywords(
        { projectId, seedKeyword: seed, location, resultCount: count },
        accessToken,
      );
      setKeywords(researched);
      const names = researched.map((k) => k.keyword);
      setClusters(
        await clusterKeywords({ projectId, keywords: names, location }, accessToken),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Planner failed');
    } finally {
      setLoading(false);
    }
  }

  async function createEditor(keyword: string) {
    if (!projectId) return;
    setActionMsg(null);
    try {
      const doc = await createContent(
        { projectId, title: keyword, targetKeyword: keyword, targetLocation: location },
        accessToken,
      );
      window.location.href = `/app/content/${doc.id}`;
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : 'Could not create document');
    }
  }

  async function queueFullArticle(keyword: string) {
    if (!projectId) return;
    setActionMsg(null);
    try {
      const job = await startFullArticle({ projectId, keyword, location, title: keyword }, accessToken);
      setActionMsg(`Queued full article (job ${job.jobId.slice(0, 8)}…). Check Jobs or open the document when complete.`);
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : 'Queue failed');
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <h1 className="text-2xl font-semibold">Content planner</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        Research keywords, cluster by topic, then open the editor or queue AI articles.
      </p>

      <form
        className="mt-8 grid gap-4 rounded-xl border bg-white p-6 shadow-sm md:grid-cols-2"
        onSubmit={(e) => void runPlanner(e)}
      >
        <label className="block text-sm md:col-span-2">
          <span className="text-[var(--color-text-secondary)]">Project</span>
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
        <label className="block text-sm md:col-span-2">
          <span className="text-[var(--color-text-secondary)]">Seed keyword</span>
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={seed}
            onChange={(e) => setSeed(e.target.value)}
            placeholder="e.g. restaurant pos system"
            required
          />
        </label>
        <label className="block text-sm">
          <span className="text-[var(--color-text-secondary)]">Location</span>
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
        </label>
        <label className="block text-sm">
          <span className="text-[var(--color-text-secondary)]">Mode</span>
          <select
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={mode}
            onChange={(e) => setMode(e.target.value as PlannerMode)}
          >
            <option value="full">Full (more keywords)</option>
            <option value="quick">Quick (faster)</option>
          </select>
        </label>
        <button
          type="submit"
          disabled={loading}
          className="md:col-span-2 rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white disabled:opacity-50"
        >
          {loading ? 'Researching…' : 'Run planner'}
        </button>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}
      {actionMsg ? <p className="mt-4 text-sm text-emerald-700">{actionMsg}</p> : null}

      {clusters.length > 0 ? (
        <section className="mt-10 space-y-6">
          <h2 className="text-lg font-medium">Clusters ({clusters.length})</h2>
          {clusters.map((c) => (
            <article key={c.clusterName} className="rounded-xl border bg-white p-5 shadow-sm">
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                <h3 className="font-medium">{c.clusterName}</h3>
                <p className="text-xs text-[var(--color-text-secondary)]">
                  Pillar: {c.pillarKeyword} · avg vol {Math.round(c.averageVolume)} · KD{' '}
                  {c.averageDifficulty.toFixed(0)}
                </p>
              </div>
              <ul className="mt-3 space-y-2">
                {c.keywords.map((kw) => {
                  const metric = keywords.find(
                    (k) => k.keyword.toLowerCase() === kw.toLowerCase(),
                  );
                  return (
                    <li
                      key={kw}
                      className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-[var(--color-surface-muted)] px-3 py-2 text-sm"
                    >
                      <span>
                        {kw}
                        {metric ? (
                          <span className="ml-2 text-[var(--color-text-secondary)]">
                            {metric.searchVolume.toLocaleString()} vol · KD {metric.keywordDifficulty}
                          </span>
                        ) : null}
                      </span>
                      <span className="flex gap-2">
                        <button
                          type="button"
                          className="rounded border bg-white px-2 py-1 text-xs hover:bg-[var(--color-surface-muted)]"
                          onClick={() => void createEditor(kw)}
                        >
                          Editor
                        </button>
                        <button
                          type="button"
                          className="rounded border border-[var(--color-accent)] bg-[var(--color-accent)] px-2 py-1 text-xs text-white hover:bg-[var(--color-accent-hover)]"
                          onClick={() => void queueFullArticle(kw)}
                        >
                          AI article
                        </button>
                        <Link
                          href={`/app/briefs/new?keyword=${encodeURIComponent(kw)}`}
                          className="rounded border px-2 py-1 text-xs hover:bg-[var(--color-surface-muted)]"
                        >
                          Brief
                        </Link>
                      </span>
                    </li>
                  );
                })}
              </ul>
            </article>
          ))}
        </section>
      ) : null}
    </main>
  );
}
