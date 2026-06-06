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
  type SeoProject,
  type NicheProfileResult,
  type PillarCoverageMatrix,
  type TopicalGapSummary,
  type AuthorityProgressPoint,
} from '@/lib/seo-api';
import { AnalysisStepBreakdown } from '@/components/niche-analyzer/AnalysisStepBreakdown';
import { NicheHeader } from '@/components/niche-analyzer/NicheHeader';
import { CoverageMatrixTable } from '@/components/niche-analyzer/CoverageMatrixTable';
import { TopicalGapsPanel } from '@/components/niche-analyzer/TopicalGapsPanel';
import { AuthorityProgressChart } from '@/components/niche-analyzer/AuthorityProgressChart';
import { AnalysisStatusListener } from '@/components/niche-analyzer/AnalysisStatusListener';

type Tab = 'pillars' | 'gaps' | 'progress';

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
          await loadAnalytics(full.id);
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
            await loadAnalytics(full.id);
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
      await loadAnalytics(full.id);
    } catch {
      // no existing profile — show form
    }
  }

  async function loadAnalytics(profileId: string) {
    if (!accessToken) return;
    const [cov, g, prog] = await Promise.allSettled([
      getNicheCoverageMatrix(profileId, accessToken),
      getNicheGaps(profileId, quickWinsOnly, accessToken),
      getNicheProgress(projectId, accessToken),
    ]);
    if (cov.status === 'fulfilled') setCoverage(cov.value);
    if (g.status === 'fulfilled') setGaps(g.value);
    if (prog.status === 'fulfilled') setProgress(prog.value);
  }

  async function handleAnalyze() {
    const selected = projects.find((p) => p.id === projectId);
    if (!selected) return;
    setError(null);
    setProfile(null);
    setCoverage([]);
    setGaps([]);
    setAnalyzing(true);
    try {
      const { profileId } = await analyzeNiche(projectId, selected.url.trim(), accessToken);
      setAnalyzeProfileId(profileId);
    } catch (e) {
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
        await loadAnalytics(p.id);
        if (p.pillars.length === 0) {
          if (p.totalPillarsIdentified > 0) {
            setError(
              'Analysis saved pillar counts but the pillar list did not persist. Run Re-analyze once; if the table stays empty, we need to fix storage on the server.',
            );
          } else {
            setError(
              'No pillars were detected. For sites like geekatyourspot.com, pillars usually come from schema.org JSON-LD on the homepage (knowsAbout). Ensure the homepage exposes JSON-LD and re-run analysis.',
            );
          }
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
    if (profile) {
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
            Niche Analyzer
          </h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Identify core pillars, score topical authority, and surface content gaps — no GSC required.
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
            disabled={!projectId}
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
          <AnalysisStepBreakdown profileId={profile.id} accessToken={accessToken} />

          {/* Tabs */}
          <div className="flex gap-1 border-b border-[var(--color-border)]">
            {(['pillars', 'gaps', 'progress'] as Tab[]).map((t) => (
              <button
                key={t}
                onClick={() => setTab(t)}
                className={`px-4 py-2.5 text-sm font-medium transition-colors capitalize ${
                  tab === t
                    ? 'border-b-2 border-[var(--color-accent)] text-[var(--color-accent)]'
                    : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
                }`}
              >
                {t}
              </button>
            ))}
          </div>

          {tab === 'pillars' && (
            <CoverageMatrixTable
              pillars={profile.pillars}
              coverageFallback={coverage}
              totalPillarsIdentified={profile.totalPillarsIdentified}
            />
          )}

          {tab === 'gaps' && (
            <TopicalGapsPanel gaps={gaps} onQuickWinsToggle={handleQuickWinsToggle} />
          )}

          {tab === 'progress' && (
            <AuthorityProgressChart points={progress} />
          )}
        </div>
      )}
    </main>
  );
}
