'use client';

import { useEffect, useMemo, useState } from 'react';
import {
  getNicheAnalysisDetails,
  getNicheAnalysisStatus,
  runNicheStep,
  type NicheAnalysisDetails,
  type NicheAnalysisStatus,
  type NicheAnalysisStepLogEntry,
  type NicheStepDefinition,
  type StepStatus,
} from '@/lib/seo-api';
import { OUTPUT_LABELS } from '@/components/niche-analyzer/pillar-provenance';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';
import { waitForNicheStepViaSignalR } from '@/lib/niche-step-wait';
import {
  isNicheStepComplete,
  mergeStepStatuses,
} from '@/lib/niche-step-status';

type Props = {
  profileId: string;
  projectId?: string;
  accessToken?: string | null;
  defaultOpen?: boolean;
  pollIntervalMs?: number;
  stepStatuses?: Record<string, StepStatus>;
  anyStepRunning?: boolean;
  stepSummaries?: Record<string, string>;
  stepErrors?: Record<string, string>;
  onStepRerun?: () => void | Promise<void>;
  onStepStatusChange?: (status: NicheAnalysisStatus) => void;
};

const TERMINAL_STEP_STATUSES = new Set<StepStatus>(['complete', 'error', 'skipped']);

type Phase = {
  id: string;
  title: string;
  subtitle: string;
  slugs: string[];
};

const SE_PHASES: Phase[] = [
  {
    id: 'discover',
    title: 'Discover',
    subtitle: 'What URLs and structure are declared',
    slugs: ['schema', 'site_urls', 'nav'],
  },
  {
    id: 'fetch',
    title: 'Fetch & parse',
    subtitle: 'What the crawler read',
    slugs: ['headings', 'page_content', 'site_crawl', 'internal_links', 'url_patterns'],
  },
  {
    id: 'understand',
    title: 'Understand',
    subtitle: 'Which topics search systems would associate',
    slugs: ['merging'],
  },
  {
    id: 'validate',
    title: 'Validate',
    subtitle: 'External demand proxies (optional)',
    slugs: ['keywords', 'serp_validation'],
  },
  {
    id: 'synthesize',
    title: 'Synthesize',
    subtitle: 'Profile, geography, coverage, and scoring',
    slugs: ['profile', 'local', 'coverage', 'scoring', 'complete'],
  },
];

function formatOutputValue(value: unknown): string {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'boolean') return value ? 'yes' : 'no';
  if (typeof value === 'number') return String(value);
  if (typeof value === 'string') return value || '—';
  if (Array.isArray(value)) {
    if (value.length === 0) return 'none';
    return value.map((item) => String(item)).join(', ');
  }
  return JSON.stringify(value);
}

function StepOutputs({ outputs }: { outputs: Record<string, unknown> }) {
  const entries = Object.entries(outputs).filter(([key]) => !key.startsWith('_artifact'));
  if (entries.length === 0) return null;

  return (
    <dl className="mt-2 grid gap-1.5 text-xs sm:grid-cols-2">
      {entries.map(([key, value]) => (
        <div key={key} className="min-w-0">
          <dt className="font-medium text-[var(--color-text-muted)]">
            {OUTPUT_LABELS[key] ?? key}
          </dt>
          <dd className="break-words text-[var(--color-text-secondary)]">
            {formatOutputValue(value)}
          </dd>
        </div>
      ))}
    </dl>
  );
}

