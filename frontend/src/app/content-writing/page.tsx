'use client';

import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useMemo, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  ReviewEditorPane,
  ReviewScoreLeft,
  ReviewScoreRight,
  ReviewWorkspaceProvider,
} from '@/components/content-writing/review-workspace-context';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  createContent,
  generateBrief,
  generateDraft,
  generateOutline,
  getContent,
  listProjects,
  updateContent,
  updateContentStatus,
  type ContentBrief,
  type SeoContentDocument,
  type SeoProject,
} from '@/lib/seo-api';

const DEFAULT_LOCATION = 'United States';
const DEFAULT_DRAFT_HTML = '<h1>Article title</h1><p>Start writing your article.</p>';

type Stage = 'brief' | 'outline' | 'draft' | 'review';

function stepLabel(stage: Stage): string {
  if (stage === 'brief') return 'Step 1 of 4';
  if (stage === 'outline') return 'Step 2 of 4';
  if (stage === 'draft') return 'Step 3 of 4';
  return 'Step 4 of 4';
}

function ContentWritingPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const searchParams = useSearchParams();
  const initialKeyword = searchParams.get('keyword') ?? '';
  const initialTitle = searchParams.get('title') ?? '';
  const initialProjectId = searchParams.get('projectId') ?? '';
  const initialLocation = searchParams.get('location') ?? DEFAULT_LOCATION;
  const initialDocumentId = searchParams.get('documentId') ?? '';

  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId);
  const [title, setTitle] = useState(initialTitle || initialKeyword || 'New article');
  const [keyword, setKeyword] = useState(initialKeyword);
  const [location, setLocation] = useState(initialLocation);
  const [brief, setBrief] = useState<ContentBrief | null>(null);
  const [outline, setOutline] = useState('');
  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [stage, setStage] = useState<Stage>('brief');
  const [loadingAction, setLoadingAction] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [documentLoading, setDocumentLoading] = useState(!!initialDocumentId);

  useEffect(() => {
    if (authLoading) return;
    let cancelled = false;

    async function loadProjects() {
      try {
        const list = await listProjects(accessToken);
        if (cancelled) return;
        setProjects(list);
        if (!projectId && list[0]) {
          setProjectId(list[0].id);
          if (!searchParams.get('location')) {
            setLocation(list[0].defaultLocation || DEFAULT_LOCATION);
          }
        }
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      }
    }

    void loadProjects();
    return () => {
      cancelled = true;
    };
  }, [accessToken, authLoading, projectId, searchParams]);

  useEffect(() => {
    if (!initialDocumentId || authLoading) return;
    let cancelled = false;

    async function loadDocument() {
      setDocumentLoading(true);
      try {
        const loaded = await getContent(initialDocumentId, accessToken);
        if (cancelled) return;
        setDoc(loaded);
        setTitle(loaded.title);
        setKeyword(loaded.targetKeyword);
        setLocation(loaded.targetLocation || DEFAULT_LOCATION);
        setProjectId(loaded.projectId);
        setStage('review');
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      } finally {
        if (!cancelled) setDocumentLoading(false);
      }
    }

    void loadDocument();
    return () => {
      cancelled = true;
    };
  }, [initialDocumentId, accessToken, authLoading]);

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === projectId) ?? null,
    [projectId, projects],
  );

  const inReview = stage === 'review' && !!doc;

  async function run(action: string, fn: () => Promise<void>) {
    setLoadingAction(action);
    setError(null);
    setStatusMessage(null);
    try {
      await fn();
    } catch (runError) {
      setError(runError);
    } finally {
      setLoadingAction(null);
    }
  }

  async function ensureDocument(nextHtml: string): Promise<SeoContentDocument> {
    if (!projectId) throw new Error('Select a project first.');

    if (!doc) {
      const created = await createContent(
        {
          projectId,
          title,
          targetKeyword: keyword,
          targetLocation: location,
        },
        accessToken,
      );
      const saved = await updateContent(
        created.id,
        {
          contentHtml: nextHtml,
          title,
          targetKeyword: keyword,
          targetLocation: location,
        },
        accessToken,
      );
      setDoc(saved);
      return saved;
    }

    const saved = await updateContent(
      doc.id,
      {
        contentHtml: nextHtml,
        title,
        targetKeyword: keyword,
        targetLocation: location,
      },
      accessToken,
    );
    setDoc(saved);
    return saved;
  }

  if (authLoading || documentLoading) {
    return <main className="mx-auto max-w-5xl p-8 text-[var(--color-text-secondary)]">Loading…</main>;
  }

  const grid = (
    <div className="w-full max-w-none">
      <div className="grid min-h-[calc(100vh-8rem)] grid-cols-12 gap-4">
        <div className="col-span-2 min-w-0 space-y-4 self-start">
          {inReview ? (
            <div className="min-w-0 rounded-xl border bg-white shadow-sm">
              <ReviewScoreLeft keyword={keyword} />
            </div>
          ) : null}
        </div>

        <div className="col-span-8 min-w-0 space-y-6">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h1 className="text-2xl font-semibold tracking-tight">Content Writing</h1>
              <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                Brief, outline, draft, edit, review, and publish from one workspace.
              </p>
            </div>
            {selectedProject ? (
              <Link
                href={`/app/projects/${selectedProject.id}`}
                className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
              >
                Project documents
              </Link>
            ) : null}
          </div>

          {error ? <SeoErrorBanner error={error} /> : null}

          {!inReview ? (
            <>
              <section className="rounded-xl border bg-white p-5 shadow-sm">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <h2 className="font-semibold">Article setup</h2>
                    <p className="text-sm text-[var(--color-text-secondary)]">
                      Start from stable project and SERP inputs. Optional niche context is additive only.
                    </p>
                  </div>
                  <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
                    {stepLabel(stage)}
                  </span>
                </div>

                <div className="mt-4 grid gap-4 md:grid-cols-2">
                  <label className="text-sm font-medium text-[var(--color-text-primary)]">
                    Project
                    <select
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={projectId}
                      onChange={(event) => {
                        setProjectId(event.target.value);
                        const nextProject = projects.find((project) => project.id === event.target.value);
                        if (nextProject && !searchParams.get('location')) {
                          setLocation(nextProject.defaultLocation || DEFAULT_LOCATION);
                        }
                      }}
                    >
                      <option value="">Select a project</option>
                      {projects.map((project) => (
                        <option key={project.id} value={project.id}>
                          {project.name}
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="text-sm font-medium text-[var(--color-text-primary)]">
                    Location
                    <input
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={location}
                      onChange={(event) => setLocation(event.target.value)}
                    />
                  </label>

                  <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                    Working title
                    <input
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={title}
                      onChange={(event) => setTitle(event.target.value)}
                    />
                  </label>

                  <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                    Target keyword
                    <input
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={keyword}
                      onChange={(event) => setKeyword(event.target.value)}
                      placeholder="e.g. zapier quickbooks integration"
                    />
                  </label>
                </div>

                <div className="mt-5 flex flex-wrap gap-3">
                  <button
                    type="button"
                    disabled={!projectId || !keyword.trim() || !!loadingAction}
                    className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                    onClick={() =>
                      void run('brief', async () => {
                        const nextBrief = await generateBrief({ projectId, keyword, location }, accessToken);
                        setBrief(nextBrief);
                        setOutline('');
                        setStage('outline');
                      })
                    }
                  >
                    {loadingAction === 'brief' ? 'Generating brief…' : 'Generate brief'}
                  </button>

                  <button
                    type="button"
                    disabled={!brief || !!loadingAction}
                    className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
                    onClick={() =>
                      void run('outline', async () => {
                        if (!brief) return;
                        const result = await generateOutline({ keyword, brief, title }, accessToken);
                        setOutline(result.content);
                        setStage('draft');
                      })
                    }
                  >
                    {loadingAction === 'outline' ? 'Building outline…' : 'Generate outline'}
                  </button>

                  <button
                    type="button"
                    disabled={!brief || !outline.trim() || !!loadingAction}
                    className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
                    onClick={() =>
                      void run('draft', async () => {
                        if (!brief) return;
                        const result = await generateDraft(
                          {
                            keyword,
                            brief,
                            outline,
                            targetWordCount: brief.targetWordCount,
                            title,
                          },
                          accessToken,
                        );
                        const saved = await ensureDocument(result.content || DEFAULT_DRAFT_HTML);
                        await updateContentStatus(saved.id, 'awaiting_review', accessToken);
                        setDoc({ ...saved, status: 'awaiting_review' });
                        setStage('review');
                      })
                    }
                  >
                    {loadingAction === 'draft' ? 'Drafting article…' : 'Generate draft'}
                  </button>
                </div>

                {selectedProject ? (
                  <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
                    Project URL: {selectedProject.url}
                  </p>
                ) : null}
              </section>

              {brief ? (
                <section className="rounded-xl border bg-white p-5 shadow-sm">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <h2 className="font-semibold">Outline</h2>
                      <p className="text-sm text-[var(--color-text-secondary)]">
                        Edit the generated structure before drafting.
                      </p>
                    </div>
                  </div>
                  <textarea
                    className="mt-4 min-h-[220px] w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 font-mono text-sm"
                    value={outline}
                    onChange={(event) => setOutline(event.target.value)}
                    placeholder="<h2>Business Objectives</h2>"
                  />
                </section>
              ) : null}
            </>
          ) : (
            <ReviewEditorPane
              title={title}
              setTitle={setTitle}
              keyword={keyword}
              setKeyword={setKeyword}
              location={location}
              setLocation={setLocation}
              accessToken={accessToken}
            />
          )}
        </div>

        <aside className="col-span-2 min-w-0 space-y-4 self-start">
          {inReview ? (
            <div className="min-w-0 rounded-xl border bg-white shadow-sm">
              <ReviewScoreRight keyword={keyword} statusMessage={statusMessage} />
            </div>
          ) : null}
        </aside>
      </div>
    </div>
  );

  if (doc && inReview) {
    return (
      <ReviewWorkspaceProvider
        key={doc.id}
        doc={doc}
        accessToken={accessToken}
        title={title}
        setTitle={setTitle}
        keyword={keyword}
        setKeyword={setKeyword}
        location={location}
        setLocation={setLocation}
        onDocumentChange={setDoc}
        onError={setError}
        statusMessage={statusMessage}
        setStatusMessage={setStatusMessage}
      >
        {grid}
      </ReviewWorkspaceProvider>
    );
  }

  return grid;
}

export default function ContentWritingPage() {
  return (
    <Suspense
      fallback={
        <main className="mx-auto max-w-5xl p-8 text-[var(--color-text-secondary)]">Loading…</main>
      }
    >
      <ContentWritingPageInner />
    </Suspense>
  );
}
