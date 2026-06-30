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
  type ContentSpokeSummary,
} from '@/lib/seo-api';

function isBlogPostReady(post: ContentSpokeSummary): boolean {
  return post.status === 'body_generated' || post.wordCount > 80;
}

export function BlogPostPanel() {
  const { doc, accessToken, reloadDocument } = useWritingWorkspace();
  const [posts, setPosts] = useState<ContentSpokeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [topic, setTopic] = useState('');
  const [busy, setBusy] = useState(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const isPillar = doc.documentKind !== 'spoke';
  const isResearchBacked = Boolean(doc.analysisRunId);

  const loadPosts = useCallback(async () => {
    if (!accessToken || !isPillar) { setLoading(false); return; }
    setLoading(true);
    try {
      const list = await listContentSpokes(doc.id, accessToken);
      setPosts(list);
    } catch {
      setPosts([]);
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id, isPillar]);

  useEffect(() => { void loadPosts(); }, [loadPosts]);

  if (!isResearchBacked || !isPillar) return null;

  async function handleGenerate() {
    const phrase = topic.trim();
    if (!accessToken || !phrase) return;

    setBusy(true);
    setError(null);
    setStatusMsg(null);

    try {
      // 1. Build plan from SERP research (creates link slot map)
      setStatusMsg('Reading your SERP research…');
      const plan = await buildClusterPlan(doc.id, accessToken);

      // 2. Create the blog post shell
      setStatusMsg('Creating blog post…');
      const created = await createContentSpoke(
        doc.id,
        { phrase, sourceType: 'manual' },
        accessToken,
      );

      // 3. Generate full blog post content
      setStatusMsg('Writing blog post content…');
      const generated = await generateContentSpoke(doc.id, created.id, accessToken);
      setPosts((prev) => [generated, ...prev.filter((p) => p.id !== generated.id)]);

      // 4. Add this blog post as a FAQ link slot so the link goes into the pillar
      if (generated.publishSlug) {
        const faqSlot = {
          question: phrase.endsWith('?') ? phrase : `What should you know about ${phrase}?`,
          targetDocumentId: generated.id,
          targetPath: `/blog/${generated.publishSlug}`,
          anchorText: phrase,
          source: 'manual' as const,
        };
        const updatedPlan = {
          faqItems: [faqSlot, ...plan.faqItems],
          bodyLinks: plan.bodyLinks,
        };
        await saveClusterPlan(doc.id, updatedPlan, accessToken);
      }

      // 5. Insert the link into the pillar's FAQ section
      setStatusMsg('Adding link to your article…');
      const linked = await generateLinkedFaqs(doc.id, accessToken);
      await reloadDocument();

      setStatusMsg(
        linked.linkedCount > 0
          ? 'Blog post created and linked in your article.'
          : 'Blog post created. Open the article editor to add the link manually.',
      );
      setTopic('');
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
          Generate a linked blog post from your pillar article&apos;s research.
        </p>
      </div>

      <div className="space-y-5 p-5">
        <div className="space-y-3">
          <label className="block text-sm font-medium text-[var(--color-text-primary)]">
            Blog post topic
            <input
              className="mt-1 block w-full rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm shadow-sm placeholder:text-[var(--color-text-muted)] disabled:bg-[var(--color-surface-muted)]"
              placeholder="e.g. 7 stages of customer journey"
              value={topic}
              onChange={(e) => setTopic(e.target.value)}
              disabled={busy}
              onKeyDown={(e) => { if (e.key === 'Enter' && !busy && topic.trim()) void handleGenerate(); }}
            />
          </label>

          <button
            type="button"
            disabled={busy || !topic.trim()}
            onClick={() => void handleGenerate()}
            className="w-full rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {busy ? (statusMsg ?? 'Working…') : 'Generate blog post'}
          </button>

          {error ? (
            <p className="text-sm text-red-700">{error}</p>
          ) : null}
          {!busy && statusMsg ? (
            <p className="text-sm text-emerald-700">{statusMsg}</p>
          ) : null}
        </div>

        {!loading && posts.length > 0 ? (
          <div className="space-y-2">
            <p className="text-xs font-medium text-[var(--color-text-secondary)]">
              Blog posts ({posts.length})
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
                    <span
                      className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
                        isBlogPostReady(post)
                          ? 'bg-emerald-100 text-emerald-800'
                          : 'bg-amber-100 text-amber-900'
                      }`}
                    >
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
