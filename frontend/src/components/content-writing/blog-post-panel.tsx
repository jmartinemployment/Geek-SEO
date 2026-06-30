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
  generateSocialPosts,
  getContent,
  getRenderedContentHtml,
  listContentSpokes,
  saveClusterPlan,
  type ContentClusterCandidate,
  type ContentClusterPlanResult,
  type ContentSocialPostResult,
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
  const [socialResult, setSocialResult] = useState<ContentSocialPostResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [regeneratingId, setRegeneratingId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState<string | null>(null);

  const isPillar = doc.documentKind !== 'spoke';
  const isResearchBacked = Boolean(doc.analysisRunId);

  const load = useCallback(async () => {
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

  const loadCandidates = useCallback(async () => {
    if (!accessToken || !isPillar) return;
    try {
      const plan = await buildClusterPlan(doc.id, accessToken);
      setPlanResult(plan);
      const existingPhrases = new Set(posts.map((p) => p.spokeSourcePhrase?.toLowerCase()).filter(Boolean));
      setCandidates(plan.spokeCandidates.filter((c) => !existingPhrases.has(c.phrase.toLowerCase())).slice(0, 3));
    } catch {
      setCandidates([]);
    }
  }, [accessToken, doc.id, isPillar, posts]);

  useEffect(() => { void load(); }, [load]);

  if (!isResearchBacked || !isPillar) return null;

  async function handleGenerate(phrase: string) {
    if (!accessToken || !phrase || busy) return;
    setBusy(true);
    setError(null);
    setStatusMsg('Writing blog post…');
    try {
      const plan = planResult ?? await buildClusterPlan(doc.id, accessToken);
      const created = await createContentSpoke(doc.id, { phrase, sourceType: 'pasf' }, accessToken);
      setStatusMsg('Writing blog post content…');
      const generated = await generateContentSpoke(doc.id, created.id, accessToken);
      setPosts((prev) => [generated, ...prev.filter((p) => p.id !== generated.id)]);
      setCandidates((prev) => prev.filter((c) => c.phrase.toLowerCase() !== phrase.toLowerCase()));
      if (generated.publishSlug) {
        await saveClusterPlan(doc.id, {
          faqItems: [{
            question: `What should you know about ${phrase}?`,
            targetDocumentId: generated.id,
            targetPath: `/blog/${generated.publishSlug}`,
            anchorText: phrase,
            source: 'manual' as const,
          }, ...plan.faqItems],
          bodyLinks: plan.bodyLinks,
        }, accessToken);
      }
      setStatusMsg('Linking in your article…');
      const linked = await generateLinkedFaqs(doc.id, accessToken);
      await reloadDocument();

      setStatusMsg('Generating social posts…');
      try {
        const social = await generateSocialPosts(
          doc.id,
          { blogPostTitle: generated.title, blogPostSlug: generated.publishSlug ?? undefined },
          accessToken,
        );
        setSocialResult(social);
      } catch {
        // social posts are best-effort
      }

      setStatusMsg(
        linked.linkedCount > 0
          ? 'Done — blog post linked, social posts ready below.'
          : 'Done — blog post created, social posts ready below.',
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create blog post');
      setStatusMsg(null);
    } finally {
      setBusy(false);
    }
  }

  async function handleRegenerate(post: ContentSpokeSummary) {
    if (!accessToken || !doc.id || regeneratingId) return;
    setRegeneratingId(post.id);
    setError(null);
    try {
      const updated = await generateContentSpoke(doc.id, post.id, accessToken);
      setPosts((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
      setStatusMsg('Blog post regenerated.');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Regeneration failed');
    } finally {
      setRegeneratingId(null);
    }
  }

  async function handleSave() {
    if (!accessToken || saving) return;
    setSaving(true);
    setError(null);
    try {
      const pillarRendered = await getRenderedContentHtml(doc.id, accessToken);
      const pillarHtml = pillarRendered.renderedHtml || pillarRendered.bodyHtml;

      const blogPosts: Array<{ slug: string; html: string; title: string }> = [];
      for (const post of posts.filter(isBlogPostReady)) {
        const postDoc = await getContent(post.id, accessToken);
        blogPosts.push({
          slug: post.publishSlug ?? post.id,
          title: post.title,
          html: postDoc.contentHtml,
        });
      }

      const res = await fetch('/api/save-content', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ keyword: doc.targetKeyword, pillarHtml, blogPosts }),
      });

      if (!res.ok) throw new Error('Save failed');
      const result = (await res.json()) as { dir: string };
      setStatusMsg(`Saved to ${result.dir}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  }

  const readyPosts = posts.filter(isBlogPostReady);

  return (
    <div className="rounded-xl border bg-white shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b px-5 py-4">
        <div>
          <h2 className="font-semibold">Blog posts</h2>
          <p className="mt-0.5 text-sm text-[var(--color-text-secondary)]">
            Generate a linked blog post from your SERP research.
          </p>
        </div>
        {readyPosts.length > 0 ? (
          <button
            type="button"
            disabled={saving}
            onClick={() => void handleSave()}
            className="rounded-lg border px-3 py-1.5 text-sm font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save files'}
          </button>
        ) : null}
      </div>

      <div className="space-y-4 p-5">
        {error ? <p className="text-sm text-red-700">{error}</p> : null}
        {!busy && !saving && statusMsg ? <p className="text-sm text-emerald-700">{statusMsg}</p> : null}
        {(busy || saving) ? <p className="text-sm text-[var(--color-text-secondary)]">{statusMsg ?? 'Working…'}</p> : null}

        {!loading && !busy && candidates.length === 0 ? (
          <button
            type="button"
            onClick={() => void loadCandidates()}
            className="w-full rounded-lg border px-4 py-2 text-sm font-medium hover:bg-[var(--color-surface-muted)]"
          >
            Find blog post topics from research
          </button>
        ) : null}

        {!loading && !busy && candidates.length > 0 ? (
          <div className="space-y-2">
            <p className="text-xs font-medium text-[var(--color-text-secondary)]">
              From your research — click to generate:
            </p>
            <ul className="space-y-2">
              {candidates.map((c) => (
                <li key={c.phrase}>
                  <button
                    type="button"
                    onClick={() => void handleGenerate(c.phrase)}
                    disabled={busy}
                    className="w-full rounded-lg border px-3 py-2 text-left text-sm hover:border-[var(--color-accent)] hover:bg-[var(--color-accent)]/5 disabled:opacity-50"
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
                    <div className="flex shrink-0 items-center gap-2">
                      <button
                        type="button"
                        disabled={regeneratingId === post.id || busy}
                        onClick={() => void handleRegenerate(post)}
                        className="text-xs text-[var(--color-accent)] underline disabled:opacity-50"
                      >
                        {regeneratingId === post.id ? 'Regenerating…' : 'Regenerate'}
                      </button>
                      <span className={`rounded-full px-2 py-0.5 text-[10px] font-medium ${
                        isBlogPostReady(post) ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-900'
                      }`}>
                        {isBlogPostReady(post) ? 'Ready' : 'Draft'}
                      </span>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        {socialResult ? (
          <div className="space-y-3 rounded-xl border bg-[var(--color-surface-muted)]/40 p-4">
            <p className="text-xs font-semibold text-[var(--color-text-primary)]">Social posts</p>
            {copied ? <p className="text-xs text-emerald-700">{copied}</p> : null}

            <div className="space-y-1">
              <div className="flex items-center justify-between">
                <p className="text-xs font-medium text-[var(--color-text-secondary)]">Facebook</p>
                <button
                  type="button"
                  onClick={() => {
                    void navigator.clipboard.writeText(socialResult.facebookPost).then(() => {
                      setCopied('Facebook post copied');
                      setTimeout(() => setCopied(null), 2500);
                    });
                  }}
                  className="text-xs text-[var(--color-accent)] underline"
                >
                  Copy
                </button>
              </div>
              <p className="rounded-lg border bg-white px-3 py-2 text-xs leading-relaxed text-[var(--color-text-primary)]">
                {socialResult.facebookPost}
              </p>
            </div>

            <div className="space-y-1">
              <div className="flex items-center justify-between">
                <p className="text-xs font-medium text-[var(--color-text-secondary)]">LinkedIn</p>
                <button
                  type="button"
                  onClick={() => {
                    void navigator.clipboard.writeText(socialResult.linkedInPost).then(() => {
                      setCopied('LinkedIn post copied');
                      setTimeout(() => setCopied(null), 2500);
                    });
                  }}
                  className="text-xs text-[var(--color-accent)] underline"
                >
                  Copy
                </button>
              </div>
              <p className="rounded-lg border bg-white px-3 py-2 text-xs leading-relaxed text-[var(--color-text-primary)]">
                {socialResult.linkedInPost}
              </p>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}
