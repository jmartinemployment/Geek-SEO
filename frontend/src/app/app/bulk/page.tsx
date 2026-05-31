'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  getJobStatus,
  listProjects,
  startBulkArticles,
  type BackgroundJobStatus,
  type SeoProject,
} from '@/lib/seo-api';

export default function BulkArticlesPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [keywordsText, setKeywordsText] = useState('');
  const [location, setLocation] = useState('United States');
  const [job, setJob] = useState<BackgroundJobStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) {
        setProjectId(list[0].id);
        setLocation(list[0].defaultLocation || 'United States');
      }
    });
  }, [accessToken]);

  useEffect(() => {
    if (!job || job.status === 'completed' || job.status === 'failed') return;

    const timer = setInterval(() => {
      void getJobStatus(job.jobId, accessToken)
        .then(setJob)
        .catch(() => undefined);
    }, 4000);

    return () => clearInterval(timer);
  }, [job, accessToken]);

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

    setLoading(true);
    setError(null);
    try {
      const started = await startBulkArticles({ projectId, keywords, location }, accessToken);
      setJob(started);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bulk job failed to start');
    } finally {
      setLoading(false);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-3xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Bulk article generation</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        Queue up to 20 keywords — each runs brief → outline → draft in the background (Professional tier).
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
          {loading ? 'Starting…' : 'Start bulk job'}
        </button>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

      {job ? (
        <section className="mt-8 rounded-xl border bg-white p-6 shadow-sm">
          <h2 className="text-lg font-semibold">Job status</h2>
          <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
            {job.status} · {job.progressPercent}% complete
          </p>
          {job.errorMessage ? <p className="mt-2 text-sm text-red-600">{job.errorMessage}</p> : null}
          {job.status === 'completed' && job.resultId ? (
            <Link
              href={`/app/content/${job.resultId}`}
              className="mt-4 inline-flex rounded-lg border px-3 py-2 text-sm font-medium hover:bg-slate-50"
            >
              Open last document →
            </Link>
          ) : null}
          <Link
            href="/app/content"
            className="mt-4 ml-3 inline-flex text-sm text-[var(--color-brand)] hover:underline"
          >
            View all content
          </Link>
        </section>
      ) : null}
    </main>
  );
}
