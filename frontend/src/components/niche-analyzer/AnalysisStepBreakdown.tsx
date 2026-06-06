'use client';

import { useEffect, useState } from 'react';
import {
  getNicheAnalysisDetails,
  type NicheAnalysisDetails,
  type NicheAnalysisStepLogEntry,
} from '@/lib/seo-api';
import { OUTPUT_LABELS } from '@/components/niche-analyzer/pillar-provenance';
import { TopicCandidateMatrix } from '@/components/niche-analyzer/TopicCandidateMatrix';

type Props = {
  profileId: string;
  accessToken?: string | null;
  defaultOpen?: boolean;
  /** When set, refetch on this interval (e.g. while analysis is in progress). */
  pollIntervalMs?: number;
};

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
  const entries = Object.entries(outputs);
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

function StepRow({ step }: { step: NicheAnalysisStepLogEntry }) {
  return (
    <li className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-[var(--color-text-primary)]">
            {step.stepNumber}. {step.title}
          </p>
          <p className="mt-0.5 text-sm text-[var(--color-text-secondary)]">{step.summary}</p>
        </div>
        <span className="shrink-0 text-xs uppercase tracking-wide text-[var(--color-text-muted)]">
          {step.slug}
        </span>
      </div>
      <StepOutputs outputs={step.outputs} />
    </li>
  );
}

export function AnalysisStepBreakdown({
  profileId,
  accessToken,
  defaultOpen = true,
  pollIntervalMs,
}: Readonly<Props>) {
  const [open, setOpen] = useState(defaultOpen);
  const [details, setDetails] = useState<NicheAnalysisDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

    void load(true);

    if (!pollIntervalMs || pollIntervalMs <= 0) {
      return () => {
        cancelled = true;
      };
    }

    const id = window.setInterval(() => {
      void load(false);
    }, pollIntervalMs);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [profileId, accessToken, pollIntervalMs]);

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
            How this scan worked
          </h2>
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
            What each step found — saved with this run (no re-analyze needed).
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
          {!loading && details && details.steps.length === 0 ? (
            <p className="text-sm text-[var(--color-text-secondary)]">
              {pollIntervalMs
                ? 'Step log will appear as each discovery step completes…'
                : 'No step log for this run. Re-analyze once to capture discovery detail.'}
            </p>
          ) : null}
          {details && details.steps.length > 0 ? (
            <ol className="space-y-3">
              {details.steps.map((step) => (
                <StepRow key={`${step.stepNumber}-${step.slug}`} step={step} />
              ))}
            </ol>
          ) : null}
          {details?.fusionSnapshot ? (
            <TopicCandidateMatrix fusion={details.fusionSnapshot} />
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
