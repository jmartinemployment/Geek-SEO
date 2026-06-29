'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import { contentWritingPath } from '@/lib/content-writing-search-params';
import {
  buildClusterPlan,
  createContentSpoke,
  generateContentSpoke,
  getClusterPlan,
  listContentSpokes,
  type ContentClusterPlanResult,
  type ContentSpokeSummary,
} from '@/lib/seo-api';

function isSpokeGenerated(spoke: ContentSpokeSummary): boolean {
  return spoke.status === 'body_generated' || spoke.wordCount > 80;
}

export function ClusterPlanPanel() {
  const { doc, accessToken } = useWritingWorkspace();
  const [savedFaqCount, setSavedFaqCount] = useState(0);
  const [result, setResult] = useState<ContentClusterPlanResult | null>(null);
  const [spokes, setSpokes] = useState<ContentSpokeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [creatingPhrase, setCreatingPhrase] = useState<string | null>(null);
  const [generatingSpokeId, setGeneratingSpokeId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const researchReady = Boolean(doc.analysisRunId);
  const isPillar = doc.documentKind !== 'spoke';

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
      setSavedFaqCount(plan.faqItems?.length ?? 0);
      setSpokes(spokeList);
    } catch {
      setSavedFaqCount(0);
      setSpokes([]);
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id, isPillar, researchReady]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleBuild() {
    if (!accessToken) return;
    setBusy(true);
    setError(null);
    try {
      const built = await buildClusterPlan(doc.id, accessToken);
      setResult(built);
      setSavedFaqCount(built.faqItems.length);
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
      <div className="border-t px-3 py-4 text-sm text-[var(--color-text-secondary)] xl:px-4">
        Loading cluster plan…
      </div>
    );
  }

  return (
    <div className="border-t px-3 py-4 xl:px-4">
      <div className="mb-3 flex items-start justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">Cluster link plan</h3>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
            Filter SERP PAA/PASF into spoke targets and pillar FAQs. Saved plan: {savedFaqCount} FAQ
            {savedFaqCount === 1 ? '' : 's'} · {spokes.length} spoke
            {spokes.length === 1 ? '' : 's'}.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void handleBuild()}
          disabled={busy}
          className="shrink-0 rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-60"
        >
          {busy ? 'Building…' : 'Build plan'}
        </button>
      </div>

      {error ? <p className="mb-3 text-xs text-red-700">{error}</p> : null}

      {spokes.length > 0 ? (
        <section className="mb-4 space-y-2 text-xs">
          <h4 className="font-medium text-[var(--color-text-primary)]">Created spokes</h4>
          <ul className="space-y-2">
            {spokes.map((spoke) => (
              <li key={spoke.id} className="rounded-md border px-2 py-1.5">
                <Link
                  href={contentWritingPath({ documentId: spoke.id })}
                  className="font-medium text-[var(--color-accent)] underline"
                >
                  {spoke.title}
                </Link>
                {spoke.publishSlug ? (
                  <span className="text-[var(--color-text-secondary)]"> · /blog/{spoke.publishSlug}</span>
                ) : null}
                <p className="text-[var(--color-text-secondary)]">
                  {isSpokeGenerated(spoke) ? 'Generated' : 'Shell only'} · {spoke.wordCount} words
                </p>
                {!isSpokeGenerated(spoke) ? (
                  <button
                    type="button"
                    disabled={generatingSpokeId === spoke.id}
                    onClick={() => void handleGenerateSpoke(spoke)}
                    className="mt-1 rounded border px-2 py-0.5 text-[var(--color-text-primary)] disabled:opacity-50"
                  >
                    {generatingSpokeId === spoke.id ? 'Generating…' : 'Generate spoke'}
                  </button>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {result ? (
        <div className="space-y-4 text-xs">
          <section>
            <h4 className="mb-1 font-medium text-[var(--color-text-primary)]">
              Spoke candidates ({result.spokeCandidates.length})
            </h4>
            {result.spokeCandidates.length === 0 ? (
              <p className="text-[var(--color-text-secondary)]">None</p>
            ) : (
              <ul className="space-y-2">
                {result.spokeCandidates.map((item) => {
                  const existing = spokeExists(item.phrase);
                  return (
                    <li key={item.phrase} className="rounded-md border bg-slate-50 px-2 py-1.5">
                      <p className="font-medium text-[var(--color-text-primary)]">{item.phrase}</p>
                      <p className="text-[var(--color-text-secondary)]">
                        {item.suggestedQuestion ?? '—'}
                      </p>
                      {item.suggestedSlug ? (
                        <p className="text-[var(--color-text-secondary)]">/blog/{item.suggestedSlug}</p>
                      ) : null}
                      {existing ? (
                        <div className="mt-1 flex flex-wrap items-center gap-2">
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
                              className="rounded border px-2 py-0.5 text-[var(--color-text-primary)] disabled:opacity-50"
                            >
                              {generatingSpokeId === existing.id ? 'Generating…' : 'Generate spoke'}
                            </button>
                          ) : null}
                        </div>
                      ) : (
                        <button
                          type="button"
                          disabled={creatingPhrase === item.phrase}
                          onClick={() => void handleCreateSpoke(item)}
                          className="mt-1 rounded border px-2 py-0.5 text-[var(--color-text-primary)] disabled:opacity-50"
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

          <section>
            <h4 className="mb-1 font-medium text-[var(--color-text-primary)]">
              Pillar FAQs ({result.faqItems.length})
            </h4>
            <ul className="space-y-2">
              {result.faqItems.map((item) => (
                <li key={item.question} className="rounded-md border px-2 py-1.5">
                  <p className="text-[var(--color-text-primary)]">{item.question}</p>
                  <p className="text-[var(--color-text-secondary)]">
                    {item.source ?? 'unknown'}
                    {item.targetPath ? ` → ${item.targetPath}` : ''}
                  </p>
                </li>
              ))}
            </ul>
          </section>

          <section>
            <h4 className="mb-1 font-medium text-[var(--color-text-primary)]">
              Filtered out ({result.filteredOut.length})
            </h4>
            {result.filteredOut.length === 0 ? (
              <p className="text-[var(--color-text-secondary)]">None</p>
            ) : (
              <ul className="space-y-1 text-[var(--color-text-secondary)]">
                {result.filteredOut.map((item) => (
                  <li key={`${item.phrase}-${item.rejectReason}`}>
                    <span className="text-[var(--color-text-primary)]">{item.phrase}</span>
                    {' — '}
                    {item.rejectReason}
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      ) : savedFaqCount > 0 ? (
        <p className="text-xs text-[var(--color-text-secondary)]">
          A saved plan exists. Build again to refresh spoke candidates and filtered-out reasons.
        </p>
      ) : (
        <p className="text-xs text-[var(--color-text-secondary)]">
          Run Build plan to score PAA/PASF from your frozen research pack.
        </p>
      )}
    </div>
  );
}
