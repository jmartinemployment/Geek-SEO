'use client';

import { useCallback, useEffect, useState } from 'react';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import {
  generateMarketingBlogSpoke,
  generateMarketingSocial,
  generateMarketingSummaries,
  getMarketingBundle,
  saveMarketingBundle,
  validateMarketingBundle,
  type ContentMarketingBundle,
  type ContentMarketingValidationResult,
} from '@/lib/seo-api';
import { copyTextFromPromise } from '@/lib/copy-to-clipboard';

const DEPARTMENTS = [
  { value: 'marketing', label: 'Marketing' },
  { value: 'accounting', label: 'Accounting' },
  { value: 'customer-service', label: 'Customer service' },
  { value: 'human-resources', label: 'Human resources' },
];

const SPOKE_TYPES = [
  { value: 'comparison', label: 'Comparison' },
  { value: 'cost', label: 'Cost' },
  { value: 'local', label: 'Local angle' },
  { value: 'how-to', label: 'How-to' },
  { value: 'myth-bust', label: 'Myth-bust' },
  { value: 'case-style', label: 'Case-style' },
];

function fieldButton(label: string, onClick: () => void, busy: boolean) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={busy}
      className="rounded-md border border-[var(--color-border-strong)] px-2 py-1 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
    >
      {busy ? '…' : label}
    </button>
  );
}

