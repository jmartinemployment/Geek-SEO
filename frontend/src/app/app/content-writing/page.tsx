'use client';

import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useMemo, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { ContentEditor, type ContentEditorHandle } from '@/components/editor/content-editor';
import { EditorAiToolbar } from '@/components/editor/editor-ai-toolbar';
import { ScoreSidebar } from '@/components/editor/score-sidebar';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { useContentScoring } from '@/hooks/useContentScoring';
import {
  applyScoreSuggestion,
  createContent,
  deleteSerpCache,
  generateBrief,
  generateDraft,
  generateOutline,
  getRenderedContentHtml,
  listProjects,
  updateContent,
  updateContentStatus,
  type ContentBrief,
  type SeoContentDocument,
  type SeoProject,
} from '@/lib/seo-api';

const DEFAULT_LOCATION = 'United States';
const DEFAULT_DRAFT_HTML = '<h1>Article title</h1><p>Start writing your article.</p>';

type Stage = 'brief' | 'outline' | 'draft' | 'review';

function ContentWritingPageInner() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const searchParams = useSearchParams();
  const initialKeyword = searchParams.get('keyword') ?? '';
  const initialTitle = searchParams.get('title') ?? '';
  const initialProjectId = searchParams.get('projectId') ?? '';
  const initialLocation = searchParams.get('location') ?? DEFAULT_LOCATION;

  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId);
  const [title, setTitle] = useState(initialTitle || initialKeyword || 'New article');
  const [keyword, setKeyword] = useState(initialKeyword);
  const [location, setLocation] = useState(initialLocation);
  const [brief, setBrief] = useState<ContentBrief | null>(null);
  const [outline, setOutline] = useState('');
  const [doc, setDoc] = useState<SeoContentDocument | null>(null);
  const [stage, setStage] = useState<Stage>('brief');
  const [loadingAction, setLoadingAction] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

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

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === projectId) ?? null,
    [projectId, projects],
  );

  async function run(action: string, fn: () => Promise<void>) {
    setLoadingAction(action);
    setError(null);
    setStatusMessage(null);
    try {
      await fn();
    } catch (runError) {
      setError(runError);
    } finally {
      setLoadingAction(null);
    }
  }

  async function ensureDocument(nextHtml: string): Promise<SeoContentDocument> {
    if (!projectId) throw new Error('Select a project first.');

    if (!doc) {
      const created = await createContent(
        {
          projectId,
          title,
          targetKeyword: keyword,
          targetLocation: location,
        },
        accessToken,
      );
      const saved = await updateContent(
        created.id,
        {
          contentHtml: nextHtml,
          title,
          targetKeyword: keyword,
          targetLocation: location,
        },
        accessToken,
      );
      setDoc(saved);
      return saved;
    }

    const saved = await updateContent(
      doc.id,
      {
        contentHtml: nextHtml,
        title,
        targetKeyword: keyword,
        targetLocation: location,
      },
      accessToken,
    );
    setDoc(saved);
    return saved;
  }

  if (authLoading) {
    return <main className="mx-auto max-w-5xl p-8 text-[var(--color-text-secondary)]">Loading…</main>;
  }

  return (
    <main className="mx-auto max-w-7xl px-4 py-6 lg:px-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Content Writing</h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Brief, outline, draft, edit, review, and publish from one workspace.
          </p>
        </div>
        <Link
          href="/app/content"
          className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
        >
          View content documents
        </Link>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="space-y-6">
          {error ? <SeoErrorBanner error={error} /> : null}

          <section className="rounded-xl border bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between gap-3">
              <div>
                <h2 className="font-semibold">Article setup</h2>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  Start from stable project and SERP inputs. Optional niche context is additive only.
                </p>
              </div>
              <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
                {stage === 'brief'
                  ? 'Step 1 of 4'
                  : stage === 'outline'
                    ? 'Step 2 of 4'
                    : stage === 'draft'
                      ? 'Step 3 of 4'
                      : 'Step 4 of 4'}
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

              <label className="text-sm font-medium text-[var(--color-text-primary)]">
                Location
                <input
                  className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                  value={location}
                  onChange={(event) => setLocation(event.target.value)}
                />
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
                Target keyword
                <input
                  className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2"
                  value={keyword}
                  onChange={(event) => setKeyword(event.target.value)}
                  placeholder="e.g. zapier quickbooks integration"
                />
              </label>
            </div>

            <div className="mt-5 flex flex-wrap gap-3">
              <button
                type="button"
                disabled={!projectId || !keyword.trim() || !!loadingAction}
                className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                onClick={() =>
                  void run('brief', async () => {
                    const nextBrief = await generateBrief({ projectId, keyword, location }, accessToken);
                    setBrief(nextBrief);
                    setOutline('');
                    setStage('outline');
                  })
                }
              >
                {loadingAction === 'brief' ? 'Generating brief…' : 'Generate brief'}
              </button>

              <button
                type="button"
                disabled={!brief || !!loadingAction}
                className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
                onClick={() =>
                  void run('outline', async () => {
                    if (!brief) return;
                    const result = await generateOutline({ keyword, brief, title }, accessToken);
                    setOutline(result.content);
                    setStage('draft');
                  })
                }
              >
                {loadingAction === 'outline' ? 'Building outline…' : 'Generate outline'}
              </button>

              <button
                type="button"
                disabled={!brief || !outline.trim() || !!loadingAction}
                className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
                onClick={() =>
                  void run('draft', async () => {
                    if (!brief) return;
                    const result = await generateDraft(
                      {
                        keyword,
                        brief,
                        outline,
                        targetWordCount: brief.targetWordCount,
                        title,
                      },
                      accessToken,
                    );
                    const saved = await ensureDocument(result.content || DEFAULT_DRAFT_HTML);
                    await updateContentStatus(saved.id, 'awaiting_review', accessToken);
                    setDoc({ ...saved, status: 'awaiting_review' });
                    setStage('review');
                  })
                }
              >
                {loadingAction === 'draft' ? 'Drafting article…' : 'Generate draft'}
              </button>
            </div>

            {selectedProject ? (
              <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
                Project URL: {selectedProject.url}
              </p>
            ) : null}
          </section>

          {brief ? (
            <section className="rounded-xl border bg-white p-5 shadow-sm">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h2 className="font-semibold">Brief</h2>
                  <p className="text-sm text-[var(--color-text-secondary)]">
                    {brief.methodology.name} · {brief.location} · ~{brief.targetWordCount} words
                  </p>
                </div>
                <span className="rounded-full bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-medium text-[var(--color-text-secondary)]">
                  {brief.schemaBlueprint.primaryType}
                </span>
              </div>

              <div className="mt-4 grid gap-4 lg:grid-cols-2">
                <div className="rounded-lg border bg-[var(--color-surface-muted)] p-4">
                  <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
                    Methodology movements
                  </h3>
                  <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
                    Four topic-native H2 sections in this order. Corporate labels are intent guides, not required headings.
                  </p>
                  <ol className="mt-3 space-y-3">
                    {(brief.methodology.phaseDefinitions?.length
                      ? brief.methodology.phaseDefinitions
                      : brief.methodology.phases.map((label, index) => ({
                          id: `phase-${index + 1}`,
                          label,
                          intent: '',
                          headingFamilies: [] as string[],
                        }))
                    ).map((phase, index) => (
                      <li key={phase.id} className="text-sm">
                        <p className="font-medium text-[var(--color-text-primary)]">
                          {index + 1}. {phase.label}
                        </p>
                        {phase.intent ? (
                          <p className="mt-1 text-[var(--color-text-secondary)]">{phase.intent}</p>
                        ) : null}
                        {phase.headingFamilies.length > 0 ? (
                          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
                            Heading ideas: {phase.headingFamilies.join(', ')}
                          </p>
                        ) : null}
                      </li>
                    ))}
                  </ol>
                </div>
                <InfoList title="Recommended terms" items={brief.recommendedTerms} />
                <InfoList title="Movement heading hints" items={brief.suggestedHeadings} />
                <InfoList title="Competitor heading patterns" items={brief.competitorHeadingHighlights} />
                <InfoList title="Geo anchor nodes" items={brief.geoAnchorNodes} />
                <InfoList title="Competitor domains" items={brief.competitorDomains} />
                <InfoList title="Competitor schema signals" items={brief.competitorSchemaTypes} />
                <InfoList title="Related searches" items={brief.serpIntelligence.relatedSearches} />
                <InfoList title="SERP features" items={brief.serpIntelligence.featureFlags} />
                <InfoList title="Review checklist" items={brief.reviewChecklist} />
              </div>

              {brief.nicheContext.primaryNiche || brief.nicheContext.matchedPillar ? (
                <div className="mt-4 rounded-lg border bg-[var(--color-surface-muted)] p-4 text-sm">
                  <p className="font-medium text-[var(--color-text-primary)]">Optional niche context</p>
                  <p className="mt-1 text-[var(--color-text-secondary)]">
                    {brief.nicheContext.primaryNiche || 'No primary niche available'}
                  </p>
                  {brief.nicheContext.matchedPillar ? (
                    <p className="mt-2 text-[var(--color-text-primary)]">
                      Matched pillar: {brief.nicheContext.matchedPillar}
                    </p>
                  ) : null}
                </div>
              ) : null}

              {brief.serpIntelligence.featuredSnippet ? (
                <div className="mt-4 rounded-lg border bg-[var(--color-surface-muted)] p-4 text-sm">
                  <p className="font-medium text-[var(--color-text-primary)]">Featured snippet target</p>
                  <p className="mt-1 text-[var(--color-text-secondary)]">
                    {brief.serpIntelligence.featuredSnippet}
                  </p>
                </div>
              ) : null}
            </section>
          ) : null}

          {brief ? (
            <section className="rounded-xl border bg-white p-5 shadow-sm">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h2 className="font-semibold">Outline</h2>
                  <p className="text-sm text-[var(--color-text-secondary)]">
                    Edit the generated structure before drafting.
                  </p>
                </div>
              </div>
              <textarea
                className="mt-4 min-h-[220px] w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 font-mono text-sm"
                value={outline}
                onChange={(event) => setOutline(event.target.value)}
                placeholder="<h2>Business Objectives</h2>"
              />
            </section>
          ) : null}

          {doc ? (
            <ReviewWorkspace
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
            />
          ) : null}
        </div>

        <aside className="space-y-6">
          <section className="rounded-xl border bg-white p-5 shadow-sm">
            <h2 className="font-semibold">Workflow</h2>
            <ol className="mt-3 space-y-2 text-sm text-[var(--color-text-secondary)]">
              <li>1. Generate a structured brief from project + SERP context.</li>
              <li>2. Turn the brief into an outline you can edit.</li>
              <li>3. Draft the article and persist it as a document.</li>
              <li>4. Review, score, approve, and copy rendered HTML from this page.</li>
            </ol>
          </section>

          {brief ? (
            <section className="rounded-xl border bg-white p-5 shadow-sm">
              <h2 className="font-semibold">Schema blueprint</h2>
              <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                Primary type: {brief.schemaBlueprint.primaryType}
              </p>
              <InfoList title="Additional types" items={brief.schemaBlueprint.additionalTypes} compact />
              <InfoList title="Software entities" items={brief.schemaBlueprint.softwareEntities} compact />
            </section>
          ) : null}
        </aside>
      </div>
    </main>
  );
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

