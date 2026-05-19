'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { listProjects, researchKeywords, type KeywordResult, type SeoProject } from '@/lib/seo-api';

export default function KeywordResearchPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [seed, setSeed] = useState('');
  const [location, setLocation] = useState('United States');
  const [results, setResults] = useState<KeywordResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const list = await listProjects(accessToken);
        setProjects(list);
        if (list[0]) setProjectId(list[0].id);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load projects');
      }
    })();
  }, [accessToken]);

  async function onResearch(e: React.FormEvent) {
    e.preventDefault();
    if (!projectId || !seed.trim()) return;
    setLoading(true);
    setError(null);
    try {
      setResults(
        await researchKeywords(
          { projectId, seedKeyword: seed, location, resultCount: 30 },
          accessToken,
        ),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Research failed');
    } finally {
      setLoading(false);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="text-2xl font-semibold">Keyword research</h1>
      <p className="mt-1 text-sm text-zinc-600">
        Ideas from DataForSEO — cached per project for clustering and guided mode.
      </p>

      <form onSubmit={onResearch} className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
        <label className="block text-sm font-medium text-zinc-700">
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
        <label className="block text-sm font-medium text-zinc-700">
          Seed keyword
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={seed}
            onChange={(e) => setSeed(e.target.value)}
            placeholder="e.g. plumber near me"
            required
          />
        </label>
        <label className="block text-sm font-medium text-zinc-700">
          Location
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
        </label>
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-zinc-900 px-4 py-2 text-sm text-white hover:bg-zinc-800 disabled:opacity-50"
        >
          {loading ? 'Researching…' : 'Find keywords'}
        </button>
      </form>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      {results.length > 0 && (
        <div className="mt-10 overflow-hidden rounded-xl border bg-white shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-zinc-50 text-left text-xs uppercase tracking-wide text-zinc-500">
              <tr>
                <th className="px-4 py-3">Keyword</th>
                <th className="px-4 py-3">Volume</th>
                <th className="px-4 py-3">Difficulty</th>
                <th className="px-4 py-3">CPC</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {results.map((r) => (
                <tr key={r.keyword} className="hover:bg-zinc-50">
                  <td className="px-4 py-3 font-medium">{r.keyword}</td>
                  <td className="px-4 py-3">{r.searchVolume.toLocaleString()}</td>
                  <td className="px-4 py-3">{r.keywordDifficulty}</td>
                  <td className="px-4 py-3">${r.cpcUsd.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="mt-8 text-sm text-zinc-500">
        <Link href="/app/guided" className="underline hover:text-zinc-800">
          Use Guided mode
        </Link>{' '}
        to turn a keyword into a scored article.
      </p>
    </main>
  );
}