export function MarketingExportPanel() {
  const { doc, accessToken } = useWritingWorkspace();
  const [bundle, setBundle] = useState<ContentMarketingBundle | null>(null);
  const [validation, setValidation] = useState<ContentMarketingValidationResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [spokeType, setSpokeType] = useState('comparison');
  const [spokeKeyword, setSpokeKeyword] = useState('');
  const [copyHint, setCopyHint] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!accessToken) return;
    setLoading(true);
    try {
      setError(null);
      const data = await getMarketingBundle(doc.id, accessToken);
      setBundle(data);
      setValidation(await validateMarketingBundle(doc.id, data, accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load marketing bundle');
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id]);

  useEffect(() => {
    void load();
  }, [load]);

  async function persist(next: ContentMarketingBundle) {
    if (!accessToken) return;
    const saved = await saveMarketingBundle(doc.id, next, accessToken);
    setBundle(saved);
    setValidation(await validateMarketingBundle(doc.id, saved, accessToken));
  }

  async function runGenerate(
    key: string,
    action: () => Promise<ContentMarketingBundle>,
  ) {
    if (!accessToken) return;
    setBusy(key);
    setError(null);
    try {
      const saved = await action();
      setBundle(saved);
      setValidation(await validateMarketingBundle(doc.id, saved, accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Generation failed');
    } finally {
      setBusy(null);
    }
  }

  function updateBundle(patch: Partial<ContentMarketingBundle>) {
    if (!bundle) return;
    setBundle({ ...bundle, ...patch });
  }

  async function saveFields() {
    if (!bundle || !accessToken) return;
    setBusy('save');
    try {
      await persist(bundle);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      setBusy(null);
    }
  }

  function copyText(label: string, text: string) {
    void copyTextFromPromise(Promise.resolve(text))
      .then(() => {
        setCopyHint(`${label} copied`);
        setTimeout(() => setCopyHint(null), 2500);
      })
      .catch(() => setError('Copy failed'));
  }

  if (loading) {
    return (
      <div className="border-t px-3 py-4 text-sm text-[var(--color-text-secondary)] xl:px-4">
        Loading marketing bundle…
      </div>
    );
  }

  if (!bundle) return null;

  return (
    <div className="border-t px-3 py-4 xl:px-4">
      <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold">Marketing export</h3>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
            Pillar-first bundle for geekatyourspot.com — summaries, blog spoke, and social.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void saveFields()}
          disabled={busy !== null}
          className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          Save bundle
        </button>
      </div>

      {error ? <p className="mb-3 text-xs text-red-700">{error}</p> : null}
      {copyHint ? <p className="mb-3 text-xs text-emerald-700">{copyHint}</p> : null}

      <div className="mb-4 grid gap-3 sm:grid-cols-2">
        <label className="text-xs font-medium">
          Department
          <select
            className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
            value={bundle.departmentSlug}
            onChange={(e) => updateBundle({ departmentSlug: e.target.value })}
          >
            {DEPARTMENTS.map((d) => (
              <option key={d.value} value={d.value}>{d.label}</option>
            ))}
          </select>
        </label>
        <label className="text-xs font-medium">
          Use case slug
          <input
            className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
            value={bundle.useCaseSlug}
            onChange={(e) => updateBundle({ useCaseSlug: e.target.value })}
          />
        </label>
      </div>

      <section className="mb-4 space-y-2 rounded-lg border bg-slate-50 p-3">
        <div className="flex items-center justify-between gap-2">
          <h4 className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
            Catalog summaries
          </h4>
          {fieldButton(
            'Generate from pillar',
            () => runGenerate('summaries', () => generateMarketingSummaries(doc.id, accessToken)),
            busy === 'summaries',
          )}
        </div>
        {(['homeSummary', 'hubSummary', 'metaDescription'] as const).map((key) => (
          <label key={key} className="block text-xs font-medium">
            {key}
            <textarea
              className="mt-1 block w-full rounded-md border bg-white px-2 py-1.5 text-sm"
              rows={key === 'metaDescription' ? 2 : 3}
              value={bundle[key] ?? ''}
              onChange={(e) => updateBundle({ [key]: e.target.value })}
            />
            {bundle[key] ? (
              <button
                type="button"
                className="mt-1 text-xs text-[var(--color-accent)] hover:underline"
                onClick={() => copyText(key, bundle[key] ?? '')}
              >
                Copy
              </button>
            ) : null}
          </label>
        ))}
      </section>

      <section className="mb-4 space-y-2 rounded-lg border p-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h4 className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
            Blog spoke
          </h4>
          {fieldButton(
            'Generate spoke',
            () => runGenerate('blog', () =>
              generateMarketingBlogSpoke(doc.id, {
                spokeType,
                spokeKeyword: spokeKeyword.trim() || undefined,
              }, accessToken)),
            busy === 'blog',
          )}
        </div>
        <div className="grid gap-2 sm:grid-cols-2">
          <label className="text-xs font-medium">
            Spoke type
            <select
              className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
              value={spokeType}
              onChange={(e) => setSpokeType(e.target.value)}
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
            />
          </label>
        </div>
        {bundle.blogSpoke ? (
          <div className="space-y-2 text-sm">
            <p className="font-medium">{bundle.blogSpoke.title}</p>
            <p className="text-xs text-[var(--color-text-secondary)]">
              {bundle.blogSpoke.slug} · {bundle.blogSpoke.primaryKeyword}
            </p>
            {bundle.blogSpoke.excerpt ? (
              <p className="text-xs">{bundle.blogSpoke.excerpt}</p>
            ) : null}
            <button
              type="button"
              className="text-xs text-[var(--color-accent)] hover:underline"
              onClick={() => copyText('Blog HTML', bundle.blogSpoke?.contentHtml ?? '')}
            >
              Copy blog HTML
            </button>
          </div>
        ) : (
          <p className="text-xs text-[var(--color-text-secondary)]">No blog spoke yet.</p>
        )}
      </section>

      <section className="mb-4 space-y-2 rounded-lg border p-3">
        <div className="flex items-center justify-between gap-2">
          <h4 className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
            Social
          </h4>
          {fieldButton(
            'Generate social',
            () => runGenerate('social', () => generateMarketingSocial(doc.id, accessToken)),
            busy === 'social',
          )}
        </div>
        {bundle.social?.linkedin ? (
          <label className="block text-xs font-medium">
            LinkedIn
            <textarea
              className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
              rows={4}
              value={bundle.social.linkedin.body}
              onChange={(e) =>
                updateBundle({
                  social: {
                    ...bundle.social,
                    linkedin: { ...bundle.social!.linkedin!, body: e.target.value },
                  },
                })
              }
            />
          </label>
        ) : null}
        {bundle.social?.facebook ? (
          <label className="block text-xs font-medium">
            Facebook
            <textarea
              className="mt-1 block w-full rounded-md border px-2 py-1.5 text-sm"
              rows={3}
              value={bundle.social.facebook.body}
              onChange={(e) =>
                updateBundle({
                  social: {
                    ...bundle.social,
                    facebook: { ...bundle.social!.facebook!, body: e.target.value },
                  },
                })
              }
            />
          </label>
        ) : null}
        {!bundle.social?.linkedin && !bundle.social?.facebook ? (
          <p className="text-xs text-[var(--color-text-secondary)]">No social drafts yet.</p>
        ) : null}
      </section>

      {validation ? (
        <div
          className={`rounded-lg px-3 py-2 text-xs ${
            validation.isValid
              ? 'bg-emerald-50 text-emerald-900'
              : 'bg-amber-50 text-amber-950'
          }`}
        >
          {validation.isValid ? (
            <p>Bundle passes export validation.</p>
          ) : (
            <ul className="list-disc pl-4">
              {validation.errors.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
