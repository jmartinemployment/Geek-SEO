'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { CompetitorPanel } from '@/components/editor/competitor-panel';
import { EditorAiToolbar } from '@/components/editor/editor-ai-toolbar';
import { ContentEditor } from '@/components/editor/content-editor';
import { useContentScoring } from '@/hooks/useContentScoring';
import {
  deleteSerpCache,
  getContent,
  getWordPressStatus,
  publishToWordPress,
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
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [publishedUrl, setPublishedUrl] = useState<string | null>(null);
  const [wpConnected, setWpConnected] = useState(false);
  const [copyHint, setCopyHint] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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
    void (async () => {
      try {
        const loaded = await getContent(documentId, accessToken);
        setDoc(loaded);
        setHtml(loaded.contentHtml || DEFAULT_HTML);
        setKeyword(loaded.targetKeyword);
        keywordRef.current = loaded.targetKeyword;
        setLocation(loaded.targetLocation || 'United States');
        setTitle(loaded.title);
        try {
          const wp = await getWordPressStatus(loaded.projectId, accessToken);
          setWpConnected(wp.connected);
        } catch {
          setWpConnected(false);
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load document');
      }
    })();
  }, [documentId, accessToken, authLoading]);

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
      setError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  }

  function onHtmlChange(nextHtml: string) {
    setHtml(nextHtml);
    scheduleScore(nextHtml, keyword);
  }

  if (authLoading) {
    return <main className="p-8 text-zinc-500">Loading session…</main>;
  }

  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      <div className="flex flex-1 flex-col border-r">
        <header className="flex items-center gap-4 border-b px-6 py-4">
          <Link href={doc ? `/app/projects/${doc.projectId}` : '/app/projects'} className="text-sm text-zinc-500">
            ← Back
          </Link>
          <input
            className="flex-1 text-lg font-semibold outline-none"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => void save(html, keyword, title)}
          />
          <span className="text-xs text-zinc-400">{saving ? 'Saving…' : connected ? 'Live' : 'Offline'}</span>
        </header>

        <div className="flex flex-1 flex-col gap-4 p-6">
          <div className="grid max-w-md gap-3 sm:grid-cols-2">
            <label className="text-sm font-medium text-zinc-600 sm:col-span-2">
              Target keyword
              <input
                className="mt-1 block w-full rounded border px-3 py-2"
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
            <label className="text-sm font-medium text-zinc-600 sm:col-span-2">
              Location
              <input
                className="mt-1 block w-full rounded border px-3 py-2"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                onBlur={() => {
                  void save(html, keyword, title);
                  void notifyKeywordChanged(html, keyword, location);
                }}
              />
            </label>
          </div>

          <ContentEditor html={html} onChange={onHtmlChange} />
        </div>

        {error && <p className="px-6 pb-4 text-sm text-red-600">{error}</p>}
      </div>

      <aside className="w-full shrink-0 bg-zinc-50 p-6 lg:w-96">
        <h2 className="text-lg font-semibold">Content score</h2>

        <button
          type="button"
          className="mt-3 text-xs text-zinc-500 underline hover:text-zinc-800"
          onClick={() =>
            void (async () => {
              try {
                await deleteSerpCache(keyword, location, accessToken);
                void notifyKeywordChanged(html, keyword, location);
              } catch (e) {
                setError(e instanceof Error ? e.message : 'Refresh failed');
              }
            })()
          }
        >
          Refresh SERP benchmarks
        </button>

        {scoreError && <p className="mt-2 text-sm text-red-600">{scoreError}</p>}
        {(benchmarkRefreshing || pendingReason) && (
          <p className="mt-4 text-sm text-amber-700">
            Benchmarks loading… {pendingReason ? `(${pendingReason})` : ''}
          </p>
        )}

        {scoreUpdate?.benchmarkQuality === 'low_sample_count' && (
          <p className="mt-2 rounded border border-amber-200 bg-amber-50 p-2 text-xs text-amber-900">
            Fewer than 3 competitor pages could be crawled — word-count targets are estimated from SERP snippets.
          </p>
        )}

        {scoreUpdate ? (
          <div className="mt-4 space-y-4">
            <div className="flex items-baseline gap-2">
              <span className="text-4xl font-bold">{scoreUpdate.score}</span>
              <span className="text-xl text-zinc-500">/ 100</span>
              <span className="rounded bg-zinc-900 px-2 py-0.5 text-sm text-white">{scoreUpdate.grade}</span>
            </div>

            <ul className="space-y-1 text-sm">
              {Object.entries(scoreUpdate.components).map(([key, value]) => (
                <li key={key} className="flex justify-between">
                  <span className="text-zinc-600">{key}</span>
                  <span>{value}</span>
                </li>
              ))}
            </ul>

            {scoreUpdate.suggestions.length > 0 && (
              <div>
                <h3 className="font-medium">Top suggestions</h3>
                <ul className="mt-2 space-y-2 text-sm">
                  {scoreUpdate.suggestions.slice(0, 5).map((s) => (
                    <li key={s.component} className="rounded border bg-white p-2">
                      +{s.pointValue} pts — {s.actionText}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {scoreUpdate.serpFeatures.length > 0 && (
              <div>
                <h3 className="font-medium">SERP features</h3>
                <ul className="mt-2 space-y-1 text-sm text-zinc-700">
                  {scoreUpdate.serpFeatures.map((f) => (
                    <li key={f.feature}>{f.actionText}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        ) : (
          <p className="mt-4 text-sm text-zinc-500">Edit content to see your score.</p>
        )}

        <div className="mt-6 border-t pt-4">
          <h3 className="text-sm font-semibold">Export</h3>
          <p className="mt-1 text-xs text-zinc-500">
            Copy HTML to paste into any CMS, or connect WordPress on the project page.
          </p>
          <button
            type="button"
            className="mt-3 w-full rounded-lg border bg-white px-3 py-2 text-xs font-medium hover:bg-zinc-50"
            onClick={() => {
              void navigator.clipboard.writeText(html).then(() => {
                setCopyHint('HTML copied to clipboard');
                setTimeout(() => setCopyHint(null), 3000);
              });
            }}
          >
            Copy HTML
          </button>
          {copyHint && <p className="mt-2 text-xs text-green-800">{copyHint}</p>}
          {!wpConnected && doc && (
            <Link
              href={`/app/projects/${doc.projectId}`}
              className="mt-2 block text-xs text-zinc-500 underline hover:text-zinc-800"
            >
              Optional: connect WordPress on project settings
            </Link>
          )}
        </div>

        {wpConnected && (
          <div className="mt-6 border-t pt-4">
            <h3 className="text-sm font-semibold">WordPress</h3>
          {publishedUrl && (
            <a
              href={publishedUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-2 block text-xs text-green-700 underline"
            >
              View on WordPress
            </a>
          )}
          <div className="mt-3 flex flex-col gap-2">
            <button
              type="button"
              disabled={publishing}
              className="rounded-lg border bg-white px-3 py-2 text-xs font-medium hover:bg-zinc-50 disabled:opacity-50"
              onClick={() =>
                void (async () => {
                  setPublishing(true);
                  setError(null);
                  try {
                    await save(html, keyword, title);
                    const result = await publishToWordPress(
                      documentId,
                      { postStatus: 'draft' },
                      accessToken,
                    );
                    setPublishedUrl(result.url);
                  } catch (e) {
                    setError(e instanceof Error ? e.message : 'Publish failed');
                  } finally {
                    setPublishing(false);
                  }
                })()
              }
            >
              {publishing ? 'Publishing…' : 'Save draft to WordPress'}
            </button>
            <button
              type="button"
              disabled={publishing}
              className="rounded-lg bg-zinc-900 px-3 py-2 text-xs font-medium text-white hover:bg-zinc-800 disabled:opacity-50"
              onClick={() =>
                void (async () => {
                  setPublishing(true);
                  setError(null);
                  try {
                    await save(html, keyword, title);
                    const result = await publishToWordPress(
                      documentId,
                      { postStatus: 'publish' },
                      accessToken,
                    );
                    setPublishedUrl(result.url);
                  } catch (e) {
                    setError(e instanceof Error ? e.message : 'Publish failed');
                  } finally {
                    setPublishing(false);
                  }
                })()
              }
            >
              Publish live
            </button>
          </div>
          </div>
        )}

        <EditorAiToolbar
          documentId={documentId}
          contentHtml={html}
          accessToken={accessToken}
          onApplyHtml={(next) => {
            setHtml(next);
            scheduleScore(next, keyword);
          }}
          onError={setError}
        />

        <CompetitorPanel documentId={documentId} accessToken={accessToken} />
      </aside>
    </div>
  );
}
