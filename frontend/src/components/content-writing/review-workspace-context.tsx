'use client';

import {
  createContext,
  use,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { ContentEditor, type ContentEditorHandle } from '@/components/editor/content-editor';
import { EditorAiToolbar } from '@/components/editor/editor-ai-toolbar';
import { InternalLinksPanel } from '@/components/editor/internal-links-panel';
import { ScoreSidebar } from '@/components/editor/score-sidebar';
import { ResearchInsightsRail } from '@/components/content-writing/research-insights-rail';
import { JsonLdPanel } from '@/components/content-writing/json-ld-panel';
import { ContentGuidelinesPanel } from '@/components/content-writing/content-guidelines-panel';
import { BlogSpokePanel } from '@/components/content-writing/blog-spoke-panel';
import { SpokePillarBanner } from '@/components/content-writing/spoke-pillar-banner';
import { useContentScoring, type ScoreSuggestion } from '@/hooks/useContentScoring';
import {
  applyScoreSuggestion,
  deleteSerpCache,
  formatRenderedArticleForClipboard,
  getContent,
  getRenderedContentHtml,
  scoreContentDocument,
  updateContent,
  type SeoContentDocument,
} from '@/lib/seo-api';
import { beginDraftJobWait } from '@/lib/draft-job-signalr';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import { copyTextFromPromise } from '@/lib/copy-to-clipboard';

const DEFAULT_DRAFT_HTML = '<h1>Article title</h1><p>Start writing your article.</p>';

type WritingWorkspaceContextValue = {
  doc: SeoContentDocument;
  accessToken: string | null;
  html: string;
  setHtml: (value: string) => void;
  saving: boolean;
  aiError: string | null;
  setAiError: (value: string | null) => void;
  editorRef: React.RefObject<ContentEditorHandle | null>;
  scoreUpdate: ReturnType<typeof useContentScoring>['scoreUpdate'];
  pendingReason: ReturnType<typeof useContentScoring>['pendingReason'];
  benchmarkRefreshing: ReturnType<typeof useContentScoring>['benchmarkRefreshing'];
  scoreError: ReturnType<typeof useContentScoring>['error'];
  connected: ReturnType<typeof useContentScoring>['connected'];
  applyingSuggestionId: string | null;
  copyHint: string | null;
  save: (
    nextHtml: string,
    nextKeyword: string,
    nextTitle: string,
    nextLocation: string,
    options?: { scheduleScore?: boolean },
  ) => Promise<void>;
  handleApplySuggestion: (suggestion: ScoreSuggestion) => Promise<void>;
  refreshSerp: () => void;
  copyRenderedHtml: () => void;
  scheduleScore: (nextHtml: string, nextKeyword: string) => void;
  notifyKeywordChanged: ReturnType<typeof useContentScoring>['notifyKeywordChanged'];
  blogSpokeRevision: number;
  refreshBlogSpoke: () => void;
  reloadDocument: () => Promise<void>;
};

const WritingWorkspaceContext = createContext<WritingWorkspaceContextValue | null>(null);

export function useWritingWorkspace(): WritingWorkspaceContextValue {
  const value = use(WritingWorkspaceContext);
  if (!value) {
    throw new Error('useWritingWorkspace must be used within WritingWorkspaceProvider');
  }
  return value;
}

/** @deprecated Use useWritingWorkspace */
export const useReviewWorkspace = useWritingWorkspace;

export function WritingWorkspaceProvider({
  doc,
  accessToken,
  onDocumentChange,
  onError,
  children,
}: {
  doc: SeoContentDocument;
  accessToken: string | null;
  onDocumentChange: (value: SeoContentDocument) => void;
  onError: (value: unknown) => void;
  children: ReactNode;
}) {
  const [html, setHtml] = useState(doc.contentHtml || DEFAULT_DRAFT_HTML);
  const [saving, setSaving] = useState(false);
  const [copyHint, setCopyHint] = useState<string | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
  const [applyingSuggestionId, setApplyingSuggestionId] = useState<string | null>(null);
  const [blogSpokeRevision, setBlogSpokeRevision] = useState(0);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const initialScoreSentRef = useRef(false);
  const editorRef = useRef<ContentEditorHandle>(null);
  const hub = useSeoHub();

  const {
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    error: scoreError,
    connected,
    notifyContentChanged,
    notifyKeywordChanged,
    receiveScoreUpdate,
  } = useContentScoring(doc.id, accessToken);

  useEffect(() => {
    if (!connected || initialScoreSentRef.current) return;
    initialScoreSentRef.current = true;
    void notifyContentChanged(html, doc.targetKeyword);
  }, [connected, doc.targetKeyword, html, notifyContentChanged]);

  function scheduleScore(nextHtml: string, nextKeyword: string) {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      void notifyContentChanged(nextHtml, nextKeyword);
    }, 800);
  }

  async function save(
    nextHtml: string,
    nextKeyword: string,
    nextTitle: string,
    nextLocation: string,
    options?: { scheduleScore?: boolean },
  ) {
    setSaving(true);
    try {
      onError(null);
      const updated = await updateContent(
        doc.id,
        {
          contentHtml: nextHtml,
          targetKeyword: nextKeyword,
          targetLocation: nextLocation,
          title: nextTitle,
        },
        accessToken,
      );
      onDocumentChange(updated);
      if (options?.scheduleScore !== false) {
        scheduleScore(nextHtml, nextKeyword);
      }
    } catch (saveError) {
      onError(saveError);
    } finally {
      setSaving(false);
    }
  }

  async function handleApplySuggestion(suggestion: ScoreSuggestion) {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
      debounceRef.current = null;
    }
    setApplyingSuggestionId(suggestion.id);
    const isAsyncSources = suggestion.id === 'geo_citations' && suggestion.applyMode === 'ai';
    const listener = isAsyncSources
      ? beginDraftJobWait({ hub, accessToken })
      : null;
    try {
      onError(null);
      setAiError(null);
      if (listener) {
        await listener.whenReady();
      }
      await save(html, doc.targetKeyword, doc.title, doc.targetLocation ?? '', {
        scheduleScore: false,
      });
      const outcome = await applyScoreSuggestion(
        doc.id,
        suggestion.id,
        accessToken,
        html,
      );
      if (outcome.kind === 'queued') {
        const terminal = await listener!.waitFor(outcome.job.jobId);
        if (terminal.status === 'failed') {
          throw new Error(terminal.errorMessage ?? 'Source discovery failed');
        }
        const updated = await getContent(doc.id, accessToken);
        setHtml(updated.contentHtml);
        onDocumentChange(updated);
        const scored = await scoreContentDocument(
          doc.id,
          { contentHtml: updated.contentHtml, targetKeyword: doc.targetKeyword },
          accessToken,
        );
        if (scored.scoreUpdate) {
          receiveScoreUpdate(scored.scoreUpdate);
        }
        await save(
          updated.contentHtml,
          doc.targetKeyword,
          doc.title,
          doc.targetLocation ?? '',
          { scheduleScore: false },
        );
        return;
      }

      setHtml(outcome.result.contentHtml);
      if (outcome.result.scoreUpdate) {
        receiveScoreUpdate(outcome.result.scoreUpdate);
      }
      await save(
        outcome.result.contentHtml,
        doc.targetKeyword,
        doc.title,
        doc.targetLocation ?? '',
        { scheduleScore: false },
      );
    } catch (applyError) {
      const detail = applyError instanceof Error ? applyError.message : 'Apply failed';
      setAiError(`Could not apply “${suggestion.proposedChange}”: ${detail}`);
      onError(applyError);
    } finally {
      listener?.dispose();
      setApplyingSuggestionId(null);
    }
  }

  const isResearchBacked = Boolean(doc.analysisRunId);

  function refreshSerp() {
    if (isResearchBacked) return;
    void deleteSerpCache(doc.targetKeyword, doc.targetLocation ?? '', accessToken)
      .then(() =>
        notifyKeywordChanged(html, doc.targetKeyword, doc.targetLocation ?? ''),
      )
      .catch(onError);
  }

  function copyRenderedHtml() {
    void copyTextFromPromise(async () => {
      const result = await getRenderedContentHtml(doc.id, accessToken);
      return formatRenderedArticleForClipboard(result);
    })
      .then(() => {
        setCopyHint('HTML + JSON-LD copied');
        setTimeout(() => setCopyHint(null), 3000);
      })
      .catch(onError);
  }

  async function reloadDocument() {
    if (!accessToken) return;
    const refreshed = await getContent(doc.id, accessToken);
    onDocumentChange(refreshed);
    setHtml(refreshed.contentHtml || DEFAULT_DRAFT_HTML);
  }

  const value: WritingWorkspaceContextValue = {
    doc,
    accessToken,
    html,
    setHtml,
    saving,
    aiError,
    setAiError,
    editorRef,
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    scoreError,
    connected,
    applyingSuggestionId,
    copyHint,
    save,
    handleApplySuggestion,
    refreshSerp,
    copyRenderedHtml,
    scheduleScore,
    notifyKeywordChanged,
    blogSpokeRevision,
    refreshBlogSpoke: () => setBlogSpokeRevision((n) => n + 1),
    reloadDocument,
  };

  return (
    <WritingWorkspaceContext value={value}>
      {children}
    </WritingWorkspaceContext>
  );
}

