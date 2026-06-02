'use client';

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { TopicalMapGraph } from '@/components/strategy/topical-map-graph';
import { LinkingBlueprintTab } from '@/components/strategy/linking-blueprint-tab';
import {
  createContent,
  generateTopicalMap,
  getGoogleIntegrationStatus,
  getTopicalMap,
  getEntityGaps,
  type EntityGapAnalysis,
  type TopicalMapCoverage,
  type TopicalMapResult,
  type TopicalMapTopic,
} from '@/lib/seo-api';

type ViewMode = 'table' | 'map' | 'links' | 'entity-gaps';
type SortKey = 'priority' | 'impressions' | 'position' | 'volume';

function coverageStyle(coverage: TopicalMapCoverage): string {
  if (coverage === 'covered') return 'bg-green-50 text-green-800 border-green-200';
  if (coverage === 'partial') return 'bg-amber-50 text-amber-900 border-amber-200';
  if (coverage === 'opportunity') return 'bg-indigo-50 text-indigo-900 border-indigo-200';
  return 'bg-red-50 text-red-800 border-red-200';
}

type TopicalMapWorkspaceProps = {
  projectId: string;
  projectName: string;
  accessToken: string | null;
};

type GenerationMode = 'gsc' | 'seed';

export function TopicalMapWorkspace({ projectId, projectName, accessToken }: TopicalMapWorkspaceProps) {
  const [result, setResult] = useState<TopicalMapResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [initialLoad, setInitialLoad] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [gscConnected, setGscConnected] = useState<boolean | null>(null);
  const [view, setView] = useState<ViewMode>('table');
  const [coverageFilter, setCoverageFilter] = useState<TopicalMapCoverage | 'all'>('all');
  const [pillarFilter, setPillarFilter] = useState<string>('all');
  const [sortKey, setSortKey] = useState<SortKey>('priority');
  const [selected, setSelected] = useState<TopicalMapTopic | null>(null);
  const [creatingId, setCreatingId] = useState<string | null>(null);
  const [mode, setMode] = useState<GenerationMode>('gsc');
  const [seedKeyword, setSeedKeyword] = useState('');
  const [entityGaps, setEntityGaps] = useState<EntityGapAnalysis[] | null>(null);

  const loadCached = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const status = await getGoogleIntegrationStatus(projectId, accessToken);
      setGscConnected(status.connected);
      if (!status.connected) {
        setResult(null);
        setInitialLoad(false);
        return;
      }
      const cached = await getTopicalMap(projectId, accessToken);
      setResult(cached);
    } catch (err: unknown) {
      const isNotFound = err instanceof Error && err.message.includes('404');
      if (!isNotFound) {
        setError(err);
      }
      setResult(null);
    } finally {
      setLoading(false);
      setInitialLoad(false);
    }
  }, [projectId, accessToken]);

  useEffect(() => {
    setInitialLoad(true);
    setResult(null);
    setSelected(null);
    void loadCached();
  }, [loadCached]);

  useEffect(() => {
    if (view === 'entity-gaps' && projectId) {
      const loadEntityGaps = async () => {
        try {
          const gaps = await getEntityGaps(projectId, accessToken);
          setEntityGaps(gaps);
        } catch (err) {
          console.error('Failed to load entity gaps:', err);
        }
      };
      void loadEntityGaps();
    }
  }, [view, projectId, accessToken]);

  async function regenerate(force = true) {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      if (mode === 'seed') {
        if (!seedKeyword.trim()) {
          setError(new Error('Enter a seed keyword to generate map.'));
          return;
        }
        const next = await generateTopicalMap(projectId, accessToken, { seedKeyword: seedKeyword.trim() });
        setResult(next);
        setSelected(next.recommendations?.[0] ?? next.topics[0] ?? null);
      } else {
        const status = await getGoogleIntegrationStatus(projectId, accessToken);
        setGscConnected(status.connected);
        if (!status.connected) {
          setError(new Error('Connect Google Search Console under Rankings before generating a topical map.'));
          return;
        }
        const next = await generateTopicalMap(projectId, accessToken, { force });
        setResult(next);
        setSelected(next.recommendations?.[0] ?? next.topics[0] ?? null);
      }
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  }

  const pillars = useMemo(() => {
    const names = new Set(result?.topics.map((t) => t.pillarName ?? 'General') ?? []);
    return ['all', ...[...names].sort((a, b) => a.localeCompare(b))];
  }, [result?.topics]);

  const filteredTopics = useMemo(() => {
    let list = result?.topics ?? [];
    if (coverageFilter !== 'all') {
      list = list.filter((t) => t.coverage === coverageFilter);
    }
    if (pillarFilter !== 'all') {
      list = list.filter((t) => (t.pillarName ?? 'General') === pillarFilter);
    }
    return [...list].sort((a, b) => {
      if (sortKey === 'priority') return (b.priorityScore ?? 0) - (a.priorityScore ?? 0);
      if (sortKey === 'impressions') return b.totalImpressions - a.totalImpressions;
      if (sortKey === 'position') return (a.averagePosition ?? 99) - (b.averagePosition ?? 99);
      return (b.searchVolume ?? 0) - (a.searchVolume ?? 0);
    });
  }, [result?.topics, coverageFilter, pillarFilter, sortKey]);

  async function writeTopic(topic: TopicalMapTopic) {
    const keyword = topic.mainKeyword ?? topic.queries[0] ?? topic.name;
    setCreatingId(topic.name);
    setError(null);
    try {
      const doc = await createContent(
        { projectId, title: topic.name, targetKeyword: keyword },
        accessToken,
      );
      window.location.href = `/app/content/${doc.id}`;
    } catch (err) {
      setError(err);
      setCreatingId(null);
    }
  }

  const summary = result
    ? {
        covered: result.coveredCount,
        partial: result.partialCount,
        gap: result.gapCount,
        opportunity: result.opportunityCount ?? 0,
      }
    : null;

  return (
    <div className="space-y-6">
      <div className="rounded-lg border bg-white p-4 space-y-3">
        <div className="flex flex-wrap items-center gap-3">
          <span className="text-sm font-medium">Mode:</span>
          <div className="inline-flex rounded-lg border p-0.5 text-sm">
            <button
              type="button"
              className={`rounded-md px-3 py-1.5 ${mode === 'gsc' ? 'bg-[var(--color-accent)] text-white' : ''}`}
              onClick={() => setMode('gsc')}
            >
              GSC / Rankings
            </button>
            <button
              type="button"
              className={`rounded-md px-3 py-1.5 ${mode === 'seed' ? 'bg-[var(--color-accent)] text-white' : ''}`}
              onClick={() => setMode('seed')}
            >
              Seed keyword
            </button>
          </div>
        </div>
        {mode === 'seed' ? (
          <input
            type="text"
            placeholder="Enter seed keyword (e.g., 'WordPress plugins')"
            value={seedKeyword}
            onChange={(e) => setSeedKeyword(e.target.value)}
            className="w-full rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"
          />
        ) : null}
      </div>

      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm text-[var(--color-text-secondary)]">
            Project: <span className="font-medium text-[var(--color-text-primary)]">{projectName}</span>
            {result?.version === 2 ? (
              <span className="ml-2 rounded-full bg-green-50 px-2 py-0.5 text-xs text-green-800">Strategy map v2</span>
            ) : null}
          </p>
          {result?.generatedAt ? (
            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              Generated {new Date(result.generatedAt).toLocaleString()}
              {result.expiresAt ? ` · refreshes ${new Date(result.expiresAt).toLocaleDateString()}` : ''}
            </p>
          ) : null}
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            disabled={loading || !projectId || (mode === 'seed' && !seedKeyword.trim())}
            onClick={() => void regenerate(true)}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {loading ? 'Working…' : result ? 'Refresh map' : 'Generate map'}
          </button>
          <Link href="/app/planner" className="rounded-lg border px-4 py-2 text-sm hover:bg-slate-50">
            Keyword planner
          </Link>
          <Link href="/app/rankings" className="rounded-lg border px-4 py-2 text-sm hover:bg-slate-50">
            Rankings / GSC
          </Link>
        </div>
      </div>

      <SeoErrorBanner error={error} />

      {gscConnected === false ? (
        <p className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Google Search Console is not connected for this project.{' '}
          <Link href="/app/rankings" className="font-medium underline">
            Connect GSC
          </Link>{' '}
          to build a topical map from real query data.
        </p>
      ) : null}

      {summary ? (
        <div className="flex flex-wrap gap-2 text-sm">
          <span className="rounded-full border border-green-200 bg-green-50 px-3 py-1">{summary.covered} covered</span>
          <span className="rounded-full border border-amber-200 bg-amber-50 px-3 py-1">{summary.partial} partial</span>
          <span className="rounded-full border border-red-200 bg-red-50 px-3 py-1">{summary.gap} gaps</span>
          {summary.opportunity > 0 ? (
            <span className="rounded-full border border-indigo-200 bg-indigo-50 px-3 py-1">
              {summary.opportunity} opportunities
            </span>
          ) : null}
        </div>
      ) : null}

      {result?.recommendations && result.recommendations.length > 0 ? (
        <section className="rounded-xl border bg-[var(--color-surface)] p-4">
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Do this next</h2>
          <ul className="mt-3 space-y-2">
            {result.recommendations.map((topic) => (
              <li key={`rec-${topic.name}`} className="flex flex-wrap items-center justify-between gap-2 text-sm">
                <button
                  type="button"
                  className="text-left font-medium text-[var(--color-brand)] hover:underline"
                  onClick={() => setSelected(topic)}
                >
                  {topic.name}
                </button>
                <span className="text-xs text-[var(--color-text-muted)]">
                  Priority {topic.priorityScore ?? '—'} · {topic.coverage}
                </span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {result?.quickWins && result.quickWins.length > 0 ? (
        <section className="rounded-xl border bg-[var(--color-surface)] p-4">
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Quick wins</h2>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">Fastest-ranking opportunities (low KD, good volume)</p>
          <ul className="mt-3 space-y-2">
            {result.quickWins.map((win) => (
              <li key={`quickwin-${win.topicName}`} className="rounded-lg border border-green-200 bg-green-50 p-2 text-xs">
                <div className="font-medium text-green-900">{win.topicName}</div>
                <div className="mt-1 text-green-800">{win.reason}</div>
                {win.searchVolume && <div className="mt-1 text-green-700">{win.searchVolume.toLocaleString()} vol</div>}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {result?.semanticEntities && result.semanticEntities.length > 0 ? (
        <section className="rounded-xl border bg-[var(--color-surface)] p-4">
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Semantic entities</h2>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">Key concepts required for topical authority</p>
          <ul className="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
            {result.semanticEntities.map((entity) => (
              <li key={`entity-${entity.name}`} className="rounded-lg border border-indigo-200 bg-indigo-50 p-2 text-xs">
                <div className="font-medium text-indigo-900">{entity.name}</div>
                <div className="text-indigo-700">{entity.type}</div>
                {entity.pillarRefs && entity.pillarRefs.length > 0 && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {entity.pillarRefs.map((ref) => (
                      <span key={`${entity.name}-${ref}`} className="rounded bg-indigo-200 px-1 text-indigo-900">{ref}</span>
                    ))}
                  </div>
                )}
              </li>
            ))}
          </ul>
        </section>
      ) : null}


      {initialLoad ? (
        <p className="text-sm text-[var(--color-text-muted)]">Loading cached map…</p>
      ) : null}

      {!initialLoad && !result && gscConnected ? (
        <div className="rounded-xl border border-dashed p-8 text-center">
          <p className="text-sm text-[var(--color-text-secondary)]">
            No topical map yet. Generate to cluster GSC queries (SERP-assisted for ambiguous terms).
          </p>
          <button
            type="button"
            onClick={() => regenerate()}
            disabled={loading}
            className="mt-4 inline-block rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {loading ? 'Generating…' : 'Generate map'}
          </button>
        </div>
      ) : null}

      {result ? (
        <>
          <div className="flex flex-wrap items-center gap-3">
            <div className="inline-flex rounded-lg border p-0.5 text-sm">
              <button
                type="button"
                className={`rounded-md px-3 py-1.5 ${view === 'table' ? 'bg-[var(--color-accent)] text-white' : ''}`}
                onClick={() => setView('table')}
              >
                Table
              </button>
              <button
                type="button"
                className={`rounded-md px-3 py-1.5 ${view === 'map' ? 'bg-[var(--color-accent)] text-white' : ''}`}
                onClick={() => setView('map')}
              >
                Map
              </button>
              <button
                type="button"
                className={`rounded-md px-3 py-1.5 ${view === 'links' ? 'bg-[var(--color-accent)] text-white' : ''}`}
                onClick={() => setView('links')}
              >
                Internal Links
              </button>
              <button
                type="button"
                className={`rounded-md px-3 py-1.5 ${view === 'entity-gaps' ? 'bg-[var(--color-accent)] text-white' : ''}`}
                onClick={() => setView('entity-gaps')}
              >
                Entity Gaps
              </button>
            </div>
            <label className="text-xs font-medium text-[var(--color-text-secondary)]">
              Coverage
              <select
                className="ml-2 rounded border px-2 py-1 text-sm"
                value={coverageFilter}
                onChange={(e) => setCoverageFilter(e.target.value as TopicalMapCoverage | 'all')}
              >
                <option value="all">All</option>
                <option value="gap">Gaps</option>
                <option value="opportunity">Opportunities</option>
                <option value="partial">Partial</option>
                <option value="covered">Covered</option>
              </select>
            </label>
            <label className="text-xs font-medium text-[var(--color-text-secondary)]">
              Pillar
              <select
                className="ml-2 rounded border px-2 py-1 text-sm"
                value={pillarFilter}
                onChange={(e) => setPillarFilter(e.target.value)}
              >
                {pillars.map((p) => (
                  <option key={p} value={p}>
                    {p === 'all' ? 'All pillars' : p}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-xs font-medium text-[var(--color-text-secondary)]">
              Sort
              <select
                className="ml-2 rounded border px-2 py-1 text-sm"
                value={sortKey}
                onChange={(e) => setSortKey(e.target.value as SortKey)}
              >
                <option value="priority">Priority</option>
                <option value="impressions">Impressions</option>
                <option value="position">Avg position</option>
                <option value="volume">Search volume</option>
              </select>
            </label>
          </div>

          <div className={`grid gap-6 ${view === 'links' ? '' : 'xl:grid-cols-[1fr_320px]'}`}>
            <div>
              {view === 'map' ? (
                <TopicalMapGraph
                  topics={filteredTopics}
                  selectedName={selected?.name ?? null}
                  onSelect={setSelected}
                />
              ) : view === 'links' ? (
                <div className="rounded-xl border bg-white p-6">
                  <LinkingBlueprintTab projectId={projectId} accessToken={accessToken} />
                </div>
              ) : view === 'entity-gaps' ? (
                <div className="overflow-x-auto rounded-xl border bg-white">
                  <table className="min-w-full text-left text-sm">
                    <thead className="border-b bg-[var(--color-surface)] text-xs uppercase text-[var(--color-text-muted)]">
                      <tr>
                        <th className="px-3 py-2">Topic</th>
                        <th className="px-3 py-2">Coverage %</th>
                        <th className="px-3 py-2">Gap Count</th>
                        <th className="px-3 py-2">Missing Entities</th>
                      </tr>
                    </thead>
                    <tbody>
                      {entityGaps && entityGaps.length > 0 ? (
                        entityGaps.map((gap) => (
                          <tr key={gap.name} className="border-b hover:bg-slate-50">
                            <td className="px-3 py-2 font-medium">{gap.name}</td>
                            <td className="px-3 py-2 tabular-nums">{(gap.entityCoverage * 100).toFixed(1)}%</td>
                            <td className="px-3 py-2 tabular-nums text-red-600 font-medium">{gap.gapCount}</td>
                            <td className="px-3 py-2 text-xs">
                              <div className="flex flex-wrap gap-1">
                                {gap.entityGaps.slice(0, 3).map((entity) => (
                                  <span
                                    key={entity}
                                    className="rounded-full bg-red-50 border border-red-200 px-2 py-0.5 text-red-700"
                                  >
                                    {entity}
                                  </span>
                                ))}
                                {gap.entityGaps.length > 3 && (
                                  <span className="rounded-full bg-red-50 border border-red-200 px-2 py-0.5 text-red-700">
                                    +{gap.entityGaps.length - 3}
                                  </span>
                                )}
                              </div>
                            </td>
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td colSpan={4} className="px-3 py-4 text-center text-sm text-[var(--color-text-secondary)]">
                            No entity gaps found
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="overflow-x-auto rounded-xl border bg-white">
                  <table className="min-w-full text-left text-sm">
                    <thead className="border-b bg-[var(--color-surface)] text-xs uppercase text-[var(--color-text-muted)]">
                      <tr>
                        <th className="px-3 py-2">Topic</th>
                        <th className="px-3 py-2">Coverage</th>
                        <th className="px-3 py-2">Priority</th>
                        <th className="px-3 py-2">Impressions</th>
                        <th className="px-3 py-2">Position</th>
                        <th className="px-3 py-2">Volume</th>
                        <th className="px-3 py-2">KD</th>
                        <th className="px-3 py-2">Pillar</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredTopics.map((topic) => (
                        <tr
                          key={`${topic.name}-${topic.matchedPageUrl ?? topic.queries[0]}`}
                          className={`cursor-pointer border-b hover:bg-slate-50 ${selected?.name === topic.name ? 'bg-green-50/50' : ''}`}
                          onClick={() => setSelected(topic)}
                        >
                          <td className="px-3 py-2 font-medium">
                            <div className="flex flex-wrap items-center gap-2">
                              {topic.name}
                              {topic.isDuplicate && (
                                <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800">
                                  Duplicate of {topic.duplicateOf}
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="px-3 py-2">
                            <span className={`rounded-full border px-2 py-0.5 text-xs ${coverageStyle(topic.coverage)}`}>
                              {topic.coverage}
                            </span>
                          </td>
                          <td className="px-3 py-2 tabular-nums">{topic.priorityScore ?? '—'}</td>
                          <td className="px-3 py-2 tabular-nums">{topic.totalImpressions.toLocaleString()}</td>
                          <td className="px-3 py-2 tabular-nums">{topic.averagePosition?.toFixed(1) ?? '—'}</td>
                          <td className="px-3 py-2 tabular-nums">{topic.searchVolume?.toLocaleString() ?? '—'}</td>
                          <td className="px-3 py-2 tabular-nums">{topic.keywordDifficulty ?? '—'}</td>
                          <td className="px-3 py-2 text-xs">{topic.pillarName ?? '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {view !== 'links' ? <aside className="rounded-xl border bg-white p-4 shadow-sm">
              {selected ? (
                <>
                  <div className="flex items-start justify-between gap-2">
                    <h2 className="font-semibold">{selected.name}</h2>
                    <span className={`shrink-0 rounded-full border px-2 py-0.5 text-xs ${coverageStyle(selected.coverage)}`}>
                      {selected.coverage}
                    </span>
                  </div>
                  <dl className="mt-3 space-y-2 text-xs text-[var(--color-text-secondary)]">
                    <div className="flex justify-between gap-2">
                      <dt>Priority</dt>
                      <dd className="font-medium tabular-nums">{selected.priorityScore ?? '—'}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt>Cluster method</dt>
                      <dd>{selected.clusterMethod ?? '—'}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt>Main keyword</dt>
                      <dd className="text-right">{selected.mainKeyword ?? '—'}</dd>
                    </div>
                    {selected.intent ? (
                      <div className="flex justify-between gap-2">
                        <dt>Intent</dt>
                        <dd>{selected.intent}</dd>
                      </div>
                    ) : null}
                  </dl>
                  {selected.matchedPageUrl ? (
                    <p className="mt-3 text-xs">
                      <a
                        href={selected.matchedPageUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-[var(--color-brand)] hover:underline break-all"
                      >
                        {selected.matchedPageUrl.replace(/^https?:\/\/(www\.)?/, '')}
                      </a>
                    </p>
                  ) : null}
                  {selected.competitorDomains && selected.competitorDomains.length > 0 ? (
                    <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
                      Competitors: {selected.competitorDomains.join(', ')}
                    </p>
                  ) : null}
                  <ul className="mt-3 max-h-32 overflow-y-auto text-xs text-[var(--color-text-secondary)]">
                    {selected.queries.map((q) => (
                      <li key={q} className="truncate">
                        {q}
                      </li>
                    ))}
                  </ul>
                  {selected.coverage !== 'covered' ? (
                    <button
                      type="button"
                      disabled={creatingId === selected.name}
                      onClick={() => void writeTopic(selected)}
                      className="mt-4 w-full rounded-lg border px-3 py-2 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
                    >
                      {creatingId === selected.name ? 'Creating…' : 'Write this topic'}
                    </button>
                  ) : null}
                  {selected.mainKeyword ? (
                    <Link
                      href={`/app/serp?q=${encodeURIComponent(selected.mainKeyword)}`}
                      className="mt-2 block text-center text-xs text-[var(--color-brand)] hover:underline"
                    >
                      Deep SERP →
                    </Link>
                  ) : null}
                </>
              ) : (
                <p className="text-sm text-[var(--color-text-muted)]">Select a topic from the table or map.</p>
              )}
            </aside> : null}
          </div>
        </>
      ) : null}
    </div>
  );
}
