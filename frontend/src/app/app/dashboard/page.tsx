'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { listContent, listProjects, type SeoContentDocument, type SeoProject } from '@/lib/seo-api';

type ProjectSummary = SeoProject & { documents: SeoContentDocument[] };

export default function DashboardPage() {
  const { accessToken } = useAuth();
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void (async () => {
      try {
        const list = await listProjects(accessToken);
        const withDocs = await Promise.all(
          list.map(async (p) => ({
            ...p,
            documents: await listContent(p.id, accessToken),
          })),
        );
        setProjects(withDocs);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load dashboard');
      } finally {
        setLoading(false);
      }
    })();
  }, [accessToken]);

  const totalDocs = projects.reduce((n, p) => n + p.documents.length, 0);
  const avgScore =
    totalDocs === 0
      ? 0
      : Math.round(
          projects
            .flatMap((p) => p.documents)
            .reduce((sum, d) => sum + d.seoScore, 0) / totalDocs,
        );

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
      <p className="mt-1 text-sm text-zinc-600">Overview of your SEO projects and content.</p>

      {loading && <p className="mt-8 text-sm text-zinc-500">Loading…</p>}
      {error && <p className="mt-8 text-sm text-red-600">{error}</p>}

      {!loading && !error && (
        <>
          <div className="mt-8 grid gap-4 sm:grid-cols-3">
            <div className="rounded-xl border bg-white p-5 shadow-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-zinc-500">Projects</p>
              <p className="mt-2 text-3xl font-semibold">{projects.length}</p>
            </div>
            <div className="rounded-xl border bg-white p-5 shadow-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-zinc-500">Documents</p>
              <p className="mt-2 text-3xl font-semibold">{totalDocs}</p>
            </div>
            <div className="rounded-xl border bg-white p-5 shadow-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-zinc-500">Avg. SEO score</p>
              <p className="mt-2 text-3xl font-semibold">{avgScore || '—'}</p>
            </div>
          </div>

          <section className="mt-10">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-medium">Recent content</h2>
              <Link href="/app/guided" className="text-sm text-zinc-600 hover:text-zinc-900">
                Start guided flow →
              </Link>
            </div>
            {totalDocs === 0 ? (
              <div className="mt-6 rounded-xl border border-dashed bg-zinc-50 p-10 text-center">
                <p className="text-sm text-zinc-600">No content yet.</p>
                <Link
                  href="/app/projects"
                  className="mt-4 inline-block rounded-lg bg-zinc-900 px-4 py-2 text-sm text-white hover:bg-zinc-800"
                >
                  Create a project
                </Link>
              </div>
            ) : (
              <ul className="mt-4 divide-y rounded-xl border bg-white">
                {projects.flatMap((p) =>
                  p.documents.slice(0, 5).map((d) => (
                    <li key={d.id} className="flex items-center justify-between px-4 py-3 text-sm">
                      <div>
                        <Link
                          href={`/app/content/${d.id}`}
                          className="font-medium text-zinc-900 hover:underline"
                        >
                          {d.title || 'Untitled'}
                        </Link>
                        <p className="text-xs text-zinc-500">
                          {p.name} · {d.targetKeyword || 'No keyword'}
                        </p>
                      </div>
                      <span className="rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-medium">
                        {d.seoScore > 0 ? `${d.seoScore}` : '—'}
                      </span>
                    </li>
                  )),
                )}
              </ul>
            )}
          </section>
        </>
      )}
    </main>
  );
}
