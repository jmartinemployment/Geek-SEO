'use client';

import Link from 'next/link';
import { useState } from 'react';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import { contentWritingPath } from '@/lib/content-writing-search-params';
import { generateContentSpoke } from '@/lib/seo-api';

export function SpokePillarBanner() {
  const { doc, accessToken, reloadDocument } = useWritingWorkspace();
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!doc.parentDocumentId || doc.documentKind !== 'spoke') {
    return null;
  }

  const isShell =
    doc.status !== 'body_generated' &&
    (doc.wordCount ?? 0) < 80 &&
    (doc.contentHtml?.includes('Spoke draft shell') ?? false);

  async function handleGenerate() {
    if (!accessToken || !doc.parentDocumentId) return;
    setGenerating(true);
    setError(null);
    try {
      await generateContentSpoke(doc.parentDocumentId, doc.id, accessToken);
      await reloadDocument();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not generate spoke');
    } finally {
      setGenerating(false);
    }
  }

  return (
    <div className="space-y-2">
      <p className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-950">
        Spoke article in a content cluster.{' '}
        <Link
          href={contentWritingPath({ documentId: doc.parentDocumentId })}
          className="font-medium underline"
        >
          Open pillar document
        </Link>
        {isShell ? (
          <>
            {' · '}
            <button
              type="button"
              disabled={generating}
              onClick={() => void handleGenerate()}
              className="font-medium underline disabled:opacity-50"
            >
              {generating ? 'Generating…' : 'Generate spoke content'}
            </button>
          </>
        ) : null}
      </p>
      {error ? <p className="text-xs text-red-700">{error}</p> : null}
    </div>
  );
}
