'use client';

import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { createContent } from '@/lib/seo-api';
import { loadAllContentDocuments, type ProjectWithDocuments, type RecentDocument } from '@/lib/dashboard-data';

export default function ContentListPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const searchParams = useSearchParams();
  const router = useRouter();
  const filterProjectId = searchParams.get('projectId') ?? '';

  const [projects, setProjects] = useState<ProjectWithDocuments[]>([]);
  const [documents, setDocuments] = useState<RecentDocument[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [creating, setCreating] = useState(false);
  const [title, setTitle] = useState('New article');
  const [keyword, setKeyword] = useState('');
  const [createProjectId, setCreateProjectId] = useState('');

  const effectiveCreateProjectId =
    filterProjectId || createProjectId || projects[0]?.id || '';

  useEffect(() => {
    if (authLoading) return;
    let cancelled = false;

    async function init() {
      setLoading(true);
      try {
        setError(null);
        const data = await loadAllContentDocuments(accessToken);
        if (cancelled) return;
        setProjects(data.projects);
        setDocuments(data.allDocuments);
      } catch (e) {
        if (!cancelled) setError(e);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void init();
    return () => {
      cancelled = true;
    };
  }, [accessToken, authLoading]);

  const filtered = useMemo(() => {
    if (!filterProjectId) return documents;
    return documents.filter((d) => d.projectId === filterProjectId);
  }, [documents, filterProjectId]);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!effectiveCreateProjectId) return;
    setCreating(true);
    try {
      setError(null);
      const doc = await createContent(
        { projectId: effectiveCreateProjectId, title, targetKeyword: keyword },
        accessToken,
      );
      router.push(`/app/content/${doc.id}`);
    } catch (err) {
      setError(err);
      setCreating(false);
    }
  }

  if (authLoading) {
    return <main className="mx-auto max-w-4xl p-8 text-[var(--color-text-secondary)]">Loading…</main>;
  }

  return (
    <main className="mx-auto max-w-4xl px-2 py-4 md:px-0">
      <h1 className="text-2xl font-semibold tracking-tight">Content documents</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        All articles across your sites — open the editor or filter by project.
      </p>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <label className="text-sm font-medium text-[var(--color-text-primary)]">
          Project
          <select
            className="ml-2 rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
            value={filterProjectId}
            onChange={(e) => {
              const value = e.target.value;
              router.push(value ? `/app/content?projectId=${value}` : '/app/content');
            }}
          >
            <option value="">All projects</option>
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
        <Link
          href="/app/projects"
          className="text-sm text-[var(--color-brand)] hover:underline"
        >
          Manage sites
        </Link>
      </div>

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      <form
        onSubmit={onCreate}
        className="mt-8 flex flex-col gap-3 rounded-xl border bg-white p-5 shadow-sm"
      >
        <h2 className="font-medium">New document</h2>
        <select
          className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
          value={effectiveCreateProjectId}
          onChange={(e) => setCreateProjectId(e.target.value)}
          required
        >
          {projects.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <input
          className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
          placeholder="Title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
        />
        <input
          className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
          placeholder="Target keyword"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
        />
        <button
          type="submit"
          disabled={creating || projects.length === 0}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {creating ? 'Opening editor…' : 'Create & open editor'}
        </button>
      </form>

      {loading ? (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">Loading documents…</p>
      ) : filtered.length === 0 ? (
        <div className="mt-8 rounded-xl border border-dashed bg-[var(--color-bg)] p-10 text-center text-sm text-[var(--color-text-secondary)]">
          No documents yet. Create one to open the live SEO editor.
        </div>
      ) : (
        <ul className="mt-8 space-y-3">
          {filtered.map((d) => (
            <li key={d.id} className="rounded-xl border bg-white p-4 shadow-sm">
              <Link href={`/app/content/${d.id}`} className="font-medium hover:underline">
                {d.title || 'Untitled'}
              </Link>
              <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                {d.projectName} · {d.targetKeyword || 'No keyword'} · Score{' '}
                {d.seoScore > 0 ? d.seoScore : '—'} · {d.wordCount} words · {d.status}
              </p>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
