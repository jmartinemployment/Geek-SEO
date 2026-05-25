import Link from 'next/link';

type FeaturePlaceholderProps = {
  title: string;
  description: string;
  planStep: string;
};

export function FeaturePlaceholder({ title, description, planStep }: FeaturePlaceholderProps) {
  return (
    <main className="mx-auto max-w-3xl px-6 py-12">
      <p className="text-xs font-medium uppercase tracking-wide text-zinc-500">{planStep}</p>
      <h1 className="mt-2 text-2xl font-semibold text-zinc-900">{title}</h1>
      <p className="mt-3 text-zinc-600">{description}</p>
      <p className="mt-6 text-sm text-zinc-500">
        Complete per <code className="rounded bg-zinc-100 px-1">GEEKSEO-PLAN.md</code> ({planStep}).
      </p>
      <Link
        href="/app/dashboard"
        className="mt-8 inline-flex rounded-lg bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800"
      >
        Back to dashboard
      </Link>
    </main>
  );
}
