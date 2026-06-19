'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
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
  type SiteAnalyzerStepRunResponse,
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
  { step: 10, title: 'Open Content Writing', subtitle: 'Finalize frozen research and go to Content Writing (requires steps 5–9 green; no validation on this step)' },
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
  if (!priorComplete) return false;
  const minStep = stepNumber <= 4 ? 1 : 5;
  if (stepNumber === minStep) return true;
  for (let n = minStep; n < stepNumber; n++) {
    const row = steps.find((s) => s.stepNumber === n);
    if (!row || row.status === 'red' || row.status !== 'green') return false;
  }
  return true;
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
      validationMessage:
        existing?.validationMessage ??
        (existing?.status === 'red' && existing?.message ? existing.message : null),
      log: existing?.log ?? null,
      counts: existing?.counts ?? null,
      updatedAt: existing?.updatedAt ?? null,
    };
  });
}

function stepTitle(stepNumber: number): string {
  const def =
    stepNumber <= 4
      ? SITE_INDEX_STEPS.find((d) => d.step === stepNumber)
      : PACK_STEPS.find((d) => d.step === stepNumber);
  return def?.title ?? `Step ${stepNumber}`;
}

function stepRunToState(result: SiteAnalyzerStepRunResponse): SiteAnalyzerStepState {
  const validationMessage =
    result.validationMessage ??
    (result.status === 'red' ? result.message : null);
  return {
    stepNumber: result.stepNumber,
    title: stepTitle(result.stepNumber),
    status: result.status,
    message: result.message,
    validationMessage,
    log: result.log ?? null,
    counts: result.counts ?? null,
    updatedAt: null,
  };
}

function upsertRemoteStep(
  remote: SiteAnalyzerStepState[],
  update: SiteAnalyzerStepState,
): SiteAnalyzerStepState[] {
  const idx = remote.findIndex((s) => s.stepNumber === update.stepNumber);
  if (idx >= 0) {
    const next = [...remote];
    next[idx] = { ...next[idx], ...update, title: update.title };
    return next;
  }
  return [...remote, update];
}

function shouldKeepLocalStepRun(
  remote: SiteAnalyzerStepState | undefined,
  local: SiteAnalyzerStepState,
): boolean {
  if (local.status !== 'green' && local.status !== 'red') return false;
  if (!remote || remote.status === 'pending') return true;
  if (remote.status === 'running') return true;
  return false;
}

function stepRunLostOnServerMessage(
  run: SiteAnalyzerStepRunResponse,
  packId?: string,
): string {
  const scope = run.stepNumber <= 4 ? 'site index' : 'keyword pack';
  const packHint = packId && run.stepNumber > 4 ? ` (pack ${packId})` : '';
  const detail =
    run.validationMessage ??
    (run.status === 'red' ? run.message : null) ??
    run.message;
  const outcome = run.status === 'green' ? 'completed' : 'failed validation';
  return `Step ${run.stepNumber} ${outcome} on the server, but step status was not saved${packHint}. ${scope} step-run could not be loaded after refresh — deploy GeekRepository step-runs if missing.${detail ? ` Response: ${detail}` : ''}`;
}

function reinforceStepRunAfterRefresh(
  refreshed: SiteAnalyzerProjectState,
  run: SiteAnalyzerStepRunResponse | null,
  packId?: string,
): { state: SiteAnalyzerProjectState; lostOnServer: boolean } {
  if (!run || (run.status !== 'green' && run.status !== 'red')) {
    return { state: refreshed, lostOnServer: false };
  }
  let remote: SiteAnalyzerStepState | undefined;
  if (run.stepNumber <= 4) {
    remote = refreshed.siteIndexSteps?.find((s) => s.stepNumber === run.stepNumber);
  } else {
    const pack = refreshed.packs.find((p) => p.urlResearchId === packId);
    remote = pack?.steps?.find((s) => s.stepNumber === run.stepNumber);
  }
  if (!shouldKeepLocalStepRun(remote, stepRunToState(run))) {
    return { state: refreshed, lostOnServer: false };
  }
  return {
    state: applyStepRunToProjectState(refreshed, run, packId),
    lostOnServer: true,
  };
}

