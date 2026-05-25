'use client';

import Link from 'next/link';
import { SeoApiError } from '@/lib/seo-api-errors';

type SeoErrorBannerProps = {
  error: unknown;
};

export function SeoErrorBanner({ error }: SeoErrorBannerProps) {
  if (!error) return null;

  const apiError = error instanceof SeoApiError ? error : null;
  const message = error instanceof Error ? error.message : 'Something went wrong';
  const showPricing =
    apiError?.isUpgradeRequired || apiError?.isUsageLimit || apiError?.body.upgradeUrl;

  return (
    <div
      className={`rounded-lg border p-4 text-sm ${
        apiError?.isUpgradeRequired || apiError?.isUsageLimit
          ? 'border-amber-200 bg-amber-50 text-amber-950'
          : 'border-red-200 bg-red-50 text-red-900'
      }`}
      role="alert"
    >
      <p className="font-medium">{message}</p>
      {apiError?.body.requiredTier && (
        <p className="mt-1 text-xs opacity-90">Required plan: {apiError.body.requiredTier}</p>
      )}
      {apiError?.body.feature && apiError.body.limit !== undefined && (
        <p className="mt-1 text-xs opacity-90">
          Meter: {apiError.body.feature} ({apiError.body.usage ?? 0}/{apiError.body.limit})
        </p>
      )}
      {showPricing && (
        <Link href="/pricing" className="mt-3 inline-block text-xs font-medium underline">
          View plans
        </Link>
      )}
    </div>
  );
}
