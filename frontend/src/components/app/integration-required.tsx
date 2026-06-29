import Link from 'next/link';

type IntegrationRequiredProps = Readonly<{
  title: string;
  description: string;
  integrationName: string;
  docsHref?: string;
}>;

export function IntegrationRequired({
  title,
  description,
  integrationName,
  docsHref,
}: IntegrationRequiredProps) {
  return (
    <main className="mx-auto max-w-2xl px-6 py-16">
      <p className="text-xs font-medium uppercase tracking-wide text-amber-700">Requires setup</p>
      <h1 className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">{title}</h1>
      <p className="mt-3 text-sm leading-relaxed text-[var(--color-text-secondary)]">{description}</p>
      <div className="mt-8 rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-950">
        <p className="font-medium">{integrationName} is not connected for this project.</p>
        <p className="mt-2 text-amber-900/90">
          Connect the integration in project settings, or configure OAuth credentials in your GeekSeoBackend
          environment. Until then, this screen shows what will appear once data is available — no mock metrics.
        </p>
      </div>
      <div className="mt-8 flex flex-wrap gap-4 text-sm">
        <Link
          href="/projects"
          className="font-medium text-[var(--color-text-primary)] underline-offset-2 hover:underline"
        >
          ← Projects (connect Google)
        </Link>
        <Link
          href="/rankings"
          className="font-medium text-[var(--color-text-primary)] underline-offset-2 hover:underline"
        >
          GSC rankings
        </Link>
        <Link
          href="/analytics"
          className="font-medium text-[var(--color-text-primary)] underline-offset-2 hover:underline"
        >
          GA4 analytics
        </Link>
      </div>
      {docsHref ? (
        <p className="mt-4 text-xs text-[var(--color-text-secondary)]">{docsHref}</p>
      ) : (
        <p className="mt-4 text-xs text-[var(--color-text-secondary)]">
          Local setup: see <code className="rounded bg-[var(--color-surface-muted)] px-1">scripts/LOCAL_DEV.md</code> in the repo.
        </p>
      )}
    </main>
  );
}