function StepRow({
  step,
  stepDefinition,
  stepStatuses,
  anyStepRunning,
  stepSummaries,
  stepErrors,
  showOutputs,
  profileId,
  accessToken,
  onStepRerun,
  onStepStatusChange,
}: {
  step?: NicheAnalysisStepLogEntry;
  stepDefinition: NicheStepDefinition;
  stepStatuses?: Record<string, StepStatus>;
  anyStepRunning?: boolean;
  stepSummaries?: Record<string, string>;
  stepErrors?: Record<string, string>;
  showOutputs: boolean;
  profileId: string;
  accessToken?: string | null;
  onStepRerun?: () => void | Promise<void>;
  onStepStatusChange?: (status: NicheAnalysisStatus) => void;
}) {
  const [rerunning, setRerunning] = useState(false);
  const [optimisticRunning, setOptimisticRunning] = useState(false);
  const [liveProgress, setLiveProgress] = useState<string | null>(null);
  const [rerunError, setRerunError] = useState<string | null>(null);

  const isolatedStatus = stepStatuses?.[stepDefinition.slug];
  const isolatedTerminal =
    isolatedStatus !== undefined && TERMINAL_STEP_STATUSES.has(isolatedStatus);
  const displayStatus: StepStatus | undefined = isolatedTerminal
    ? isolatedStatus
    : optimisticRunning || rerunning
      ? 'running'
      : isolatedStatus;
  const deps = stepDefinition?.dependencies ?? [];
  const depsKnown = deps.length === 0 || Boolean(stepStatuses);
  const depsComplete = deps.every((dep) => isNicheStepComplete(dep, stepStatuses));
  const canRun = Boolean(stepStatuses);
  const rerunDisabled =
    !depsKnown || !depsComplete || anyStepRunning || (rerunning && !isolatedTerminal);
  const visibleStep = showOutputs ? step : undefined;
  const persistedError = stepErrors?.[stepDefinition.slug];
  const persistedSummary = stepSummaries?.[stepDefinition.slug];

  const statusLabel = displayStatus === 'running' ? 'in progress'
    : displayStatus === 'error' ? 'error'
    : displayStatus === 'complete' ? 'complete'
    : displayStatus === 'skipped' ? 'skipped'
    : 'pending';

  const statusColor = displayStatus === 'error' ? 'text-red-600'
    : displayStatus === 'running' ? 'text-amber-600'
    : 'text-[var(--color-text-muted)]';

  const summary = liveProgress
    ?? visibleStep?.summary
    ?? persistedError
    ?? persistedSummary
    ?? (!depsComplete
      ? `Blocked until: ${deps.filter((dep) => !isNicheStepComplete(dep, stepStatuses)).join(', ')}`
      : displayStatus === 'running'
        ? 'Step is currently running.'
      : displayStatus === 'complete'
        ? 'Step completed.'
        : displayStatus === 'error'
          ? 'Previous run failed. Run again after correcting inputs.'
          : 'Ready to execute.');

  const buttonLabel = !isolatedTerminal && (rerunning || optimisticRunning)
    ? 'Running…'
    : displayStatus === 'running'
      ? 'Running…'
    : displayStatus === 'complete'
        ? 'Re-run'
        : displayStatus === 'error'
          ? 'Retry'
          : 'Run';

  async function handleRerun() {
    setRerunning(true);
    setOptimisticRunning(true);
    setRerunError(null);
    setLiveProgress(null);
    try {
      await waitForNicheStepViaSignalR({
        profileId,
        slug: stepDefinition.slug,
        accessToken,
        triggerRun: () => runNicheStep(profileId, stepDefinition.slug, accessToken),
        onProgress: setLiveProgress,
        onStatus: (status) => {
          onStepStatusChange?.(status);
          const stepState = status.stepStatuses?.[stepDefinition.slug];
          if (stepState && TERMINAL_STEP_STATUSES.has(stepState)) {
            setOptimisticRunning(false);
          }
        },
      });
      await onStepRerun?.();
    } catch (e) {
      setRerunError(e instanceof Error ? e.message : 'Re-run failed');
    } finally {
      setOptimisticRunning(false);
      setRerunning(false);
      setLiveProgress(null);
    }
  }

  return (
    <li className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm font-medium text-[var(--color-text-primary)]">
            {stepDefinition.stepNumber}. {stepDefinition.title}
          </p>
          <p className={`mt-0.5 text-sm ${persistedError ? 'text-red-700' : 'text-[var(--color-text-secondary)]'}`}>
            {summary}
          </p>
          {rerunError ? (
            <p className="mt-1 text-xs text-red-600">{rerunError}</p>
          ) : null}
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1.5">
          <span className={`text-xs uppercase tracking-wide ${statusColor}`}>
            {statusLabel}
          </span>
          {canRun ? (
            <button
              type="button"
              onClick={handleRerun}
              disabled={rerunDisabled || displayStatus === 'running'}
              title={
                !depsKnown
                  ? 'Step status map unavailable for this run.'
                  : !depsComplete
                    ? `Dependencies not complete: ${deps.filter((d) => !isNicheStepComplete(d, stepStatuses)).join(', ')}`
                    : ''
              }
              className="rounded px-2 py-0.5 text-[10px] font-medium transition-colors bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:cursor-not-allowed disabled:opacity-40"
            >
              {buttonLabel}
            </button>
          ) : null}
        </div>
      </div>
      {visibleStep ? <StepOutputs outputs={visibleStep.outputs} /> : null}
    </li>
  );
}

