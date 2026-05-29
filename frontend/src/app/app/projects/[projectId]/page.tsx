'use client';

import Link from 'next/link';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { GoogleSettings } from '@/components/google/google-settings';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { createContent, listContent, type SeoContentDocument } from '@/lib/seo-api';

export default function ProjectDocumentsPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const params = useParams();
  const router = useRouter();
  const searchParams = useSearchParams();
  const projectId = params.projectId as string;
  const googleNotice = searchParams.get('google');
  const googleMessage = searchParams.get('message');
  const [documents, setDocuments] = useState<SeoContentDocument[]>([]);
  const [error, setError] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [title, setTitle] = useState('New article');
  const [keyword, setKeyword] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);
      setDocuments(await listContent(projectId, accessToken));
    } catch (e) {
      setError(e);
    } finally {
      setLoading(false);
    }
  }, [projectId, accessToken]);

  useEffect(() => {
    if (authLoading) return;
    const timer = setTimeout(() => {
      void load();
    }, 0);
    return () => clearTimeout(timer);
  }, [authLoading, load]);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    setCreating(true);
    try {
      setError(null);
      const doc = await createContent(
        { projectId, title, targetKeyword: keyword },
        accessToken,
      );
      router.push(`/app/content/${doc.id}`);
    } catch (err) {
      setError(err);
      setCreating(false);
    }
  }

  if (authLoading) {
    return <main className="mx-auto max-w-3xl p-8 text-[var(--color-text-secondary)]">Loading…</main>;
  }

  return (
    <main className="mx-auto max-w-3xl p-8">
      <Link href="/app/projects" className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
        ← Projects
      </Link>
      <h1 className="mt-4 text-2xl font-semibold tracking-tight">Content documents</h1>

      {googleNotice === 'connected' ? (
        <p className="mt-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
          Google Search Console and Analytics are connected for this project.
        </p>
      ) : null}
      {googleNotice === 'error' ? (
        <p className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          Google connect failed{googleMessage ? `: ${googleMessage}` : '.'}
        </p>
      ) : null}

      <div className="mt-6">
        <GoogleSettings projectId={projectId} accessToken={accessToken} />
      </div>

      {error ? <div className="mt-4"><SeoErrorBanner error={error} /></div> : null}

      <form
        onSubmit={onCreate}
        className="mt-8 flex flex-col gap-3 rounded-xl border bg-white p-5 shadow-sm"
      >
        <h2 className="font-medium">New document</h2>
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
          disabled={creating}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {creating ? 'Opening editor…' : 'Create & open editor'}
        </button>
      </form>

      {loading ? (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">Loading documents…</p>
      ) : documents.length === 0 ? (
        <div className="mt-8 rounded-xl border border-dashed bg-[var(--color-bg)] p-10 text-center text-sm text-[var(--color-text-secondary)]">
          No documents yet. Create one to open the live SEO editor.
        </div>
      ) : (
        <ul className="mt-8 space-y-3">
          {documents.map((d) => (
            <li key={d.id} className="rounded-xl border bg-white p-4 shadow-sm">
              <Link href={`/app/content/${d.id}`} className="font-medium hover:underline">
                {d.title}
              </Link>
              <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                {d.targetKeyword || 'No keyword'} · Score {d.seoScore > 0 ? d.seoScore : '—'} ·{' '}
                {d.wordCount} words
              </p>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
