'use client';

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import {
  createSiteAnalyzerPack,
  getSiteAnalyzerProjectState,
  listProjects,
  runSiteAnalyzerPackStep,
  runSiteIndexStep,
  type SeoProject,
  type SiteAnalyzerPackSummary,
  type SiteAnalyzerProjectState,
  type SiteAnalyzerStepState,
  type SiteAnalyzerStepStatus,
} from '@/lib/seo-api';

const SITE_INDEX_STEPS: Array<{ step: number; title: string; subtitle: string }> = [
  { step: 1, title: 'Discover URLs', subtitle: 'Sitemap and seed URL discovery' },
  { step: 2, title: 'Crawl pages', subtitle: 'Fetch HTML for site pages (cap 50)' },
  { step: 3, title: 'Extract signals', subtitle: 'Headings and JSON-LD per page' },
  { step: 4, title: 'Site summary', subtitle: 'Business summary and internal link map' },
];

const PACK_STEPS: Array<{ step: number; title: string; subtitle: string }> = [
  { step: 5, title: 'Fetch SERP', subtitle: 'Organic, PAA, PASF, and PAF snapshot' },
  { step: 6, title: 'Crawl competitors', subtitle: 'Competitor pages and headings' },
  { step: 7, title: 'Benchmarks & terms', subtitle: 'Length targets and recommended terms' },
  { step: 8, title: 'Structure', subtitle: 'Intent, section hints, and closing FAQs' },
  { step: 9, title: 'Merge site context', subtitle: 'Cross-reference site index with keyword pack' },
  { step: 10, title: 'Finalize', subtitle: 'Validate all gates and mark pack complete' },
];

function statusStyles(status: SiteAnalyzerStepStatus): string {
  if (status === 'green') return 'bg-emerald-100 text-emerald-800';
  if (status === 'red') return 'bg-red-100 text-red-800';
  if (status === 'running') return 'bg-amber-100 text-amber-900';
  return 'bg-slate-100 text-slate-700';
}

function stepUnlocked(
  steps: SiteAnalyzerStepState[],
  stepNumber: number,
  priorComplete: boolean,
): boolean {
  if (stepNumber === 1 || stepNumber === 5) return priorComplete;
  const prev = steps.find((s) => s.stepNumber === stepNumber - 1);
  return priorComplete && prev?.status === 'green';
}

function mergeStepDefinitions(
  definitions: Array<{ step: number; title: string; subtitle: string }>,
  remote: SiteAnalyzerStepState[] | undefined,
): SiteAnalyzerStepState[] {
  const steps = remote ?? [];
  return definitions.map((def) => {
    const existing = steps.find((s) => s.stepNumber === def.step);
    return {
      stepNumber: def.step,
      title: def.title,
      status: existing?.status ?? 'pending',
      message: existing?.message ?? '',
      log: existing?.log ?? null,
      counts: existing?.counts ?? null,
      updatedAt: existing?.updatedAt ?? null,
    };
  });
}

