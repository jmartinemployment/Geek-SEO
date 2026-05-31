'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  getBillingPlans,
  getSubscription,
  setSubscriptionTier,
  type BillingCatalogTier,
  type SubscriptionSummary,
} from '@/lib/seo-api';

export function SubscriptionBillingPanel() {
  const { accessToken, isAuthenticated, isLoading: authLoading } = useAuth();
  const [subscription, setSubscription] = useState<SubscriptionSummary | null>(null);
  const [tiers, setTiers] = useState<BillingCatalogTier[]>([]);
  const [manualTierEnabled, setManualTierEnabled] = useState(false);
  const [checkoutDeferred, setCheckoutDeferred] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [savingTier, setSavingTier] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      const plans = await getBillingPlans();
      setTiers(plans.tiers);
      setManualTierEnabled(plans.manualTierChangeEnabled);
      setCheckoutDeferred(plans.checkout.deferred);
      if (isAuthenticated) {
        setSubscription(await getSubscription(accessToken));
      }
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : 'Failed to load billing.');
    }
  }, [accessToken, isAuthenticated]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const handleManualTier = async (tierKey: string) => {
    setSavingTier(tierKey);
    setMessage(null);
    try {
      const updated = await setSubscriptionTier(tierKey, accessToken);
      setSubscription(updated);
      setMessage(`Plan set to ${updated.tier}. Feature gates use this tier immediately.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Could not update plan.');
    } finally {
      setSavingTier(null);
    }
  };

  const currentTier = subscription?.tier ?? 'starter';

  return (
    <Card>
      <CardHeader>
        <CardTitle>Subscription</CardTitle>
        <CardDescription>
          Your plan controls feature access (GSC, GA4, bulk writing, and more).
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {loadError ? <p className="text-sm text-red-600">{loadError}</p> : null}
        {message ? (
          <p className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)] px-3 py-2 text-sm text-[var(--color-text-secondary)]">
            {message}
          </p>
        ) : null}

        {!authLoading && !isAuthenticated ? (
          <p className="text-sm text-[var(--color-text-secondary)]">Sign in to view your plan.</p>
        ) : (
          <div className="flex flex-wrap items-center gap-3">
            <span className="text-sm text-[var(--color-text-secondary)]">Current plan</span>
            <span className="rounded-full bg-[var(--color-surface-muted)] px-3 py-1 text-sm font-semibold capitalize">
              {currentTier}
            </span>
            {subscription?.status ? (
              <span className="text-xs text-[var(--color-text-secondary)]">({subscription.status})</span>
            ) : null}
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          <Link
            href="/pricing"
            className="inline-flex rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90"
          >
            View plans
          </Link>
        </div>

        {checkoutDeferred ? (
          <p className="text-sm text-[var(--color-text-secondary)]">
            PayPal plan IDs are not configured yet (they are created via our setup script, not found in PayPal).
            Tier limits still apply. Use manual tier buttons below if enabled.
          </p>
        ) : null}

        {manualTierEnabled && isAuthenticated ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
            <p className="text-sm font-medium text-amber-950">Developer: change plan without PayPal</p>
            <p className="mt-1 text-xs text-amber-900">
              Enabled via SUBSCRIPTION_MANUAL_TIER_ENABLED on GeekSeoBackend. Do not use in production.
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              {tiers.map((tier) => (
                <button
                  key={tier.key}
                  type="button"
                  disabled={savingTier !== null || currentTier === tier.key}
                  onClick={() => void handleManualTier(tier.key)}
                  className="rounded-md border border-amber-300 bg-white px-3 py-1.5 text-xs font-medium disabled:opacity-50"
                >
                  {savingTier === tier.key ? 'Saving…' : tier.name}
                </button>
              ))}
            </div>
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
