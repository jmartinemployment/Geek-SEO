'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { createProject, listProjects, type SeoProject } from '@/lib/seo-api';

export default function ProjectsPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [url, setUrl] = useState('https://');

  async function load() {
    try {
      setError(null);
      setProjects(await listProjects(accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load projects');
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    try {
      await createProject({ name, url }, accessToken);
      setName('');
      setUrl('https://');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Create failed');
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-3xl p-8">
      <h1 className="text-2xl font-semibold">Projects</h1>
      <p className="mt-2 text-sm text-zinc-600">
        Geek SEO — calls GeekAPI at {process.env.NEXT_PUBLIC_API_URL}. Local dev uses{' '}
        <code className="rounded bg-zinc-100 px-1">X-User-Id</code> when JWT is not configured.
      </p>

      {error && (
        <p className="mt-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error}</p>
      )}

      <form onSubmit={onCreate} className="mt-8 flex flex-col gap-3 rounded-lg border p-4">
        <h2 className="font-medium">New project</h2>
        <input
          className="rounded border px-3 py-2"
          placeholder="Site name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
        <input
          className="rounded border px-3 py-2"
          placeholder="https://example.com"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          required
        />
        <button type="submit" className="rounded bg-zinc-900 px-4 py-2 text-white hover:bg-zinc-800">
          Create
        </button>
      </form>

      <ul className="mt-8 space-y-2">
        {projects.map((p) => (
          <li key={p.id} className="rounded border p-4">
            <Link href={`/app/projects/${p.id}`} className="font-medium hover:underline">
              {p.name}
            </Link>
            <p className="text-sm text-zinc-500">{p.url}</p>
          </li>
        ))}
      </ul>
    </main>
  );
}
