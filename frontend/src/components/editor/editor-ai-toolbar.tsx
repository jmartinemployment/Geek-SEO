'use client';

import { useState } from 'react';
import { humanizeContent } from '@/lib/seo-api';

type EditorAiToolbarProps = {
  documentId: string;
  contentHtml: string;
  accessToken: string | null;
  onApplyHtml: (html: string) => void;
  onError: (message: string) => void;
};

export function EditorAiToolbar({
  documentId,
  contentHtml,
  accessToken,
  onApplyHtml,
  onError,
}: EditorAiToolbarProps) {
  const [busy, setBusy] = useState(false);

  return (
    <div className="mt-6 border-t pt-4">
      <h3 className="text-sm font-semibold">Writing assist</h3>
      <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
        Polish tone and phrasing without leaving the editor.
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={busy}
          className="rounded-lg border bg-white px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          onClick={() => {
            setBusy(true);
            onError('');
            void humanizeContent({ documentId, contentHtml }, accessToken)
              .then((result) => onApplyHtml(result.content))
              .catch((e) => onError(e instanceof Error ? e.message : 'Humanize failed'))
              .finally(() => setBusy(false));
          }}
        >
          {busy ? 'Humanizing…' : 'Humanize draft'}
        </button>
      </div>
    </div>
  );
}
