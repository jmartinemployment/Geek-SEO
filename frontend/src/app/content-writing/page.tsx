'use client';

import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import {
  ReviewEditorPane,
  ReviewScoreLeft,
  ReviewScoreRight,
  ReviewWorkspaceProvider,
} from '@/components/content-writing/review-workspace-context';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  analysisRunBlockReason,
  attachAnalysisRun,
  createContent,
  generateFeaturedImage,
  getContent,
  listAnalysisRuns,
  listProjects,
  describeDraftJobProgress,
  updateContentStatus,
  type AnalysisRunSummary,
  type SeoContentDocument,
  type SeoProject,
} from '@/lib/seo-api';
import {
  CONTENT_WRITING_DEFAULT_LOCATION,
  contentWritingPath,
  defaultTitleForKeyword,
  parseContentWritingSearchParams,
  type ContentWritingSearchParams,
} from '@/lib/content-writing-search-params';
import { runResearchContentDraft } from '@/lib/draft-job-signalr';

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

function ContentWritingPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const hub = useSeoHub();
  const router = useRouter();
  const searchParams = useSearchParams();
  const urlParams = useMemo(
    () => parseContentWritingSearchParams(searchParams),
    [searchParams],
  );
  const skipUrlSyncRef = useRef(false);

  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(urlParams.projectId);
  const [title, setTitle] = useState(
    urlParams.title || defaultTitleForKeyword(urlParams.keyword, 'New article'),
  );
  const [keyword, setKeyword] = useState(urlParams.keyword);
  const [location, setLocation] = useState(urlParams.location);
  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [stage, setStage] = useState<Stage>('setup');
  const [selectedRunId, setSelectedRunId] = useState(urlParams.analysisRunId);
  const [analysisRuns, setAnalysisRuns] = useState<AnalysisRunSummary[]>([]);
  const [runsLoading, setRunsLoading] = useState(false);
  const [loadingAction, setLoadingAction] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [generateFeaturedImageWithDraft, setGenerateFeaturedImageWithDraft] = useState(false);
  const [draftProgress, setDraftProgress] = useState<{
    label: string;
    percent: number;
    elapsedMs: number;
  } | null>(null);
  const [documentLoading, setDocumentLoading] = useState(!!urlParams.documentId);

  const replaceUrlParams = useCallback(
    (patch: Partial<ContentWritingSearchParams>) => {
      skipUrlSyncRef.current = true;
      router.replace(
        contentWritingPath({
          ...urlParams,
          ...patch,
          documentId: '',
        }),
        { scroll: false },
      );
    },
    [router, urlParams],
  );

  useEffect(() => {
    if (skipUrlSyncRef.current) {
      skipUrlSyncRef.current = false;
      return;
    }
    if (urlParams.documentId) return;

    if (urlParams.projectId) setProjectId(urlParams.projectId);
    if (urlParams.analysisRunId) setSelectedRunId(urlParams.analysisRunId);
    if (urlParams.keyword) {
      setKeyword(urlParams.keyword);
      setTitle((current) =>
        urlParams.title
          ? urlParams.title
          : defaultTitleForKeyword(urlParams.keyword, current),
      );
    } else if (urlParams.title) {
      setTitle(urlParams.title);
    }
    if (urlParams.location) setLocation(urlParams.location);
  }, [urlParams]);

  const readyRuns = useMemo(
    () => analysisRuns.filter((run) => run.contentWritingReady),
    [analysisRuns],
  );

  const selectableRuns = useMemo(() => {
    if (!selectedRunId || readyRuns.some((run) => run.id === selectedRunId)) {
      return readyRuns;
    }
    const pinned = analysisRuns.find((run) => run.id === selectedRunId);
    return pinned ? [pinned, ...readyRuns] : readyRuns;
  }, [analysisRuns, readyRuns, selectedRunId]);

  const selectedRun = useMemo(
    () => analysisRuns.find((run) => run.id === selectedRunId) ?? null,
    [analysisRuns, selectedRunId],
  );

  const blockReason = useMemo(
    () => analysisRunBlockReason(analysisRuns, selectedRunId || urlParams.analysisRunId),
    [analysisRuns, selectedRunId, urlParams.analysisRunId],
  );
  const writingBlocked = Boolean(projectId && !runsLoading && blockReason);

  useEffect(() => {
    if (authLoading) return;
    let cancelled = false;

    async function loadProjects() {
      try {
        const list = await listProjects(accessToken);
        if (cancelled) return;
        setProjects(list);
        if (!urlParams.projectId && list[0]) {
          setProjectId(list[0].id);
          if (!searchParams.get('location')) {
            setLocation(list[0].defaultLocation || CONTENT_WRITING_DEFAULT_LOCATION);
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
  }, [accessToken, authLoading, searchParams, urlParams.projectId]);

  useEffect(() => {
    if (!urlParams.documentId || authLoading) return;
    let cancelled = false;

    async function loadDocument() {
      setDocumentLoading(true);
      try {
        const loaded = await getContent(urlParams.documentId, accessToken);
        if (cancelled) return;
        setDoc(loaded);
        setTitle(loaded.title);
        setKeyword(loaded.targetKeyword);
        setLocation(loaded.targetLocation || CONTENT_WRITING_DEFAULT_LOCATION);
        setProjectId(loaded.projectId);
        if (loaded.analysisRunId) {
          setSelectedRunId(loaded.analysisRunId);
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
  }, [urlParams.documentId, accessToken, authLoading]);

  useEffect(() => {
    if (!projectId || authLoading || urlParams.documentId) return;
    let cancelled = false;

    async function loadRuns() {
      setRunsLoading(true);
      try {
        const runs = await listAnalysisRuns(projectId, accessToken);
        if (cancelled) return;
        setAnalysisRuns(runs);

        const preferredId =
          urlParams.analysisRunId ||
          runs.find((run) => run.contentWritingReady)?.id ||
          '';

        if (urlParams.analysisRunId) {
          setSelectedRunId(urlParams.analysisRunId);
        } else {
          setSelectedRunId((current) => current || preferredId);
        }

        const activeRunId = urlParams.analysisRunId || preferredId;
        const picked = runs.find((run) => run.id === activeRunId);
        if (picked?.keyword && !urlParams.keyword && !keyword) {
          setKeyword(picked.keyword);
          setTitle((current) => defaultTitleForKeyword(picked.keyword, current));
        }
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      } finally {
        if (!cancelled) setRunsLoading(false);
      }
    }

    void loadRuns();
    return () => {
      cancelled = true;
    };
  }, [
    accessToken,
    authLoading,
    keyword,
    projectId,
    urlParams.analysisRunId,
    urlParams.documentId,
    urlParams.keyword,
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
    hub,
    onProgress: (status: Parameters<typeof describeDraftJobProgress>[0], elapsedMs: number) => {
      setDraftProgress({
        label: describeDraftJobProgress(status, 'research'),
        percent: status.progressPercent,
        elapsedMs,
      });
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
                Link an analysis run with SERP data, then generate a draft from frozen keyword research.
              </p>
            </div>
            {selectedProject ? (
              <Link
                href={contentWritingPath({ projectId: selectedProject.id })}
                className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
              >
                Project documents
              </Link>
            ) : null}
          </div>

          {error ? <SeoErrorBanner error={error} /> : null}

          {statusMessage ? (
            <p className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-950">
              {statusMessage}
            </p>
          ) : null}

          {writingBlocked && !inReview ? (
            <section className="rounded-xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm text-amber-950">
              <p className="font-semibold">SERP research not ready</p>
              <p className="mt-2 text-amber-900">{blockReason}</p>
              <p className="mt-2 text-xs text-amber-800">
                Content Writing uses organic SERP results from an analysis run. Site crawl and business
                context are not included in this export yet.
              </p>
            </section>
          ) : null}

          {!inReview && !writingBlocked ? (
            <section className="rounded-xl border bg-white p-5 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="font-semibold">Article setup</h2>
                  <p className="text-sm text-[var(--color-text-secondary)]">
                    Pick SERP research from an analysis run, then specify the keyword this article
                    should target (they can differ).
                  </p>
                </div>
                <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
                  Analysis run → draft
                </span>
              </div>

              <div className="mt-4 grid gap-4 md:grid-cols-2">
                <label className="text-sm font-medium text-[var(--color-text-primary)]">
                  Project
                  <select
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={projectId}
                    onChange={(event) => {
                      const nextProjectId = event.target.value;
                      setProjectId(nextProjectId);
                      setSelectedRunId('');
                      const nextProject = projects.find((project) => project.id === nextProjectId);
                      const nextLocation =
                        nextProject && !searchParams.get('location')
                          ? nextProject.defaultLocation || CONTENT_WRITING_DEFAULT_LOCATION
                          : location;
                      if (nextProject && !searchParams.get('location')) {
                        setLocation(nextLocation);
                      }
                      replaceUrlParams({
                        projectId: nextProjectId,
                        analysisRunId: '',
                        location: nextLocation,
                      });
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

                <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                  Working title
                  <input
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={title}
                    onChange={(event) => {
                      const nextTitle = event.target.value;
                      setTitle(nextTitle);
                      replaceUrlParams({ title: nextTitle });
                    }}
                  />
                </label>

                <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                  SERP research (analysis run)
                  <select
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={selectedRunId}
                    onChange={(event) => {
                      const nextId = event.target.value;
                      setSelectedRunId(nextId);
                      const run = readyRuns.find((item) => item.id === nextId);
                      const nextKeyword =
                        run?.keyword && !keyword.trim() ? run.keyword : keyword;
                      if (run?.keyword && !keyword.trim()) {
                        setKeyword(run.keyword);
                        setTitle((current) => defaultTitleForKeyword(run.keyword, current));
                      }
                      replaceUrlParams({
                        analysisRunId: nextId,
                        keyword: nextKeyword,
                        title: defaultTitleForKeyword(nextKeyword, title),
                      });
                    }}
                    disabled={runsLoading || selectableRuns.length === 0}
                  >
                    <option value="">
                      {runsLoading ? 'Loading analysis runs…' : 'Select an analysis run with SERP data'}
                    </option>
                    {selectableRuns.map((run) => (
                      <option key={run.id} value={run.id}>
                        SERP: {run.keyword} — {formatResearchWhen(run.createdAt)}
                        {run.status ? ` (${run.status})` : ''}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                  Article keyword
                  <input
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={keyword}
                    onChange={(event) => {
                      const nextKeyword = event.target.value;
                      setKeyword(nextKeyword);
                      replaceUrlParams({
                        keyword: nextKeyword,
                      });
                    }}
                    placeholder="Keyword to write and score this article for"
                  />
                  {selectedRun?.keyword &&
                  keyword.trim() &&
                  keyword.trim().toLowerCase() !== selectedRun.keyword.trim().toLowerCase() ? (
                    <span className="mt-1 block text-xs text-[var(--color-text-muted)]">
                      SERP data is from “{selectedRun.keyword}”; this article targets “{keyword.trim()}”.
                    </span>
                  ) : selectedRun?.keyword ? (
                    <span className="mt-1 block text-xs text-[var(--color-text-muted)]">
                      Using SERP research for “{selectedRun.keyword}”.
                    </span>
                  ) : null}
                </label>
              </div>

              <label className="mt-4 flex items-center gap-2 text-sm text-[var(--color-text-primary)]">
                <input
                  type="checkbox"
                  checked={generateFeaturedImageWithDraft}
                  onChange={(event) => setGenerateFeaturedImageWithDraft(event.target.checked)}
                />
                Generate featured image with draft (OpenAI)
              </label>

              <div className="mt-5 flex flex-wrap gap-3">
                <button
                  type="button"
                  disabled={!projectId || !selectedRunId || !keyword.trim() || !!loadingAction}
                  className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                  onClick={() =>
                    void run('research-draft', async () => {
                      if (!selectedRunId) {
                        throw new Error('Select an analysis run with SERP data first.');
                      }
                      const articleKeyword = keyword.trim() || selectedRun?.keyword || '';
                      if (!articleKeyword) {
                        throw new Error('Enter the article keyword you want to write for.');
                      }
                      let workingDoc = doc;
                      if (!workingDoc) {
                        workingDoc = await createContent(
                          {
                            projectId,
                            title,
                            targetKeyword: articleKeyword,
                            targetLocation: location,
                            analysisRunId: selectedRunId,
                            siteProfileId: urlParams.siteProfile || undefined,
                          },
                          accessToken,
                        );
                        setDoc(workingDoc);
                      } else if (!workingDoc.analysisRunId) {
                        workingDoc = await attachAnalysisRun(
                          workingDoc.id,
                          {
                            analysisRunId: selectedRunId,
                            targetKeyword: articleKeyword,
                            siteProfileId: urlParams.siteProfile || undefined,
                          },
                          accessToken,
                        );
                        setDoc(workingDoc);
                      } else if (workingDoc.analysisRunId !== selectedRunId) {
                        throw new Error('This document is already linked to a different analysis run.');
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
              </div>

              {selectedProject ? (
                <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
                  Project URL: {selectedProject.url}
                </p>
              ) : null}
            </section>
          ) : inReview ? (
            <ReviewEditorPane
              title={title}
              setTitle={setTitle}
              keyword={keyword}
              setKeyword={setKeyword}
              location={location}
              setLocation={setLocation}
              accessToken={accessToken}
            />
          ) : null}
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
