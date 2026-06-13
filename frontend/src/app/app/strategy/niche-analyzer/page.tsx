'use client';

import { useCallback, useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import {
  listProjects,
  analyzeNiche,
  getNicheAnalysisStatus,
  getLatestNicheProfile,
  getNicheProfile,
  getNicheCoverageMatrix,
  getNicheGaps,
  getNicheProgress,
  getNicheHistory,
  SeoApiError,
  type SeoProject,
  type NicheProfileResult,
  type NicheAnalysisStatus,
  type PillarCoverageMatrix,
  type TopicalGapSummary,
  type AuthorityProgressPoint,
} from '@/lib/seo-api';
import { ContentGuardContextBanner } from '@/components/niche-analyzer/ContentGuardContextBanner';
import { AnalysisStepBreakdown } from '@/components/niche-analyzer/AnalysisStepBreakdown';
import { TopicProfileSection } from '@/components/niche-analyzer/TopicProfileSection';
import { PillarProvenanceCallout } from '@/components/niche-analyzer/PillarProvenanceCallout';
import { NicheHeader } from '@/components/niche-analyzer/NicheHeader';
import { CoverageMatrixTable } from '@/components/niche-analyzer/CoverageMatrixTable';
import { TopicalGapsPanel } from '@/components/niche-analyzer/TopicalGapsPanel';
import { AuthorityProgressChart } from '@/components/niche-analyzer/AuthorityProgressChart';
import { AnalysisStatusListener } from '@/components/niche-analyzer/AnalysisStatusListener';
import { PillarSerpInsightsPanel } from '@/components/niche-analyzer/PillarSerpInsightsPanel';
import { NicheCompetitorPanel } from '@/components/niche-analyzer/NicheCompetitorPanel';

type Tab = 'pillars' | 'competitors' | 'contentIdeas' | 'progress';

const TAB_LABELS: Record<Tab, string> = {
  pillars: 'Pillars',
  competitors: 'Competitors',
  contentIdeas: 'Content ideas',
  progress: 'Progress',
};

export default function NicheAnalyzerPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [profile, setProfile] = useState<NicheProfileResult | null>(null);
  const [coverage, setCoverage] = useState<PillarCoverageMatrix[]>([]);
  const [gaps, setGaps] = useState<TopicalGapSummary[]>([]);
  const [progress, setProgress] = useState<AuthorityProgressPoint[]>([]);
  const [analyzing, setAnalyzing] = useState(false);
  const [analyzeProfileId, setAnalyzeProfileId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>('pillars');
  const [quickWinsOnly, setQuickWinsOnly] = useState(false);
  const [stepStatuses, setStepStatuses] = useState<Record<string, string> | undefined>();
  const analysisStarting = analyzing && !analyzeProfileId;

  const anyStepRunning = Object.values(stepStatuses ?? {}).some(s => s === 'running');

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken, authReady]);

  useEffect(() => {
    if (!authReady || !projectId) return;
    setAnalyzing(false);
    setAnalyzeProfileId(null);
    setProfile(null);
    setCoverage([]);
    setGaps([]);
    setProgress([]);
    void loadExisting();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectId, authReady, accessToken]);

  useEffect(() => {
    if (!authReady || !accessToken || !profile || tab !== 'pillars') return;
    if (profile.status !== 'complete' || profile.pillars.length > 0) return;
    void resolveProfileWithPillars(profile).then((full) => {
      if (full.pillars.length > 0) setProfile(full);
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, profile?.id, profile?.pillars.length, authReady, accessToken]);

  useEffect(() => {
    if (!authReady || !analyzing || analyzeProfileId || !projectId) return;

    let cancelled = false;
    const startedAt = Date.now();

    async function recoverQueuedProfile() {
      try {
        const latest = await getLatestNicheProfile(projectId, accessToken);
        if (cancelled || !latest) return;
        if (latest.status === 'queued' || latest.status === 'processing') {
          setAnalyzeProfileId(latest.id);
        } else if (Date.now() - startedAt > 15_000) {
          setError('Analysis is taking longer than expected to start. Please try again in a moment.');
          setAnalyzing(false);
        }
      } catch {
        if (!cancelled && Date.now() - startedAt > 15_000) {
          setError('Analysis did not report a queued job. Please try again.');
          setAnalyzing(false);
        }
      }
    }

    void recoverQueuedProfile();
    const id = window.setInterval(() => {
      void recoverQueuedProfile();
    }, 1_500);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [accessToken, analyzeProfileId, analyzing, authReady, projectId]);

  async function resolveProfileWithPillars(p: NicheProfileResult): Promise<NicheProfileResult> {
    const needsFull =
      p.status === 'complete' &&
      (p.pillars.length === 0 ||
        (p.totalPillarsIdentified > 0 && p.pillars.length < p.totalPillarsIdentified));
    if (!needsFull) return p;
    return getNicheProfile(p.id, accessToken);
  }

  async function loadExisting() {
    setError(null);
    try {
      const p = await getLatestNicheProfile(projectId, accessToken);
      if (!p) return;

      if (p.status === 'failed') {
        const status = await getNicheAnalysisStatus(p.id, accessToken);
        const raw = status.errorMessage ?? '';
        const isPillarSaveValidation =
          raw.includes('NicheProfile field is required') ||
          raw.includes('[0].NicheProfile');
        setError(
          isPillarSaveValidation
            ? 'The last run failed while saving pillars (a server deploy fix is required). Click Re-analyze after GeekRepository and GeekSeoBackend have redeployed — the red message will clear on success.'
            : raw || 'The last analysis failed. Click Re-analyze to try again.',
        );

        const history = await getNicheHistory(projectId, accessToken);
        const lastComplete = history.find((h) => h.status === 'complete');
        if (lastComplete) {
          const full = await getNicheProfile(lastComplete.id, accessToken);
          setProfile(await resolveProfileWithPillars(full));
          await loadAnalytics(full.id, isStructureComplete(full));
        }
        return;
      }

      if (p.status === 'processing' || p.status === 'queued') {
        const status = await getNicheAnalysisStatus(p.id, accessToken);
        if (status.status === 'failed') {
          setError(status.errorMessage ?? 'The last analysis failed. Click Re-analyze to try again.');
          const history = await getNicheHistory(projectId, accessToken);
          const lastComplete = history.find((h) => h.status === 'complete');
          if (lastComplete) {
            const full = await getNicheProfile(lastComplete.id, accessToken);
            setProfile(await resolveProfileWithPillars(full));
            await loadAnalytics(full.id, isStructureComplete(full));
          }
          return;
        }

        // Resume in-progress run — poll until complete or failed (no stale banner on load).
        setProfile(null);
        setCoverage([]);
        setGaps([]);
        setAnalyzeProfileId(p.id);
        setAnalyzing(true);
        return;
      }

      const full = await resolveProfileWithPillars(p);
      setProfile(full);
      await loadAnalytics(full.id, isStructureComplete(full));
    } catch (e) {
      if (e instanceof SeoApiError && e.status !== 404) {
        setError(
          e.status === 503
            ? 'Analysis service is temporarily unavailable. Try again in a moment.'
            : `Failed to load existing analysis (${e.status}).`,
        );
      }
      // 404 = no existing profile, show form
    }
  }

  async function loadAnalytics(profileId: string, structureReady = true) {
    if (!accessToken || !structureReady) return;
    const [cov, g, prog] = await Promise.allSettled([
      getNicheCoverageMatrix(profileId, accessToken),
      getNicheGaps(profileId, quickWinsOnly, accessToken),
      getNicheProgress(projectId, accessToken),
    ]);
    if (cov.status === 'fulfilled') setCoverage(cov.value);
    if (g.status === 'fulfilled') setGaps(g.value);
    if (prog.status === 'fulfilled') setProgress(prog.value);
  }

  function isStructureComplete(status: NicheAnalysisStatus | NicheProfileResult): boolean {
    if ('structureStatus' in status && status.structureStatus)
      return status.structureStatus === 'complete';
    return status.status === 'complete';
  }

  async function handleAnalyze() {
    const selected = projects.find((p) => p.id === projectId);
    if (!selected) return;
    setError(null);
    setStepStatuses(undefined);
    setAnalyzeProfileId(null);
    setProfile(null);
    setCoverage([]);
    setGaps([]);
    setAnalyzing(true);
    try {
      const { profileId } = await analyzeNiche(projectId, selected.url.trim(), accessToken);
      setAnalyzeProfileId(profileId);
    } catch (e) {
      try {
        const latest = await getLatestNicheProfile(projectId, accessToken);
        if (latest && (latest.status === 'queued' || latest.status === 'processing')) {
          setAnalyzeProfileId(latest.id);
          return;
        }
      } catch {
        // Fall through to the original error message when recovery fails.
      }

      setError(e instanceof Error ? e.message : 'Failed to start analysis');
      setAnalyzing(false);
    }
  }

  const handleAnalysisComplete = useCallback(
    async (completedProfileId: string) => {
      setAnalyzing(false);
      setAnalyzeProfileId(null);
      try {
        const p = await getNicheProfile(completedProfileId, accessToken);
        setProfile(p);
        // Hydrate step statuses from the status endpoint
        void getNicheAnalysisStatus(completedProfileId, accessToken).then(s => {
          if (s.stepStatuses) setStepStatuses(s.stepStatuses as Record<string, string>);
        });
        await loadAnalytics(p.id, isStructureComplete(p));
        if (p.pillars.length > 0) {
          setError(null);
        } else if (p.totalPillarsIdentified > 0) {
          setError(
            'Analysis saved pillar counts but the pillar list did not persist. Run Re-analyze once; if the table stays empty, we need to fix storage on the server.',
          );
        } else {
          setError(
            'No pillars were detected. For sites like geekatyourspot.com, pillars usually come from schema.org JSON-LD on the homepage (knowsAbout). Ensure the homepage exposes JSON-LD and re-run analysis.',
          );
        }
      } catch {
        setError('Analysis complete but failed to load results.');
      }
    },
    [accessToken, projectId, quickWinsOnly],
  );

  const handleAnalysisError = useCallback((msg: string) => {
    setAnalyzing(false);
    setAnalyzeProfileId(null);
    setError(msg);
  }, []);

  async function handleQuickWinsToggle(qw: boolean) {
    setQuickWinsOnly(qw);
    if (profile && isStructureComplete(profile)) {
      const g = await getNicheGaps(profile.id, qw, accessToken);
      setGaps(g);
    }
  }

  const selected = projects.find((p) => p.id === projectId);

  if (authLoading) return <main className="p-8 text-sm text-[var(--color-text-muted)]">Loading…</main>;

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">
            How search engines understand your site
          </h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Composite view of public crawl and schema signals — what search systems would associate
            with your domain, plus optional keyword and SERP validation. Not a Google-only clone.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <select
            className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm"
            value={projectId}
            onChange={(e) => setProjectId(e.target.value)}
          >
            {projects.map((p) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
          <button
            onClick={handleAnalyze}
            disabled={!projectId || anyStepRunning}
            title={anyStepRunning ? 'A step is currently running — wait for it to complete' : undefined}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {analyzing ? 'Re-analyze (restart)' : profile ? 'Re-analyze' : 'Analyze'}
          </button>
        </div>
      </div>

      {error && (
        <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {analysisStarting && (
        <div className="mt-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
          <p className="text-sm font-medium text-[var(--color-text-primary)]">
            Starting analysis for {selected?.url}…
          </p>
          <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
            Creating a new run and attaching live progress.
          </p>
        </div>
      )}

      {analyzing && analyzeProfileId && (
        <div className="mt-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
          <p className="mb-4 text-sm font-medium text-[var(--color-text-primary)]">
            Analyzing {selected?.url}…
          </p>
          <AnalysisStatusListener
            profileId={analyzeProfileId}
            accessToken={accessToken}
            onComplete={handleAnalysisComplete}
            onError={handleAnalysisError}
          />
          <AnalysisStepBreakdown
            profileId={analyzeProfileId}
            projectId={projectId}
            accessToken={accessToken}
            defaultOpen={false}
            pollIntervalMs={4000}
          />
        </div>
      )}

      {!analyzing && !profile && (
        <div className="mt-12 rounded-xl border border-dashed border-[var(--color-border)] p-12 text-center">
          <p className="text-lg font-medium text-[var(--color-text-primary)]">No analysis yet</p>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Select a project and click <strong>Analyze</strong> to identify your core niche pillars.
          </p>
        </div>
      )}

      {profile && !analyzing && (
        <div className="mt-6 space-y-6">
          <NicheHeader profile={profile} />
          <ContentGuardContextBanner projectId={projectId} />

          {/* Tabs */}
          <div className="flex gap-1 border-b border-[var(--color-border)]">
            {(['pillars', 'competitors', 'contentIdeas', 'progress'] as Tab[]).map((t) => (
              <button
                key={t}
                onClick={() => setTab(t)}
                className={`px-4 py-2.5 text-sm font-medium transition-colors ${
                  tab === t
                    ? 'border-b-2 border-[var(--color-accent)] text-[var(--color-accent)]'
                    : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
                }`}
              >
                {TAB_LABELS[t]}
              </button>
            ))}
          </div>

          {tab === 'pillars' && (
            <div className="space-y-6">
              <PillarProvenanceCallout
                profileId={profile.id}
                accessToken={accessToken}
                pillarCount={profile.pillars.length || profile.totalPillarsIdentified}
              />
              <CoverageMatrixTable
                pillars={profile.pillars}
                coverageFallback={coverage}
                totalPillarsIdentified={profile.totalPillarsIdentified}
                pillarsCovered={profile.pillarsCovered}
                pillarsPartial={profile.pillarsPartial}
                pillarsGap={profile.pillarsGap}
              />
              <PillarSerpInsightsPanel pillars={profile.pillars} />
              <div className="space-y-2">
                <div>
                  <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">
                    Discovery signals
                  </h3>
                  <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">
                    How topics were discovered from your site signals. The coverage sections above list only
                    your {profile.pillars.length || profile.totalPillarsIdentified} selected
                    pillars. The topic candidate matrix is in the scan breakdown below (one place
                    only).
                  </p>
                </div>
                <TopicProfileSection
                  profileId={profile.id}
                  projectId={projectId}
                  accessToken={accessToken}
                  showMatrix={false}
                />
              </div>
            </div>
          )}

          {tab === 'competitors' && (
            <NicheCompetitorPanel
              profileId={profile.id}
              competitors={profile.competitors}
              accessToken={accessToken}
              onCompetitorsUpdated={() => {
                void getNicheProfile(profile.id, accessToken).then(setProfile);
              }}
            />
          )}

          {tab === 'contentIdeas' && (
            <TopicalGapsPanel
              gaps={gaps}
              projectId={projectId}
              accessToken={accessToken}
              onQuickWinsToggle={handleQuickWinsToggle}
            />
          )}

          {tab === 'progress' && (
            <AuthorityProgressChart points={progress} />
          )}

          <AnalysisStepBreakdown
            profileId={profile.id}
            projectId={projectId}
            accessToken={accessToken}
            defaultOpen={false}
            stepStatuses={stepStatuses as Record<string, import('@/lib/seo-api').StepStatus> | undefined}
            anyStepRunning={anyStepRunning}
            onStepRerun={() => {
              void getNicheAnalysisStatus(profile.id, accessToken).then(s => {
                if (s.stepStatuses) setStepStatuses(s.stepStatuses as Record<string, string>);
              });
            }}
          />
        </div>
      )}
    </main>
  );
}
