import Link from 'next/link';
import { SubscriptionBillingPanel } from '@/components/billing/subscription-billing-panel';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function SettingsPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <header className="mb-2">
        <h1 className="text-2xl font-semibold tracking-[-0.02em]">Settings</h1>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          Manage billing, integrations, and account preferences.
        </p>
      </header>

      <SubscriptionBillingPanel />

      <Card>
        <CardHeader>
          <CardTitle>Integrations</CardTitle>
          <CardDescription>Google Search Console, GA4, and WordPress are configured per project.</CardDescription>
        </CardHeader>
        <CardContent>
          <Link href="/app/projects" className="text-sm font-semibold text-[var(--color-accent)]">
            Open projects →
          </Link>
        </CardContent>
      </Card>

      <Link href="/app/dashboard" className="inline-block text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
        ← Back to dashboard
      </Link>
    </div>
  );
}
