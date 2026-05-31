'use client';

import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { analyzeDeepSerp, type DeepSerpResult } from '@/lib/seo-api';

export default function DeepSerpPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const searchParams = useSearchParams();
  const [keyword, setKeyword] = useState(() => searchParams.get('q') ?? '');
  const [location, setLocation] = useState('United States');
  const [result, setResult] = useState<DeepSerpResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function downloadCsv() {
    if (!result) return;
    const header = 'position,title,domain,url,snippet';
    const rows = result.organic.map((row) =>
      [
        row.position,
        `"${(row.title ?? '').replaceAll('"', '""')}"`,
        `"${(row.domain ?? '').replaceAll('"', '""')}"`,
        `"${row.url.replaceAll('"', '""')}"`,
        `"${(row.snippet ?? '').replaceAll('"', '""')}"`,
      ].join(','),
    );
    const blob = new Blob([[header, ...rows].join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `serp-${result.keyword.replaceAll(/\s+/g, '-').toLowerCase()}.csv`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  async function onAnalyze(e: React.FormEvent) {
    e.preventDefault();
    if (!keyword.trim()) return;
    setLoading(true);
    setError(null);
    try {
      setResult(await analyzeDeepSerp({ keyword: keyword.trim(), location }, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'SERP analysis failed');
      setResult(null);
    } finally {
      setLoading(false);
    }
  }

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <h1 className="text-2xl font-semibold">Deep SERP analysis</h1>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        Top organic results, People Also Ask, related searches, and intent summary from DataForSEO.
      </p>

      <form onSubmit={onAnalyze} className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm">
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Keyword
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="e.g. best crm for small business"
            required
          />
        </label>
        <label className="block text-sm font-medium text-[var(--color-text-primary)]">
          Location
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
        </label>
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
        >
          {loading ? 'Analyzing…' : 'Analyze SERP'}
        </button>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

      {result ? (
        <div className="mt-10 space-y-8">
          <section className="rounded-xl border bg-white p-6 shadow-sm">
            <h2 className="text-lg font-semibold">Intent</h2>
            <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
              Primary: <span className="font-medium text-[var(--color-text-primary)]">{result.intent.primaryIntent}</span>
              {' · '}
              Avg snippet: {Math.round(result.intent.avgSnippetLength)} chars
            </p>
            {result.intent.contentFormats.length > 0 ? (
              <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                Formats: {result.intent.contentFormats.join(', ')}
              </p>
            ) : null}
            <p className="mt-2 text-xs text-[var(--color-text-muted)]">Provider: {result.provider}</p>
          </section>

          <section>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-lg font-semibold">Organic results ({result.organic.length})</h2>
              <button
                type="button"
                onClick={downloadCsv}
                className="rounded-lg border px-3 py-1.5 text-xs font-medium hover:bg-slate-50"
              >
                Export CSV
              </button>
            </div>
            <div className="mt-4 overflow-x-auto rounded-xl border bg-white shadow-sm">
              <table className="min-w-full text-left text-sm">
                <thead className="border-b bg-[var(--color-surface-muted)] text-xs uppercase tracking-wide text-[var(--color-text-secondary)]">
                  <tr>
                    <th className="px-4 py-3">#</th>
                    <th className="px-4 py-3">Title</th>
                    <th className="px-4 py-3">Domain</th>
                    <th className="px-4 py-3">Snippet</th>
                  </tr>
                </thead>
                <tbody>
                  {result.organic.map((row) => (
                    <tr key={`${row.position}-${row.url}`} className="border-b last:border-0">
                      <td className="px-4 py-3 font-medium">{row.position}</td>
                      <td className="max-w-xs px-4 py-3">
                        <Link
                          href={row.url}
                          target="_blank"
                          rel="noreferrer"
                          className="font-medium text-[var(--color-brand)] hover:underline"
                        >
                          {row.title ?? row.url}
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-[var(--color-text-secondary)]">{row.domain ?? '—'}</td>
                      <td className="max-w-md px-4 py-3 text-[var(--color-text-secondary)]">{row.snippet ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {result.peopleAlsoAsk.length > 0 ? (
            <section className="rounded-xl border bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold">People also ask</h2>
              <ul className="mt-3 list-disc space-y-1 pl-5 text-sm text-[var(--color-text-primary)]">
                {result.peopleAlsoAsk.map((q) => (
                  <li key={q}>{q}</li>
                ))}
              </ul>
            </section>
          ) : null}

          {result.relatedSearches.length > 0 ? (
            <section className="rounded-xl border bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold">Related searches</h2>
              <div className="mt-3 flex flex-wrap gap-2">
                {result.relatedSearches.map((term) => (
                  <button
                    key={term}
                    type="button"
                    className="rounded-full border px-3 py-1 text-xs font-medium hover:bg-[var(--color-surface-muted)]"
                    onClick={() => {
                      setKeyword(term);
                      setResult(null);
                    }}
                  >
                    {term}
                  </button>
                ))}
              </div>
            </section>
          ) : null}
        </div>
      ) : null}
    </main>
  );
}
