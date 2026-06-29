'use client';

import Link from 'next/link';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import { contentWritingPath } from '@/lib/content-writing-search-params';

export function SpokePillarBanner() {
  const { doc } = useWritingWorkspace();

  if (!doc.parentDocumentId || doc.documentKind !== 'spoke') {
    return null;
  }

  return (
    <p className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-950">
      Spoke article in a content cluster.{' '}
      <Link
        href={contentWritingPath({ documentId: doc.parentDocumentId })}
        className="font-medium underline"
      >
        Open pillar document
      </Link>
    </p>
  );
}
