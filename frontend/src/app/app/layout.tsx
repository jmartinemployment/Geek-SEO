'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/components/auth/auth-provider';

const nav = [
  { href: '/app/dashboard', label: 'Dashboard' },
  { href: '/app/guided', label: 'Guided' },
  { href: '/app/keywords', label: 'Keywords' },
  { href: '/app/briefs/new', label: 'Briefs' },
  { href: '/app/calendar', label: 'Calendar' },
  { href: '/app/projects', label: 'Projects' },
];

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { logout, isAuthenticated } = useAuth();

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-3">
          <Link href="/app/dashboard" className="font-semibold tracking-tight">
            Geek SEO
          </Link>
          <nav className="flex items-center gap-4 text-sm">
            {nav.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={
                  pathname.startsWith(item.href)
                    ? 'font-medium text-zinc-900'
                    : 'text-zinc-500 hover:text-zinc-800'
                }
              >
                {item.label}
              </Link>
            ))}
            {isAuthenticated && (
              <button
                type="button"
                onClick={() => void logout()}
                className="text-zinc-500 hover:text-zinc-800"
              >
                Sign out
              </button>
            )}
          </nav>
        </div>
      </header>
      <div className="flex-1">{children}</div>
    </div>
  );
}
