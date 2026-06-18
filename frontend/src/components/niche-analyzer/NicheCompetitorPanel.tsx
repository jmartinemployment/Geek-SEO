'use client';

import { Fragment, useEffect, useState } from 'react';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import type { NicheAnalysisStatus, NicheCompetitorResult, StepStatus } from '@/lib/seo-api';
import { analyzeCompetitors, getNicheProfileCompetitors, runNicheStep } from '@/lib/seo-api';
import { waitForNicheStepViaSignalR } from '@/lib/niche-step-wait';

type Props = {
  profileId: string;
  competitors: NicheCompetitorResult[];
  accessToken?: string | null;
  onCompetitorsUpdated?: () => void;
  /** Set when SERP validation found competitors but none were persisted (re-run that step). */
  serpValidationSummary?: string | null;
  /** Loud warning when local SERP queries failed or returned no local-scoped competitors. */
  serpLocalWarning?: string | null;
  serpStepStatus?: StepStatus;
  anyStepRunning?: boolean;
  onStepStatusChange?: (status: NicheAnalysisStatus) => void;
};

type ProgressState = { done: number; total: number; message: string } | null;

const STRENGTH_COLORS: Record<string, string> = {
  dominant: 'bg-rose-100 text-rose-800',
  strong: 'bg-amber-100 text-amber-800',
  moderate: 'bg-stone-100 text-stone-600',
};

const SCOPE_COLORS: Record<string, string> = {
  both: 'bg-violet-100 text-violet-800',
  national: 'bg-blue-100 text-blue-800',
  local: 'bg-emerald-100 text-emerald-800',
};

function LocalSerpWarningBanner({ message }: { message: string }) {
  return (
    <div className="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950">
      <p className="font-medium">Local SERP issue</p>
      <p className="mt-1 text-xs leading-relaxed">{message.replace(/^Local SERP issue:\s*/i, '')}</p>
    </div>
  );
}

function serpSummaryImpliesCompetitors(summary: string | null | undefined): boolean {
  if (!summary) return false;
  const match = /\b(\d+)\s+competitor/i.exec(summary);
  return match !== null && Number(match[1]) > 0;
}

function serpSummaryImpliesLocalResults(summary: string | null | undefined): boolean {
  if (!summary) return false;
  return /pillars returned local results/i.test(summary);
}

function scopeLabelsStaleAfterLocalSerp(
  rows: NicheCompetitorResult[],
  serpSummary: string | null | undefined,
): boolean {
  if (rows.length === 0 || !serpSummaryImpliesLocalResults(serpSummary)) return false;
  return rows.every((c) => c.scope === 'national');
}

function ScopePersistenceBanner({
  onRerun,
  rerunning,
  disabled,
  progress,
}: {
  onRerun: () => void;
  rerunning: boolean;
  disabled: boolean;
  progress: string | null;
}) {
  return (
    <div className="rounded-lg border border-violet-300 bg-violet-50 px-4 py-3 text-sm text-violet-950">
      <p className="font-medium">Local competitor labels need a refresh</p>
      <p className="mt-1 text-xs leading-relaxed">
        SERP validation found local results for your service area, but every saved competitor is still
        labeled national — a persistence bug from an earlier run. Re-run SERP validation to rewrite
        competitor scope (local / both / national). This re-queries SERP and can take several minutes.
      </p>
      {progress ? (
        <p className="mt-2 text-xs text-violet-900">{progress}</p>
      ) : null}
      <button
        type="button"
        onClick={onRerun}
        disabled={disabled || rerunning}
        className="mt-3 rounded-lg bg-violet-700 px-4 py-2 text-xs font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {rerunning ? 'Re-running SERP validation…' : 'Re-run SERP validation'}
      </button>
    </div>
  );
}

