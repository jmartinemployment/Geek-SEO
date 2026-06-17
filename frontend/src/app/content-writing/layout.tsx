import { AppShell } from '@/components/app/app-shell';

export default function ContentWritingLayout({ children }: { children: React.ReactNode }) {
  return <AppShell mainClassName="px-2 py-4 sm:px-4 lg:px-6">{children}</AppShell>;
}
