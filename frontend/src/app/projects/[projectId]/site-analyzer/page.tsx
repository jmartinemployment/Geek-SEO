'use client';

import { useParams, useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { SiteAnalyzerWorkspace } from '@/components/site-analyzer/site-analyzer-workspace';
import { useAuthReady } from '@/hooks/use-auth-ready';

function ProjectSiteAnalyzerPageInner() {
  const { accessToken, authLoading } = useAuthReady();
  const params = useParams();
  const searchParams = useSearchParams();
  const projectId = typeof params.projectId === 'string' ? params.projectId : '';
  const initialPackId = searchParams.get('urlResearchId') ?? '';

  if (authLoading) return <div className="p-8">Loading…</div>;

  return (
    <SiteAnalyzerWorkspace
      accessToken={accessToken}
      initialProjectId={projectId}
      initialPackId={initialPackId}
    />
  );
}

export default function ProjectSiteAnalyzerPage() {
  return (
    <Suspense fallback={<div className="p-8">Loading…</div>}>
      <ProjectSiteAnalyzerPageInner />
    </Suspense>
  );
}
