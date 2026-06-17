'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { CompetitorPanel } from '@/components/editor/competitor-panel';
import { EditorAiToolbar } from '@/components/editor/editor-ai-toolbar';
import { InternalLinksPanel } from '@/components/editor/internal-links-panel';
import { PlagiarismPanel } from '@/components/editor/plagiarism-panel';
import { ContentEditor, type ContentEditorHandle } from '@/components/editor/content-editor';
import { ScoreSidebar } from '@/components/editor/score-sidebar';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { useContentScoring } from '@/hooks/useContentScoring';
import {
  applyScoreSuggestion,
  deleteSerpCache,
  getContent,
  updateContent,
  type SeoContentDocument,
} from '@/lib/seo-api';

const DEFAULT_HTML = '<h1>Article title</h1><p>Start writing your content here.</p>';

export default function ContentEditorPage() {
  const params = useParams();
  const documentId = params.id as string;
  const { accessToken, isLoading: authLoading } = useAuth();
  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [html, setHtml] = useState(DEFAULT_HTML);
  const [keyword, setKeyword] = useState('');
  const [location, setLocation] = useState('United States');
  const [title, setTitle] = useState('');
  const keywordRef = useRef(keyword);
  const [error, setError] = useState<unknown>(null);
  const [saving, setSaving] = useState(false);
  const [copyHint, setCopyHint] = useState<string | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
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
  } = useContentScoring(documentId, accessToken);

  useEffect(() => {
    if (authLoading) return;
    initialScoreSentRef.current = false;
    void (async () => {
      try {
        setError(null);
        const loaded = await getContent(documentId, accessToken);
        setDoc(loaded);
        setHtml(loaded.contentHtml || DEFAULT_HTML);
        setKeyword(loaded.targetKeyword);
        keywordRef.current = loaded.targetKeyword;
        setLocation(loaded.targetLocation || 'United States');
        setTitle(loaded.title);
      } catch (e) {
        setError(e);
      }
    })();
  }, [documentId, accessToken, authLoading]);

  useEffect(() => {
    if (!doc || !connected || initialScoreSentRef.current) return;
    initialScoreSentRef.current = true;
    void notifyContentChanged(html, keyword);
  }, [doc, connected, html, keyword, notifyContentChanged]);

  const scheduleScore = useCallback(
    (nextHtml: string, nextKeyword: string) => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        void notifyContentChanged(nextHtml, nextKeyword);
      }, 800);
    },
    [notifyContentChanged],
  );

  async function save(nextHtml: string, nextKeyword: string, nextTitle: string) {
    setSaving(true);
    try {
      setError(null);
      const updated = await updateContent(
        documentId,
        {
          contentHtml: nextHtml,
          targetKeyword: nextKeyword,
          targetLocation: location,
          title: nextTitle,
        },
        accessToken,
      );
      setDoc(updated);
      scheduleScore(nextHtml, nextKeyword);
    } catch (e) {
      setError(e);
    } finally {
      setSaving(false);
    }
  }

  function onHtmlChange(nextHtml: string) {
    setHtml(nextHtml);
    scheduleScore(nextHtml, keyword);
  }

  function applyEditorHtml(nextHtml: string) {
    setHtml(nextHtml);
    void save(nextHtml, keyword, title);
  }

  async function handleApplySuggestion(suggestionId: string) {
    setApplyingSuggestionId(suggestionId);
    try {
      setError(null);
      const result = await applyScoreSuggestion(documentId, suggestionId, accessToken);
      applyEditorHtml(result.contentHtml);
    } catch (e) {
      setError(e);
    } finally {
      setApplyingSuggestionId(null);
    }
  }

  function insertInternalLink(href: string, anchorText: string) {
    editorRef.current?.insertLink(href, anchorText);
  }

  async function refreshSerp() {
    try {
      setError(null);
      await deleteSerpCache(keyword, location, accessToken);
      void notifyKeywordChanged(html, keyword, location);
    } catch (e) {
      setError(e);
    }
  }

  if (authLoading) {
    return <main className="p-8 text-[var(--color-text-secondary)]">Loading session…</main>;
  }

  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      <div className="flex flex-1 flex-col lg:border-r">
        <header className="flex items-center gap-4 border-b bg-white px-6 py-4">
          <Link
            href={doc ? `/app/projects/${doc.projectId}` : '/app/projects'}
            className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
          >
            ← Back
          </Link>
          <input
            className="flex-1 rounded-lg border border-transparent bg-transparent text-lg font-semibold outline-none focus:border-[var(--color-border-strong)] focus:bg-white focus:px-2"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => void save(html, keyword, title)}
            aria-label="Document title"
          />
          <span className="text-xs text-[var(--color-text-muted)]">{saving ? 'Saving…' : null}</span>
        </header>

        <div className="flex flex-1 flex-col gap-4 p-6">
          {error ? <SeoErrorBanner error={error} /> : null}

          <div className="grid max-w-lg gap-4 sm:grid-cols-2">
            <label className="text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
              Target keyword
              <input
                className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 shadow-sm focus:border-[var(--color-accent)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                onBlur={() => {
                  void save(html, keyword, title);
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
                className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 shadow-sm focus:border-[var(--color-accent)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                onBlur={() => {
                  void save(html, keyword, title);
                  void notifyKeywordChanged(html, keyword, location);
                }}
              />
            </label>
          </div>

          <ContentEditor ref={editorRef} html={html} onChange={onHtmlChange} />

          <EditorAiToolbar
            documentId={documentId}
            contentHtml={html}
            accessToken={accessToken}
            onApplyHtml={applyEditorHtml}
            onError={(message) => setAiError(message || null)}
          />
          {aiError ? <p className="text-sm text-red-700">{aiError}</p> : null}
        </div>
      </div>

      <div className="flex flex-col">
        <ScoreSidebar
          keyword={keyword}
          scoreUpdate={scoreUpdate}
          pendingReason={pendingReason}
          benchmarkRefreshing={benchmarkRefreshing}
          scoreError={scoreError}
          connected={connected}
          onRefreshSerp={() => void refreshSerp()}
          applyingSuggestionId={applyingSuggestionId}
          onApplySuggestion={handleApplySuggestion}
          onCopyHtml={() => {
            void navigator.clipboard.writeText(html).then(() => {
              setCopyHint('HTML copied');
              setTimeout(() => setCopyHint(null), 3000);
            });
          }}
        />
        {copyHint && (
          <p className="px-6 pb-2 text-xs text-emerald-700 lg:hidden">{copyHint}</p>
        )}
        <div className="border-t px-6 pb-4 lg:hidden">
          <button
            type="button"
            className="w-full rounded-lg border bg-white px-3 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)]"
            onClick={() => {
              void navigator.clipboard.writeText(html).then(() => {
                setCopyHint('HTML copied');
                setTimeout(() => setCopyHint(null), 3000);
              });
            }}
          >
            Copy HTML
          </button>
        </div>
        <div className="bg-[var(--color-bg)] px-6 pb-6 lg:w-96">
          <CompetitorPanel documentId={documentId} accessToken={accessToken} />
          {doc ? (
            <InternalLinksPanel
              projectId={doc.projectId}
              documentId={documentId}
              accessToken={accessToken}
              onInsertLink={insertInternalLink}
              onAutoInsertHtml={applyEditorHtml}
            />
          ) : null}
          <PlagiarismPanel documentId={documentId} accessToken={accessToken} />
        </div>
      </div>
    </div>
  );
}
