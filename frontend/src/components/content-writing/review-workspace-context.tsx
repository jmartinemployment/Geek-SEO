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
import { ScoreSidebar } from '@/components/editor/score-sidebar';
import { ReviewFeaturedImage } from '@/components/content-writing/review-featured-image';
import { useContentScoring, type ScoreSuggestion } from '@/hooks/useContentScoring';
import {
  applyScoreSuggestion,
  deleteSerpCache,
  getRenderedContentHtml,
  updateContent,
  updateContentStatus,
  type SeoContentDocument,
} from '@/lib/seo-api';
import { copyTextFromPromise } from '@/lib/copy-to-clipboard';

const DEFAULT_DRAFT_HTML = '<h1>Article title</h1><p>Start writing your article.</p>';

type ReviewWorkspaceContextValue = {
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
  statusUpdating: string | null;
  copyHint: string | null;
  save: (
    nextHtml: string,
    nextKeyword: string,
    nextTitle: string,
    nextLocation: string,
    options?: { scheduleScore?: boolean },
  ) => Promise<void>;
  handleApplySuggestion: (suggestion: ScoreSuggestion) => Promise<void>;
  changeStatus: (nextStatus: string) => Promise<void>;
  refreshSerp: () => void;
  copyRenderedHtml: () => void;
  scheduleScore: (nextHtml: string, nextKeyword: string) => void;
  notifyKeywordChanged: ReturnType<typeof useContentScoring>['notifyKeywordChanged'];
  setFeaturedImageUrl: (featuredImageUrl: string) => void;
};

const ReviewWorkspaceContext = createContext<ReviewWorkspaceContextValue | null>(null);

export function useReviewWorkspace(): ReviewWorkspaceContextValue {
  const value = use(ReviewWorkspaceContext);
  if (!value) {
    throw new Error('useReviewWorkspace must be used within ReviewWorkspaceProvider');
  }
  return value;
}

export function useReviewWorkspaceOptional(): ReviewWorkspaceContextValue | null {
  return use(ReviewWorkspaceContext);
}

export function ReviewWorkspaceProvider({
  doc,
  accessToken,
  title,
  setTitle,
  keyword,
  setKeyword,
  location,
  setLocation,
  onDocumentChange,
  onError,
  statusMessage,
  setStatusMessage,
  children,
}: {
  doc: SeoContentDocument;
  accessToken: string | null;
  title: string;
  setTitle: (value: string) => void;
  keyword: string;
  setKeyword: (value: string) => void;
  location: string;
  setLocation: (value: string) => void;
  onDocumentChange: (value: SeoContentDocument) => void;
  onError: (value: unknown) => void;
  statusMessage: string | null;
  setStatusMessage: (value: string | null) => void;
  children: ReactNode;
}) {
  const [html, setHtml] = useState(doc.contentHtml || DEFAULT_DRAFT_HTML);
  const [saving, setSaving] = useState(false);
  const [copyHint, setCopyHint] = useState<string | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
  const [statusUpdating, setStatusUpdating] = useState<string | null>(null);
  const [applyingSuggestionId, setApplyingSuggestionId] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const initialScoreSentRef = useRef(false);
  const editorRef = useRef<ContentEditorHandle>(null);

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
    void notifyContentChanged(html, keyword);
  }, [connected, html, keyword, notifyContentChanged]);

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

  async function changeStatus(nextStatus: string) {
    setStatusUpdating(nextStatus);
    try {
      const updated = await updateContentStatus(doc.id, nextStatus, accessToken);
      onDocumentChange(updated);
      setStatusMessage(
        nextStatus === 'approved_for_publish'
          ? 'Approved for publish. Copy rendered HTML from the sidebar when ready.'
          : nextStatus === 'awaiting_review'
            ? 'Marked as awaiting review.'
            : `Status updated to ${nextStatus}.`,
      );
    } catch (statusError) {
      onError(statusError);
    } finally {
      setStatusUpdating(null);
    }
  }

  async function handleApplySuggestion(suggestion: ScoreSuggestion) {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
      debounceRef.current = null;
    }
    setApplyingSuggestionId(suggestion.id);
    try {
      onError(null);
      setAiError(null);
      await save(html, keyword, title, location, { scheduleScore: false });
      const result = await applyScoreSuggestion(doc.id, suggestion.id, accessToken, html);
      setHtml(result.contentHtml);
      if (result.scoreUpdate) {
        receiveScoreUpdate(result.scoreUpdate);
      }
      await save(result.contentHtml, keyword, title, location, { scheduleScore: false });
    } catch (applyError) {
      const detail = applyError instanceof Error ? applyError.message : 'Apply failed';
      setAiError(`Could not apply “${suggestion.proposedChange}”: ${detail}`);
      onError(applyError);
    } finally {
      setApplyingSuggestionId(null);
    }
  }

  const isResearchBacked = Boolean(doc.urlResearchId);

  function refreshSerp() {
    if (isResearchBacked) return;
    void deleteSerpCache(keyword, location, accessToken)
      .then(() => notifyKeywordChanged(html, keyword, location))
      .catch(onError);
  }

  function copyRenderedHtml() {
    void copyTextFromPromise(async () => {
      const result = await getRenderedContentHtml(doc.id, accessToken);
      return result.renderedHtml;
    })
      .then(() => {
        setCopyHint('Rendered HTML copied');
        setTimeout(() => setCopyHint(null), 3000);
      })
      .catch(onError);
  }

  function setFeaturedImageUrl(featuredImageUrl: string) {
    onDocumentChange({ ...doc, featuredImageUrl });
  }

  const value: ReviewWorkspaceContextValue = {
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
    statusUpdating,
    copyHint,
    save,
    handleApplySuggestion,
    changeStatus,
    refreshSerp,
    copyRenderedHtml,
    scheduleScore,
    notifyKeywordChanged,
    setFeaturedImageUrl,
  };

  return (
    <ReviewWorkspaceContext value={value}>
      {children}
    </ReviewWorkspaceContext>
  );
}

