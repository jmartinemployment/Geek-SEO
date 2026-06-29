import Link from 'next/link';
import { PricingCheckout } from '@/components/billing/pricing-checkout';

export default function PricingPage() {
  return (
    <main className="mx-auto max-w-5xl px-6 py-16">
      <h1 className="text-3xl font-semibold tracking-tight text-[var(--color-text-primary)]">Geek SEO pricing</h1>
      <p className="mt-2 text-[var(--color-text-secondary)]">
        Compare plans and subscribe when checkout is enabled. Sandbox mode uses test PayPal accounts only.
      </p>
      <PricingCheckout />
      <Link
        href="/dashboard"
        className="mt-10 inline-block text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
      >
        ← Back to app
      </Link>
    </main>
  );
}