function PhaseSection({
  phase,
  steps,
  stepDefinitions,
  defaultExpanded,
  stepStatuses,
  anyStepRunning,
  stepSummaries,
  stepErrors,
  showOutputs,
  profileId,
  accessToken,
  onStepRerun,
  onStepStatusChange,
}: {
  phase: Phase;
  steps: NicheAnalysisStepLogEntry[];
  stepDefinitions: NicheStepDefinition[];
  defaultExpanded: boolean;
  stepStatuses?: Record<string, StepStatus>;
  anyStepRunning?: boolean;
  stepSummaries?: Record<string, string>;
  stepErrors?: Record<string, string>;
  showOutputs: boolean;
  profileId: string;
  accessToken?: string | null;
  onStepRerun?: () => void | Promise<void>;
  onStepStatusChange?: (status: NicheAnalysisStatus) => void;
}) {
  const [open, setOpen] = useState(defaultExpanded);
  const phaseDefinitions = stepDefinitions.filter((definition) => phase.slugs.includes(definition.slug));
  const stepsBySlug = new Map(steps.map((step) => [step.slug, step]));

  if (phaseDefinitions.length === 0) return null;

  const phaseHasError = phaseDefinitions.some((definition) => stepStatuses?.[definition.slug] === 'error');

  return (
    <div className={`rounded-lg border ${phaseHasError ? 'border-red-200' : 'border-[var(--color-border)]'}`}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center justify-between px-4 py-3 text-left"
        aria-expanded={open}
      >
        <div>
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">{phase.title}</p>
          <p className="text-xs text-[var(--color-text-muted)]">{phase.subtitle}</p>
        </div>
        <span className="text-xs text-[var(--color-text-muted)]">
          {phaseDefinitions.length} step{phaseDefinitions.length === 1 ? '' : 's'} · {open ? 'Hide' : 'Show'}
        </span>
      </button>
      {open ? (
        <ol className="space-y-3 border-t border-[var(--color-border)] px-4 py-3">
          {phaseDefinitions.map((definition) => (
            <StepRow
              key={`${definition.stepNumber}-${definition.slug}`}
              step={stepsBySlug.get(definition.slug)}
              stepDefinition={definition}
              stepStatuses={stepStatuses}
              anyStepRunning={anyStepRunning}
              stepSummaries={stepSummaries}
              stepErrors={stepErrors}
              showOutputs={showOutputs}
              profileId={profileId}
              accessToken={accessToken}
              onStepRerun={onStepRerun}
              onStepStatusChange={onStepStatusChange}
            />
          ))}
        </ol>
      ) : null}
    </div>
  );
}

