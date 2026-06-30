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
import { BlogPostPanel } from '@/components/content-writing/blog-post-panel';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  createContent,
  describeDraftJobProgress,
  getContent,
  type SeoContentDocument,
} from '@/lib/seo-api';
import {
  CONTENT_WRITING_DEFAULT_LOCATION,
  contentWritingPath,
  defaultTitleForKeyword,
  isCompleteContentWritingHandoff,
  missingContentWritingHandoffFields,
  parseContentWritingSearchParams,
  rejectedLegacyHandoffParams,
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
  legacyFields,
}: {
  missingFields: string[];
  legacyFields: string[];
}) {
  return (
    <section className="rounded-xl border bg-white p-6 shadow-sm">
      <h2 className="text-lg font-semibold">Open from Site Analyzer</h2>
      <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
        Content Writing does not run keyword research here. Site Analyzer sends
        analysisRunId only — keyword and site context are resolved from sa2.
      </p>
      {legacyFields.length > 0 ? (
        <p className="mt-3 text-sm text-red-800">
          This link uses removed parameters ({legacyFields.join(', ')}). Use Write
          article in Site Analyzer again — old bookmarks will not work.
        </p>
      ) : null}
      {missingFields.length > 0 ? (
        <p className="mt-3 text-sm text-amber-900">
          This link is missing: {missingFields.join(', ')}.
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
  const [regenerating, setRegenerating] = useState(false);

  const handoffStartedRef = useRef<string | null>(null);

  const legacyHandoff = useMemo(
    () => rejectedLegacyHandoffParams(searchParams),
    [searchParams],
  );
  const completeHandoff =
    legacyHandoff.length === 0 && isCompleteContentWritingHandoff(urlParams);
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

    const handoffKey = urlParams.analysisRunId;

    if (handoffStartedRef.current === handoffKey) return;
    handoffStartedRef.current = handoffKey;

    setHandoffRunning(true);
    setError(null);
    setDraftProgress(null);

    try {
      const targetLocation = urlParams.location;
      const articleTitle =
        urlParams.title ||
        defaultTitleForKeyword(urlParams.keyword, 'New article');
      const articleKeyword = urlParams.keyword.trim();

      const created = await createContent(
        {
          title: articleTitle,
          targetKeyword: articleKeyword || undefined,
          targetLocation,
          analysisRunId: urlParams.analysisRunId,
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

  async function handleRegenerate() {
    if (!doc || !doc.analysisRunId || regenerating) return;
    setRegenerating(true);
    setError(null);
    setDraftProgress(null);
    try {
      const saved = await runResearchContentDraft(doc.id, accessToken, draftJobOptions);
      setDoc(saved);
      setTitle(saved.title);
      setKeyword(saved.targetKeyword);
    } catch (err) {
      setError(err);
    } finally {
      setRegenerating(false);
      setDraftProgress(null);
    }
  }

  const inWriting = Boolean(doc);
  const showGate = !inWriting && !handoffRunning;

  const workspace = inWriting ? (
    <div className="content-writing-workspace grid min-h-0 flex-1 grid-cols-12 items-stretch gap-4 overflow-hidden">
      <aside className="col-span-12 hidden min-h-0 xl:col-span-2 xl:flex xl:flex-col">
        <WritingScoreLeft keyword={keyword} />
      </aside>
      <div className="col-span-12 min-h-0 overflow-y-auto xl:col-span-7">
        <div className="space-y-4">
          <BlogPostPanel />
          <WritingEditorPane
            title={title}
            setTitle={setTitle}
            keyword={keyword}
            setKeyword={setKeyword}
            location={location}
            setLocation={setLocation}
            accessToken={accessToken}
          />
        </div>
      </div>
      <aside className="col-span-12 min-h-0 xl:col-span-3 xl:flex xl:flex-col">
        <WritingInsightsRight keyword={keyword} />
      </aside>
    </div>
  ) : null;

  const main = (
    <div
      className={
        inWriting
          ? 'mx-auto flex h-full min-h-0 w-full max-w-[1600px] flex-1 flex-col gap-3 overflow-hidden'
          : 'mx-auto w-full max-w-[1600px] space-y-6 px-4 py-6'
      }
    >
      <div className={inWriting ? 'flex shrink-0 flex-wrap items-center justify-between gap-3' : undefined}>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Content Writing</h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            {regenerating
              ? draftLoadingLabel(draftProgress, 'Regenerating draft…')
              : inWriting
                ? 'Surfer-style editor: live content score, term guidelines, suggestions, and JSON-LD.'
                : handoffRunning
                  ? 'Loading frozen research from Site Analyzer and generating your draft…'
                  : 'SEO article writing workspace — open from Site Analyzer with research attached.'}
          </p>
        </div>
        {inWriting && doc?.analysisRunId && !handoffRunning ? (
          <button
            type="button"
            disabled={regenerating}
            onClick={() => void handleRegenerate()}
            className="rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          >
            {regenerating ? 'Regenerating…' : 'Regenerate draft'}
          </button>
        ) : null}
      </div>

      {error ? <SeoErrorBanner error={error} /> : null}

      {handoffRunning ? (
        <section className="rounded-xl border bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-[var(--color-text-primary)]">
            {draftLoadingLabel(draftProgress, 'Starting draft…')}
          </p>
          <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
            Run: {urlParams.analysisRunId}
          </p>
        </section>
      ) : null}

      {showGate ? (
        <ContentWritingGate
          missingFields={missingHandoff}
          legacyFields={legacyHandoff}
        />
      ) : null}

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
