'use client';

import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { UrlAnalyzerWorkspace } from '@/components/url-analyzer/url-analyzer-workspace';
import { useAuthReady } from '@/hooks/use-auth-ready';

function UrlAnalyzerPageInner() {
  const { accessToken, authLoading } = useAuthReady();
  const searchParams = useSearchParams();
  const initialProjectId = searchParams.get('projectId') ?? '';

  const initialUrlResearchId = searchParams.get('urlResearchId') ?? '';

  if (authLoading) return <div className="p-8">Loading…</div>;

  return (
    <UrlAnalyzerWorkspace
      accessToken={accessToken}
      initialProjectId={initialProjectId}
      initialUrlResearchId={initialUrlResearchId}
    />
  );
}

export default function UrlAnalyzerPage() {
  return (
    <Suspense fallback={<div className="p-8">Loading…</div>}>
      <UrlAnalyzerPageInner />
    </Suspense>
  );
}