function StepRow({
  step,
  subtitle,
  canRun,
  running,
  onRun,
}: {
  step: SiteAnalyzerStepState;
  subtitle: string;
  canRun: boolean;
  running: boolean;
  onRun: () => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const hasOutput = Boolean(step.log || step.message || step.counts);

  return (
    <li className="rounded-lg border border-[var(--color-border)] bg-white">
      <div className="flex flex-wrap items-start gap-3 px-4 py-3">
        <span
          className={`mt-0.5 shrink-0 rounded-full px-2 py-0.5 text-xs font-medium ${statusStyles(step.status)}`}
        >
          {step.status}
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium text-[var(--color-text-primary)]">
            Step {step.stepNumber}: {step.title}
          </p>
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">{subtitle}</p>
          {step.message ? (
            <p
              className={`mt-1 text-xs ${step.status === 'red' ? 'text-red-700' : 'text-[var(--color-text-secondary)]'}`}
            >
              {step.message}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 items-center gap-2">
          {hasOutput ? (
            <button
              type="button"
              onClick={() => setExpanded((v) => !v)}
              className="flex items-center gap-1 rounded-lg border px-2 py-1 text-xs hover:bg-[var(--color-surface-muted)]"
            >
              {expanded ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
              Log
            </button>
          ) : null}
          <button
            type="button"
            disabled={!canRun || running || step.status === 'green'}
            onClick={onRun}
            className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {running ? 'Running…' : 'Run'}
          </button>
        </div>
      </div>
      {expanded && hasOutput ? (
        <div className="border-t bg-[var(--color-surface-muted)]/40 px-4 py-3">
          {step.counts && Object.keys(step.counts).length > 0 ? (
            <dl className="mb-3 grid gap-1 text-xs sm:grid-cols-2">
              {Object.entries(step.counts).map(([key, value]) => (
                <div key={key}>
                  <dt className="font-medium text-[var(--color-text-muted)]">{key}</dt>
                  <dd className="break-words text-[var(--color-text-secondary)]">{String(value)}</dd>
                </div>
              ))}
            </dl>
          ) : null}
          {step.log ? (
            <pre className="max-h-48 overflow-auto whitespace-pre-wrap font-mono text-xs text-[var(--color-text-secondary)]">
              {step.log}
            </pre>
          ) : null}
        </div>
      ) : null}
    </li>
  );
}

type SiteAnalyzerWorkspaceProps = {
  accessToken: string | null;
  initialProjectId?: string;
  initialPackId?: string;
};

export function SiteAnalyzerWorkspace({
  accessToken,
  initialProjectId = '',
  initialPackId = '',
}: SiteAnalyzerWorkspaceProps) {
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState(initialProjectId);
  const [state, setState] = useState<SiteAnalyzerProjectState | null>(null);
  const [selectedPackId, setSelectedPackId] = useState(initialPackId);
  const [keyword, setKeyword] = useState('');
  const [location, setLocation] = useState('United States');
  const [loadingState, setLoadingState] = useState(false);
  const [runningStep, setRunningStep] = useState<number | null>(null);
  const [creatingPack, setCreatingPack] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const selectedProject = useMemo(
    () => projects.find((p) => p.id === projectId) ?? null,
    [projectId, projects],
  );

  const refreshState = useCallback(async () => {
    if (!projectId) {
      setState(null);
      return;
    }
    setLoadingState(true);
    try {
      const next = await getSiteAnalyzerProjectState(projectId, accessToken);
      setState(next);
      const packs = next.packs ?? [];
      if (!selectedPackId && packs[0]) {
        setSelectedPackId(packs[0].urlResearchId);
      }
    } catch (loadError) {
      setError(loadError);
    } finally {
      setLoadingState(false);
    }
  }, [accessToken, projectId, selectedPackId]);

  useEffect(() => {
    let cancelled = false;
    async function loadProjects() {
      try {
        const list = await listProjects(accessToken);
        if (cancelled) return;
        setProjects(list);
        if (!projectId && list[0]) {
          setProjectId(list[0].id);
          setLocation(list[0].defaultLocation || 'United States');
        }
      } catch (loadError) {
        if (!cancelled) setError(loadError);
      }
    }
    void loadProjects();
    return () => {
      cancelled = true;
    };
  }, [accessToken, projectId]);

  useEffect(() => {
    if (!projectId) return;
    void refreshState();
  }, [projectId, refreshState]);

  useEffect(() => {
    if (selectedProject?.defaultLocation) {
      setLocation(selectedProject.defaultLocation);
    }
  }, [selectedProject?.defaultLocation, selectedProject?.id]);

  const siteIndexSteps = useMemo(
    () => mergeStepDefinitions(SITE_INDEX_STEPS, state?.siteIndexSteps ?? []),
    [state?.siteIndexSteps],
  );

  const packs = state?.packs ?? [];

  const selectedPack: SiteAnalyzerPackSummary | null = useMemo(
    () => packs.find((p) => p.urlResearchId === selectedPackId) ?? null,
    [packs, selectedPackId],
  );

  const packSteps = useMemo(
    () => mergeStepDefinitions(PACK_STEPS, selectedPack?.steps ?? []),
    [selectedPack?.steps],
  );

  async function handleRunSiteIndexStep(stepNumber: number) {
    if (!projectId) return;
    setRunningStep(stepNumber);
    setError(null);
    try {
      await runSiteIndexStep(projectId, stepNumber, accessToken);
      await refreshState();
    } catch (runError) {
      setError(runError);
      await refreshState();
    } finally {
      setRunningStep(null);
    }
  }

  async function handleCreatePack(e: React.FormEvent) {
    e.preventDefault();
    if (!projectId || !keyword.trim()) return;
    setCreatingPack(true);
    setError(null);
    try {
      const created = await createSiteAnalyzerPack(
        projectId,
        { keyword: keyword.trim(), location: location.trim() || undefined },
        accessToken,
      );
      setSelectedPackId(created.urlResearchId);
      setKeyword('');
      await refreshState();
    } catch (createError) {
      setError(createError);
    } finally {
      setCreatingPack(false);
    }
  }

  async function handleRunPackStep(stepNumber: number) {
    if (!selectedPackId) return;
    setRunningStep(stepNumber);
    setError(null);
    try {
      await runSiteAnalyzerPackStep(selectedPackId, stepNumber, accessToken);
      await refreshState();
    } catch (runError) {
      setError(runError);
      await refreshState();
    } finally {
      setRunningStep(null);
    }
  }

  const handoffPack = packs.find((p) => p.handoffReady || p.dataQuality === 'full') ?? null;

  return (
    <div className="mx-auto max-w-4xl">
      <h1 className="text-2xl font-semibold">Site Analyzer</h1>
      <p className="mt-1 max-w-2xl text-sm text-[var(--color-text-secondary)]">
        Crawl your site, then build keyword research packs step by step. Content Writing unlocks only
        when a pack passes all gates (step 10 green).
      </p>

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      <div className="mt-6 rounded-xl border bg-white p-5 shadow-sm">
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Project
          <select
            className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
            value={projectId}
            onChange={(e) => {
              setProjectId(e.target.value);
              setSelectedPackId('');
              setState(null);
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
        {selectedProject ? (
          <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
            Site: <span className="font-mono">{selectedProject.url}</span>
          </p>
        ) : null}
      </div>

      {projectId ? (
        <>
          <section className="mt-8">
            <div className="flex items-center justify-between gap-3">
              <div>
                <h2 className="text-lg font-semibold">Site index</h2>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  Steps 1–4 — crawl and understand your site before keyword research.
                </p>
              </div>
              <button
                type="button"
                onClick={() => void refreshState()}
                disabled={loadingState}
                className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] disabled:opacity-50"
              >
                {loadingState ? 'Refreshing…' : 'Refresh'}
              </button>
            </div>
            <ul className="mt-4 space-y-3">
              {siteIndexSteps.map((step) => (
                <StepRow
                  key={step.stepNumber}
                  step={step}
                  subtitle={SITE_INDEX_STEPS.find((d) => d.step === step.stepNumber)?.subtitle ?? ''}
                  canRun={stepUnlocked(siteIndexSteps, step.stepNumber, Boolean(projectId))}
                  running={runningStep === step.stepNumber}
                  onRun={() => void handleRunSiteIndexStep(step.stepNumber)}
                />
              ))}
            </ul>
          </section>

          <section className="mt-10">
            <h2 className="text-lg font-semibold">Keyword pack</h2>
            <p className="text-sm text-[var(--color-text-secondary)]">
              Steps 5–10 — SERP research for one keyword, merged with your site index.
            </p>

            {!state?.siteIndexComplete ? (
              <p className="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-950">
                Finish site index steps 1–4 before creating a keyword pack.
              </p>
            ) : (
              <form onSubmit={handleCreatePack} className="mt-4 flex flex-wrap items-end gap-3">
                <label className="min-w-[12rem] flex-1 text-sm font-medium">
                  Keyword
                  <input
                    className="mt-1 block w-full rounded-lg border px-3 py-2 text-sm"
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    placeholder="e.g. quickbooks automation"
                    required
                  />
                </label>
                <label className="min-w-[10rem] flex-1 text-sm font-medium">
                  Location
                  <input
                    className="mt-1 block w-full rounded-lg border px-3 py-2 text-sm"
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                  />
                </label>
                <button
                  type="submit"
                  disabled={creatingPack || !keyword.trim()}
                  className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
                >
                  {creatingPack ? 'Creating…' : 'New pack'}
                </button>
              </form>
            )}

            {packs.length ? (
              <label className="mt-4 block text-sm font-medium">
                Active pack
                <select
                  className="mt-1 block w-full rounded-lg border px-3 py-2 text-sm"
                  value={selectedPackId}
                  onChange={(e) => setSelectedPackId(e.target.value)}
                >
                  <option value="">Select a pack</option>
                  {packs.map((pack) => (
                    <option key={pack.urlResearchId} value={pack.urlResearchId}>
                      {pack.keyword} — {pack.dataQuality ?? pack.status}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}

            {selectedPack ? (
              <ul className="mt-4 space-y-3">
                {packSteps.map((step) => (
                  <StepRow
                    key={step.stepNumber}
                    step={step}
                    subtitle={PACK_STEPS.find((d) => d.step === step.stepNumber)?.subtitle ?? ''}
                    canRun={stepUnlocked(
                      packSteps,
                      step.stepNumber,
                      Boolean(state?.siteIndexComplete && selectedPackId),
                    )}
                    running={runningStep === step.stepNumber}
                    onRun={() => void handleRunPackStep(step.stepNumber)}
                  />
                ))}
              </ul>
            ) : null}

            {handoffPack && projectId ? (
              <div className="mt-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-emerald-950">Pack ready for Content Writing</p>
                  <p className="text-xs text-emerald-800">
                    Keyword: {handoffPack.keyword} · quality: {handoffPack.dataQuality ?? 'full'}
                  </p>
                </div>
                <Link
                  href={`/content-writing?projectId=${encodeURIComponent(projectId)}&urlResearchId=${encodeURIComponent(handoffPack.urlResearchId)}`}
                  className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-sm text-white hover:bg-[var(--color-accent-hover)]"
                >
                  Write with this research
                </Link>
              </div>
            ) : null}
          </section>
        </>
      ) : (
        <p className="mt-8 text-sm text-[var(--color-text-secondary)]">Select a project to begin.</p>
      )}
    </div>
  );
}