function applyStepRunToProjectState(
  state: SiteAnalyzerProjectState,
  result: SiteAnalyzerStepRunResponse,
  packId?: string,
): SiteAnalyzerProjectState {
  const update = stepRunToState(result);
  if (result.stepNumber <= 4) {
    const siteIndexSteps = mergeStepDefinitions(
      SITE_INDEX_STEPS,
      upsertRemoteStep(state.siteIndexSteps ?? [], update),
    );
    const firstRed = siteIndexSteps.find((s) => s.status === 'red')?.stepNumber ?? null;
    const siteIndexComplete =
      siteIndexSteps.length >= 4 && siteIndexSteps.every((s) => s.status === 'green');
    return {
      ...state,
      siteIndexSteps,
      siteIndexComplete,
      firstRedSiteIndexStep: firstRed,
    };
  }
  if (!packId) return state;
  const packs = state.packs.map((pack) => {
    if (pack.urlResearchId !== packId) return pack;
    const steps = mergeStepDefinitions(
      PACK_STEPS,
      upsertRemoteStep(pack.steps ?? [], update),
    );
    const firstRedStep = steps.find((s) => s.status === 'red')?.stepNumber ?? null;
    return { ...pack, steps, firstRedStep };
  });
  return { ...state, packs };
}

function parseBlockedPriorStep(message: string): { priorStep: number; detail: string } | null {
  const passMatch = /^Step (\d+) must pass before running step \d+: (.+)$/.exec(message);
  if (passMatch) {
    return { priorStep: Number(passMatch[1]), detail: passMatch[2] };
  }
  const greenMatch = /^Step (\d+) must be green before running step \d+\.?$/i.exec(message.trim());
  if (greenMatch) {
    return {
      priorStep: Number(greenMatch[1]),
      detail: `Step ${greenMatch[1]} is not recorded as complete on the server. Re-run step ${greenMatch[1]} or refresh after deploy.`,
    };
  }
  return null;
}

function failureRunFromError(
  stepNumber: number,
  message: string,
): SiteAnalyzerStepRunResponse {
  const blocked = parseBlockedPriorStep(message);
  const targetStep = blocked?.priorStep ?? stepNumber;
  const detail = blocked?.detail ?? message;
  return {
    stepNumber: targetStep,
    status: 'red',
    message: detail,
    validationMessage: detail,
    log: message,
  };
}

