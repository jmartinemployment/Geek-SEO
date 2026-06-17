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
import { useContentScoring, type ScoreSuggestion } from '@/hooks/useContentScoring';
import {
  applyScoreSuggestion,
  deleteSerpCache,
  getRenderedContentHtml,
  updateContent,
  updateContentStatus,
  type SeoContentDocument,
} from '@/lib/seo-api';

const DEFAULT_DRAFT_HTML = '<h1>Article title</h1><p>Start writing your article.</p>';

type ReviewWorkspaceContextValue = {
  doc: SeoContentDocument;
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
  save: (nextHtml: string, nextKeyword: string, nextTitle: string, nextLocation: string) => Promise<void>;
  handleApplySuggestion: (suggestion: ScoreSuggestion) => Promise<void>;
  changeStatus: (nextStatus: string) => Promise<void>;
  refreshSerp: () => void;
  copyRenderedHtml: () => void;
  scheduleScore: (nextHtml: string, nextKeyword: string) => void;
  notifyKeywordChanged: ReturnType<typeof useContentScoring>['notifyKeywordChanged'];
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

  async function save(nextHtml: string, nextKeyword: string, nextTitle: string, nextLocation: string) {
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
      scheduleScore(nextHtml, nextKeyword);
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
    setApplyingSuggestionId(suggestion.id);
    try {
      onError(null);
      setAiError(null);
      await save(html, keyword, title, location);
      const result = await applyScoreSuggestion(doc.id, suggestion.id, accessToken, html);
      setHtml(result.contentHtml);
      await save(result.contentHtml, keyword, title, location);
    } catch (applyError) {
      const detail = applyError instanceof Error ? applyError.message : 'Apply failed';
      setAiError(`Could not apply “${suggestion.proposedChange}”: ${detail}`);
      onError(applyError);
    } finally {
      setApplyingSuggestionId(null);
    }
  }

  function refreshSerp() {
    void deleteSerpCache(keyword, location, accessToken)
      .then(() => notifyKeywordChanged(html, keyword, location))
      .catch(onError);
  }

  function copyRenderedHtml() {
    void getRenderedContentHtml(doc.id, accessToken)
      .then((result) => navigator.clipboard.writeText(result.renderedHtml))
      .then(() => {
        setCopyHint('Rendered HTML copied');
        setTimeout(() => setCopyHint(null), 3000);
      })
      .catch(onError);
  }

  const value: ReviewWorkspaceContextValue = {
    doc,
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
  };

  return (
    <ReviewWorkspaceContext value={value}>
      {children}
    </ReviewWorkspaceContext>
  );
}

export function ReviewScoreLeft({ keyword }: { keyword: string }) {
  const {
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
        <div className="grid max-w-lg gap-4 sm:grid-cols-2">
          <label className="text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
            Target keyword
            <input
              className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 shadow-sm"
              value={keyword}
              onChange={(event) => setKeyword(event.target.value)}
              onBlur={() => {
                void save(html, keyword, title, location);
                if (keyword !== keywordRef.current) {
                  keywordRef.current = keyword;
                  void notifyKeywordChanged(html, keyword, location);
                }
              }}
            />
          </label>
          <label className="text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
            Location
            <input
              className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 shadow-sm"
              value={location}
              onChange={(event) => setLocation(event.target.value)}
              onBlur={() => {
                void save(html, keyword, title, location);
                void notifyKeywordChanged(html, keyword, location);
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