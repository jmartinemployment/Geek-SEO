import { ToolsShell } from '@/components/layout/tools-shell';

export default function ToolsLayout({ children }: { children: React.ReactNode }) {
  return <ToolsShell>{children}</ToolsShell>;
}
