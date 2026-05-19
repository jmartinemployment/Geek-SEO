'use client';

import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { WordPressSettings } from '@/components/wordpress/wordpress-settings';
import { createContent, listContent, type SeoContentDocument } from '@/lib/seo-api';

export default function ProjectDocumentsPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const params = useParams();
  const router = useRouter();
  const projectId = params.projectId as string;
  const [documents, setDocuments] = useState<SeoContentDocument[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [title, setTitle] = useState('New article');
  const [keyword, setKeyword] = useState('');

  async function load() {
    try {
      setError(null);
      setDocuments(await listContent(projectId, accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load documents');
    }
  }

  useEffect(() => {
    void load();
  }, [projectId]);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    try {
      const doc = await createContent(
        { projectId, title, targetKeyword: keyword },
        accessToken,
      );
      router.push(`/app/content/${doc.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Create failed');
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-3xl p-8">
      <Link href="/app/projects" className="text-sm text-zinc-500 hover:text-zinc-800">
        ← Projects
      </Link>
      <h1 className="mt-4 text-2xl font-semibold">Content documents</h1>

      {error && (
        <p className="mt-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error}</p>
      )}

      <WordPressSettings projectId={projectId} accessToken={accessToken} />

      <form onSubmit={onCreate} className="mt-8 flex flex-col gap-3 rounded-lg border p-4">
        <h2 className="font-medium">New document</h2>
        <input
          className="rounded border px-3 py-2"
          placeholder="Title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
        />
        <input
          className="rounded border px-3 py-2"
          placeholder="Target keyword"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
        />
        <button type="submit" className="rounded bg-zinc-900 px-4 py-2 text-white hover:bg-zinc-800">
          Create &amp; open editor
        </button>
      </form>

      <ul className="mt-8 space-y-2">
        {documents.map((d) => (
          <li key={d.id} className="rounded border p-4">
            <Link href={`/app/content/${d.id}`} className="font-medium hover:underline">
              {d.title}
            </Link>
            <p className="text-sm text-zinc-500">
              {d.targetKeyword || 'No keyword'} · Score {d.seoScore} · {d.wordCount} words
            </p>
          </li>
        ))}
      </ul>
    </main>
  );
}
