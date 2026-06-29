'use client';

import { Suspense } from 'react';
import { SiteAnalyzer2Workspace } from '@/components/site-analyzer2/site-analyzer2-workspace';
import { CrawlStreamProvider } from '@/context/crawl-stream-context';
import { useAuthReady } from '@/hooks/use-auth-ready';

function SiteAnalyzer2PageInner() {
  const { accessToken, authLoading } = useAuthReady();

  if (authLoading) {
    return <main className="p-8 text-[var(--color-text-secondary)]">Loading…</main>;
  }

  return (
    <CrawlStreamProvider accessToken={accessToken}>
      <SiteAnalyzer2Workspace accessToken={accessToken} />
    </CrawlStreamProvider>
  );
}

export default function SiteAnalyzer2Page() {
  return (
    <Suspense
      fallback={
        <main className="p-8 text-[var(--color-text-secondary)]">Loading…</main>
      }
    >
      <SiteAnalyzer2PageInner />
    </Suspense>
  );
}
