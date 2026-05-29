'use client';

import Link from 'next/link';
import { ArrowRight, Globe } from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useRef, useState } from 'react';
import { HomeFeatureCards } from '@/components/marketing/home-feature-cards';
import { ScanResultsPanel } from '@/components/marketing/scan-results-panel';
import { normalizeWebsiteUrl, websiteUrlError } from '@/lib/website-url';

export function HomeHero() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const resultsRef = useRef<HTMLElement>(null);

  const urlFromQuery = searchParams.get('url')?.trim() ?? '';
  const [input, setInput] = useState(urlFromQuery);
  const [lastSyncedUrl, setLastSyncedUrl] = useState(urlFromQuery);
  const [validationError, setValidationError] = useState<string | null>(null);

  if (urlFromQuery !== lastSyncedUrl) {
    setLastSyncedUrl(urlFromQuery);
    setInput(urlFromQuery);
    setValidationError(null);
  }

  const scannedUrl = normalizeWebsiteUrl(urlFromQuery);

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const error = websiteUrlError(input);
    if (error) {
      setValidationError(error);
      return;
    }

    const normalized = normalizeWebsiteUrl(input);
    if (!normalized) {
      setValidationError('Enter a valid website URL (for example, geekatyourspot.com).');
      return;
    }

    setValidationError(null);
    router.replace(`/?url=${encodeURIComponent(normalized)}`, { scroll: false });
    requestAnimationFrame(() => {
      resultsRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  return (
    <div className="flex flex-col">
      <section className="flex min-h-[calc(100vh-3.5rem)] flex-col items-center justify-center px-6 pb-12 pt-10 text-center md:px-10">
        <p className="text-sm font-semibold uppercase tracking-[0.12em] text-[var(--color-accent)]">
          AI-powered SEO for small businesses
        </p>
        <h1 className="mt-4 max-w-3xl text-[42px] font-bold leading-[1.1] tracking-[-0.03em] text-[var(--color-text-primary)]">
          Be found everywhere search happens
        </h1>
        <p className="mt-4 max-w-2xl text-lg text-[var(--color-text-secondary)]">
          Enter your website to run a free site audit preview — topical map, competitors, and keyword gaps unlock after sign-up.
        </p>

        <form onSubmit={handleSubmit} className="mt-10 w-full max-w-3xl">
          <div className="flex flex-col gap-3 sm:flex-row">
            <div className="relative flex-1">
              <Globe className="pointer-events-none absolute left-5 top-1/2 size-5 -translate-y-1/2 text-[var(--color-text-muted)]" />
              <input
                type="url"
                inputMode="url"
                autoComplete="url"
                value={input}
                onChange={(event) => {
                  setInput(event.target.value);
                  if (validationError) setValidationError(null);
                }}
                placeholder="yourwebsite.com"
                aria-invalid={validationError ? true : undefined}
                aria-describedby={validationError ? 'url-error' : 'url-hint'}
                className="h-[52px] w-full rounded-[var(--radius-search)] border-[1.5px] border-[var(--color-border-strong)] bg-white pl-14 pr-4 text-base outline-none transition-[border-color,box-shadow] placeholder:text-[var(--color-text-muted)] focus:border-[var(--color-accent)] focus:shadow-[0_0_0_3px_rgba(59,179,122,0.15)]"
              />
            </div>
            <button
              type="submit"
              className="inline-flex h-[52px] items-center justify-center gap-2 rounded-[var(--radius-button)] bg-[var(--color-accent)] px-8 text-base font-semibold text-white hover:bg-[var(--color-accent-hover)]"
            >
              Scan site
              <ArrowRight className="size-4" />
            </button>
          </div>
          {validationError ? (
            <p id="url-error" className="mt-3 text-sm text-[var(--color-bad)]">
              {validationError}
            </p>
          ) : (
            <p id="url-hint" className="mt-3 text-sm text-[var(--color-text-muted)]">
              No account needed. We analyze your homepage for speed and on-page SEO signals.
            </p>
          )}
        </form>
      </section>

      <section className="border-t border-[var(--color-border)] bg-[var(--color-bg)] px-6 py-10 md:px-10">
        <div className="mx-auto max-w-6xl">
          <HomeFeatureCards />
        </div>
      </section>

      {scannedUrl ? (
        <section ref={resultsRef} className="scroll-mt-20 px-6 py-10 md:px-10">
          <div className="mx-auto max-w-6xl">
            <ScanResultsPanel url={scannedUrl} />
          </div>
        </section>
      ) : null}

      {scannedUrl ? (
        <section className="border-t border-[var(--color-border)] bg-[var(--color-bg)] px-6 py-10 md:px-10">
          <div className="mx-auto flex max-w-6xl flex-col items-start justify-between gap-4 rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white p-6 shadow-[var(--shadow-card)] sm:flex-row sm:items-center">
            <div>
              <p className="font-semibold text-[var(--color-text-primary)]">Your scan is ready.</p>
              <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                Sign up free to unlock site audit, topical map, and competitor analysis for {scannedUrl}.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <Link
                href="/api/auth/start"
                className="inline-flex h-10 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-5 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]"
              >
                Create free account →
              </Link>
              <Link href="/api/auth/start" className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
                Sign in
              </Link>
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
}
