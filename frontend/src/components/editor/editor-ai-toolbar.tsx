'use client';

import { useState } from 'react';
import {
  autoOptimizeContent,
  detectAiContent,
  humanizeContent,
  type AiDetectionResult,
} from '@/lib/seo-api';

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
  const [busy, setBusy] = useState<string | null>(null);
  const [detection, setDetection] = useState<AiDetectionResult | null>(null);
  const [optimizeDelta, setOptimizeDelta] = useState<string | null>(null);

  async function run(action: string, fn: () => Promise<void>) {
    setBusy(action);
    onError('');
    try {
      await fn();
    } catch (e) {
      onError(e instanceof Error ? e.message : `${action} failed`);
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="mt-6 border-t pt-4">
      <h3 className="text-sm font-semibold">AI tools</h3>
      <p className="mt-1 text-xs text-[var(--color-text-secondary)]">Requires ANTHROPIC_API_KEY on GeekSeoBackend.</p>
      <div className="mt-3 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={!!busy}
          className="rounded-lg border bg-white px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          onClick={() =>
            void run('humanize', async () => {
              const result = await humanizeContent({ documentId, contentHtml }, accessToken);
              onApplyHtml(result.content);
            })
          }
        >
          {busy === 'humanize' ? 'Humanizing…' : 'Humanize'}
        </button>
        <button
          type="button"
          disabled={!!busy}
          className="rounded-lg border bg-white px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          onClick={() =>
            void run('optimize', async () => {
              const result = await autoOptimizeContent(documentId, accessToken);
              onApplyHtml(result.contentHtml);
              setOptimizeDelta(`+${result.estimatedScore - result.previousScore} pts (${result.previousScore} → ${result.estimatedScore})`);
            })
          }
        >
          {busy === 'optimize' ? 'Optimizing…' : 'Auto-optimize'}
        </button>
        <button
          type="button"
          disabled={!!busy}
          className="rounded-lg border bg-white px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)] disabled:opacity-50"
          onClick={() =>
            void run('detect', async () => {
              setDetection(await detectAiContent({ documentId, contentHtml }, accessToken));
            })
          }
        >
          {busy === 'detect' ? 'Checking…' : 'AI detect'}
        </button>
      </div>
      {optimizeDelta && <p className="mt-2 text-xs text-green-800">{optimizeDelta}</p>}
      {detection && (
        <p className="mt-2 rounded border bg-white p-2 text-xs text-[var(--color-text-primary)]">
          AI probability: {Math.round(detection.aiProbability * 100)}% — {detection.summary}
        </p>
      )}
    </div>
  );
}
