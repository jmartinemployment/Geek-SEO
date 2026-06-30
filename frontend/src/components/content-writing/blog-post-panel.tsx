'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import { contentWritingPath } from '@/lib/content-writing-search-params';
import {
  buildClusterPlan,
  createContentSpoke,
  generateContentSpoke,
  generateLinkedFaqs,
  listContentSpokes,
  saveClusterPlan,
  type ContentClusterCandidate,
  type ContentClusterPlanResult,
  type ContentSpokeSummary,
} from '@/lib/seo-api';

function isBlogPostReady(post: ContentSpokeSummary): boolean {
  return post.status === 'body_generated' || post.wordCount > 80;
}

export function BlogPostPanel() {
  const { doc, accessToken, reloadDocument } = useWritingWorkspace();
  const [posts, setPosts] = useState<ContentSpokeSummary[]>([]);
  const [candidates, setCandidates] = useState<ContentClusterCandidate[]>([]);
  const [planResult, setPlanResult] = useState<ContentClusterPlanResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const isPillar = doc.documentKind !== 'spoke';
  const isResearchBacked = Boolean(doc.analysisRunId);

  const load = useCallback(async () => {
    if (!accessToken || !isPillar) { setLoading(false); return; }
    setLoading(true);
    try {
      const [list, plan] = await Promise.all([
        listContentSpokes(doc.id, accessToken),
        buildClusterPlan(doc.id, accessToken),
      ]);
      setPosts(list);
      setPlanResult(plan);
      // Only show candidates that don't already have a blog post
      const existingPhrases = new Set(list.map((p) => p.spokeSourcePhrase?.toLowerCase()).filter(Boolean));
      const unused = plan.spokeCandidates.filter(
        (c) => !existingPhrases.has(c.phrase.toLowerCase()),
      );
      setCandidates(unused.slice(0, 3));
    } catch {
      setPosts([]);
      setCandidates([]);
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id, isPillar]);

  useEffect(() => { void load(); }, [load]);

  if (!isResearchBacked || !isPillar) return null;

  async function handleGenerate(phrase: string) {
    if (!accessToken || !phrase || busy) return;

    setBusy(true);
    setError(null);
    setStatusMsg('Writing blog post…');

    try {
      const plan = planResult ?? await buildClusterPlan(doc.id, accessToken);

      const created = await createContentSpoke(
        doc.id,
        { phrase, sourceType: 'pasf' },
        accessToken,
      );

      setStatusMsg('Writing blog post content…');
      const generated = await generateContentSpoke(doc.id, created.id, accessToken);
      setPosts((prev) => [generated, ...prev.filter((p) => p.id !== generated.id)]);
      setCandidates((prev) => prev.filter((c) => c.phrase.toLowerCase() !== phrase.toLowerCase()));

      if (generated.publishSlug) {
        const faqSlot = {
          question: `What should you know about ${phrase}?`,
          targetDocumentId: generated.id,
          targetPath: `/blog/${generated.publishSlug}`,
          anchorText: phrase,
          source: 'manual' as const,
        };
        await saveClusterPlan(doc.id, {
          faqItems: [faqSlot, ...plan.faqItems],
          bodyLinks: plan.bodyLinks,
        }, accessToken);
      }

      setStatusMsg('Linking in your article…');
      const linked = await generateLinkedFaqs(doc.id, accessToken);
      await reloadDocument();

      setStatusMsg(
        linked.linkedCount > 0
          ? 'Blog post created and linked in your article.'
          : 'Blog post created.',
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create blog post');
      setStatusMsg(null);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="rounded-xl border bg-white shadow-sm">
      <div className="border-b px-5 py-4">
        <h2 className="font-semibold">Blog posts</h2>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          Generate a linked blog post from your SERP research.
        </p>
      </div>

      <div className="space-y-4 p-5">
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        {!busy && statusMsg ? <p className="text-sm text-emerald-700">{statusMsg}</p> : null}
        {busy ? <p className="text-sm text-[var(--color-text-secondary)]">{statusMsg ?? 'Working…'}</p> : null}

        {!loading && !busy && candidates.length > 0 ? (
          <div className="space-y-2">
            <p className="text-xs font-medium text-[var(--color-text-secondary)]">
              Suggested from your research — click to generate:
            </p>
            <ul className="space-y-2">
              {candidates.map((c) => (
                <li key={c.phrase}>
                  <button
                    type="button"
                    onClick={() => void handleGenerate(c.phrase)}
                    className="w-full rounded-lg border px-3 py-2 text-left text-sm hover:border-[var(--color-accent)] hover:bg-[var(--color-accent)]/5"
                  >
                    <span className="font-medium text-[var(--color-text-primary)]">
                      {c.suggestedQuestion ?? c.phrase}
                    </span>
                    <span className="ml-2 text-xs text-[var(--color-text-muted)]">
                      {c.sourceType === 'paa' ? 'PAA' : 'Related search'}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        {!loading && !busy && candidates.length === 0 && posts.length === 0 ? (
          <p className="text-sm text-[var(--color-text-secondary)]">
            No candidates found — ensure Site Analyzer research is complete.
          </p>
        ) : null}

        {!loading && posts.length > 0 ? (
          <div className="space-y-2">
            <p className="text-xs font-medium text-[var(--color-text-secondary)]">
              Your blog posts ({posts.length})
            </p>
            <ul className="space-y-2">
              {posts.map((post) => (
                <li key={post.id} className="rounded-lg border px-3 py-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <Link
                        href={contentWritingPath({ documentId: post.id })}
                        className="block truncate text-sm font-medium text-[var(--color-accent)] underline"
                      >
                        {post.title}
                      </Link>
                      <p className="mt-0.5 truncate text-xs text-[var(--color-text-secondary)]">
                        {isBlogPostReady(post) ? `${post.wordCount} words` : 'Draft'}
                        {post.publishSlug ? ` · /blog/${post.publishSlug}` : ''}
                      </p>
                    </div>
                    <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
                      isBlogPostReady(post) ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-900'
                    }`}>
                      {isBlogPostReady(post) ? 'Ready' : 'Draft'}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </div>
    </div>
  );
}
