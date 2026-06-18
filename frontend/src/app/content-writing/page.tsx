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
  attachUrlResearch,
  createContent,
  generateFeaturedImage,
  getContent,
  listProjects,
  listUrlResearch,
  runKeywordContentDraft,
  runResearchContentDraft,
  describeDraftJobProgress,
  updateContentStatus,
  type SeoContentDocument,
  type SeoProject,
  type UrlResearchSummary,
} from '@/lib/seo-api';

const DEFAULT_LOCATION = 'United States';
const DEFAULT_DRAFT_HTML = '<h1>Article title</h1><p>Start writing your article.</p>';

function normalizePageUrl(url: string): string {
  return url.trim().toLowerCase().replace(/\/$/, '');
}

function researchTimestamp(row: UrlResearchSummary): number {
  const raw = row.researchedAt ?? row.createdAt;
  const t = Date.parse(raw);
  return Number.isFinite(t) ? t : 0;
}

function formatResearchWhen(iso?: string | null): string {
  if (!iso) return 'recently';
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

function formatDraftElapsed(elapsedMs: number): string {
  const totalSeconds = Math.floor(elapsedMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
}

function draftLoadingLabel(
  action: string | null,
  progress: { label: string; percent: number; elapsedMs: number } | null,
  fallback: string,
): string {
  if (!action || !progress) return fallback;
  const pct = progress.percent > 0 ? ` · ${progress.percent}%` : '';
  return `${progress.label} (${formatDraftElapsed(progress.elapsedMs)}${pct})`;
}

type Stage = 'setup' | 'review';
type WritingPath = 'research' | 'keyword';

function ContentWritingPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const searchParams = useSearchParams();
  const initialKeyword = searchParams.get('keyword') ?? '';
  const initialTitle = searchParams.get('title') ?? '';
  const initialProjectId = searchParams.get('projectId') ?? '';
  const initialLocation = searchParams.get('location') ?? DEFAULT_LOCATION;
  const initialDocumentId = searchParams.get('documentId') ?? '';
  const initialUrlResearchId = searchParams.get('urlResearchId') ?? '';

  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId);
  const [title, setTitle] = useState(initialTitle || initialKeyword || 'New article');
  const [keyword, setKeyword] = useState(initialKeyword);
  const [location, setLocation] = useState(initialLocation);
  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [stage, setStage] = useState<Stage>('setup');
  const [writingPath, setWritingPath] = useState<WritingPath>(
    initialUrlResearchId ? 'research' : 'keyword',
  );
  const [selectedResearchId, setSelectedResearchId] = useState(initialUrlResearchId);
  const [researchRows, setResearchRows] = useState<UrlResearchSummary[]>([]);
  const [loadingAction, setLoadingAction] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [generateFeaturedImageWithDraft, setGenerateFeaturedImageWithDraft] = useState(true);
  const [draftProgress, setDraftProgress] = useState<{
    label: string;
    percent: number;
    elapsedMs: number;
  } | null>(null);
  const [documentLoading, setDocumentLoading] = useState(!!initialDocumentId);

  const isResearchDoc = Boolean(doc?.urlResearchId);
  const useResearchPath = isResearchDoc || writingPath === 'research';

  const completedResearch = useMemo(
    () => researchRows.filter((row) => row.status === 'completed'),
    [researchRows],
  );

  const selectedResearch = useMemo(
    () => completedResearch.find((row) => row.id === selectedResearchId) ?? null,
    [completedResearch, selectedResearchId],
  );

  const newerResearchForAttached = useMemo(() => {
    if (!doc?.urlResearchId || researchRows.length === 0) return null;

    const attached = researchRows.find((row) => row.id === doc.urlResearchId);
    if (!attached || attached.status !== 'completed') return null;

    const source = normalizePageUrl(attached.sourceUrl);
    const attachedAt = researchTimestamp(attached);

    return (
      researchRows
        .filter(
          (row) =>
            row.status === 'completed' &&
            row.id !== attached.id &&
            normalizePageUrl(row.sourceUrl) === source &&
            researchTimestamp(row) > attachedAt,
        )
        .sort((a, b) => researchTimestamp(b) - researchTimestamp(a))[0] ?? null
    );
  }, [doc?.urlResearchId, researchRows]);

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
        if (loaded.urlResearchId) {
          setWritingPath('research');
          setSelectedResearchId(loaded.urlResearchId);
        }
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

  useEffect(() => {
    if (!projectId || !useResearchPath || authLoading) return;
    let cancelled = false;

    async function loadResearch() {
      try {
        const rows = await listUrlResearch(projectId, accessToken);
        if (cancelled) return;
        setResearchRows(rows);
        const preferredId =
          selectedResearchId ||
          initialUrlResearchId ||
          rows.find((row) => row.status === 'completed')?.id ||
          '';
        if (!selectedResearchId && preferredId) {
          setSelectedResearchId(preferredId);
        }
        const picked = rows.find((row) => row.id === (selectedResearchId || preferredId));
        if (picked?.derivedKeyword && !initialKeyword) {
          setKeyword(picked.derivedKeyword);
          if (!initialTitle) setTitle(picked.derivedKeyword);
        }
        if (rows.some((row) => row.status === 'completed') && !initialUrlResearchId && !isResearchDoc) {
          setWritingPath('research');
        }
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      }
    }

    void loadResearch();
    return () => {
      cancelled = true;
    };
  }, [
    accessToken,
    authLoading,
    initialKeyword,
    initialTitle,
    initialUrlResearchId,
    isResearchDoc,
    projectId,
    selectedResearchId,
    useResearchPath,
  ]);

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === projectId) ?? null,
    [projectId, projects],
  );

  const inReview = stage === 'review' && !!doc;

  async function run(action: string, fn: () => Promise<void>) {
    setLoadingAction(action);
    setError(null);
    setStatusMessage(null);
    setDraftProgress(null);
    try {
      await fn();
    } catch (runError) {
      setError(runError);
    } finally {
      setLoadingAction(null);
      setDraftProgress(null);
    }
  }

  const draftJobOptions = {
    onProgress: (status: Parameters<typeof describeDraftJobProgress>[0], elapsedMs: number) => {
      setDraftProgress({
        label: describeDraftJobProgress(status),
        percent: status.progressPercent,
        elapsedMs,
      });
    },
    onFallback: (reason: string) => {
      setStatusMessage(
        `${reason} Continuing with a direct draft — this may take a few minutes.`,
      );
    },
  };

  async function finalizeDraftDocument(saved: SeoContentDocument): Promise<SeoContentDocument> {
    await updateContentStatus(saved.id, 'awaiting_review', accessToken);
    let nextDoc = { ...saved, status: 'awaiting_review' };
    setDoc(nextDoc);
    setStage('review');

    if (generateFeaturedImageWithDraft) {
      try {
        const image = await generateFeaturedImage(saved.id, {}, accessToken);
        nextDoc = { ...nextDoc, featuredImageUrl: image.dataUrl };
        setDoc(nextDoc);
      } catch (imageError) {
        setStatusMessage(
          'Draft is ready in the editor. Featured image could not be generated — you can retry from the review panel.',
        );
        setError(imageError);
      }
    }
    return nextDoc;
  }

  async function reattachNewerResearch(newerId: string) {
    if (!doc) return;
    await run('reattach-research', async () => {
      const updated = await attachUrlResearch(doc.id, newerId, accessToken);
      setDoc(updated);
      setSelectedResearchId(newerId);
      setStatusMessage(
        'Attached newer page research. Regenerate the draft if you want content aligned to the latest SERP pack.',
      );
    });
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
                {useResearchPath
                  ? 'Attach saved page research, then generate a draft in one step.'
                  : 'Generate a draft from a target keyword — SERP research runs in the background.'}
              </p>
            </div>
            {selectedProject ? (
              <div className="flex flex-col items-end gap-1 text-sm">
                <Link
                  href={`/projects/${selectedProject.id}/url-analyzer`}
                  className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                >
                  URL Analyzer
                </Link>
                <Link
                  href={`/content-writing?projectId=${selectedProject.id}`}
                  className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                >
                  Project documents
                </Link>
              </div>
            ) : null}
          </div>

          {error ? <SeoErrorBanner error={error} /> : null}

          {statusMessage ? (
            <p className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-950">
              {statusMessage}
            </p>
          ) : null}

          {newerResearchForAttached ? (
            <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-950">
              <p className="font-medium">Newer research available</p>
              <p className="mt-1 text-xs">
                A more recent analysis exists for{' '}
                <span className="font-mono">{newerResearchForAttached.sourceUrl}</span> (
                {formatResearchWhen(newerResearchForAttached.researchedAt)}).
              </p>
              <div className="mt-2 flex flex-wrap gap-3">
                {doc ? (
                  <button
                    type="button"
                    disabled={!!loadingAction}
                    className="text-xs font-medium underline disabled:opacity-50"
                    onClick={() => void reattachNewerResearch(newerResearchForAttached.id)}
                  >
                    {loadingAction === 'reattach-research'
                      ? 'Attaching…'
                      : 'Attach newer research'}
                  </button>
                ) : (
                  <button
                    type="button"
                    className="text-xs font-medium underline"
                    onClick={() => setSelectedResearchId(newerResearchForAttached.id)}
                  >
                    Use newer research
                  </button>
                )}
                <Link
                  href={
                    projectId
                      ? `/projects/${projectId}/url-analyzer`
                      : '/url-analyzer'
                  }
                  className="text-xs font-medium underline"
                >
                  Open URL Analyzer
                </Link>
              </div>
            </div>
          ) : null}

          {!inReview ? (
            <section className="rounded-xl border bg-white p-5 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="font-semibold">Article setup</h2>
                  <p className="text-sm text-[var(--color-text-secondary)]">
                    {useResearchPath
                      ? 'Recommended — uses frozen SERP data from URL Analyzer (no live SERP at draft time).'
                      : 'Fallback when you have not analyzed a page URL yet. Uses live SERP at draft time.'}
                  </p>
                </div>
                <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
                  {useResearchPath ? 'Page research → draft' : 'Keyword → draft'}
                </span>
              </div>

              {!isResearchDoc ? (
                <div className="mt-4 flex gap-2">
                  <button
                    type="button"
                    className={`rounded-lg px-3 py-1.5 text-sm ${
                      writingPath === 'research'
                        ? 'bg-[var(--color-accent)] text-white'
                        : 'border hover:bg-[var(--color-surface-muted)]'
                    }`}
                    onClick={() => setWritingPath('research')}
                  >
                    Page research
                  </button>
                  <button
                    type="button"
                    className={`rounded-lg px-3 py-1.5 text-sm ${
                      writingPath === 'keyword'
                        ? 'bg-[var(--color-accent)] text-white'
                        : 'border hover:bg-[var(--color-surface-muted)]'
                    }`}
                    onClick={() => setWritingPath('keyword')}
                  >
                    Keyword only
                  </button>
                </div>
              ) : null}

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

                {!useResearchPath ? (
                  <label className="text-sm font-medium text-[var(--color-text-primary)]">
                    Location
                    <input
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={location}
                      onChange={(event) => setLocation(event.target.value)}
                    />
                  </label>
                ) : null}

                <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                  Working title
                  <input
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={title}
                    onChange={(event) => setTitle(event.target.value)}
                  />
                </label>

                {useResearchPath ? (
                  <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                    Page research
                    <select
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={selectedResearchId}
                      onChange={(event) => {
                        const nextId = event.target.value;
                        setSelectedResearchId(nextId);
                        const row = completedResearch.find((item) => item.id === nextId);
                        if (row?.derivedKeyword) {
                          setKeyword(row.derivedKeyword);
                          if (!initialTitle && !initialKeyword) {
                            setTitle(row.derivedKeyword);
                          }
                        }
                      }}
                      disabled={isResearchDoc}
                    >
                      <option value="">Select completed research</option>
                      {completedResearch.map((row) => (
                        <option key={row.id} value={row.id}>
                          {row.derivedKeyword} — {row.sourceUrl}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : (
                  <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                    Target keyword
                    <input
                      className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                      value={keyword}
                      onChange={(event) => setKeyword(event.target.value)}
                      placeholder="e.g. zapier quickbooks integration"
                    />
                  </label>
                )}
              </div>

              <label className="mt-4 flex items-center gap-2 text-sm text-[var(--color-text-primary)]">
                <input
                  type="checkbox"
                  checked={generateFeaturedImageWithDraft}
                  onChange={(event) => setGenerateFeaturedImageWithDraft(event.target.checked)}
                />
                Generate featured image with draft (OpenAI)
              </label>

              {useResearchPath ? (
                <div className="mt-5 flex flex-wrap gap-3">
                  <button
                    type="button"
                    disabled={!projectId || !selectedResearchId || !!loadingAction}
                    className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                    onClick={() =>
                      void run('research-draft', async () => {
                        if (!selectedResearchId) {
                          throw new Error('Attach page research from URL Analyzer first.');
                        }
                        let workingDoc = doc;
                        if (!workingDoc) {
                          workingDoc = await createContent(
                            {
                              projectId,
                              title,
                              targetKeyword: keyword || selectedResearch?.derivedKeyword || '',
                              targetLocation: location,
                              urlResearchId: selectedResearchId,
                            },
                            accessToken,
                          );
                          setDoc(workingDoc);
                        } else if (!workingDoc.urlResearchId) {
                          workingDoc = await attachUrlResearch(
                            workingDoc.id,
                            selectedResearchId,
                            accessToken,
                          );
                          setDoc(workingDoc);
                        } else if (workingDoc.urlResearchId !== selectedResearchId) {
                          throw new Error('This document is already linked to different page research.');
                        }

                        const saved = await runResearchContentDraft(
                          workingDoc.id,
                          accessToken,
                          draftJobOptions,
                        );
                        setDoc(saved);
                        await finalizeDraftDocument(saved);
                      })
                    }
                  >
                    {loadingAction === 'research-draft'
                      ? draftLoadingLabel(loadingAction, draftProgress, 'Drafting from research…')
                      : 'Generate draft'}
                  </button>
                  {!selectedResearchId ? (
                    <Link
                      href={projectId ? `/projects/${projectId}/url-analyzer` : '/url-analyzer'}
                      className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)]"
                    >
                      Analyze a page first
                    </Link>
                  ) : null}
                </div>
              ) : (
                <div className="mt-5 flex flex-wrap gap-3">
                  <button
                    type="button"
                    disabled={!projectId || !keyword.trim() || !!loadingAction}
                    className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                    onClick={() =>
                      void run('keyword-draft', async () => {
                        const placeholder = await createContent(
                          {
                            projectId,
                            title,
                            targetKeyword: keyword,
                            targetLocation: location,
                          },
                          accessToken,
                        );
                        setDoc(placeholder);

                        const saved = await runKeywordContentDraft(
                          placeholder.id,
                          projectId,
                          { keyword, location, title },
                          accessToken,
                          draftJobOptions,
                        );
                        setDoc(saved);
                        await finalizeDraftDocument(saved);
                      })
                    }
                  >
                    {loadingAction === 'keyword-draft'
                      ? draftLoadingLabel(
                          loadingAction,
                          draftProgress,
                          'Researching SERP and drafting…',
                        )
                      : 'Generate draft'}
                  </button>
                </div>
              )}

              {selectedProject ? (
                <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
                  Project URL: {selectedProject.url}
                </p>
              ) : null}

              {useResearchPath && selectedResearch?.dataQuality === 'partial' ? (
                <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-900">
                  This research is partial — you can still draft, but some SERP signals may be missing.
                </p>
              ) : null}

              {!useResearchPath ? (
                <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
                  For better benchmarks and frozen scoring, analyze the target page in URL Analyzer first.
                </p>
              ) : null}
            </section>
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
