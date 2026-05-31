'use client';

import Script from 'next/script';
import Link from 'next/link';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import {
  cancelSubscription,
  getBillingPlans,
  getSubscription,
  type BillingCatalogTier,
  type BillingPlansResponse,
  type SubscriptionSummary,
} from '@/lib/seo-api';

declare global {
  interface Window {
    paypal?: {
      Buttons: (config: Record<string, unknown>) => { render: (selector: string) => Promise<void> };
    };
  }
}

function resolveBillingUserId(accessToken: string | null): string | null {
  const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID;
  if (devUserId) return devUserId;
  if (!accessToken) return null;

  const parts = accessToken.split('.');
  if (parts.length < 2) return null;
  try {
    const payload = JSON.parse(atob(parts[1].replaceAll('-', '+').replaceAll('_', '/'))) as {
      sub?: string;
    };
    return payload.sub ?? null;
  } catch {
    return null;
  }
}

export function PricingCheckout() {
  const { accessToken, isAuthenticated, isLoading: authLoading } = useAuth();
  const [plans, setPlans] = useState<BillingPlansResponse | null>(null);
  const [subscription, setSubscription] = useState<SubscriptionSummary | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [sdkReady, setSdkReady] = useState(false);
  const renderedRef = useRef(false);

  const billingUserId = useMemo(() => resolveBillingUserId(accessToken), [accessToken]);
  const tiers = plans?.tiers ?? [];
  const checkoutAvailable = plans?.checkout.available === true;

  const refreshSubscription = useCallback(async () => {
    if (!isAuthenticated) return;
    try {
      const summary = await getSubscription(accessToken);
      setSubscription(summary);
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : 'Failed to load subscription.');
    }
  }, [accessToken, isAuthenticated]);

  useEffect(() => {
    void (async () => {
      try {
        const billingPlans = await getBillingPlans();
        setPlans(billingPlans);
        await refreshSubscription();
      } catch (error) {
        setLoadError(error instanceof Error ? error.message : 'Failed to load billing plans.');
      }
    })();
  }, [refreshSubscription]);

  useEffect(() => {
    renderedRef.current = false;
  }, [checkoutAvailable, plans?.checkout.clientId]);

  useEffect(() => {
    if (!sdkReady || !checkoutAvailable || !plans?.checkout.clientId || !plans.checkout.planIds || !billingUserId) {
      return;
    }
    if (!window.paypal || renderedRef.current) return;

    renderedRef.current = true;

    for (const tier of tiers) {
      const planId = plans.checkout.planIds[tier.key];
      const containerId = `paypal-${tier.key}`;
      if (!planId || !document.getElementById(containerId)) continue;

      void window.paypal
        .Buttons({
          style: { shape: 'rect', color: 'gold', layout: 'vertical', label: 'subscribe' },
          createSubscription: (_data: unknown, actions: { subscription: { create: (args: Record<string, string>) => Promise<string> } }) =>
            actions.subscription.create({
              plan_id: planId,
              custom_id: billingUserId,
            }),
          onApprove: async () => {
            setActionMessage(
              'PayPal approved your subscription. Your plan activates once PayPal sends the webhook (usually within a minute).',
            );
            await refreshSubscription();
          },
          onError: (error: unknown) => {
            setActionMessage(error instanceof Error ? error.message : 'PayPal checkout failed.');
          },
        })
        .render(`#${containerId}`);
    }
  }, [billingUserId, checkoutAvailable, plans, refreshSubscription, sdkReady, tiers]);

  const handleCancel = async () => {
    setCancelling(true);
    setActionMessage(null);
    try {
      await cancelSubscription(accessToken);
      setActionMessage('Subscription cancelled. Your account returns to the Starter tier.');
      await refreshSubscription();
    } catch (error) {
      setActionMessage(error instanceof Error ? error.message : 'Cancellation failed.');
    } finally {
      setCancelling(false);
    }
  };

  const activePaid =
    subscription?.status === 'active' &&
    subscription.tier !== 'starter' &&
    Boolean(subscription.paypalSubscriptionId);

  const currentTierKey = subscription?.tier ?? 'starter';

  return (
    <>
      {checkoutAvailable && plans?.checkout.clientId ? (
        <Script
          src={`https://www.paypal.com/sdk/js?client-id=${encodeURIComponent(plans.checkout.clientId)}&vault=true&intent=subscription`}
          strategy="afterInteractive"
          onLoad={() => setSdkReady(true)}
        />
      ) : null}

      {loadError ? (
        <p className="mt-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{loadError}</p>
      ) : null}

      {actionMessage ? (
        <p className="mt-6 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)] px-4 py-3 text-sm text-[var(--color-text-secondary)]">
          {actionMessage}
        </p>
      ) : null}

      {!authLoading && !isAuthenticated ? (
        <p className="mt-6 text-sm text-[var(--color-text-secondary)]">
          <Link href="/auth/login" className="font-medium underline">
            Sign in
          </Link>{' '}
          to see your current plan or subscribe when checkout is enabled.
        </p>
      ) : null}

      {subscription ? (
        <p className="mt-6 text-sm text-[var(--color-text-secondary)]">
          Current plan:{' '}
          <span className="font-medium capitalize text-[var(--color-text-primary)]">{subscription.tier}</span>{' '}
          ({subscription.status})
        </p>
      ) : null}

      {plans?.checkout.deferred ? (
        <p className="mt-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)] px-4 py-3 text-sm text-[var(--color-text-secondary)]">
          PayPal checkout is not set up yet. PayPal does not ship{' '}
          <code className="text-xs">PAYPAL_PLAN_STARTER</code> (or other plan env vars) — those IDs are
          created with{' '}
          <code className="text-xs">node scripts/paypal-create-subscription-plans.mjs</code>. Until then,
          use Settings to change tier if manual mode is enabled.
          {plans.checkout.plansSetupHint ? (
            <>
              <br />
              <span className="mt-2 block text-xs">{plans.checkout.plansSetupHint}</span>
            </>
          ) : null}
        </p>
      ) : null}

      {activePaid ? (
        <button
          type="button"
          onClick={() => void handleCancel()}
          disabled={cancelling}
          className="mt-4 rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)] disabled:opacity-60"
        >
          {cancelling ? 'Cancelling…' : 'Cancel subscription'}
        </button>
      ) : null}

      <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {tiers.map((tier) => (
          <TierCard
            key={tier.key}
            tier={tier}
            isCurrent={currentTierKey === tier.key}
            checkoutAvailable={checkoutAvailable}
            isAuthenticated={isAuthenticated}
            hasBillingUser={Boolean(billingUserId)}
          />
        ))}
      </div>
    </>
  );
}

