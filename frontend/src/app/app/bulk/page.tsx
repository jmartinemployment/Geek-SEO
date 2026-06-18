'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import { describeDraftJobProgress, listProjects, type SeoContentDocument, type SeoProject } from '@/lib/seo-api';
import { runBulkKeywordDrafts, type BulkDraftProgress } from '@/lib/draft-job-signalr';

function formatElapsed(elapsedMs: number): string {
  const totalSeconds = Math.floor(elapsedMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
}

export default function BulkArticlesPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const hub = useSeoHub();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [keywordsText, setKeywordsText] = useState('');
  const [location, setLocation] = useState('United States');
  const [progress, setProgress] = useState<BulkDraftProgress | null>(null);
  const [completed, setCompleted] = useState<SeoContentDocument[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!projectId) return;

    const keywords = keywordsText
      .split('\n')
      .map((k) => k.trim())
      .filter(Boolean);

    if (keywords.length === 0) {
      setError('Enter at least one keyword (one per line).');
      return;
    }

    if (keywords.length > 20) {
      setError('Enter at most 20 keywords.');
      return;
    }

    setLoading(true);
    setError(null);
    setCompleted([]);
    setProgress(null);
    try {
      const documents = await runBulkKeywordDrafts(projectId, keywords, location, accessToken, {
        hub,
        onProgress: setProgress,
      });
      setCompleted(documents);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bulk draft failed');
    } finally {
      setLoading(false);
      setProgress(null);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  const progressLabel = progress
    ? `Keyword ${progress.keywordIndex}/${progress.keywordTotal}: ${progress.keyword} — ${describeDraftJobProgress(progress.step)} (${formatElapsed(progress.elapsedMs)}${progress.step.progressPercent > 0 ? ` · ${progress.step.progressPercent}%` : ''})`
    : null;

  return (
    <main className="mx-auto max-w-3xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Bulk article generation</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        Draft up to 20 keywords sequentially — each keyword runs as a background job with live progress over SignalR.
      </p>

      <form onSubmit={onSubmit} className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
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
          Keywords (one per line)
          <textarea
            className="mt-1 min-h-[160px] w-full rounded-lg border px-3 py-2 font-mono text-sm"
            value={keywordsText}
            onChange={(e) => setKeywordsText(e.target.value)}
            placeholder={'best crm for small business\nlocal seo checklist\n...'}
            required
          />
        </label>
        <label className="block text-sm font-medium">
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
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? progressLabel ?? 'Drafting…' : 'Generate all drafts'}
        </button>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

      {completed.length > 0 ? (
        <section className="mt-8 rounded-xl border bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold">Completed ({completed.length})</h2>
          <ul className="mt-3 space-y-2 text-sm">
            {completed.map((doc) => (
              <li key={doc.id}>
                <Link
                  href={`/content-writing?documentId=${doc.id}`}
                  className="text-[var(--color-brand)] hover:underline"
                >
                  {doc.title || doc.targetKeyword || doc.id}
                </Link>
              </li>
            ))}
          </ul>
          <Link
            href="/content-writing"
            className="mt-4 inline-flex text-sm text-[var(--color-brand)] hover:underline"
          >
            View all content →
          </Link>
        </section>
      ) : null}
    </main>
  );
}
