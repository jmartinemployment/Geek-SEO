import Link from 'next/link';
import { PricingCheckout } from '@/components/billing/pricing-checkout';

const tiers = [
  {
    key: 'starter',
    name: 'Starter',
    price: '$29',
    highlights: ['20 documents', '3 full articles', 'Local SERP on every tier'],
  },
  {
    key: 'professional',
    name: 'Professional',
    price: '$59',
    highlights: ['GSC + GA4', 'Topical map', 'Content audit'],
  },
  {
    key: 'team',
    name: 'Team',
    price: '$89',
    highlights: ['Bulk jobs', 'Content Guard', 'Higher GEO limits'],
  },
  {
    key: 'agency',
    name: 'Agency',
    price: '$149',
    highlights: ['Public API', 'Unlimited caps', 'White-label reports'],
  },
];

export default function PricingPage() {
  return (
    <main className="mx-auto max-w-5xl px-6 py-16">
      <h1 className="text-3xl font-semibold tracking-tight text-[var(--color-text-primary)]">Geek SEO pricing</h1>
      <p className="mt-2 text-[var(--color-text-secondary)]">
        Subscribe with PayPal. Tier enforcement runs on GeekSeoBackend — upgrades apply after PayPal activates your
        subscription.
      </p>
      <PricingCheckout tiers={tiers} />
      <Link
        href="/app/dashboard"
        className="mt-10 inline-block text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
      >
        ← Back to app
      </Link>
    </main>
  );
}