/** @deprecated Use WritingWorkspaceProvider */
export const ReviewWorkspaceProvider = WritingWorkspaceProvider;

export function WritingScoreLeft({ keyword }: { keyword: string }) {
  const {
    doc,
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    scoreError,
    connected,
    refreshSerp,
  } = useWritingWorkspace();

  return (
    <div className="content-writing-sticky-rail h-full min-h-0 min-w-0 flex-1 rounded-xl border bg-white shadow-sm">
      <ScoreSidebar
        placement="left"
        keyword={keyword}
        scoreUpdate={scoreUpdate}
        pendingReason={pendingReason}
        benchmarkRefreshing={benchmarkRefreshing}
        scoreError={scoreError}
        connected={connected}
        onRefreshSerp={refreshSerp}
        serpRefreshEnabled={!doc.analysisRunId}
      />
    </div>
  );
}

/** @deprecated Use WritingScoreLeft */
export const ReviewScoreLeft = WritingScoreLeft;

export function WritingInsightsRight({ keyword }: { keyword: string }) {
  const {
    doc,
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    scoreError,
    connected,
    applyingSuggestionId,
    handleApplySuggestion,
    refreshSerp,
    copyRenderedHtml,
    copyHint,
  } = useWritingWorkspace();

  return (
    <div className="content-writing-sticky-rail h-full min-h-0 min-w-0 flex-1 rounded-xl border bg-white shadow-sm">
      {doc.documentKind !== 'spoke' && !doc.analysisRunId ? <BlogSpokePanel /> : null}
      <ScoreSidebar
        placement="right"
        keyword={keyword}
        scoreUpdate={scoreUpdate}
        pendingReason={pendingReason}
        benchmarkRefreshing={benchmarkRefreshing}
        scoreError={scoreError}
        connected={connected}
        applyingSuggestionId={applyingSuggestionId}
        onApplySuggestion={handleApplySuggestion}
        onRefreshSerp={refreshSerp}
        serpRefreshEnabled={!doc.analysisRunId}
        onCopyHtml={copyRenderedHtml}
      />
      {copyHint ? (
        <p className="border-t px-3 py-2 text-xs text-emerald-700 xl:px-4">{copyHint}</p>
      ) : null}
      <ContentGuidelinesPanel keyword={keyword} />
      <JsonLdPanel />
      {doc.analysisRunId ? (
        <ResearchInsightsRail
          articleKeyword={doc.targetKeyword}
          serpKeyword={doc.serpKeyword}
        />
      ) : null}
    </div>
  );
}

