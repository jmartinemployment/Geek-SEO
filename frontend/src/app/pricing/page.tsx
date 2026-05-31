import Link from 'next/link';
import { PricingCheckout } from '@/components/billing/pricing-checkout';

export default function PricingPage() {
  return (
    <main className="mx-auto max-w-5xl px-6 py-16">
      <h1 className="text-3xl font-semibold tracking-tight text-[var(--color-text-primary)]">Geek SEO pricing</h1>
      <p className="mt-2 text-[var(--color-text-secondary)]">
        Compare plans and see what your account includes. PayPal checkout can be turned on later without changing
        these tiers or feature gates.
      </p>
      <PricingCheckout />
      <Link
        href="/app/dashboard"
        className="mt-10 inline-block text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
      >
        ← Back to app
      </Link>
    </main>
  );
}
