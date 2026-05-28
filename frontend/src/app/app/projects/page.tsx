'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/auth/auth-provider';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { createProject, getSeoApiUrl, listProjects, type SeoProject } from '@/lib/seo-api';

export default function ProjectsPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const router = useRouter();
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
      const project = await createProject({ name, url }, accessToken);
      router.push(`/app/projects/${project.id}`);
    } catch (err) {
      setError(err);
      setCreating(false);
    }
  }

  if (authLoading) {
    return (
      <main className="mx-auto max-w-3xl p-8">
        <div className="h-8 w-48 animate-pulse rounded bg-zinc-200" />
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-3xl p-8">
      <h1 className="text-2xl font-semibold tracking-tight">Projects</h1>
      <p className="mt-2 text-sm text-zinc-600">
        Step 1 — create a site, then add content documents with the live editor.
      </p>
      <p className="mt-1 text-xs text-zinc-500">
        API: <code className="rounded bg-zinc-100 px-1">{getSeoApiUrl()}</code>
      </p>

      {error ? <div className="mt-4"><SeoErrorBanner error={error} /></div> : null}

      <form
        onSubmit={onCreate}
        className="mt-8 flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-5 shadow-sm"
      >
        <div>
          <h2 className="font-medium">New project</h2>
          <p className="mt-1 text-sm text-zinc-500">
            A project represents one website. Add content documents inside it to write and score SEO articles.
          </p>
        </div>
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-zinc-700">Project name</label>
          <input
            className="rounded-lg border border-zinc-300 px-3 py-2 focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500"
            placeholder="e.g. Geek At Your Spot"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-zinc-700">Website URL</label>
          <input
            className="rounded-lg border border-zinc-300 px-3 py-2 focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500"
            placeholder="https://yourdomain.com"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            required
          />
          <p className="text-xs text-zinc-400">Root domain only — used for competitor analysis and scoring.</p>
        </div>
        <button
          type="submit"
          disabled={creating}
          className="rounded-lg bg-zinc-900 px-4 py-2 text-white hover:bg-zinc-800 disabled:opacity-50"
        >
          {creating ? 'Creating…' : 'Create project'}
        </button>
      </form>

      {loading ? (
        <p className="mt-8 text-sm text-zinc-500">Loading projects…</p>
      ) : projects.length === 0 ? (
        <div className="mt-8 rounded-xl border border-dashed border-zinc-300 bg-zinc-50 p-10 text-center">
          <p className="text-sm text-zinc-600">No projects yet. Create one above to start writing.</p>
        </div>
      ) : (
        <ul className="mt-8 space-y-3">
          {projects.map((p) => (
            <li key={p.id} className="rounded-xl border bg-white p-4 shadow-sm transition hover:border-zinc-300">
              <Link href={`/app/projects/${p.id}`} className="font-medium hover:underline">
                {p.name}
              </Link>
              <p className="mt-1 text-sm text-zinc-500">{p.url}</p>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
