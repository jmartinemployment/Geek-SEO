'use client';

import { useState } from 'react';
import { generateBlogSpoke, humanizeContent } from '@/lib/seo-api';

type EditorAiToolbarProps = {
  documentId: string;
  contentHtml: string;
  accessToken: string | null;
  onApplyHtml: (html: string) => void;
  onError: (message: string) => void;
  onBlogSpokeCreated?: () => void;
};

export function EditorAiToolbar({
  documentId,
  contentHtml,
  accessToken,
  onApplyHtml,
  onError,
  onBlogSpokeCreated,
}: EditorAiToolbarProps) {
  const [busy, setBusy] = useState<'humanize' | 'blog' | null>(null);

  return (
    <div className="mt-6 border-t pt-4">
      <h3 className="text-sm font-semibold">Writing assist</h3>
      <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
        Polish the pillar or generate a distinct-intent blog spoke.
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={busy !== null}
          className="rounded-lg border bg-white px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          onClick={() => {
            setBusy('humanize');
            onError('');
            void humanizeContent({ documentId, contentHtml }, accessToken)
              .then((result) => onApplyHtml(result.content))
              .catch((e) => onError(e instanceof Error ? e.message : 'Humanize failed'))
              .finally(() => setBusy(null));
          }}
        >
          {busy === 'humanize' ? 'Humanizing…' : 'Humanize draft'}
        </button>
        <button
          type="button"
          disabled={busy !== null || !accessToken}
          className="rounded-lg bg-[var(--color-accent)] px-3 py-1.5 text-xs font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          onClick={() => {
            setBusy('blog');
            onError('');
            void generateBlogSpoke(documentId, { spokeType: 'comparison' }, accessToken)
              .then(() => onBlogSpokeCreated?.())
              .catch((e) => onError(e instanceof Error ? e.message : 'Blog generation failed'))
              .finally(() => setBusy(null));
          }}
        >
          {busy === 'blog' ? 'Creating blog version…' : 'Create blog version'}
        </button>
      </div>
      <p className="mt-2 text-xs text-[var(--color-text-secondary)]">
        Blog options (spoke type, keyword, copy fields) are in the right rail under Blog version.
      </p>
    </div>
  );
}