export function NicheCompetitorPanel({
  profileId,
  competitors,
  accessToken,
  onCompetitorsUpdated,
  serpValidationSummary,
  serpLocalWarning,
  anyStepRunning = false,
  serpStepStatus,
  onStepStatusChange,
}: Readonly<Props>) {
  const hub = useSeoHub();
  const [expandedDomain, setExpandedDomain] = useState<string | null>(null);
  const [progress, setProgress] = useState<ProgressState>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [serpRerunning, setSerpRerunning] = useState(false);
  const [serpProgress, setSerpProgress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loadedCompetitors, setLoadedCompetitors] = useState(competitors);
  const [loadingCompetitors, setLoadingCompetitors] = useState(false);

  useEffect(() => {
    setLoadedCompetitors(competitors);
  }, [competitors]);

  useEffect(() => {
    if (!accessToken) return;
    let cancelled = false;
    setLoadingCompetitors(true);
    void getNicheProfileCompetitors(profileId, accessToken)
      .then((rows) => {
        if (!cancelled) setLoadedCompetitors(rows);
      })
      .catch(() => {
        if (!cancelled) setLoadedCompetitors(competitors);
      })
      .finally(() => {
        if (!cancelled) setLoadingCompetitors(false);
      });
    return () => {
      cancelled = true;
    };
  }, [profileId, accessToken, competitors]);

  const analyzed = loadedCompetitors.some((c) => c.competitorAnalyzedAt);
  const totalPages = loadedCompetitors.reduce((sum, c) => sum + (c.pagesCrawled ?? 0), 0);
  const showLocalSerpWarning =
    Boolean(serpLocalWarning) && serpStepStatus !== 'running' && !serpRerunning;
  const showScopePersistenceBanner =
    scopeLabelsStaleAfterLocalSerp(loadedCompetitors, serpValidationSummary)
    && serpStepStatus !== 'running'
    && !serpRerunning;

  async function handleRerunSerpValidation() {
    setError(null);
    setSerpRerunning(true);
    setSerpProgress('Starting SERP validation…');
    try {
      await waitForNicheStepViaSignalR({
        profileId,
        slug: 'serp_validation',
        accessToken,
        hub,
        timeoutMs: 900_000,
        triggerRun: () => runNicheStep(profileId, 'serp_validation', accessToken),
        onProgress: setSerpProgress,
        onStatus: onStepStatusChange,
      });
      await onCompetitorsUpdated?.();
      const rows = await getNicheProfileCompetitors(profileId, accessToken);
      setLoadedCompetitors(rows);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'SERP validation re-run failed');
    } finally {
      setSerpRerunning(false);
      setSerpProgress(null);
    }
  }

  async function handleGetCompetitors() {
    setError(null);
    setAnalyzing(true);
    setProgress({ done: 0, total: loadedCompetitors.length, message: 'Starting…' });

    const leave = hub.joinNicheProfile(profileId);
    const unsub = hub.subscribe(
      'CompetitorAnalysisProgress',
      (data: unknown) => {
        const progress = data as { done: number; total: number; message: string };
        setProgress(progress);
        if (progress.done >= progress.total) {
          setAnalyzing(false);
          onCompetitorsUpdated?.();
        }
      },
    );

    try {
      await analyzeCompetitors(profileId, accessToken);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start competitor analysis');
      setAnalyzing(false);
      setProgress(null);
    } finally {
      unsub();
      leave();
    }
  }

  if (loadingCompetitors && loadedCompetitors.length === 0) {
    return (
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-8 text-center">
        <p className="text-sm text-[var(--color-text-muted)]">Loading competitors…</p>
      </div>
    );
  }

  if (loadedCompetitors.length === 0) {
    const serpFoundCompetitors = serpSummaryImpliesCompetitors(serpValidationSummary);
    return (
      <div className="space-y-4">
        {showLocalSerpWarning && serpLocalWarning ? (
          <LocalSerpWarningBanner message={serpLocalWarning} />
        ) : null}
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-5 py-8 text-center">
        {serpFoundCompetitors ? (
          <>
            <p className="text-sm text-[var(--color-text-primary)]">
              SERP validation found competitors, but they were not saved to this profile.
            </p>
            <p className="mt-2 text-sm text-[var(--color-text-muted)]">
              Re-run SERP validation to populate this tab. This re-queries SERP for your pillars and
              can take a few minutes.
            </p>
            {serpProgress ? (
              <p className="mt-3 text-xs text-[var(--color-text-secondary)]">{serpProgress}</p>
            ) : null}
            <button
              type="button"
              onClick={handleRerunSerpValidation}
              disabled={serpRerunning || anyStepRunning}
              className="mt-4 rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {serpRerunning ? 'Re-running SERP validation…' : 'Re-run SERP validation'}
            </button>
            {error ? (
              <p className="mt-3 text-xs text-red-600">{error}</p>
            ) : null}
          </>
        ) : (
          <p className="text-sm text-[var(--color-text-muted)]">
            No competitors identified yet. Complete SERP validation during niche analysis first.
          </p>
        )}
        </div>
      </div>
    );
  }

  return (
    <section className="space-y-4">
      {showLocalSerpWarning && serpLocalWarning ? (
        <LocalSerpWarningBanner message={serpLocalWarning} />
      ) : null}
      {showScopePersistenceBanner ? (
        <ScopePersistenceBanner
          onRerun={handleRerunSerpValidation}
          rerunning={serpRerunning}
          disabled={anyStepRunning}
          progress={serpProgress}
        />
      ) : null}
      {/* Header + action */}
      <div className="flex items-center justify-between">
        <div>
          <p className="text-xs text-[var(--color-text-muted)]">
            {loadedCompetitors.length} competitor{loadedCompetitors.length !== 1 ? 's' : ''} identified via SERP
            {totalPages > 0 ? ` · ${totalPages} pages crawled` : ''}
            {analyzed ? ` · analyzed` : ''}
          </p>
        </div>
        {!analyzing && (
          <button
            type="button"
            onClick={handleGetCompetitors}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90"
          >
            Get Competitors
          </button>
        )}
      </div>

      {/* Progress */}
      {analyzing && progress && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
          <p className="text-xs text-[var(--color-text-secondary)]">{progress.message}</p>
          <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-[var(--color-border)]">
            <div
              className="h-full rounded-full bg-[var(--color-accent)] transition-all"
              style={{ width: `${progress.total > 0 ? Math.round((progress.done / progress.total) * 100) : 0}%` }}
            />
          </div>
          <p className="mt-1 text-[10px] text-[var(--color-text-muted)]">
            {progress.done}/{progress.total} sites
          </p>
        </div>
      )}

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-xs text-red-700">{error}</div>
      )}

      {/* Competitor table */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-xs">
            <thead className="bg-[var(--color-surface-muted)] text-[var(--color-text-muted)]">
              <tr>
                <th className="w-8 px-2 py-2" aria-label="Expand" />
                <th className="px-3 py-2 font-medium">Domain</th>
                <th className="px-3 py-2 font-medium">Scope</th>
                <th className="px-3 py-2 font-medium">Strength</th>
                <th className="px-3 py-2 font-medium">SERP presence</th>
                <th className="px-3 py-2 font-medium">Their pillars</th>
                <th className="px-3 py-2 font-medium">Pages crawled</th>
              </tr>
            </thead>
            <tbody>
              {loadedCompetitors.map((c) => {
                const expanded = expandedDomain === c.domain;
                const hasCrawlData = c.pagesCrawled > 0 || (c.pillars?.length ?? 0) > 0;
                return (
                  <Fragment key={c.id}>
                    <tr className="border-t border-[var(--color-border)]">
                      <td className="px-2 py-2">
                        {hasCrawlData ? (
                          <button
                            type="button"
                            aria-expanded={expanded}
                            onClick={() => setExpandedDomain(expanded ? null : c.domain)}
                            className="flex h-6 w-6 items-center justify-center rounded text-[var(--color-text-muted)] hover:bg-[var(--color-surface-muted)]"
                          >
                            {expanded ? '−' : '+'}
                          </button>
                        ) : null}
                      </td>
                      <td className="px-3 py-2 font-medium text-[var(--color-text-primary)]">{c.domain}</td>
                      <td className="px-3 py-2">
                        <span className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${SCOPE_COLORS[c.scope] ?? 'bg-stone-100 text-stone-600'}`}>
                          {c.scope}
                        </span>
                      </td>
                      <td className="px-3 py-2">
                        <span className={`rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide ${STRENGTH_COLORS[c.strengthAssessment] ?? 'bg-stone-100 text-stone-600'}`}>
                          {c.strengthAssessment}
                        </span>
                      </td>
                      <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">{c.serpPresence}</td>
                      <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">
                        {c.pillars?.length ?? '—'}
                      </td>
                      <td className="px-3 py-2 tabular-nums text-[var(--color-text-secondary)]">
                        {c.pagesCrawled > 0 ? c.pagesCrawled : '—'}
                      </td>
                    </tr>
                    {expanded && hasCrawlData ? (
                      <tr key={`${c.domain}-detail`} className="border-t border-[var(--color-border)] bg-[var(--color-surface-muted)]/40">
                        <td colSpan={7} className="px-5 py-4">
                          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                            {c.description ? (
                              <div className="col-span-full">
                                <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Description</p>
                                <p className="mt-1 text-xs text-[var(--color-text-secondary)]">{c.description}</p>
                              </div>
                            ) : null}
                            {(c.pillars?.length ?? 0) > 0 ? (
                              <div className="col-span-full">
                                <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Their pillars ({c.pillars!.length})</p>
                                <div className="mt-2 flex flex-wrap gap-1.5">
                                  {c.pillars!.map((p) => (
                                    <span key={p.slug} className="rounded-full bg-[var(--color-surface)] border border-[var(--color-border)] px-2 py-0.5 text-xs text-[var(--color-text-secondary)]">
                                      {p.name}
                                    </span>
                                  ))}
                                </div>
                              </div>
                            ) : null}
                            {(c.services?.length ?? 0) > 0 ? (
                              <div>
                                <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Services</p>
                                <ul className="mt-1 space-y-0.5">
                                  {c.services!.slice(0, 8).map((s) => <li key={s} className="text-xs text-[var(--color-text-secondary)]">{s}</li>)}
                                </ul>
                              </div>
                            ) : null}
                            {(c.knowsAbout?.length ?? 0) > 0 ? (
                              <div>
                                <p className="text-[10px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">Knows about</p>
                                <ul className="mt-1 space-y-0.5">
                                  {c.knowsAbout!.slice(0, 8).map((k) => <li key={k} className="text-xs text-[var(--color-text-secondary)]">{k}</li>)}
                                </ul>
                              </div>
                            ) : null}
                            {c.hasFaqSchema ? (
                              <div>
                                <span className="rounded bg-blue-50 px-2 py-0.5 text-[10px] font-medium text-blue-700">FAQ schema</span>
                              </div>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    ) : null}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
