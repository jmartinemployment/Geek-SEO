'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  createContent,
  generateTopicalMap,
  listProjects,
  type SeoProject,
  type TopicalMapTopic,
} from '@/lib/seo-api';
function coverageStyle(coverage: TopicalMapTopic['coverage']): string {
  if (coverage === 'covered') return 'bg-green-50 text-green-800 border-green-200';
  if (coverage === 'partial') return 'bg-amber-50 text-amber-900 border-amber-200';
  return 'bg-red-50 text-red-800 border-red-200';
}

export default function TopicalMapPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [topics, setTopics] = useState<TopicalMapTopic[]>([]);
  const [summary, setSummary] = useState<{ covered: number; partial: number; gap: number } | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [creatingId, setCreatingId] = useState<string | null>(null);

  useEffect(() => {
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken]);

  async function generate() {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const result = await generateTopicalMap(projectId, accessToken, { force: true });
      setTopics(result.topics);
      setSummary({
        covered: result.coveredCount,
        partial: result.partialCount,
        gap: result.gapCount,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Topical map failed');
      setTopics([]);
      setSummary(null);
    } finally {
      setLoading(false);
    }
  }

  async function writeTopic(topic: TopicalMapTopic) {
    if (!projectId) return;
    const keyword = topic.queries[0] ?? topic.name;
    setCreatingId(topic.name);
    setError(null);
    try {
      const doc = await createContent(
        { projectId, title: topic.name, targetKeyword: keyword },
        accessToken,
      );
      window.location.href = `/app/content/${doc.id}`;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create document');
      setCreatingId(null);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Topical map</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        GSC query clusters mapped to live landing pages on your site — covered, partial, or gap (Professional tier + GSC).
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
          onClick={() => void generate()}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? 'Generating…' : 'Generate map'}
        </button>
        <Link href="/app/planner" className="text-sm text-[var(--color-brand)] hover:underline">
          Keyword planner →
        </Link>
      </div>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

      {summary ? (
        <div className="mt-6 flex flex-wrap gap-3 text-sm">
          <span className="rounded-full border border-green-200 bg-green-50 px-3 py-1">{summary.covered} covered</span>
          <span className="rounded-full border border-amber-200 bg-amber-50 px-3 py-1">{summary.partial} partial</span>
          <span className="rounded-full border border-red-200 bg-red-50 px-3 py-1">{summary.gap} gaps</span>
        </div>
      ) : null}

      <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {topics.map((topic) => (
          <article
            key={`${topic.name}-${topic.matchedPageUrl ?? topic.queries[0] ?? 'topic'}`}
            className="rounded-xl border bg-white p-4 shadow-sm"
          >
            <div className="flex items-start justify-between gap-2">
              <h2 className="font-semibold text-[var(--color-text-primary)]">{topic.name}</h2>
              <span className={`shrink-0 rounded-full border px-2 py-0.5 text-xs font-medium ${coverageStyle(topic.coverage)}`}>
                {topic.coverage}
              </span>
            </div>
            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              {topic.totalImpressions.toLocaleString()} impressions
            </p>
            {topic.matchedPageUrl ? (
              <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
                Live page:{' '}
                <a
                  href={topic.matchedPageUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="font-medium text-[var(--color-brand)] hover:underline break-all"
                >
                  {topic.matchedPageUrl.replace(/^https?:\/\/(www\.)?/, '')}
                </a>
                {topic.matchSource === 'gsc' ? (
                  <span className="ml-1 text-[var(--color-text-muted)]">(Search Console)</span>
                ) : null}
              </p>
            ) : null}
            {topic.matchedDocumentTitle ? (
              <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
                Matched:{' '}
                {topic.matchedDocumentId ? (
                  <Link href={`/app/content/${topic.matchedDocumentId}`} className="font-medium text-[var(--color-brand)] hover:underline">
                    {topic.matchedDocumentTitle}
                  </Link>
                ) : (
                  topic.matchedDocumentTitle
                )}
              </p>
            ) : null}
            <ul className="mt-3 max-h-24 overflow-y-auto text-xs text-[var(--color-text-secondary)]">
              {topic.queries.map((q) => (
                <li key={q} className="truncate">
                  {q}
                </li>
              ))}
            </ul>
            {topic.coverage !== 'covered' ? (
              <button
                type="button"
                disabled={creatingId === topic.name}
                onClick={() => void writeTopic(topic)}
                className="mt-3 rounded-lg border px-3 py-1.5 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
              >
                {creatingId === topic.name ? 'Creating…' : 'Write this topic'}
              </button>
            ) : null}
          </article>
        ))}
      </div>
    </main>
  );
}
