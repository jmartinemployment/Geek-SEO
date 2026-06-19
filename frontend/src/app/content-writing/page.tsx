'use client';

import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useMemo, useState } from 'react';
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
  attachUrlResearch,
  createContent,
  generateFeaturedImage,
  getContent,
  getSiteAnalyzerProjectState,
  listProjects,
  describeDraftJobProgress,
  siteAnalyzerBlockReason,
  updateContentStatus,
  type SeoContentDocument,
  type SeoProject,
  type SiteAnalyzerPackSummary,
  type SiteAnalyzerProjectState,
} from '@/lib/seo-api';
import { runResearchContentDraft } from '@/lib/draft-job-signalr';

const DEFAULT_LOCATION = 'United States';

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

function isCompletePack(pack: SiteAnalyzerPackSummary): boolean {
  return pack.handoffReady || pack.dataQuality === 'full';
}

type Stage = 'setup' | 'review';

function ContentWritingPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const hub = useSeoHub();
  const searchParams = useSearchParams();
  const initialTitle = searchParams.get('title') ?? '';
  const initialProjectId = searchParams.get('projectId') ?? '';
  const initialLocation = searchParams.get('location') ?? DEFAULT_LOCATION;
  const initialDocumentId = searchParams.get('documentId') ?? '';
  const initialUrlResearchId = searchParams.get('urlResearchId') ?? '';

  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId);
  const [title, setTitle] = useState(initialTitle || 'New article');
  const [keyword, setKeyword] = useState('');
  const [location, setLocation] = useState(initialLocation);
  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [stage, setStage] = useState<Stage>('setup');
  const [selectedResearchId, setSelectedResearchId] = useState(initialUrlResearchId);
  const [analyzerState, setAnalyzerState] = useState<SiteAnalyzerProjectState | null>(null);
  const [analyzerLoading, setAnalyzerLoading] = useState(false);
  const [loadingAction, setLoadingAction] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [generateFeaturedImageWithDraft, setGenerateFeaturedImageWithDraft] = useState(false);
  const [draftProgress, setDraftProgress] = useState<{
    label: string;
    percent: number;
    elapsedMs: number;
  } | null>(null);
  const [documentLoading, setDocumentLoading] = useState(!!initialDocumentId);

  const completePacks = useMemo(
    () => (analyzerState?.packs ?? []).filter(isCompletePack),
    [analyzerState?.packs],
  );

  const selectedPack = useMemo(
    () => completePacks.find((pack) => pack.urlResearchId === selectedResearchId) ?? null,
    [completePacks, selectedResearchId],
  );

  const blockReason = useMemo(() => siteAnalyzerBlockReason(analyzerState), [analyzerState]);
  const writingBlocked = Boolean(projectId && !analyzerLoading && blockReason);

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
    if (!projectId || authLoading) return;
    let cancelled = false;

    async function loadAnalyzerState() {
      setAnalyzerLoading(true);
      try {
        const state = await getSiteAnalyzerProjectState(projectId, accessToken);
        if (cancelled) return;
        setAnalyzerState(state);
        const preferredId =
          selectedResearchId ||
          initialUrlResearchId ||
          state.packs.find(isCompletePack)?.urlResearchId ||
          '';
        if (!selectedResearchId && preferredId) {
          setSelectedResearchId(preferredId);
        }
        const picked = state.packs.find((pack) => pack.urlResearchId === (selectedResearchId || preferredId));
        if (picked?.keyword && !initialTitle) {
          setKeyword(picked.keyword);
          setTitle(picked.keyword);
        }
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      } finally {
        if (!cancelled) setAnalyzerLoading(false);
      }
    }

    void loadAnalyzerState();
    return () => {
      cancelled = true;
    };
  }, [accessToken, authLoading, initialTitle, initialUrlResearchId, projectId, selectedResearchId]);

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === projectId) ?? null,
    [projectId, projects],
  );

  const inReview = stage === 'review' && !!doc;
  const siteAnalyzerHref = projectId
    ? `/projects/${projectId}/site-analyzer`
    : '/site-analyzer';

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
        label: describeDraftJobProgress(status),
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
                Attach a complete Site Analyzer keyword pack, then generate a draft from frozen research.
              </p>
            </div>
            {selectedProject ? (
              <div className="flex flex-col items-end gap-1 text-sm">
                <Link
                  href={siteAnalyzerHref}
                  className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                >
                  Site Analyzer
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

          {writingBlocked && !inReview ? (
            <section className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-950">
              <p className="font-semibold">Site must be crawled first</p>
              <p className="mt-2 text-red-900">{blockReason}</p>
              <Link
                href={siteAnalyzerHref}
                className="mt-3 inline-block rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)]"
              >
                Open Site Analyzer
              </Link>
            </section>
          ) : null}

          {!inReview && !writingBlocked ? (
            <section className="rounded-xl border bg-white p-5 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="font-semibold">Article setup</h2>
                  <p className="text-sm text-[var(--color-text-secondary)]">
                    Select a completed keyword pack from Site Analyzer — all 10 steps must be green.
                  </p>
                </div>
                <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
                  Site Analyzer pack → draft
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

                <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                  Working title
                  <input
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={title}
                    onChange={(event) => setTitle(event.target.value)}
                  />
                </label>

                <label className="text-sm font-medium text-[var(--color-text-primary)] md:col-span-2">
                  Keyword pack
                  <select
                    className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                    value={selectedResearchId}
                    onChange={(event) => {
                      const nextId = event.target.value;
                      setSelectedResearchId(nextId);
                      const pack = completePacks.find((item) => item.urlResearchId === nextId);
                      if (pack?.keyword) {
                        setKeyword(pack.keyword);
                        if (!initialTitle) setTitle(pack.keyword);
                      }
                    }}
                    disabled={analyzerLoading || completePacks.length === 0}
                  >
                    <option value="">
                      {analyzerLoading ? 'Loading packs…' : 'Select a complete pack'}
                    </option>
                    {completePacks.map((pack) => (
                      <option key={pack.urlResearchId} value={pack.urlResearchId}>
                        {pack.keyword} — {formatResearchWhen(pack.researchedAt ?? pack.createdAt)}
                      </option>
                    ))}
                  </select>
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
                  disabled={!projectId || !selectedResearchId || !!loadingAction}
                  className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                  onClick={() =>
                    void run('research-draft', async () => {
                      if (!selectedResearchId) {
                        throw new Error('Select a complete keyword pack from Site Analyzer first.');
                      }
                      let workingDoc = doc;
                      if (!workingDoc) {
                        workingDoc = await createContent(
                          {
                            projectId,
                            title,
                            targetKeyword: keyword || selectedPack?.keyword || '',
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
                        throw new Error('This document is already linked to different research.');
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
                    href={siteAnalyzerHref}
                    className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)]"
                  >
                    Complete Site Analyzer first
                  </Link>
                ) : null}
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
