'use client';

import { useCallback, useEffect, useState } from 'react';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import {
  generateBlogSpoke,
  getBlogSpoke,
  type ContentBlogSpoke,
} from '@/lib/seo-api';
import { copyTextFromPromise } from '@/lib/copy-to-clipboard';

const SPOKE_TYPES = [
  { value: 'comparison', label: 'Comparison' },
  { value: 'cost', label: 'Cost' },
  { value: 'local', label: 'Local angle' },
  { value: 'how-to', label: 'How-to' },
  { value: 'myth-bust', label: 'Myth-bust' },
  { value: 'case-style', label: 'Case-style' },
];

export function BlogSpokePanel() {
  const { doc, accessToken, blogSpokeRevision } = useWritingWorkspace();
  const [spoke, setSpoke] = useState<ContentBlogSpoke | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [spokeType, setSpokeType] = useState('comparison');
  const [spokeKeyword, setSpokeKeyword] = useState('');
  const [copyHint, setCopyHint] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!accessToken) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      setError(null);
      const data = await getBlogSpoke(doc.id, accessToken);
      setSpoke(data);
    } catch {
      setSpoke(null);
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id, blogSpokeRevision]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleGenerate() {
    if (!accessToken) return;
    setBusy(true);
    setError(null);
    try {
      const generated = await generateBlogSpoke(
        doc.id,
        {
          spokeType,
          spokeKeyword: spokeKeyword.trim() || undefined,
        },
        accessToken,
      );
      setSpoke(generated);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Blog generation failed');
    } finally {
      setBusy(false);
    }
  }

  function copyText(label: string, text: string) {
    void copyTextFromPromise(async () => text)
      .then(() => {
        setCopyHint(`${label} copied`);
        setTimeout(() => setCopyHint(null), 2500);
      })
      .catch(() => setError('Copy failed'));
  }

  if (loading) {
    return (
      <div className="border-t px-3 py-4 text-sm text-[var(--color-text-secondary)] xl:px-4">
        Loading blog version…
      </div>
    );
  }

  return (
    <div className="border-t px-3 py-4 xl:px-4">
      <div className="mb-3">
        <h3 className="text-sm font-semibold">Blog version</h3>
        <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
          Generate an 800–1,200 word spoke article with a different search intent than the pillar.
        </p>
      </div>

      {error ? <p className="mb-3 text-xs text-red-700">{error}</p> : null}
      {copyHint ? <p className="mb-3 text-xs text-emerald-700">{copyHint}</p> : null}

      <div className="mb-3 grid gap-3 sm:grid-cols-2">
        <label className="text-xs font-medium">
          Spoke type
          <select
            className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
            value={spokeType}
            onChange={(e) => setSpokeType(e.target.value)}
            disabled={busy}
          >
            {SPOKE_TYPES.map((t) => (
              <option key={t.value} value={t.value}>{t.label}</option>
            ))}
          </select>
        </label>
        <label className="text-xs font-medium">
          Spoke keyword (optional)
          <input
            className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
            value={spokeKeyword}
            onChange={(e) => setSpokeKeyword(e.target.value)}
            placeholder="Distinct from pillar keyword"
            disabled={busy}
          />
        </label>
      </div>

      <button
        type="button"
        onClick={() => void handleGenerate()}
        disabled={busy || !accessToken}
        className="mb-4 w-full rounded-lg bg-[var(--color-accent)] px-3 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
      >
        {busy ? 'Generating blog version…' : spoke ? 'Regenerate blog version' : 'Create blog version'}
      </button>

      {spoke ? (
        <div className="space-y-3 rounded-lg border bg-[var(--color-surface-muted)] p-3 text-xs">
          <div className="flex items-start justify-between gap-2">
            <div>
              <p className="font-medium text-[var(--color-text-primary)]">{spoke.title}</p>
              <p className="mt-1 text-[var(--color-text-secondary)]">
                /{spoke.slug} · {spoke.primaryKeyword}
              </p>
            </div>
          </div>
          {spoke.excerpt ? (
            <p className="text-[var(--color-text-secondary)]">{spoke.excerpt}</p>
          ) : null}
          {spoke.metaDescription ? (
            <p className="italic text-[var(--color-text-secondary)]">{spoke.metaDescription}</p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => copyText('Title', spoke.title)}
              className="rounded-md border px-2 py-1 font-medium hover:bg-white"
            >
              Copy title
            </button>
            <button
              type="button"
              onClick={() => copyText('HTML', spoke.contentHtml)}
              className="rounded-md border px-2 py-1 font-medium hover:bg-white"
            >
              Copy HTML
            </button>
            {spoke.excerpt ? (
              <button
                type="button"
                onClick={() => copyText('Excerpt', spoke.excerpt!)}
                className="rounded-md border px-2 py-1 font-medium hover:bg-white"
              >
                Copy excerpt
              </button>
            ) : null}
            {spoke.metaDescription ? (
              <button
                type="button"
                onClick={() => copyText('Meta', spoke.metaDescription!)}
                className="rounded-md border px-2 py-1 font-medium hover:bg-white"
              >
                Copy meta
              </button>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}
