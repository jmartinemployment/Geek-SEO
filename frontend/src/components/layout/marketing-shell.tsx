import { SiteHeader } from '@/components/layout/site-header';

export function MarketingShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-bg)]">
      <SiteHeader variant="marketing" />
      <div className="flex flex-1 flex-col">{children}</div>
    </div>
  );
}
