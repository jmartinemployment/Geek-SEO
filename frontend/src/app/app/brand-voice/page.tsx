'use client';

import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  createBrandVoice,
  deleteBrandVoice,
  listBrandVoices,
  type BrandVoice,
} from '@/lib/seo-api';

export default function BrandVoicePage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const [voices, setVoices] = useState<BrandVoice[]>([]);
  const [name, setName] = useState('');
  const [sample, setSample] = useState('');
  const [instructions, setInstructions] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setVoices(await listBrandVoices(accessToken));
  }, [accessToken]);

  useEffect(() => {
    const timer = setTimeout(() => {
      void refresh().catch((e) => setError(e instanceof Error ? e.message : 'Load failed'));
    }, 0);
    return () => clearTimeout(timer);
  }, [refresh]);

  if (authLoading) return <main className="p-8">Loading…</main>;

  return (
    <main className="mx-auto max-w-2xl px-6 py-10">
      <h1 className="text-2xl font-semibold">Brand voice</h1>
      <p className="mt-1 text-sm text-zinc-600">
        Save writing samples and style notes. AI drafting uses these profiles when selected in the editor.
      </p>

      <form
        className="mt-8 space-y-4 rounded-xl border bg-white p-6 shadow-sm"
        onSubmit={(e) => {
          e.preventDefault();
          void (async () => {
            setLoading(true);
            setError(null);
            try {
              await createBrandVoice(
                { name, sampleText: sample, styleInstructions: instructions || undefined },
                accessToken,
              );
              setName('');
              setSample('');
              setInstructions('');
              await refresh();
            } catch (err) {
              setError(err instanceof Error ? err.message : 'Save failed');
            } finally {
              setLoading(false);
            }
          })();
        }}
      >
        <input
          className="w-full rounded-lg border px-3 py-2 text-sm"
          placeholder="Profile name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
        <textarea
          className="min-h-[120px] w-full rounded-lg border px-3 py-2 text-sm"
          placeholder="Paste a sample of your brand's writing…"
          value={sample}
          onChange={(e) => setSample(e.target.value)}
          required
        />
        <textarea
          className="min-h-[80px] w-full rounded-lg border px-3 py-2 text-sm"
          placeholder="Optional style instructions (tone, avoid words, etc.)"
          value={instructions}
          onChange={(e) => setInstructions(e.target.value)}
        />
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-zinc-900 px-4 py-2 text-sm text-white disabled:opacity-50"
        >
          {loading ? 'Saving…' : 'Add profile'}
        </button>
      </form>

      {error ? <p className="mt-4 text-sm text-red-600">{error}</p> : null}

      <ul className="mt-10 space-y-4">
        {voices.map((v) => (
          <li key={v.id} className="rounded-xl border bg-white p-4 shadow-sm">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 className="font-medium">{v.name}</h2>
                <p className="mt-2 line-clamp-3 text-sm text-zinc-600">{v.sampleText}</p>
                {v.styleInstructions ? (
                  <p className="mt-2 text-xs text-zinc-500">{v.styleInstructions}</p>
                ) : null}
              </div>
              <button
                type="button"
                className="text-xs text-red-600 hover:underline"
                onClick={() =>
                  void (async () => {
                    if (!confirm(`Delete "${v.name}"?`)) return;
                    await deleteBrandVoice(v.id, accessToken);
                    await refresh();
                  })()
                }
              >
                Delete
              </button>
            </div>
          </li>
        ))}
        {voices.length === 0 ? (
          <p className="text-sm text-zinc-500">No brand voices yet.</p>
        ) : null}
      </ul>
    </main>
  );
}