function StepRow({
  step,
  subtitle,
  canRun,
  running,
  onRun,
  allowRunWhenGreen = false,
  greenRunLabel = 'Run',
}: {
  step: SiteAnalyzerStepState;
  subtitle: string;
  canRun: boolean;
  running: boolean;
  onRun: () => void;
  allowRunWhenGreen?: boolean;
  greenRunLabel?: string;
}) {
  const [expanded, setExpanded] = useState(step.status === 'red');
  const validationMessage =
    step.validationMessage ?? (step.status === 'red' ? step.message : null);
  const hasOutput = Boolean(step.log || validationMessage || step.message || step.counts);

  useEffect(() => {
    if (step.status === 'red') setExpanded(true);
  }, [step.status]);

  const runDisabled =
    running || (!canRun && !(allowRunWhenGreen && step.status === 'green'));
  const runLabel =
    running
      ? 'Running…'
      : step.status === 'green' && allowRunWhenGreen
        ? greenRunLabel
        : 'Run';

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
          {validationMessage ? (
            <p className="mt-1 text-xs text-red-700" role="alert">
              <span className="font-medium">Validation: </span>
              {validationMessage}
            </p>
          ) : step.status === 'green' && step.message ? (
            <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{step.message}</p>
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
            disabled={runDisabled || (step.status === 'green' && !allowRunWhenGreen)}
            onClick={onRun}
            className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {runLabel}
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
  const router = useRouter();
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

  const refreshState = useCallback(
    async (reinforceRun?: SiteAnalyzerStepRunResponse | null, reinforcePackId?: string) => {
      if (!projectId) {
        setState(null);
        return;
      }
      setLoadingState(true);
      try {
        const next = await getSiteAnalyzerProjectState(projectId, accessToken);
        if (reinforceRun) {
          const { state: merged, lostOnServer } = reinforceStepRunAfterRefresh(
            next,
            reinforceRun,
            reinforcePackId,
          );
          setState(merged);
          if (lostOnServer) {
            setError(new Error(stepRunLostOnServerMessage(reinforceRun, reinforcePackId)));
          }
        } else {
          setState(next);
        }
        const packs = next.packs ?? [];
        if (!selectedPackId && packs[0]) {
          setSelectedPackId(packs[0].urlResearchId);
        }
      } catch (loadError) {
        setError(loadError);
      } finally {
        setLoadingState(false);
      }
    },
    [accessToken, projectId, selectedPackId],
  );

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
    let reinforceRun: SiteAnalyzerStepRunResponse | null = null;
    try {
      const result = await runSiteIndexStep(projectId, stepNumber, accessToken);
      reinforceRun = result;
      setState((prev) => (prev ? applyStepRunToProjectState(prev, result) : prev));
      await refreshState(result);
      if (result.status === 'red') {
        setError(
          new Error(
            result.validationMessage ??
              result.message ??
              `Step ${stepNumber} failed validation.`,
          ),
        );
      }
    } catch (runError) {
      setError(runError);
      const message = runError instanceof Error ? runError.message : 'Step failed';
      reinforceRun = failureRunFromError(stepNumber, message);
      setState((prev) =>
        prev ? applyStepRunToProjectState(prev, reinforceRun!) : prev,
      );
      await refreshState(reinforceRun);
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

  function openContentWriting(packId: string) {
    if (!projectId) return;
    router.push(
      `/content-writing?projectId=${encodeURIComponent(projectId)}&urlResearchId=${encodeURIComponent(packId)}`,
    );
  }

  async function handleRunPackStep(stepNumber: number) {
    if (!selectedPackId) return;
    if (stepNumber === 10) {
      const step10 = packSteps.find((s) => s.stepNumber === 10);
      if (step10?.status === 'green') {
        openContentWriting(selectedPackId);
        return;
      }
    }
    setRunningStep(stepNumber);
    setError(null);
    let reinforceRun: SiteAnalyzerStepRunResponse | null = null;
    try {
      const result: SiteAnalyzerStepRunResponse = await runSiteAnalyzerPackStep(
        selectedPackId,
        stepNumber,
        accessToken,
      );
      reinforceRun = result;
      setState((prev) =>
        prev ? applyStepRunToProjectState(prev, result, selectedPackId) : prev,
      );
      await refreshState(result, selectedPackId);
      if (result.status === 'red') {
        setError(
          new Error(
            result.validationMessage ??
              result.message ??
              `Step ${stepNumber} failed validation.`,
          ),
        );
      }
      if (
        stepNumber === 10 &&
        result.status === 'green' &&
        projectId
      ) {
        openContentWriting(selectedPackId);
      }
    } catch (runError) {
      setError(runError);
      const message = runError instanceof Error ? runError.message : 'Step failed';
      reinforceRun = failureRunFromError(stepNumber, message);
      setState((prev) =>
        prev ? applyStepRunToProjectState(prev, reinforceRun!, selectedPackId) : prev,
      );
      await refreshState(reinforceRun, selectedPackId);
    } finally {
      setRunningStep(null);
    }
  }

  const handoffPack = packs.find((p) => p.handoffReady || p.dataQuality === 'full') ?? null;

  return (
    <div className="mx-auto max-w-4xl">
      <h1 className="text-2xl font-semibold">Site Analyzer</h1>
      <p className="mt-1 max-w-2xl text-sm text-[var(--color-text-secondary)]">
        Crawl your site, then build keyword research step by step. A step must pass validation before the
        next unlocks. Failed steps stay red with their own validation message. Step 10 only checks that
        steps 5–9 are green — it does not re-validate pack artifacts.
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
                    allowRunWhenGreen={step.stepNumber === 10}
                    greenRunLabel="Open Content Writing"
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
