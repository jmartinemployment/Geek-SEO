'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { generateBrief, listProjects, type ContentBrief, type SeoProject } from '@/lib/seo-api';

export default function NewBriefPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [keyword, setKeyword] = useState('');
  const [location, setLocation] = useState('United States');
  const [brief, setBrief] = useState<ContentBrief | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken]);

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-3xl px-6 py-10">
      <Link href="/app/projects" className="text-sm text-zinc-500 hover:text-zinc-800">
        ← Projects
      </Link>
      <h1 className="mt-4 text-2xl font-semibold">Content brief</h1>
      <p className="mt-1 text-sm text-zinc-600">SERP-backed outline before you write.</p>

      <form
        className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm"
        onSubmit={(e) => {
          e.preventDefault();
          void (async () => {
            setLoading(true);
            setError(null);
            try {
              setBrief(await generateBrief({ projectId, keyword, location }, accessToken));
            } catch (err) {
              setError(err instanceof Error ? err.message : 'Brief generation failed');
            } finally {
              setLoading(false);
            }
          })();
        }}
      >
        <select
          className="w-full rounded-lg border px-3 py-2"
          value={projectId}
          onChange={(e) => setProjectId(e.target.value)}
        >
          {projects.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <input
          className="w-full rounded-lg border px-3 py-2"
          placeholder="Target keyword"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          required
        />
        <input
          className="w-full rounded-lg border px-3 py-2"
          value={location}
          onChange={(e) => setLocation(e.target.value)}
        />
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-zinc-900 px-4 py-2 text-sm text-white disabled:opacity-50"
        >
          {loading ? 'Generating…' : 'Generate brief'}
        </button>
      </form>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      {brief && (
        <article className="mt-10 space-y-6 rounded-xl border bg-white p-6 shadow-sm text-sm">
          <div>
            <h2 className="font-semibold">Target</h2>
            <p className="text-zinc-600">
              {brief.keyword} · {brief.location} · ~{brief.targetWordCount} words
            </p>
          </div>
          <div>
            <h2 className="font-semibold">Recommended terms</h2>
            <p className="mt-2 text-zinc-700">{brief.recommendedTerms.join(', ')}</p>
          </div>
          <div>
            <h2 className="font-semibold">Suggested headings</h2>
            <ul className="mt-2 list-inside list-disc text-zinc-700">
              {brief.suggestedHeadings.map((h) => (
                <li key={h}>{h}</li>
              ))}
            </ul>
          </div>
          <div>
            <h2 className="font-semibold">Top competitors</h2>
            <ul className="mt-2 space-y-1 text-zinc-700">
              {brief.topCompetitors.map((c) => (
                <li key={c.url}>
                  #{c.position} {c.title ?? c.url}
                </li>
              ))}
            </ul>
          </div>
        </article>
      )}
    </main>
  );
}
