'use client';

import { useParams, useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { UrlAnalyzerWorkspace } from '@/components/url-analyzer/url-analyzer-workspace';
import { useAuthReady } from '@/hooks/use-auth-ready';

function ProjectUrlAnalyzerPageInner() {
  const { accessToken, authLoading } = useAuthReady();
  const params = useParams();
  const searchParams = useSearchParams();
  const projectId = typeof params.projectId === 'string' ? params.projectId : '';
  const initialUrlResearchId = searchParams.get('urlResearchId') ?? '';

  if (authLoading) return <div className="p-8">Loading…</div>;

  return (
    <UrlAnalyzerWorkspace
      accessToken={accessToken}
      initialProjectId={projectId}
      initialUrlResearchId={initialUrlResearchId}
    />
  );
}

export default function ProjectUrlAnalyzerPage() {
  return (
    <Suspense fallback={<div className="p-8">Loading…</div>}>
      <ProjectUrlAnalyzerPageInner />
    </Suspense>
  );
}
