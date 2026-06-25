'use client';

import { useCallback, useEffect, useState } from 'react';
import { getRenderedContentHtml } from '@/lib/seo-api';
import { copyTextFromPromise } from '@/lib/copy-to-clipboard';
import { useWritingWorkspace } from '@/components/content-writing/review-workspace-context';

function extractJsonFromScript(script: string): string {
  const match = script.match(/<script[^>]*>([\s\S]*?)<\/script>/i);
  if (!match?.[1]) return script.trim();
  try {
    return JSON.stringify(JSON.parse(match[1]), null, 2);
  } catch {
    return match[1].trim();
  }
}

export function JsonLdPanel() {
  const { doc, accessToken } = useWritingWorkspace();
  const [schemaScripts, setSchemaScripts] = useState<string[]>([]);
  const [schemaTypes, setSchemaTypes] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copyHint, setCopyHint] = useState<string | null>(null);

  const loadSchema = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getRenderedContentHtml(doc.id, accessToken);
      setSchemaScripts(result.schemaScripts);
      setSchemaTypes(result.schemaTypes);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Could not load schema');
    } finally {
      setLoading(false);
    }
  }, [accessToken, doc.id]);

  useEffect(() => {
    void loadSchema();
  }, [loadSchema, doc.contentHtml, doc.title]);

  function copyJsonLd() {
    void copyTextFromPromise(async () =>
      schemaScripts.map(extractJsonFromScript).join('\n\n'),
    )
      .then(() => {
        setCopyHint('JSON-LD copied');
        setTimeout(() => setCopyHint(null), 2500);
      })
      .catch(() => setError('Copy failed'));
  }

  return (
    <div className="space-y-3 border-t px-3 py-4 xl:px-4">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3 className="text-xs font-semibold xl:text-sm">JSON-LD schema</h3>
          <p className="mt-1 text-[10px] text-[var(--color-text-secondary)] xl:text-xs">
            Structured data for rich results — generated from your article and site context.
          </p>
        </div>
        <button
          type="button"
          className="shrink-0 rounded-md border px-2 py-1 text-[10px] font-medium hover:bg-[var(--color-surface-muted)] xl:text-xs"
          onClick={() => void loadSchema()}
          disabled={loading}
        >
          {loading ? '…' : 'Refresh'}
        </button>
      </div>

      {schemaTypes.length ? (
        <p className="text-[10px] text-[var(--color-text-muted)] xl:text-xs">
          Types: {schemaTypes.join(', ')}
        </p>
      ) : null}

      {error ? <p className="text-xs text-red-600">{error}</p> : null}
      {copyHint ? <p className="text-xs text-emerald-700">{copyHint}</p> : null}

      {schemaScripts.length ? (
        <>
          <button
            type="button"
            className="w-full rounded-lg border bg-white px-2 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-muted)]"
            onClick={copyJsonLd}
          >
            Copy JSON-LD
          </button>
          <pre className="max-h-48 overflow-auto rounded-lg border bg-slate-950 p-2 text-[10px] leading-relaxed text-slate-100">
            {schemaScripts.map(extractJsonFromScript).join('\n\n')}
          </pre>
        </>
      ) : !loading ? (
        <p className="text-xs text-[var(--color-text-secondary)]">
          No schema blocks yet. Add headings and body content, then refresh.
        </p>
      ) : null}
    </div>
  );
}
