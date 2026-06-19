'use client';

import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { SiteAnalyzerWorkspace } from '@/components/site-analyzer/site-analyzer-workspace';
import { useAuthReady } from '@/hooks/use-auth-ready';

function SiteAnalyzerPageInner() {
  const { accessToken, authLoading } = useAuthReady();
  const searchParams = useSearchParams();
  const initialProjectId = searchParams.get('projectId') ?? '';
  const initialPackId = searchParams.get('urlResearchId') ?? '';

  if (authLoading) return <div className="p-8">Loading…</div>;

  return (
    <SiteAnalyzerWorkspace
      accessToken={accessToken}
      initialProjectId={initialProjectId}
      initialPackId={initialPackId}
    />
  );
}

export default function SiteAnalyzerPage() {
  return (
    <Suspense fallback={<div className="p-8">Loading…</div>}>
      <SiteAnalyzerPageInner />
    </Suspense>
  );
}
