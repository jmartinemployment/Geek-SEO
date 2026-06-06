'use client';

import Link from 'next/link';

type Props = {
  projectId: string;
};

export function ContentGuardContextBanner({ projectId }: Readonly<Props>) {
  const href = `/app/content-guard?projectId=${encodeURIComponent(projectId)}`;

  return (
    <section className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-muted)]/30 px-5 py-3">
      <div>
        <p className="text-sm font-medium text-[var(--color-text-primary)]">
          Monitor published page decay
        </p>
        <p className="text-xs text-[var(--color-text-muted)]">
          Content Guard flags GSC click drops on live URLs — useful after you publish pillar or gap
          content from this analysis.
        </p>
      </div>
      <Link
        href={href}
        className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-xs font-medium hover:bg-[var(--color-surface-muted)]"
      >
        Open Content Guard
      </Link>
    </section>
  );
}
