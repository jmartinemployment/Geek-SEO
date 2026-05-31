'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  checkPlagiarism,
  getLatestPlagiarismCheck,
  getPlagiarismStatus,
  type PlagiarismCheckResult,
} from '@/lib/seo-api';

type PlagiarismPanelProps = Readonly<{
  documentId: string;
  accessToken?: string | null;
}>;

export function PlagiarismPanel({ documentId, accessToken }: PlagiarismPanelProps) {
  const [configured, setConfigured] = useState<boolean | null>(null);
  const [result, setResult] = useState<PlagiarismCheckResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadLatest = useCallback(async () => {
    try {
      const latest = await getLatestPlagiarismCheck(documentId, accessToken);
      setResult(latest);
    } catch {
      setResult(null);
    }
  }, [documentId, accessToken]);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      try {
        const status = await getPlagiarismStatus(accessToken);
        if (cancelled) return;
        setConfigured(status.configured);
        if (status.configured) {
          await loadLatest();
        }
      } catch {
        if (!cancelled) setConfigured(false);
      }
    }

    void init();
    return () => {
      cancelled = true;
    };
  }, [accessToken, loadLatest]);

  async function runCheck(forceRefresh: boolean) {
    setLoading(true);
    setError(null);
    try {
      const checked = await checkPlagiarism(documentId, accessToken, forceRefresh);
      setResult(checked);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Plagiarism check failed');
    } finally {
      setLoading(false);
    }
  }

  if (configured === null) {
    return (
      <section className="mt-6 rounded-xl border border-[var(--color-border)] bg-white p-4 shadow-sm">
        <p className="text-xs text-[var(--color-text-secondary)]">Loading plagiarism settings…</p>
      </section>
    );
  }

  if (!configured) {
    return (
      <section className="mt-6 rounded-xl border border-[var(--color-border)] bg-white p-4 shadow-sm">
        <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Plagiarism (Copyscape)</h2>
        <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
          Copyscape is not configured on this server. Set <code className="text-[11px]">COPYSCAPE_USERNAME</code> and{' '}
          <code className="text-[11px]">COPYSCAPE_API_KEY</code> on GeekSeoBackend to enable web duplicate checks.
          Publishing works without it.
        </p>
      </section>
    );
  }

  return (
    <section className="mt-6 rounded-xl border border-[var(--color-border)] bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Plagiarism (Copyscape)</h2>
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
            Publish is blocked when match exceeds 15%. Results cache for 24 hours.
          </p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            disabled={loading}
            onClick={() => void runCheck(false)}
            className="rounded-lg border border-[var(--color-border)] px-3 py-1.5 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
          >
            {loading ? 'Checking…' : 'Check'}
          </button>
          <button
            type="button"
            disabled={loading}
            onClick={() => void runCheck(true)}
            className="rounded-lg border border-[var(--color-border)] px-3 py-1.5 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
          >
            Force refresh
          </button>
        </div>
      </div>

      {error ? <p className="mt-3 text-sm text-red-700">{error}</p> : null}

      {result ? (
        <div className="mt-4">
          <div
            className={`rounded-lg border px-3 py-2 text-sm ${
              result.publishBlocked
                ? 'border-red-200 bg-red-50 text-red-900'
                : 'border-emerald-200 bg-emerald-50 text-emerald-900'
            }`}
          >
            <p className="font-medium">
              {result.matchPercent}% matched
              {result.cached ? ' (cached)' : ''}
            </p>
            <p className="mt-1 text-xs opacity-90">
              {result.publishBlocked
                ? 'Publishing should wait until overlap is reduced below 15%.'
                : 'Within acceptable overlap for publish.'}
            </p>
            <p className="mt-1 text-xs opacity-75">
              Checked {new Date(result.checkedAt).toLocaleString()}
            </p>
          </div>

          {result.matches.length > 0 ? (
            <ul className="mt-3 max-h-48 space-y-2 overflow-y-auto text-xs">
              {result.matches.map((match) => (
                <li key={`${match.url}-${match.matchPercent}`} className="rounded border border-slate-200 p-2">
                  <p className="font-medium text-[var(--color-text-primary)]">
                    {match.title ?? match.url}
                  </p>
                  <p className="text-[var(--color-text-secondary)]">
                    {match.matchPercent}% · {match.wordsMatched} words
                  </p>
                  <a
                    href={match.viewUrl ?? match.url}
                    target="_blank"
                    rel="noreferrer"
                    className="text-[var(--color-brand)] underline-offset-2 hover:underline"
                  >
                    View match
                  </a>
                </li>
              ))}
            </ul>
          ) : (
            <p className="mt-3 text-xs text-[var(--color-text-secondary)]">No external matches found.</p>
          )}
        </div>
      ) : (
        <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
          Run a check before publishing to scan the web for duplicate content.
        </p>
      )}
    </section>
  );
}