export function ReviewScoreLeft({ keyword }: { keyword: string }) {
  const {
    doc,
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    scoreError,
    connected,
    refreshSerp,
  } = useReviewWorkspace();

  return (
    <ScoreSidebar
      placement="left"
      keyword={keyword}
      scoreUpdate={scoreUpdate}
      pendingReason={pendingReason}
      benchmarkRefreshing={benchmarkRefreshing}
      scoreError={scoreError}
      connected={connected}
      onRefreshSerp={refreshSerp}
      serpRefreshEnabled={!doc.urlResearchId}
    />
  );
}

export function ReviewScoreRight({
  keyword,
  statusMessage,
}: {
  keyword: string;
  statusMessage: string | null;
}) {
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
    changeStatus,
    statusUpdating,
    copyHint,
  } = useReviewWorkspace();

  return (
    <>
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
        serpRefreshEnabled={!doc.urlResearchId}
        onCopyHtml={copyRenderedHtml}
      />

      <div className="space-y-3 border-t px-3 py-4 xl:px-4">
        <h3 className="text-xs font-semibold xl:text-sm">Review gate</h3>
        <button
          type="button"
          disabled={!!statusUpdating}
          className="w-full rounded-lg border px-2 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50 xl:px-3 xl:py-2 xl:text-sm"
          onClick={() => void changeStatus('awaiting_review')}
        >
          {statusUpdating === 'awaiting_review' ? 'Updating…' : 'Mark awaiting review'}
        </button>
        <button
          type="button"
          disabled={!!statusUpdating}
          className="w-full rounded-lg border px-2 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50 xl:px-3 xl:py-2 xl:text-sm"
          onClick={() => void changeStatus('approved_for_publish')}
        >
          {statusUpdating === 'approved_for_publish' ? 'Updating…' : 'Approve for publish'}
        </button>
        {statusMessage ? (
          <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800">
            {statusMessage}
          </p>
        ) : null}
        {copyHint ? <p className="text-xs text-emerald-700">{copyHint}</p> : null}
      </div>

      <ReviewFeaturedImage />
    </>
  );
}

export function ReviewEditorPane({
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
  } = useReviewWorkspace();

  const keywordRef = useRef(keyword);
  const isResearchBacked = Boolean(doc.urlResearchId);

  return (
    <section className="rounded-xl border bg-white shadow-sm">
      <div className="border-b px-5 py-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-semibold">Review workspace</h2>
            <p className="text-sm text-[var(--color-text-secondary)]">
              Edit the draft, review score, then approve and export HTML.
            </p>
          </div>
          <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
            {doc.status}
          </span>
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
        <span className="text-xs text-[var(--color-text-muted)]">{saving ? 'Saving…' : null}</span>
      </header>

      <div className="space-y-4 p-5">
        {isResearchBacked ? (
          <p className="rounded-lg bg-slate-50 px-3 py-2 text-xs text-[var(--color-text-secondary)]">
            Keyword and location come from attached page research and cannot be changed here.
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

        <EditorAiToolbar
          documentId={doc.id}
          contentHtml={html}
          accessToken={accessToken}
          onApplyHtml={(nextHtml) => {
            setHtml(nextHtml);
            void save(nextHtml, keyword, title, location);
          }}
          onError={(message) => setAiError(message || null)}
        />
        {aiError ? <p className="text-sm text-red-700">{aiError}</p> : null}
      </div>
    </section>
  );
}