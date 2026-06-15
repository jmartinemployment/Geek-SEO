'use client';

import { useCallback, useEffect, useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import {
  listProjects,
  createProject,
  analyzeNiche,
  getNicheAnalysisStatus,
  getLatestNicheProfile,
  getNicheProfile,
  getNicheCoverageMatrix,
  getNicheGaps,
  getNicheProgress,
  SeoApiError,
  type SeoProject,
  type NicheProfileResult,
  type NicheAnalysisStatus,
  type PillarCoverageMatrix,
  type TopicalGapSummary,
  type AuthorityProgressPoint,
  type StepStatus,
} from '@/lib/seo-api';
import {
  isAnyNicheStepRunning,
  mergeStepStatuses,
} from '@/lib/niche-step-status';
import { useNicheAnalysisSignalR } from '@/hooks/use-niche-analysis-signalr';
import { AnalysisStepBreakdown } from '@/components/niche-analyzer/AnalysisStepBreakdown';
import { TopicProfileSection } from '@/components/niche-analyzer/TopicProfileSection';
import { PillarProvenanceCallout } from '@/components/niche-analyzer/PillarProvenanceCallout';
import { NicheHeader } from '@/components/niche-analyzer/NicheHeader';
import { CoverageMatrixTable } from '@/components/niche-analyzer/CoverageMatrixTable';
import { TopicalGapsPanel } from '@/components/niche-analyzer/TopicalGapsPanel';
import { AuthorityProgressChart } from '@/components/niche-analyzer/AuthorityProgressChart';
import { PillarSerpInsightsPanel } from '@/components/niche-analyzer/PillarSerpInsightsPanel';
import { NicheCompetitorPanel } from '@/components/niche-analyzer/NicheCompetitorPanel';
import { ContentGuardContextBanner } from '@/components/niche-analyzer/ContentGuardContextBanner';

type Tab = 'pillars' | 'competitors' | 'contentIdeas' | 'progress';

const TAB_LABELS: Record<Tab, string> = {
  pillars: 'Pillars',
  competitors: 'Competitors',
  contentIdeas: 'Content ideas',
  progress: 'Progress',
};

function isManualWorkflowStatus(status: string | undefined): boolean {
  return status === 'pending' || status === 'processing' || status === 'queued';
}

export default function NicheAnalyzerPage() {
  const { accessToken, authLoading, authReady } = useAuthReady();
  const [projects, setProjects] = useState<SeoProject[]>([]);
  const [projectId, setProjectId] = useState('');
  const [profile, setProfile] = useState<NicheProfileResult | null>(null);
  const [coverage, setCoverage] = useState<PillarCoverageMatrix[]>([]);
  const [gaps, setGaps] = useState<TopicalGapSummary[]>([]);
  const [progress, setProgress] = useState<AuthorityProgressPoint[]>([]);
  const [workflowProfileId, setWorkflowProfileId] = useState<string | null>(null);
  const [startingAnalysis, setStartingAnalysis] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>('pillars');
  const [quickWinsOnly, setQuickWinsOnly] = useState(false);
  const [stepStatuses, setStepStatuses] = useState<Record<string, StepStatus> | undefined>();
  const [stepSummaries, setStepSummaries] = useState<Record<string, string> | undefined>();
  const [stepErrors, setStepErrors] = useState<Record<string, string> | undefined>();
  const [newProjectName, setNewProjectName] = useState('');
  const [newProjectUrl, setNewProjectUrl] = useState('https://');
  const [newProjectLocation, setNewProjectLocation] = useState('');
  const [creatingProject, setCreatingProject] = useState(false);

  const anyStepRunning = isAnyNicheStepRunning(stepStatuses);

  const applyAnalysisStatus = useCallback((status: NicheAnalysisStatus) => {
    if (status.stepStatuses) {
      setStepStatuses((prev) => mergeStepStatuses(prev, status.stepStatuses));
    }
    if (status.stepSummaries) setStepSummaries(status.stepSummaries);
    if (status.stepErrors) setStepErrors(status.stepErrors);
  }, []);

  const refreshStepStatuses = useCallback(
    async (profileId: string) => {
      if (!accessToken) return;
      const status = await getNicheAnalysisStatus(profileId, accessToken);
      applyAnalysisStatus(status);
      return status;
    },
    [accessToken, applyAnalysisStatus],
  );

  useEffect(() => {
    if (!authReady) return;
    void listProjects(accessToken).then((list) => {
      setProjects(list);
      if (list[0]) setProjectId(list[0].id);
    });
  }, [accessToken, authReady]);

  useEffect(() => {
    if (!authReady || !projectId) return;
    setWorkflowProfileId(null);
    setProfile(null);
    setCoverage([]);
    setGaps([]);
    setProgress([]);
    setStepStatuses(undefined);
    setStepSummaries(undefined);
    setStepErrors(undefined);
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

  const handleAnalysisComplete = useCallback(
    async (completedProfileId: string) => {
      try {
        const p = await getNicheProfile(completedProfileId, accessToken);
        setProfile(p);
        void refreshStepStatuses(completedProfileId);
        await loadAnalytics(p.id, isStructureComplete(p));
        if (p.pillars.length > 0) {
          setError(null);
        } else if (p.totalPillarsIdentified > 0) {
          setError(
            'Analysis saved pillar counts but the pillar list did not persist. Reset and re-run steps; if the table stays empty, we need to fix storage on the server.',
          );
        } else {
          setError(
            'No pillars were detected. For sites like geekatyourspot.com, pillars usually come from schema.org JSON-LD on the homepage (knowsAbout). Ensure the homepage exposes JSON-LD and re-run the early steps.',
          );
        }
      } catch {
        setError('Analysis complete but failed to load results.');
      }
    },
    [accessToken, projectId, quickWinsOnly, refreshStepStatuses],
  );

  useNicheAnalysisSignalR(
    workflowProfileId,
    accessToken,
    applyAnalysisStatus,
    {
      onComplete: (completedProfileId) => {
        setWorkflowProfileId(null);
        void handleAnalysisComplete(completedProfileId);
      },
    },
  );

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
        applyAnalysisStatus(status);
        setWorkflowProfileId(p.id);
        setProfile(null);
        setCoverage([]);
        setGaps([]);
        // Step-level failures belong on the step row — not a page-level dead end.
        if (status.stepErrors && Object.keys(status.stepErrors).length > 0) {
          setError(null);
        } else {
          setError(
            status.errorMessage ?? 'The last analysis failed. Re-run the failed step to try again.',
          );
        }
        return;
      }

      if (isManualWorkflowStatus(p.status)) {
        setProfile(null);
        setCoverage([]);
        setGaps([]);
        setWorkflowProfileId(p.id);
        await refreshStepStatuses(p.id);
        return;
      }

      const full = await resolveProfileWithPillars(p);
      setProfile(full);
      await loadAnalytics(full.id, isStructureComplete(full));
      await refreshStepStatuses(full.id);
    } catch (e) {
      if (e instanceof SeoApiError && e.status !== 404) {
        setError(
          e.status === 503
            ? 'Analysis service is temporarily unavailable. Try again in a moment.'
            : `Failed to load existing analysis (${e.status}).`,
        );
      }
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

  async function handleCreateProject(e: React.FormEvent) {
    e.preventDefault();
    const location = newProjectLocation.trim();
    if (!location) {
      setError('Default location is required.');
      return;
    }
    setCreatingProject(true);
    setError(null);
    try {
      const project = await createProject(
        {
          name: newProjectName.trim(),
          url: newProjectUrl.trim(),
          defaultLocation: location,
        },
        accessToken,
      );
      setProjects([project]);
      setProjectId(project.id);
      setNewProjectName('');
      setNewProjectUrl('https://');
      setNewProjectLocation('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create project');
    } finally {
      setCreatingProject(false);
    }
  }

  async function handleAnalyze() {
    const selected = projects.find((p) => p.id === projectId);
    if (!selected) return;
    setError(null);
    setStepStatuses(undefined);
    setStepSummaries(undefined);
    setStepErrors(undefined);
    setProfile(null);
    setCoverage([]);
    setGaps([]);
    setStartingAnalysis(true);
    try {
      const { profileId } = await analyzeNiche(projectId, selected.url.trim(), accessToken);
      setWorkflowProfileId(profileId);
      await refreshStepStatuses(profileId);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start analysis');
      setWorkflowProfileId(null);
    } finally {
      setStartingAnalysis(false);
    }
  }

  async function handleQuickWinsToggle(qw: boolean) {
    setQuickWinsOnly(qw);
    if (profile && isStructureComplete(profile)) {
      const g = await getNicheGaps(profile.id, qw, accessToken);
      setGaps(g);
    }
  }

  const selected = projects.find((p) => p.id === projectId);
  const showWorkflow = Boolean(workflowProfileId);
  const showResults = Boolean(profile) && !showWorkflow;
  const hasProject = projects.length > 0 && Boolean(projectId);

  if (authLoading) return <main className="p-8 text-sm text-[var(--color-text-muted)]">Loading…</main>;

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">
            How search engines understand your site
          </h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Run each analysis step manually — discover, crawl, validate, and score — one at a time.
          </p>
        </div>

        <div className="flex items-center gap-3">
          {hasProject ? (
            <>
              <select
                className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm"
                value={projectId}
                onChange={(e) => setProjectId(e.target.value)}
              >
                {projects.map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
              {!showWorkflow && !profile ? (
                <button
                  onClick={handleAnalyze}
                  disabled={!projectId || anyStepRunning || startingAnalysis}
                  title={anyStepRunning ? 'A step is currently running — wait for it to complete' : undefined}
                  className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {startingAnalysis ? 'Preparing…' : 'Start analysis'}
                </button>
              ) : null}
            </>
          ) : null}
        </div>
      </div>

      {!hasProject && (
        <form
          onSubmit={handleCreateProject}
          className="mt-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6"
        >
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Create a project</h2>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            One project per website URL. Set your target market, then start niche analysis.
          </p>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5 text-sm sm:col-span-2">
              <span className="font-medium text-[var(--color-text-primary)]">Project name</span>
              <input
                className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
                placeholder="e.g. Geek At Your Spot"
                value={newProjectName}
                onChange={(e) => setNewProjectName(e.target.value)}
                required
              />
            </label>
            <label className="flex flex-col gap-1.5 text-sm sm:col-span-2">
              <span className="font-medium text-[var(--color-text-primary)]">Website URL</span>
              <input
                className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
                placeholder="https://yourdomain.com"
                value={newProjectUrl}
                onChange={(e) => setNewProjectUrl(e.target.value)}
                required
              />
            </label>
            <label className="flex flex-col gap-1.5 text-sm sm:col-span-2">
              <span className="font-medium text-[var(--color-text-primary)]">Default location</span>
              <input
                className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
                placeholder="Fort Lauderdale, Florida, United States"
                value={newProjectLocation}
                onChange={(e) => setNewProjectLocation(e.target.value)}
                required
              />
              <span className="text-xs text-[var(--color-text-muted)]">
                Target market for SERP and keyword analysis. Use &quot;United States&quot; for national targeting.
              </span>
            </label>
          </div>
          <button
            type="submit"
            disabled={creatingProject}
            className="mt-4 rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {creatingProject ? 'Creating…' : 'Create project'}
          </button>
        </form>
      )}

      {hasProject && selected && (
        <p className="mt-3 text-sm text-[var(--color-text-secondary)]">
          <span className="font-medium text-[var(--color-text-primary)]">{selected.url}</span>
          {' · '}
          {selected.defaultLocation}
        </p>
      )}

      {error && (
        <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {showWorkflow && workflowProfileId && (
        <div className="mt-6 space-y-4">
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
            <p className="text-sm font-medium text-[var(--color-text-primary)]">
              Manual analysis — {selected?.url}
            </p>
            <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
              Run steps in order. Each step reads prior results from the database — nothing runs automatically.
            </p>
          </div>
          <AnalysisStepBreakdown
            profileId={workflowProfileId}
            projectId={projectId}
            accessToken={accessToken}
            defaultOpen
            stepStatuses={stepStatuses}
            stepSummaries={stepSummaries}
            stepErrors={stepErrors}
            anyStepRunning={anyStepRunning}
            onStepStatusChange={applyAnalysisStatus}
            onStepRerun={async () => {
              await refreshStepStatuses(workflowProfileId);
            }}
          />
        </div>
      )}

      {!showWorkflow && !profile && hasProject && (
        <div className="mt-12 rounded-xl border border-dashed border-[var(--color-border)] p-12 text-center">
          <p className="text-lg font-medium text-[var(--color-text-primary)]">No analysis yet</p>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Click <strong>Start analysis</strong> for{' '}
            <strong className="text-[var(--color-text-primary)]">{selected?.url}</strong>, then run each step manually.
          </p>
        </div>
      )}

      {showResults && profile && (
        <div className="mt-6 space-y-6">
          <NicheHeader profile={profile} />
          <ContentGuardContextBanner projectId={projectId} />

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
            stepStatuses={stepStatuses}
            stepSummaries={stepSummaries}
            stepErrors={stepErrors}
            anyStepRunning={anyStepRunning}
            onStepStatusChange={applyAnalysisStatus}
            onStepRerun={async () => {
              await refreshStepStatuses(profile.id);
            }}
          />
        </div>
      )}
    </main>
  );
}
