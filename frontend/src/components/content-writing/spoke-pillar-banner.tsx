'use client';

import Link from 'next/link';
import { useState } from 'react';
import { Loader2 } from 'lucide-react';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';
import { contentWritingPath } from '@/lib/content-writing-search-params';
import { isSpokeShellDocument } from '@/lib/content-spoke-shell';
import { generateContentSpoke } from '@/lib/seo-api';
import { Button } from '@/components/ui/button';

export function SpokePillarBanner() {
  const { doc, accessToken, reloadDocument } = useWritingWorkspace();
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!doc.parentDocumentId || doc.documentKind !== 'spoke') {
    return null;
  }

  const isShell = isSpokeShellDocument(doc);

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
      {isShell ? (
        <div className="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950">
          <p className="font-semibold">Blog spoke shell — content not generated yet</p>
          <p className="mt-1 text-xs leading-relaxed text-amber-900/90">
            This page was created as a placeholder blog post linked from your pillar
            article. Generate the full 800–1,200 word spoke draft from the pillar content and research
            pack.
          </p>
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <Button
              type="button"
              size="sm"
              disabled={generating || !accessToken}
              onClick={() => void handleGenerate()}
            >
              {generating ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  Generating spoke…
                </>
              ) : (
                'Generate blog post content'
              )}
            </Button>
            <Link
              href={contentWritingPath({ documentId: doc.parentDocumentId })}
              className="text-xs font-medium text-amber-900 underline underline-offset-2"
            >
              Open pillar article
            </Link>
          </div>
          <p className="mt-2 text-[11px] text-amber-800/80">
            Requires a drafted pillar article with body content before generation can run.
          </p>
        </div>
      ) : (
        <p className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/30 px-3 py-2 text-xs text-[var(--color-text-secondary)]">
          Blog post linked from pillar.{' '}
          <Link
            href={contentWritingPath({ documentId: doc.parentDocumentId })}
            className="font-medium text-[var(--color-accent)] underline"
          >
            Open pillar article
          </Link>
        </p>
      )}
      {error ? <p className="text-xs text-red-700">{error}</p> : null}
    </div>
  );
}
