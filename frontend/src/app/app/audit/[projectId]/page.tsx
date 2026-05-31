'use client';

import { useParams } from 'next/navigation';
import { SiteAuditView } from '@/components/audit/site-audit-view';

export default function ProjectAuditPage() {
  const params = useParams();
  const projectId = params.projectId as string;

  return <SiteAuditView initialProjectId={projectId} />;
}
