'use client';

import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import {
  WritingEditorPane,
  WritingInsightsRight,
  WritingScoreLeft,
  WritingWorkspaceProvider,
} from '@/components/content-writing/review-workspace-context';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  createContent,
  describeDraftJobProgress,
  getContent,
  getProject,
  type SeoContentDocument,
} from '@/lib/seo-api';
import {
  CONTENT_WRITING_DEFAULT_LOCATION,
  contentWritingPath,
  defaultTitleForKeyword,
  isCompleteContentWritingHandoff,
  missingContentWritingHandoffFields,
  parseContentWritingSearchParams,
} from '@/lib/content-writing-search-params';
import { runResearchContentDraft } from '@/lib/draft-job-signalr';

function formatDraftElapsed(elapsedMs: number): string {
  const totalSeconds = Math.floor(elapsedMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
}

function draftLoadingLabel(
  progress: { label: string; percent: number; elapsedMs: number } | null,
  fallback: string,
): string {
  if (!progress) return fallback;
  const pct = progress.percent > 0 ? ` · ${progress.percent}%` : '';
  return `${progress.label} (${formatDraftElapsed(progress.elapsedMs)}${pct})`;
}

function ContentWritingGate({
  missingFields,
}: {
  missingFields: string[];
}) {
  return (
    <section className="rounded-xl border bg-white p-6 shadow-sm">
      <h2 className="text-lg font-semibold">Open from Site Analyzer</h2>
      <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
        Content Writing does not run keyword research here. Site Analyzer prepares your
        project, analysis run, keyword, and site profile, then sends you here to write.
      </p>
      {missingFields.length > 0 ? (
        <p className="mt-3 text-sm text-amber-900">
          This link is missing: {missingFields.join(', ')}. Use the Write article action in
          Site Analyzer so the full handoff URL is included.
        </p>
      ) : null}
      <p className="mt-4">
        <Link
          href="/site-analyzer"
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)]"
        >
          Go to Site Analyzer
        </Link>
      </p>
    </section>
  );
}

function ContentWritingPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const hub = useSeoHub();
  const router = useRouter();
  const searchParams = useSearchParams();
  const urlParams = useMemo(
    () => parseContentWritingSearchParams(searchParams),
    [searchParams],
  );

  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [title, setTitle] = useState(
    urlParams.title || defaultTitleForKeyword(urlParams.keyword, 'New article'),
  );
  const [keyword, setKeyword] = useState(urlParams.keyword);
  const [location, setLocation] = useState(urlParams.location);
  const [error, setError] = useState<unknown>(null);
  const [handoffRunning, setHandoffRunning] = useState(false);
  const [draftProgress, setDraftProgress] = useState<{
    label: string;
    percent: number;
    elapsedMs: number;
  } | null>(null);
  const [documentLoading, setDocumentLoading] = useState(!!urlParams.documentId);

  const handoffStartedRef = useRef<string | null>(null);

  const completeHandoff = isCompleteContentWritingHandoff(urlParams);
  const missingHandoff = useMemo(
    () => missingContentWritingHandoffFields(urlParams),
    [urlParams],
  );

  const draftJobOptions = useMemo(
    () => ({
      hub,
      onProgress: (
        status: Parameters<typeof describeDraftJobProgress>[0],
        elapsedMs: number,
      ) => {
        setDraftProgress({
          label: describeDraftJobProgress(status, 'research'),
          percent: status.progressPercent,
          elapsedMs,
        });
      },
    }),
    [hub],
  );

  const runHandoff = useCallback(async () => {
    if (!completeHandoff) return;

    const handoffKey = [
      urlParams.projectId,
      urlParams.analysisRunId,
      urlParams.keyword,
      urlParams.siteProfile,
    ].join('|');

    if (handoffStartedRef.current === handoffKey) return;
    handoffStartedRef.current = handoffKey;

    setHandoffRunning(true);
    setError(null);
    setDraftProgress(null);

    try {
      let targetLocation = urlParams.location;
      if (
        urlParams.projectId &&
        (!searchParams.get('location') ||
          targetLocation === CONTENT_WRITING_DEFAULT_LOCATION)
      ) {
        try {
          const project = await getProject(urlParams.projectId, accessToken);
          targetLocation = project.defaultLocation || targetLocation;
        } catch {
          // Keep URL/default location when project lookup fails.
        }
      }

      const articleTitle =
        urlParams.title ||
        defaultTitleForKeyword(urlParams.keyword, 'New article');
      const articleKeyword = urlParams.keyword.trim();

      const created = await createContent(
        {
          projectId: urlParams.projectId,
          title: articleTitle,
          targetKeyword: articleKeyword,
          targetLocation,
          analysisRunId: urlParams.analysisRunId,
          siteProfileId: urlParams.siteProfile,
        },
        accessToken,
      );

      const saved = await runResearchContentDraft(
        created.id,
        accessToken,
        draftJobOptions,
      );

      setDoc(saved);
      setTitle(saved.title);
      setKeyword(saved.targetKeyword);
      setLocation(saved.targetLocation || targetLocation);
      router.replace(contentWritingPath({ documentId: saved.id }), { scroll: false });
    } catch (handoffError) {
      handoffStartedRef.current = null;
      setError(handoffError);
    } finally {
      setHandoffRunning(false);
      setDraftProgress(null);
    }
  }, [
    accessToken,
    completeHandoff,
    draftJobOptions,
    router,
    searchParams,
    urlParams,
  ]);

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
    if (authLoading || urlParams.documentId || !completeHandoff || doc) return;
    void runHandoff();
  }, [authLoading, completeHandoff, doc, runHandoff, urlParams.documentId]);

  if (authLoading || documentLoading) {
    return (
      <main className="mx-auto max-w-5xl p-8 text-[var(--color-text-secondary)]">
        Loading…
      </main>
    );
  }

  const inWriting = Boolean(doc);
  const showGate = !inWriting && !handoffRunning;

  const workspace = inWriting ? (
    <div className="grid min-h-[calc(100vh-8rem)] grid-cols-12 gap-4">
      <aside className="col-span-12 hidden min-w-0 xl:col-span-2 xl:block">
        <WritingScoreLeft keyword={keyword} />
      </aside>
      <main className="col-span-12 min-w-0 space-y-4 xl:col-span-7">
        <WritingEditorPane
          title={title}
          setTitle={setTitle}
          keyword={keyword}
          setKeyword={setKeyword}
          location={location}
          setLocation={setLocation}
          accessToken={accessToken}
        />
      </main>
      <aside className="col-span-12 min-w-0 xl:col-span-3">
        <WritingInsightsRight keyword={keyword} />
      </aside>
    </div>
  ) : null;

  const main = (
    <div className="mx-auto w-full max-w-[1600px] space-y-6 px-4 py-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Content Writing</h1>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          {inWriting
            ? 'Surfer-style editor: live content score, term guidelines, suggestions, and JSON-LD.'
            : handoffRunning
              ? 'Loading frozen research from Site Analyzer and generating your draft…'
              : 'SEO article writing workspace — open from Site Analyzer with research attached.'}
        </p>
      </div>

      {error ? <SeoErrorBanner error={error} /> : null}

      {handoffRunning ? (
        <section className="rounded-xl border bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-[var(--color-text-primary)]">
            {draftLoadingLabel(draftProgress, 'Starting draft…')}
          </p>
          <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
            Keyword: {urlParams.keyword}
          </p>
        </section>
      ) : null}

      {showGate ? <ContentWritingGate missingFields={missingHandoff} /> : null}

      {workspace}
    </div>
  );

  if (doc && inWriting) {
    return (
      <WritingWorkspaceProvider
        key={doc.id}
        doc={doc}
        accessToken={accessToken}
        onDocumentChange={setDoc}
        onError={setError}
      >
        {main}
      </WritingWorkspaceProvider>
    );
  }

  return main;
}

export default function ContentWritingPage() {
  return (
    <Suspense
      fallback={
        <main className="mx-auto max-w-5xl p-8 text-[var(--color-text-secondary)]">
          Loading…
        </main>
      }
    >
      <ContentWritingPageInner />
    </Suspense>
  );
}