function TierCard({
  tier,
  isCurrent,
  checkoutAvailable,
  isAuthenticated,
  hasBillingUser,
}: Readonly<{
  tier: BillingCatalogTier;
  isCurrent: boolean;
  checkoutAvailable: boolean;
  isAuthenticated: boolean;
  hasBillingUser: boolean;
}>) {
  return (
    <article
      className={`rounded-xl border bg-white p-6 shadow-sm ${
        isCurrent ? 'border-[var(--color-accent)] ring-1 ring-[var(--color-accent)]' : 'border-[var(--color-border)]'
      }`}
    >
      {isCurrent ? (
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-accent)]">Current plan</p>
      ) : null}
      <h2 className="text-lg font-semibold">{tier.name}</h2>
      <p className="mt-1 text-2xl font-bold text-[var(--color-text-primary)]">{tier.priceLabel}</p>
      <p className="text-xs text-[var(--color-text-secondary)]">per month</p>
      <ul className="mt-4 space-y-2 text-sm text-[var(--color-text-secondary)]">
        {tier.highlights.map((item) => (
          <li key={item}>• {item}</li>
        ))}
      </ul>
      {checkoutAvailable && isAuthenticated && hasBillingUser ? (
        <div id={`paypal-${tier.key}`} className="mt-6 min-h-[45px]" />
      ) : (
        <button
          type="button"
          disabled
          className="mt-6 w-full cursor-default rounded-lg bg-[var(--color-surface-muted)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)]"
        >
          {checkoutAvailable ? 'Sign in to subscribe' : 'PayPal checkout coming soon'}
        </button>
      )}
    </article>
  );
}