function ReviewWorkspace({
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
}) {
  const [html, setHtml] = useState(doc.contentHtml || DEFAULT_DRAFT_HTML);
  const [saving, setSaving] = useState(false);
  const [copyHint, setCopyHint] = useState<string | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
  const [statusUpdating, setStatusUpdating] = useState<string | null>(null);
  const [applyingSuggestionId, setApplyingSuggestionId] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const keywordRef = useRef(keyword);
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

  async function handleApplySuggestion(suggestionId: string) {
    setApplyingSuggestionId(suggestionId);
    try {
      onError(null);
      const result = await applyScoreSuggestion(doc.id, suggestionId, accessToken);
      setHtml(result.contentHtml);
      await save(result.contentHtml, keyword, title, location);
    } catch (applyError) {
      onError(applyError);
    } finally {
      setApplyingSuggestionId(null);
    }
  }

  return (
    <div className="rounded-xl border bg-white shadow-sm">
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

      <div className="flex min-h-[800px] flex-col xl:flex-row">
        <div className="flex-1 border-r-0 xl:border-r">
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
        </div>

        <div className="w-full xl:w-[380px]">
          <ScoreSidebar
            keyword={keyword}
            scoreUpdate={scoreUpdate}
            pendingReason={pendingReason}
            benchmarkRefreshing={benchmarkRefreshing}
            scoreError={scoreError}
            connected={connected}
            applyingSuggestionId={applyingSuggestionId}
            onApplySuggestion={handleApplySuggestion}
            onRefreshSerp={() => {
              void deleteSerpCache(keyword, location, accessToken).then(() => {
                void notifyKeywordChanged(html, keyword, location);
              }).catch(onError);
            }}
            onCopyHtml={() => {
              void getRenderedContentHtml(doc.id, accessToken)
                .then((result) => navigator.clipboard.writeText(result.renderedHtml))
                .then(() => {
                  setCopyHint('Rendered HTML copied');
                  setTimeout(() => setCopyHint(null), 3000);
                })
                .catch(onError);
            }}
          />

          <div className="space-y-3 border-t px-6 py-5">
            <h3 className="text-sm font-semibold">Review gate</h3>
            <button
              type="button"
              disabled={!!statusUpdating}
              className="w-full rounded-lg border px-3 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
              onClick={() => void changeStatus('awaiting_review')}
            >
              {statusUpdating === 'awaiting_review' ? 'Updating…' : 'Mark awaiting review'}
            </button>
            <button
              type="button"
              disabled={!!statusUpdating}
              className="w-full rounded-lg border px-3 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
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
        </div>
      </div>
    </div>
  );
}

function InfoList({
  title,
  items,
  compact = false,
}: {
  title: string;
  items: string[];
  compact?: boolean;
}) {
  if (items.length === 0) return null;

  return (
    <div>
      <h3 className={`font-medium ${compact ? 'text-sm' : 'text-sm'}`}>{title}</h3>
      <ul className={`mt-2 ${compact ? 'space-y-1 text-xs' : 'space-y-1.5 text-sm'} text-[var(--color-text-primary)]`}>
        {items.map((item) => (
          <li key={`${title}-${item}`} className="rounded-md bg-[var(--color-surface-muted)] px-2 py-1">
            {item}
          </li>
        ))}
      </ul>
    </div>
  );
}
