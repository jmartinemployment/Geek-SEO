'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { createProject, getSeoApiUrl, listProjects, type SeoProject } from '@/lib/seo-api';

function ProjectsPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const googleNotice = searchParams.get('google');
  const googleMessage = searchParams.get('message');
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [error, setError] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [name, setName] = useState('');
  const [url, setUrl] = useState('https://');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);
      setProjects(await listProjects(accessToken));
    } catch (e) {
      setError(e);
    } finally {
      setLoading(false);
    }
  }, [accessToken]);

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
      const project = await createProject(
        { name, url, defaultLocation: 'United States' },
        accessToken,
      );
      router.push(`/projects/${project.id}`);
    } catch (err) {
      setError(err);
      setCreating(false);
    }
  }

  if (authLoading) {
    return (
      <main className="mx-auto max-w-3xl p-8">
        <div className="h-8 w-48 animate-pulse rounded bg-[var(--color-surface-muted)]" />
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-3xl p-8">
      <h1 className="text-2xl font-semibold tracking-tight">Projects</h1>
      <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
        Step 1 — create a site, then add content documents with the live editor.
      </p>
      <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
        API: <code className="rounded bg-[var(--color-surface-muted)] px-1">{getSeoApiUrl()}</code>
      </p>

      {googleNotice === 'connected' ? (
        <div className="mt-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
          Google Search Console and Analytics are connected. Open a project to view rankings and analytics.
        </div>
      ) : null}
      {googleNotice === 'error' ? (
        <p className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          Google connect failed{googleMessage ? `: ${googleMessage}` : '.'}
        </p>
      ) : null}

      {error ? <div className="mt-4"><SeoErrorBanner error={error} /></div> : null}

      <form
        onSubmit={onCreate}
        className="mt-8 flex flex-col gap-3 rounded-xl border border-[var(--color-border)] bg-white p-5 shadow-sm"
      >
        <div>
          <h2 className="font-medium">New project</h2>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            A project represents one website. Add content documents inside it to write and score SEO articles.
          </p>
        </div>
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-[var(--color-text-primary)]">Project name</label>
          <input
            className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 focus:border-[var(--color-accent)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
            placeholder="e.g. Geek At Your Spot"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-[var(--color-text-primary)]">Website URL</label>
          <input
            className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 focus:border-[var(--color-accent)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
            placeholder="https://yourdomain.com"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            required
          />
          <p className="text-xs text-[var(--color-text-muted)]">Root domain only — used for competitor analysis and scoring.</p>
        </div>
        <button
          type="submit"
          disabled={creating}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {creating ? 'Creating…' : 'Create project'}
        </button>
      </form>

      {loading ? (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">Loading projects…</p>
      ) : projects.length === 0 ? (
        <div className="mt-8 rounded-xl border border-dashed border-[var(--color-border-strong)] bg-[var(--color-bg)] p-10 text-center">
          <p className="text-sm text-[var(--color-text-secondary)]">No projects yet. Create one above to start writing.</p>
        </div>
      ) : (
        <ul className="mt-8 space-y-3">
          {projects.map((p) => (
            <li key={p.id} className="rounded-xl border bg-white p-4 shadow-sm transition hover:border-[var(--color-border-strong)]">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <Link href={`/projects/${p.id}`} className="font-medium hover:underline">
                    {p.name}
                  </Link>
                  <p className="mt-1 text-sm text-[var(--color-text-secondary)]">{p.url}</p>
                  <p className="mt-2 text-xs text-[var(--color-text-muted)]">
                    {p.gscConnected ? (
                      <span className="rounded-full bg-green-50 px-2 py-0.5 text-green-800">GSC connected</span>
                    ) : (
                      <span className="rounded-full bg-amber-50 px-2 py-0.5 text-amber-900">Google not connected</span>
                    )}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2 text-xs">
                  <Link
                    href={`/projects/${p.id}`}
                    className="rounded-lg border px-2.5 py-1.5 hover:bg-[var(--color-surface-muted)]"
                  >
                    Documents
                  </Link>
                  <Link
                    href="/rankings"
                    className="rounded-lg border px-2.5 py-1.5 hover:bg-[var(--color-surface-muted)]"
                  >
                    Rankings
                  </Link>
                  <Link
                    href="/analytics"
                    className="rounded-lg border px-2.5 py-1.5 hover:bg-[var(--color-surface-muted)]"
                  >
                    Analytics
                  </Link>
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}

export default function ProjectsPage() {
  return (
    <Suspense
      fallback={
        <main className="mx-auto max-w-3xl p-8">
          <div className="h-8 w-48 animate-pulse rounded bg-[var(--color-surface-muted)]" />
        </main>
      }
    >
      <ProjectsPageInner />
    </Suspense>
  );
}