/** @deprecated Use WritingInsightsRight */
export const ReviewScoreRight = WritingInsightsRight;

export function WritingEditorPane({
  title,
  setTitle,
  keyword,
  setKeyword,
  location,
  setLocation,
  accessToken,
}: {
  title: string;
  setTitle: (value: string) => void;
  keyword: string;
  setKeyword: (value: string) => void;
  location: string;
  setLocation: (value: string) => void;
  accessToken: string | null;
}) {
  const {
    doc,
    html,
    setHtml,
    saving,
    aiError,
    setAiError,
    editorRef,
    save,
    scheduleScore,
    notifyKeywordChanged,
    refreshBlogSpoke,
  } = useWritingWorkspace();

  const keywordRef = useRef(keyword);
  const isResearchBacked = Boolean(doc.analysisRunId);

  return (
    <section className="rounded-xl border bg-white shadow-sm">
      <div className="border-b px-5 py-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-semibold">Content editor</h2>
            <p className="text-sm text-[var(--color-text-secondary)]">
              Write against live score, guidelines, and SERP research.
            </p>
          </div>
          <span className="text-xs text-[var(--color-text-muted)]">{saving ? 'Saving…' : 'Saved'}</span>
        </div>
      </div>

      <header className="flex items-center gap-4 border-b bg-white px-5 py-4">
        <input
          className="flex-1 rounded-lg border border-transparent bg-transparent text-lg font-semibold outline-none focus:border-[var(--color-border-strong)] focus:bg-white focus:px-2"
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          onBlur={() => void save(html, keyword, title, location)}
          aria-label="Document title"
        />
      </header>

      <div className="space-y-4 p-5">
        <SpokePillarBanner />
        {isResearchBacked ? (
          <p className="rounded-lg bg-slate-50 px-3 py-2 text-xs text-[var(--color-text-secondary)]">
            Keyword and location are fixed from your Site Analyzer handoff.
          </p>
        ) : null}

        <div className="grid max-w-lg gap-4 sm:grid-cols-2">
          <label className="text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
            Target keyword
            <input
              className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 shadow-sm disabled:bg-[var(--color-surface-muted)]"
              value={keyword}
              readOnly={isResearchBacked}
              onChange={(event) => setKeyword(event.target.value)}
              onBlur={() => {
                void save(html, keyword, title, location);
                if (!isResearchBacked && keyword !== keywordRef.current) {
                  keywordRef.current = keyword;
                  void notifyKeywordChanged(html, keyword, location);
                }
              }}
            />
          </label>
          <label className="text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
            Location
            <input
              className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 shadow-sm disabled:bg-[var(--color-surface-muted)]"
              value={location}
              readOnly={isResearchBacked}
              onChange={(event) => setLocation(event.target.value)}
              onBlur={() => {
                void save(html, keyword, title, location);
                if (!isResearchBacked) {
                  void notifyKeywordChanged(html, keyword, location);
                }
              }}
            />
          </label>
        </div>

        <ContentEditor
          ref={editorRef}
          html={html}
          onChange={(nextHtml) => {
            setHtml(nextHtml);
            scheduleScore(nextHtml, keyword);
          }}
        />

        <InternalLinksPanel
          projectId={doc.projectId}
          documentId={doc.id}
          accessToken={accessToken}
          onInsertLink={(href, anchorText) => editorRef.current?.insertLink(href, anchorText)}
          onAutoInsertHtml={(nextHtml) => {
            setHtml(nextHtml);
            void save(nextHtml, keyword, title, location);
          }}
        />

        <EditorAiToolbar
          documentId={doc.id}
          contentHtml={html}
          accessToken={accessToken}
          onApplyHtml={(nextHtml) => {
            setHtml(nextHtml);
            void save(nextHtml, keyword, title, location);
          }}
          onError={(message) => setAiError(message || null)}
          onBlogSpokeCreated={refreshBlogSpoke}
          isResearchBacked={isResearchBacked}
        />
        {aiError ? <p className="text-sm text-red-700">{aiError}</p> : null}
      </div>
    </section>
  );
}

/** @deprecated Use WritingEditorPane */
export const ReviewEditorPane = WritingEditorPane;
