import { AppShell } from '@/components/app/app-shell';

export default function ContentWritingLayout({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
