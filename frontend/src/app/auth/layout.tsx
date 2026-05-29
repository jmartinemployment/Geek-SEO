import { MarketingShell } from '@/components/layout/marketing-shell';

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return <MarketingShell>{children}</MarketingShell>;
}
