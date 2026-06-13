'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  listContent,
  listProjects,
  updateContentStatus,
  type SeoContentDocument,
  type SeoProject,
} from '@/lib/seo-api';

const COLUMNS = [
  { id: 'planned', label: 'Planned', hint: 'Ideas and briefs' },
  { id: 'writing', label: 'Writing', hint: 'Draft in progress' },
  { id: 'review', label: 'Review', hint: 'Score and polish' },
  { id: 'published', label: 'Published', hint: 'Live or scheduled' },
] as const;

type ColumnId = (typeof COLUMNS)[number]['id'];

type CalendarCard = SeoContentDocument & { projectName: string };

function normalizeStatus(status: string): ColumnId {
  const lower = status.toLowerCase();
  if (COLUMNS.some((c) => c.id === lower)) return lower as ColumnId;
  if (lower === 'draft') return 'writing';
  return 'planned';
}

export default function ContentCalendarPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [cards, setCards] = useState<CalendarCard[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const [movingId, setMovingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const projects = await listProjects(accessToken);
      const nested = await Promise.all(
        projects.map(async (p: SeoProject) => {
          const docs = await listContent(p.id, accessToken);
          return docs.map((d) => ({ ...d, projectName: p.name }));
        }),
      );
      setCards(nested.flat());
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load calendar');
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

  async function moveCard(card: CalendarCard, nextStatus: ColumnId) {
    if (normalizeStatus(card.status) === nextStatus) return;
    setMovingId(card.id);
    setError(null);
    try {
      const updated = await updateContentStatus(card.id, nextStatus, accessToken);
      setCards((prev) =>
        prev.map((c) =>
          c.id === card.id ? { ...c, status: updated.status, seoScore: updated.seoScore } : c,
        ),
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not update status');
    } finally {
      setMovingId(null);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Content calendar</h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Drag cards between columns or drop on a column to update workflow status.
          </p>
        </div>
        <Link
          href="/app/content-writing"
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)]"
        >
          New article
        </Link>
      </header>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}
      {loading && <p className="mt-8 text-sm text-[var(--color-text-secondary)]">Loading board…</p>}

      {!loading && cards.length === 0 && (
        <div className="mt-10 rounded-xl border border-dashed bg-[var(--color-bg)] p-12 text-center">
          <p className="text-sm text-[var(--color-text-secondary)]">No content yet. Start with the content writing workspace or a project.</p>
          <div className="mt-4 flex justify-center gap-3">
            <Link
              href="/app/content-writing"
              className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)]"
            >
              Content Writing
            </Link>
            <Link
              href="/app/projects"
              className="rounded-lg border bg-white px-4 py-2 text-sm hover:bg-[var(--color-surface-muted)]"
            >
              Projects
            </Link>
          </div>
        </div>
      )}

      {!loading && cards.length > 0 && (
        <div className="mt-8 grid gap-4 lg:grid-cols-4">
          {COLUMNS.map((col) => {
            const columnCards = cards.filter((c) => normalizeStatus(c.status) === col.id);
            return (
              <section
                key={col.id}
                className="flex min-h-[320px] flex-col rounded-xl border bg-[var(--color-surface-muted)]/80"
                onDragOver={(e) => {
                  e.preventDefault();
                  e.dataTransfer.dropEffect = 'move';
                }}
                onDrop={(e) => {
                  e.preventDefault();
                  const id = e.dataTransfer.getData('text/plain') || dragId;
                  const card = cards.find((c) => c.id === id);
                  if (card) void moveCard(card, col.id);
                  setDragId(null);
                }}
              >
                <div className="rounded-t-xl border-b bg-white px-3 py-3">
                  <h2 className="text-sm font-semibold">{col.label}</h2>
                  <p className="text-xs text-[var(--color-text-secondary)]">{col.hint}</p>
                  <span className="mt-1 inline-block rounded-full bg-[var(--color-surface-muted)] px-2 py-0.5 text-xs font-medium text-[var(--color-text-secondary)]">
                    {columnCards.length}
                  </span>
                </div>
                <ul className="flex flex-1 flex-col gap-2 p-2">
                  {columnCards.map((card) => (
                    <li
                      key={card.id}
                      draggable={movingId !== card.id}
                      onDragStart={(e) => {
                        setDragId(card.id);
                        e.dataTransfer.setData('text/plain', card.id);
                        e.dataTransfer.effectAllowed = 'move';
                      }}
                      onDragEnd={() => setDragId(null)}
                      className={`rounded-lg border bg-white p-3 shadow-sm transition-shadow hover:shadow-md ${
                        dragId === card.id ? 'opacity-50' : ''
                      } ${movingId === card.id ? 'pointer-events-none opacity-60' : 'cursor-grab active:cursor-grabbing'}`}
                    >
                      <Link
                        href={`/app/content/${card.id}`}
                        className="block text-sm font-medium text-[var(--color-text-primary)] hover:underline"
                        onClick={(e) => e.stopPropagation()}
                      >
                        {card.title || 'Untitled'}
                      </Link>
                      <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{card.projectName}</p>
                      {card.targetKeyword && (
                        <p className="mt-1 truncate text-xs text-[var(--color-text-muted)]">{card.targetKeyword}</p>
                      )}
                      <div className="mt-2 flex items-center justify-between text-xs">
                        <span className="rounded bg-[var(--color-surface-muted)] px-1.5 py-0.5 font-medium text-[var(--color-text-primary)]">
                          {card.seoScore > 0 ? `Score ${card.seoScore}` : 'Not scored'}
                        </span>
                        <span className="text-[var(--color-text-muted)]">{card.wordCount} words</span>
                      </div>
                    </li>
                  ))}
                </ul>
              </section>
            );
          })}
        </div>
      )}
    </main>
  );
}