export function AnalysisStepBreakdown({
  profileId,
  projectId,
  accessToken,
  defaultOpen = true,
  pollIntervalMs,
  stepStatuses,
  anyStepRunning,
  stepSummaries,
  stepErrors,
  onStepRerun,
  onStepStatusChange,
}: Readonly<Props>) {
  const [open, setOpen] = useState(defaultOpen);
  const [details, setDetails] = useState<NicheAnalysisDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [liveStepStatuses, setLiveStepStatuses] = useState<Record<string, StepStatus> | undefined>();
  const [liveStepSummaries, setLiveStepSummaries] = useState<Record<string, string> | undefined>();
  const [liveStepErrors, setLiveStepErrors] = useState<Record<string, string> | undefined>();

  useEffect(() => {
    let cancelled = false;

    async function load(showSpinner: boolean) {
      if (showSpinner) setLoading(true);
      if (showSpinner) setError(null);
      try {
        const data = await getNicheAnalysisDetails(profileId, accessToken);
        if (!cancelled) {
          setDetails(data);
          setError(null);
        }
      } catch (e: unknown) {
        if (!cancelled && showSpinner) {
          setError(e instanceof Error ? e.message : 'Could not load scan breakdown');
        }
      } finally {
        if (!cancelled && showSpinner) setLoading(false);
      }
    }

    async function loadStatuses() {
      try {
        const status = await getNicheAnalysisStatus(profileId, accessToken);
        if (!cancelled && status.stepStatuses) {
          setLiveStepStatuses(status.stepStatuses);
        }
        if (!cancelled && status.stepSummaries) {
          setLiveStepSummaries(status.stepSummaries);
        }
        if (!cancelled && status.stepErrors) {
          setLiveStepErrors(status.stepErrors);
        }
      } catch {
        // keep last known canonical statuses
      }
    }

    void load(true);
    void loadStatuses();

    if (!pollIntervalMs || pollIntervalMs <= 0) {
      return () => {
        cancelled = true;
      };
    }

    const id = window.setInterval(() => {
      void load(false);
      void loadStatuses();
    }, pollIntervalMs);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [profileId, accessToken, pollIntervalMs]);

  const ungroupedSteps = useMemo(() => {
    if (!details) return [];
    const grouped = new Set(SE_PHASES.flatMap((p) => p.slugs));
    return details.steps.filter((s) => !grouped.has(s.slug));
  }, [details]);
  const stepDefinitions = details?.stepDefinitions ?? [];
  const effectiveStepStatuses = mergeStepStatuses(stepStatuses, liveStepStatuses);
  const effectiveStepSummaries = { ...(stepSummaries ?? {}), ...(liveStepSummaries ?? {}) };
  const effectiveStepErrors = { ...(stepErrors ?? {}), ...(liveStepErrors ?? {}) };
  const showOutputs = pollIntervalMs === undefined;

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)]">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center justify-between px-5 py-4 text-left"
        aria-expanded={open}
      >
        <div>
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">
            How search engines read this site
          </h2>
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
            Composite public signals — discover, crawl, understand, validate — saved with this run.
          </p>
        </div>
        <span className="text-sm text-[var(--color-text-muted)]">{open ? 'Hide' : 'Show'}</span>
      </button>

      {open ? (
        <div className="border-t border-[var(--color-border)] px-5 pb-5 pt-4">
          {loading ? (
            <p className="text-sm text-[var(--color-text-muted)]">Loading step log…</p>
          ) : null}
          {error ? <p className="text-sm text-amber-700">{error}</p> : null}
          {!loading && details && details.steps.length === 0 && stepDefinitions.length === 0 ? (
            <p className="text-sm text-[var(--color-text-secondary)]">
              {pollIntervalMs
                ? 'Step log will appear as each discovery step completes…'
                : 'No step log for this run. Re-analyze once to capture discovery detail.'}
            </p>
          ) : null}
          {details && stepDefinitions.length > 0 ? (
            <div className="space-y-3">
              {SE_PHASES.map((phase, index) => (
                <PhaseSection
                  key={phase.id}
                  phase={phase}
                  steps={details.steps}
                  stepDefinitions={stepDefinitions}
                  defaultExpanded={index < 2 || pollIntervalMs !== undefined}
                  stepStatuses={effectiveStepStatuses}
                  anyStepRunning={anyStepRunning}
                  stepSummaries={effectiveStepSummaries}
                  stepErrors={effectiveStepErrors}
                  showOutputs={showOutputs}
                  profileId={profileId}
                  accessToken={accessToken}
                  onStepRerun={onStepRerun}
                  onStepStatusChange={(status) => {
                    if (status.stepStatuses) setLiveStepStatuses(status.stepStatuses);
                    if (status.stepSummaries) setLiveStepSummaries(status.stepSummaries);
                    if (status.stepErrors) setLiveStepErrors(status.stepErrors);
                    onStepStatusChange?.(status);
                  }}
                />
              ))}
              {ungroupedSteps.length > 0 ? (
                <ol className="space-y-3">
                  {ungroupedSteps.map((step) => (
                    <StepRow
                      key={`${step.stepNumber}-${step.slug}`}
                      step={step}
                      stepDefinition={
                        stepDefinitions.find((definition) => definition.slug === step.slug)
                        ?? {
                          stepNumber: step.stepNumber,
                          slug: step.slug,
                          title: step.title,
                          phase: 'unknown',
                          dependencies: [],
                          isOptional: false,
                          isTerminal: false,
                        }
                      }
                      stepStatuses={effectiveStepStatuses}
                      anyStepRunning={anyStepRunning}
                      stepSummaries={effectiveStepSummaries}
                      stepErrors={effectiveStepErrors}
                      showOutputs={showOutputs}
                      profileId={profileId}
                      accessToken={accessToken}
                      onStepRerun={onStepRerun}
                      onStepStatusChange={(status) => {
                        if (status.stepStatuses) setLiveStepStatuses(status.stepStatuses);
                        if (status.stepSummaries) setLiveStepSummaries(status.stepSummaries);
                        if (status.stepErrors) setLiveStepErrors(status.stepErrors);
                        onStepStatusChange?.(status);
                      }}
                    />
                  ))}
                </ol>
              ) : null}
            </div>
          ) : null}
          {details?.fusionSnapshot ? (
            <div className="mt-6">
              <TopicCandidateMatrix fusion={details.fusionSnapshot} />
            </div>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
