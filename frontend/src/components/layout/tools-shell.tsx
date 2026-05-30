import { AuthStartLink } from '@/components/auth/auth-start-link';
import { SiteHeader } from '@/components/layout/site-header';

export function ToolsShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-bg)]">
      <SiteHeader variant="tools" />
      <div className="flex flex-1 flex-col">{children}</div>
      <footer className="border-t border-[var(--color-border)] bg-[var(--color-bg)] px-6 py-8 md:px-10">
        <div className="mx-auto flex max-w-5xl flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <p className="text-base font-semibold text-[var(--color-text-primary)]">
              Ready for the full platform?
            </p>
            <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
              Sign up free to unlock topical maps, audits, and the content editor.
            </p>
          </div>
          <AuthStartLink className="inline-flex h-10 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-5 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]">
            Create free account →
          </AuthStartLink>
        </div>
      </footer>
    </div>
  );
}
