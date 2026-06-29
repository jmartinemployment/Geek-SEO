'use client';

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { resolveBodyLinkStatus } from '@/components/content-writing/cluster-body-link-plan-editor';
import { ClusterBodyLinkPlanEditor } from '@/components/content-writing/cluster-body-link-plan-editor';
import { resolveFaqLinkStatus } from '@/components/content-writing/cluster-faq-plan-editor';
import { ClusterFaqPlanEditor } from '@/components/content-writing/cluster-faq-plan-editor';
import {
  ClusterStatCard,
  ClusterTabBar,
  type ClusterDashboardTab,
} from '@/components/content-writing/cluster-plan-stats';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { contentWritingPath } from '@/lib/content-writing-search-params';
import { extractPillarH2Hints } from '@/lib/pillar-h2-hints';
import {
  applyBodyLinks,
  buildClusterPlan,
  createContentSpoke,
  generateContentSpoke,
  generateAllContentSpokes,
  generateLinkedFaqs,
  getClusterPlan,
  listContentSpokes,
  saveClusterPlan,
  type ContentClusterPlanResult,
  type ContentLinkBodySlot,
  type ContentLinkFaqItem,
  type ContentLinkPlan,
  type ContentSpokeSummary,
} from '@/lib/seo-api';

function isSpokeGenerated(spoke: ContentSpokeSummary): boolean {
  return spoke.status === 'body_generated' || spoke.wordCount > 80;
}

function plansEqual(a: ContentLinkFaqItem[], b: ContentLinkFaqItem[]): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

function bodyPlansEqual(a: ContentLinkBodySlot[], b: ContentLinkBodySlot[]): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

function countLinkReady(
  faqItems: ContentLinkFaqItem[],
  bodyItems: ContentLinkBodySlot[],
  spokes: ContentSpokeSummary[],
): number {
  const faqReady = faqItems.filter((item) => resolveFaqLinkStatus(item, spokes) === 'linked').length;
  const bodyReady = bodyItems.filter((item) => resolveBodyLinkStatus(item, spokes) === 'linked').length;
  return faqReady + bodyReady;
}

