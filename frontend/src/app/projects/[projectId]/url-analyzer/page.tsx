'use client';

import { useParams } from 'next/navigation';
import { UrlAnalyzerWorkspace } from '@/components/url-analyzer/url-analyzer-workspace';
import { useAuthReady } from '@/hooks/use-auth-ready';

export default function ProjectUrlAnalyzerPage() {
  const { accessToken, authLoading } = useAuthReady();
  const params = useParams();
  const projectId = typeof params.projectId === 'string' ? params.projectId : '';

  if (authLoading) return <div className="p-8">Loading…</div>;

  return <UrlAnalyzerWorkspace accessToken={accessToken} initialProjectId={projectId} />;
}
