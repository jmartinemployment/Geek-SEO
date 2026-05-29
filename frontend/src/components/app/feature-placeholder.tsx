import Link from 'next/link';

type FeaturePlaceholderProps = {
  title: string;
  description: string;
  planStep: string;
};

export function FeaturePlaceholder({ title, description, planStep }: FeaturePlaceholderProps) {
  return (
    <main className="mx-auto max-w-3xl px-6 py-12">
      <p className="text-xs font-medium uppercase tracking-wide text-[var(--color-text-secondary)]">{planStep}</p>
      <h1 className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">{title}</h1>
      <p className="mt-3 text-[var(--color-text-secondary)]">{description}</p>
      <p className="mt-6 text-sm text-[var(--color-text-secondary)]">
        Complete per <code className="rounded bg-[var(--color-surface-muted)] px-1">GEEKSEO-PLAN.md</code> ({planStep}).
      </p>
      <Link
        href="/app/dashboard"
        className="mt-8 inline-flex rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)]"
      >
        Back to dashboard
      </Link>
    </main>
  );
}
