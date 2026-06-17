'use client';

import { useState } from 'react';
import { useAuthReady } from '@/hooks/use-auth-ready';
import { runUrlAnalyzerResearch, type SerpResearchPack } from '@/lib/seo-api';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';

export default function UrlAnalyzerPage() {
  const { accessToken, authLoading } = useAuthReady();
  const [keyword, setKeyword] = useState('');
  const [location, setLocation] = useState('United States');
  const [businessContext, setBusinessContext] = useState('');
  const [competitorUrls, setCompetitorUrls] = useState('');
  const [pack, setPack] = useState<SerpResearchPack | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [copied, setCopied] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!keyword.trim()) return;
    setLoading(true);
    setError(null);
    setCopied(false);
    try {
      const optionalUrls = competitorUrls
        .split('\n')
        .map((line) => line.trim())
        .filter(Boolean);
      const result = await runUrlAnalyzerResearch(
        {
          keyword: keyword.trim(),
          location: location.trim() || 'United States',
          language: 'en',
          businessContext: businessContext.trim() || undefined,
          competitorUrls: optionalUrls.length > 0 ? optionalUrls : undefined,
        },
        accessToken,
      );
      setPack(result);
    } catch (err) {
      setError(err);
      setPack(null);
    } finally {
      setLoading(false);
    }
  }

  function downloadJson() {
    if (!pack) return;
    const blob = new Blob([JSON.stringify(pack, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `serp-research-${pack.meta.keyword.replaceAll(/\s+/g, '-').toLowerCase()}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  async function copyJson() {
    if (!pack) return;
    await navigator.clipboard.writeText(JSON.stringify(pack, null, 2));
    setCopied(true);
  }

  if (authLoading) return <div className="p-8">Loading…</div>;

  return (
    <div className="mx-auto max-w-5xl">
      <h1 className="text-2xl font-semibold">URL Analyzer</h1>
      <p className="mt-1 max-w-2xl text-sm text-[var(--color-text-secondary)]">
        Live keyword SERP research for content writing — PAA, PASF, primary answer feature, top organic
        results, and competitor outlines. Output is a JSON research pack for downstream components.
      </p>

      <form onSubmit={onSubmit} className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Primary keyword
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="e.g. QuickBooks automation for small business"
            required
          />
        </label>
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Search location
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
        </label>
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Business context (optional, 2–4 sentences)
          <textarea
            className="mt-1 min-h-24 w-full rounded-lg border px-3 py-2"
            value={businessContext}
            onChange={(e) => setBusinessContext(e.target.value)}
            placeholder="Who you are, what you sell, service area — for intent filtering only."
          />
        </label>
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Competitor URLs (optional, one per line)
          <textarea
            className="mt-1 min-h-20 w-full rounded-lg border px-3 py-2 font-mono text-xs"
            value={competitorUrls}
            onChange={(e) => setCompetitorUrls(e.target.value)}
            placeholder="https://example.com/page"
          />
        </label>
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? 'Researching SERP…' : 'Run SERP research'}
        </button>
      </form>

      {error ? (
        <div className="mt-4">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      {pack ? (
        <div className="mt-10 space-y-6">
          <section className="rounded-xl border bg-white p-6 shadow-sm">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="text-lg font-semibold">Research pack</h2>
                <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                  {pack.meta.keyword} @ {pack.meta.location} · quality: {pack.meta.dataQuality} · intent:{' '}
                  {pack.intent.primary}
                </p>
              </div>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={copyJson}
                  className="rounded-lg border px-3 py-1.5 text-sm hover:bg-[var(--color-surface-muted)]"
                >
                  {copied ? 'Copied' : 'Copy JSON'}
                </button>
                <button
                  type="button"
                  onClick={downloadJson}
                  className="rounded-lg border px-3 py-1.5 text-sm hover:bg-[var(--color-surface-muted)]"
                >
                  Download JSON
                </button>
              </div>
            </div>

            <dl className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">PAA</dt>
                <dd className="text-lg font-medium">{pack.paa.length}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">PASF</dt>
                <dd className="text-lg font-medium">{pack.pasf.length}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">Organic</dt>
                <dd className="text-lg font-medium">{pack.organic.length}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-[var(--color-text-muted)]">Target words</dt>
                <dd className="text-lg font-medium">{pack.benchmarks.medianWordCountTop5}</dd>
              </div>
            </dl>
          </section>

          <section className="rounded-xl border bg-white p-6 shadow-sm">
            <h3 className="text-sm font-semibold">JSON output</h3>
            <pre className="mt-3 max-h-[32rem] overflow-auto rounded-lg bg-[var(--color-surface-muted)] p-4 text-xs">
              {JSON.stringify(pack, null, 2)}
            </pre>
          </section>
        </div>
      ) : null}
    </div>
  );
}
