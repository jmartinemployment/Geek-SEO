'use client';

import { useState } from 'react';
import { generateFeaturedImage } from '@/lib/seo-api';
import { useReviewWorkspace } from './review-workspace-context';

export function ReviewFeaturedImage() {
  const { doc, accessToken, setFeaturedImageUrl } = useReviewWorkspace();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleGenerate(regenerate: boolean) {
    setLoading(true);
    setError(null);
    try {
      const result = await generateFeaturedImage(doc.id, { regenerate }, accessToken);
      setFeaturedImageUrl(result.dataUrl);
    } catch (generateError) {
      setError(generateError instanceof Error ? generateError.message : 'Image generation failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-3 border-t px-3 py-4 xl:px-4">
      <div>
        <h3 className="text-xs font-semibold xl:text-sm">Featured image</h3>
        <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
          OpenAI hero image for publish and social previews.
        </p>
      </div>

      {doc.featuredImageUrl ? (
        <img
          src={doc.featuredImageUrl}
          alt={`Featured image for ${doc.title}`}
          className="w-full rounded-lg border object-cover"
        />
      ) : (
        <div className="rounded-lg border border-dashed bg-[var(--color-surface-muted)] px-3 py-8 text-center text-xs text-[var(--color-text-secondary)]">
          No featured image yet
        </div>
      )}

      <button
        type="button"
        disabled={loading}
        className="w-full rounded-lg bg-[var(--color-accent)] px-2 py-1.5 text-xs font-medium text-white disabled:opacity-50 xl:px-3 xl:py-2 xl:text-sm"
        onClick={() => void handleGenerate(Boolean(doc.featuredImageUrl))}
      >
        {loading
          ? 'Generating…'
          : doc.featuredImageUrl
            ? 'Regenerate featured image'
            : 'Generate featured image'}
      </button>

      {error ? <p className="text-xs text-red-600">{error}</p> : null}
    </div>
  );
}
