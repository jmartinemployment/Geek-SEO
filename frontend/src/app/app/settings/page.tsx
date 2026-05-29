import Link from 'next/link';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function SettingsPage() {
  return (
    <div className="mx-auto max-w-3xl">
      <header className="mb-8">
        <h1 className="text-2xl font-semibold tracking-[-0.02em]">Settings</h1>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          Manage integrations, billing, and account preferences.
        </p>
      </header>
      <Card>
        <CardHeader>
          <CardTitle>Settings shell</CardTitle>
          <CardDescription>
            Google, WordPress, and subscription panels land here in Phase 3.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Link href="/app/dashboard" className="text-sm font-semibold text-[var(--color-accent)]">
            Back to dashboard →
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