export function ClusterPlanPanel() {
  const { doc, accessToken, reloadDocument } = useWritingWorkspace();
  const [activeTab, setActiveTab] = useState<ClusterDashboardTab>('overview');
  const [savedPlan, setSavedPlan] = useState<ContentLinkPlan>({ faqItems: [], bodyLinks: [] });
  const [faqPlan, setFaqPlan] = useState<ContentLinkFaqItem[]>([]);
  const [bodyLinkPlan, setBodyLinkPlan] = useState<ContentLinkBodySlot[]>([]);
  const [planDirty, setPlanDirty] = useState(false);
  const [bodyPlanDirty, setBodyPlanDirty] = useState(false);
  const [savingPlan, setSavingPlan] = useState(false);
  const [result, setResult] = useState<ContentClusterPlanResult | null>(null);
  const [spokes, setSpokes] = useState<ContentSpokeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [creatingPhrase, setCreatingPhrase] = useState<string | null>(null);
  const [generatingSpokeId, setGeneratingSpokeId] = useState<string | null>(null);
  const [generatingAllSpokes, setGeneratingAllSpokes] = useState(false);
  const [generateAllSummary, setGenerateAllSummary] = useState<string | null>(null);
  const [generatingFaqs, setGeneratingFaqs] = useState(false);
  const [applyingBodyLinks, setApplyingBodyLinks] = useState(false);
  const [faqGenSummary, setFaqGenSummary] = useState<string | null>(null);
  const [bodyLinkSummary, setBodyLinkSummary] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const researchReady = Boolean(doc.analysisRunId);
  const isPillar = doc.documentKind !== 'spoke';
  const savedFaqCount = savedPlan.faqItems.length;
  const savedBodyLinkCount = savedPlan.bodyLinks.length;
  const headingHints = useMemo(() => extractPillarH2Hints(doc.contentHtml), [doc.contentHtml]);
  const planHasUnsavedEdits = planDirty || bodyPlanDirty;
  const generatedSpokeCount = spokes.filter(isSpokeGenerated).length;
  const shellSpokeCount = spokes.filter((s) => !isSpokeGenerated(s)).length;
  const linksReadyCount = countLinkReady(faqPlan, bodyLinkPlan, spokes);
  const hasPlan = savedFaqCount > 0 || savedBodyLinkCount > 0;

  const load = useCallback(async () => {
    if (!accessToken || !researchReady || !isPillar) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      setError(null);
      const [plan, spokeList] = await Promise.all([
        getClusterPlan(doc.id, accessToken),
        listContentSpokes(doc.id, accessToken),
      ]);
      setSavedPlan(plan);
      setFaqPlan(plan.faqItems);
      setBodyLinkPlan(plan.bodyLinks);
      setPlanDirty(false);
      setBodyPlanDirty(false);
      setSpokes(spokeList);
    } catch {
      setSavedPlan({ faqItems: [], bodyLinks: [] });
      setFaqPlan([]);
      setBodyLinkPlan([]);
      setPlanDirty(false);
      setBodyPlanDirty(false);
      setSpokes([]);
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id, isPillar, researchReady]);

  useEffect(() => {
    void load();
  }, [load]);

  function handleFaqPlanChange(items: ContentLinkFaqItem[]) {
    setFaqPlan(items);
    setPlanDirty(!plansEqual(items, savedPlan.faqItems));
  }

  function handleBodyLinkPlanChange(items: ContentLinkBodySlot[]) {
    setBodyLinkPlan(items);
    setBodyPlanDirty(!bodyPlansEqual(items, savedPlan.bodyLinks));
  }

  async function handleSavePlan() {
    if (!accessToken) return;
    setSavingPlan(true);
    setError(null);
    try {
      const saved = await saveClusterPlan(
        doc.id,
        { faqItems: faqPlan, bodyLinks: bodyLinkPlan },
        accessToken,
      );
      setSavedPlan(saved);
      setFaqPlan(saved.faqItems);
      setBodyLinkPlan(saved.bodyLinks);
      setPlanDirty(false);
      setBodyPlanDirty(false);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save link plan');
    } finally {
      setSavingPlan(false);
    }
  }

  async function handleBuild() {
    if (!accessToken) return;
    setBusy(true);
    setError(null);
    try {
      const built = await buildClusterPlan(doc.id, accessToken);
      setResult(built);
      setSavedPlan((prev) => ({
        ...prev,
        faqItems: built.faqItems,
        bodyLinks: built.bodyLinks,
      }));
      setFaqPlan(built.faqItems);
      setBodyLinkPlan(built.bodyLinks);
      setPlanDirty(false);
      setBodyPlanDirty(false);
      setActiveTab('overview');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not build cluster plan');
    } finally {
      setBusy(false);
    }
  }

  async function handleCreateSpoke(candidate: {
    phrase: string;
    sourceType: string;
    suggestedQuestion?: string | null;
    suggestedSlug?: string | null;
  }) {
    if (!accessToken) return;
    setCreatingPhrase(candidate.phrase);
    setError(null);
    try {
      const created = await createContentSpoke(
        doc.id,
        {
          phrase: candidate.phrase,
          sourceType: candidate.sourceType,
          title: candidate.suggestedQuestion ?? undefined,
          publishSlug: candidate.suggestedSlug ?? undefined,
        },
        accessToken,
      );
      setSpokes((prev) => [created, ...prev.filter((s) => s.id !== created.id)]);
      setActiveTab('spokes');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create spoke');
    } finally {
      setCreatingPhrase(null);
    }
  }

  async function handleGenerateSpoke(spoke: ContentSpokeSummary) {
    if (!accessToken) return;
    setGeneratingSpokeId(spoke.id);
    setError(null);
    try {
      const generated = await generateContentSpoke(doc.id, spoke.id, accessToken);
      setSpokes((prev) => prev.map((s) => (s.id === generated.id ? generated : s)));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not generate spoke');
    } finally {
      setGeneratingSpokeId(null);
    }
  }

  async function handleGenerateAllSpokes() {
    if (!accessToken) return;
    setGeneratingAllSpokes(true);
    setError(null);
    setGenerateAllSummary(null);
    try {
      const result = await generateAllContentSpokes(doc.id, accessToken);
      setSpokes(result.spokes);
      const failureNote =
        result.failures.length > 0 ? ` · ${result.failures.length} failed` : '';
      setGenerateAllSummary(
        `Batch generation: ${result.generatedCount} generated, ${result.skippedCount} skipped${failureNote}.`,
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not generate all spokes');
    } finally {
      setGeneratingAllSpokes(false);
    }
  }

  async function handleGenerateLinkedFaqs() {
    if (!accessToken) return;
    if (planHasUnsavedEdits) {
      setError('Save plan changes before generating linked FAQs.');
      return;
    }
    setGeneratingFaqs(true);
    setError(null);
    setFaqGenSummary(null);
    try {
      const generated = await generateLinkedFaqs(doc.id, accessToken);
      setFaqGenSummary(
        `Linked FAQs updated: ${generated.linkedCount} with links, ${generated.plainTextOnlyCount} plain text only.`,
      );
      await reloadDocument();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not generate linked FAQs');
    } finally {
      setGeneratingFaqs(false);
    }
  }

  async function handleApplyBodyLinks() {
    if (!accessToken) return;
    if (planHasUnsavedEdits) {
      setError('Save plan changes before applying body links.');
      return;
    }
    setApplyingBodyLinks(true);
    setError(null);
    setBodyLinkSummary(null);
    try {
      const applied = await applyBodyLinks(doc.id, accessToken);
      setBodyLinkSummary(
        applied.changed
          ? `Body links applied: ${applied.appliedCount} inserted${applied.pendingCount > 0 ? ` · ${applied.pendingCount} pending spoke generation` : ''}.`
          : applied.pendingCount > 0
            ? `No links inserted yet — ${applied.pendingCount} slot(s) waiting on generated spokes.`
            : 'No matching body link slots were applied.',
      );
      if (applied.changed) {
        await reloadDocument();
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not apply body links');
    } finally {
      setApplyingBodyLinks(false);
    }
  }

  function spokeExists(phrase: string): ContentSpokeSummary | undefined {
    return spokes.find(
      (s) => s.spokeSourcePhrase?.toLowerCase() === phrase.toLowerCase(),
    );
  }

  if (!researchReady || !isPillar) {
    return null;
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="py-8 text-sm text-[var(--color-text-secondary)]">
          Loading cluster dashboard…
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="overflow-hidden">
      <CardHeader className="border-b bg-[var(--color-surface-muted)]/40">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Cluster link dashboard</CardTitle>
            <CardDescription className="mt-1 max-w-2xl">
              Hub-and-spoke internal linking from your frozen SERP research — spokes, FAQ links, and
              contextual body links in one place.
            </CardDescription>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {planHasUnsavedEdits ? (
              <button
                type="button"
                onClick={() => void handleSavePlan()}
                disabled={savingPlan || busy}
                className="rounded-lg border border-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-[var(--color-accent)] hover:bg-[var(--color-accent)]/5 disabled:opacity-50"
              >
                {savingPlan ? 'Saving…' : 'Save plan'}
              </button>
            ) : null}
            <button
              type="button"
              onClick={() => void handleBuild()}
              disabled={busy}
              className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-60"
            >
              {busy ? 'Building…' : hasPlan ? 'Rebuild plan' : 'Build plan'}
            </button>
          </div>
        </div>
      </CardHeader>

      <CardContent className="space-y-4 pt-4">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <ClusterStatCard label="Spokes" value={spokes.length} hint={`${generatedSpokeCount} generated`} />
          <ClusterStatCard
            label="FAQ slots"
            value={savedFaqCount}
            hint={savedFaqCount > 0 ? 'Closing FAQ links' : 'Build plan to add'}
          />
          <ClusterStatCard
            label="Body links"
            value={savedBodyLinkCount}
            hint={headingHints.length > 0 ? `${headingHints.length} H2 sections` : 'Add H2s to pillar'}
          />
          <ClusterStatCard
            label="Links ready"
            value={linksReadyCount}
            hint="Targets with generated spokes"
            tone={linksReadyCount > 0 ? 'success' : 'muted'}
          />
        </div>

        {error ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-800">
            {error}
          </p>
        ) : null}
        {faqGenSummary ? (
          <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800">
            {faqGenSummary}
          </p>
        ) : null}
        {bodyLinkSummary ? (
          <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800">
            {bodyLinkSummary}
          </p>
        ) : null}
        {generateAllSummary ? (
          <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800">
            {generateAllSummary}
          </p>
        ) : null}

        <ClusterTabBar
          active={activeTab}
          onChange={setActiveTab}
          counts={{
            spokes: spokes.length || result?.spokeCandidates.length,
            links: savedFaqCount + savedBodyLinkCount,
            research: result?.filteredOut.length,
          }}
        />

        {activeTab === 'overview' ? (
          <div className="grid gap-4 lg:grid-cols-2">
            <section className="rounded-xl border bg-white p-4 shadow-sm">
              <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">Quick actions</h4>
              <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
                {planHasUnsavedEdits
                  ? 'Save your link plan edits before applying links to the pillar.'
                  : 'Generate spokes first, then apply FAQ and body links to the pillar HTML.'}
              </p>
              <div className="mt-3 grid gap-2 sm:grid-cols-2">
                {shellSpokeCount > 0 ? (
                  <button
                    type="button"
                    onClick={() => void handleGenerateAllSpokes()}
                    disabled={generatingAllSpokes || busy || generatingSpokeId !== null}
                    className="rounded-lg bg-[var(--color-accent)] px-3 py-2 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50 sm:col-span-2"
                  >
                    {generatingAllSpokes
                      ? `Generating ${shellSpokeCount} spoke${shellSpokeCount === 1 ? '' : 's'}…`
                      : `Generate all spokes (${shellSpokeCount} shell${shellSpokeCount === 1 ? '' : 's'})`}
                  </button>
                ) : null}
                <button
                  type="button"
                  onClick={() => void handleGenerateLinkedFaqs()}
                  disabled={generatingFaqs || busy || planHasUnsavedEdits || savedFaqCount === 0}
                  className="rounded-lg border border-[var(--color-accent)] px-3 py-2 text-xs font-medium text-[var(--color-accent)] hover:bg-[var(--color-accent)]/5 disabled:opacity-50"
                >
                  {generatingFaqs ? 'Generating FAQs…' : 'Generate linked FAQs'}
                </button>
                <button
                  type="button"
                  onClick={() => void handleApplyBodyLinks()}
                  disabled={applyingBodyLinks || busy || planHasUnsavedEdits || savedBodyLinkCount === 0}
                  className="rounded-lg border border-[var(--color-accent)] px-3 py-2 text-xs font-medium text-[var(--color-accent)] hover:bg-[var(--color-accent)]/5 disabled:opacity-50"
                >
                  {applyingBodyLinks ? 'Applying links…' : 'Apply body links'}
                </button>
              </div>
            </section>

            <section className="rounded-xl border bg-white p-4 shadow-sm">
              <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">Spoke progress</h4>
              {spokes.length === 0 ? (
                <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
                  {hasPlan
                    ? 'Open the Spokes tab to create shells from candidates.'
                    : 'Build a plan to see spoke candidates from PAA/PASF.'}
                </p>
              ) : (
                <ul className="mt-3 space-y-2 text-xs">
                  {spokes.slice(0, 4).map((spoke) => (
                    <li
                      key={spoke.id}
                      className="flex items-center justify-between gap-2 rounded-lg border px-3 py-2"
                    >
                      <div className="min-w-0">
                        <Link
                          href={contentWritingPath({ documentId: spoke.id })}
                          className="block truncate font-medium text-[var(--color-accent)] underline"
                        >
                          {spoke.title}
                        </Link>
                        <p className="truncate text-[var(--color-text-secondary)]">
                          {isSpokeGenerated(spoke) ? 'Generated' : 'Shell'} · {spoke.wordCount} words
                        </p>
                      </div>
                      {!isSpokeGenerated(spoke) ? (
                        <button
                          type="button"
                          disabled={generatingSpokeId === spoke.id}
                          onClick={() => void handleGenerateSpoke(spoke)}
                          className="shrink-0 rounded border px-2 py-1 text-[var(--color-text-primary)] disabled:opacity-50"
                        >
                          {generatingSpokeId === spoke.id ? '…' : 'Generate'}
                        </button>
                      ) : (
                        <span className="shrink-0 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-medium text-emerald-800">
                          Ready
                        </span>
                      )}
                    </li>
                  ))}
                  {spokes.length > 4 ? (
                    <li>
                      <button
                        type="button"
                        onClick={() => setActiveTab('spokes')}
                        className="text-xs font-medium text-[var(--color-accent)] underline"
                      >
                        View all {spokes.length} spokes
                      </button>
                    </li>
                  ) : null}
                </ul>
              )}
            </section>
          </div>
        ) : null}

        {activeTab === 'spokes' ? (
          <div className="grid gap-4 xl:grid-cols-2">
            <section className="rounded-xl border bg-white p-4 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">
                  Created spokes ({spokes.length})
                </h4>
                {shellSpokeCount > 0 ? (
                  <button
                    type="button"
                    onClick={() => void handleGenerateAllSpokes()}
                    disabled={generatingAllSpokes || busy}
                    className="rounded-lg border border-[var(--color-accent)] px-2 py-1 text-xs font-medium text-[var(--color-accent)] hover:bg-[var(--color-accent)]/5 disabled:opacity-50"
                  >
                    {generatingAllSpokes ? 'Generating…' : 'Generate all shells'}
                  </button>
                ) : null}
              </div>
              {spokes.length === 0 ? (
                <p className="mt-2 text-xs text-[var(--color-text-secondary)]">No spokes yet.</p>
              ) : (
                <ul className="mt-3 space-y-2 text-xs">
                  {spokes.map((spoke) => (
                    <li key={spoke.id} className="rounded-lg border px-3 py-2">
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <div className="min-w-0">
                          <Link
                            href={contentWritingPath({ documentId: spoke.id })}
                            className="font-medium text-[var(--color-accent)] underline"
                          >
                            {spoke.title}
                          </Link>
                          {spoke.publishSlug ? (
                            <p className="text-[var(--color-text-secondary)]">/blog/{spoke.publishSlug}</p>
                          ) : null}
                          <p className="text-[var(--color-text-secondary)]">
                            {isSpokeGenerated(spoke) ? 'Generated' : 'Shell only'} · {spoke.wordCount} words
                          </p>
                          {spoke.contentPreview ? (
                            <details className="mt-2">
                              <summary className="cursor-pointer text-[var(--color-accent)]">
                                Content preview
                              </summary>
                              <p className="mt-1 text-[var(--color-text-secondary)]">{spoke.contentPreview}</p>
                            </details>
                          ) : null}
                        </div>
                        {!isSpokeGenerated(spoke) ? (
                          <button
                            type="button"
                            disabled={generatingSpokeId === spoke.id}
                            onClick={() => void handleGenerateSpoke(spoke)}
                            className="rounded border px-2 py-1 text-[var(--color-text-primary)] disabled:opacity-50"
                          >
                            {generatingSpokeId === spoke.id ? 'Generating…' : 'Generate spoke'}
                          </button>
                        ) : null}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section className="rounded-xl border bg-white p-4 shadow-sm">
              <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">
                Spoke candidates {result ? `(${result.spokeCandidates.length})` : ''}
              </h4>
              {!result ? (
                <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
                  Build a plan to score PAA/PASF from your research pack.
                </p>
              ) : result.spokeCandidates.length === 0 ? (
                <p className="mt-2 text-xs text-[var(--color-text-secondary)]">None matched filters.</p>
              ) : (
                <ul className="mt-3 space-y-2 text-xs">
                  {result.spokeCandidates.map((item) => {
                    const existing = spokeExists(item.phrase);
                    return (
                      <li key={item.phrase} className="rounded-lg border bg-slate-50 px-3 py-2">
                        <p className="font-medium text-[var(--color-text-primary)]">{item.phrase}</p>
                        <p className="text-[var(--color-text-secondary)]">{item.suggestedQuestion ?? '—'}</p>
                        {item.suggestedSlug ? (
                          <p className="text-[var(--color-text-secondary)]">/blog/{item.suggestedSlug}</p>
                        ) : null}
                        {existing ? (
                          <div className="mt-2 flex flex-wrap items-center gap-2">
                            <Link
                              href={contentWritingPath({ documentId: existing.id })}
                              className="text-[var(--color-accent)] underline"
                            >
                              Open spoke
                            </Link>
                            {!isSpokeGenerated(existing) ? (
                              <button
                                type="button"
                                disabled={generatingSpokeId === existing.id}
                                onClick={() => void handleGenerateSpoke(existing)}
                                className="rounded border px-2 py-0.5 disabled:opacity-50"
                              >
                                {generatingSpokeId === existing.id ? 'Generating…' : 'Generate'}
                              </button>
                            ) : null}
                          </div>
                        ) : (
                          <button
                            type="button"
                            disabled={creatingPhrase === item.phrase}
                            onClick={() => void handleCreateSpoke(item)}
                            className="mt-2 rounded border px-2 py-0.5 disabled:opacity-50"
                          >
                            {creatingPhrase === item.phrase ? 'Creating…' : 'Create spoke shell'}
                          </button>
                        )}
                      </li>
                    );
                  })}
                </ul>
              )}
            </section>
          </div>
        ) : null}

        {activeTab === 'links' ? (
          <div className="space-y-6">
            <section>
              <div className="mb-3 flex items-center justify-between gap-2">
                <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">
                  FAQ link plan ({faqPlan.length})
                </h4>
              </div>
              <ClusterFaqPlanEditor items={faqPlan} spokes={spokes} onChange={handleFaqPlanChange} />
            </section>
            <section className="border-t pt-6">
              <div className="mb-3 flex items-center justify-between gap-2">
                <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">
                  Body link plan ({bodyLinkPlan.length})
                </h4>
              </div>
              <ClusterBodyLinkPlanEditor
                items={bodyLinkPlan}
                spokes={spokes}
                headingHints={headingHints}
                onChange={handleBodyLinkPlanChange}
              />
            </section>
          </div>
        ) : null}

        {activeTab === 'research' ? (
          <section className="rounded-xl border bg-white p-4 shadow-sm">
            <h4 className="text-sm font-semibold text-[var(--color-text-primary)]">
              Filtered out {result ? `(${result.filteredOut.length})` : ''}
            </h4>
            {!result ? (
              <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
                Rebuild the plan to refresh filtered-out PAA/PASF with rejection reasons.
              </p>
            ) : result.filteredOut.length === 0 ? (
              <p className="mt-2 text-xs text-[var(--color-text-secondary)]">Nothing filtered on last build.</p>
            ) : (
              <ul className="mt-3 max-h-80 space-y-2 overflow-y-auto text-xs">
                {result.filteredOut.map((item) => (
                  <li key={`${item.phrase}-${item.rejectReason}`} className="rounded-lg border px-3 py-2">
                    <span className="font-medium text-[var(--color-text-primary)]">{item.phrase}</span>
                    <p className="text-[var(--color-text-secondary)]">{item.rejectReason}</p>
                  </li>
                ))}
              </ul>
            )}
          </section>
        ) : null}

        {!hasPlan && activeTab === 'overview' ? (
          <p className="text-xs text-[var(--color-text-secondary)]">
            Run <strong>Build plan</strong> to score PAA/PASF from your frozen research pack and populate
            spoke candidates, FAQ slots, and body link targets.
          </p>
        ) : null}
      </CardContent>
    </Card>
  );
}
