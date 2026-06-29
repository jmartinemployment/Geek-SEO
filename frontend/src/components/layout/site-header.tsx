'use client';

import Link from 'next/link';
import { AuthStartLink } from '@/components/auth/auth-start-link';
import { Search } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { cn } from '@/lib/utils';

type SiteHeaderProps = {
  variant: 'marketing' | 'tools' | 'app';
  className?: string;
};

export function SiteHeader({ variant, className }: SiteHeaderProps) {
  const router = useRouter();
  const { isAuthenticated, logout } = useAuth();
  const [query, setQuery] = useState('');

  function handleSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = query.trim();
    if (!value) return;

    const isUrl = /^https?:\/\//i.test(value) || /\.\w{2,}/.test(value);
    if (variant === 'marketing' || variant === 'tools') {
      const params = new URLSearchParams({ q: value });
      router.push(`/?${params.toString()}`);
      return;
    }

    if (isUrl) {
      router.push('/dashboard');
      return;
    }
    router.push(`/keywords?q=${encodeURIComponent(value)}`);
  }

  const showCenterSearch = variant === 'app';

  return (
    <header
      className={cn(
        'sticky top-0 z-30 flex h-14 shrink-0 items-center gap-4 border-b border-[var(--color-border)] bg-[var(--color-bg)]',
        variant === 'app' ? 'pl-4 pr-10' : 'px-6 md:px-10',
        className,
      )}
    >
      <Link
        href={isAuthenticated ? '/dashboard' : '/'}
        className="shrink-0 text-lg font-bold tracking-[-0.02em] text-[var(--color-text-primary)]"
      >
        Geek SEO
      </Link>

      {showCenterSearch ? (
        <form onSubmit={handleSearch} className="mx-auto hidden w-full max-w-[520px] md:block">
          <div className="relative">
            <Search className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-[var(--color-text-muted)]" />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Enter your website or keyword"
              className="h-10 w-full rounded-[var(--radius-search)] border-[1.5px] border-[var(--color-border-strong)] bg-white pl-11 pr-4 text-sm outline-none transition-[border-color,box-shadow] placeholder:text-[var(--color-text-muted)] focus:border-[var(--color-accent)] focus:shadow-[0_0_0_3px_rgba(59,179,122,0.15)]"
            />
          </div>
        </form>
      ) : (
        <div className="flex-1" />
      )}

      <div className="flex shrink-0 items-center gap-3">
        {isAuthenticated ? (
          <>
            <span className="hidden rounded-md border border-[var(--color-border)] px-2.5 py-1 text-xs font-semibold text-[var(--color-text-secondary)] sm:inline">
              Upgrade
            </span>
            <button
              type="button"
              onClick={() => void logout()}
              className="hidden text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] sm:inline"
            >
              Sign out
            </button>
            <Avatar className="size-8">
              <AvatarFallback className="text-[11px]">G</AvatarFallback>
            </Avatar>
          </>
        ) : (
          <>
            <AuthStartLink className="inline-flex h-9 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-4 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]">
              Start free →
            </AuthStartLink>
            <AuthStartLink className="hidden text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] sm:inline">
              Sign in
            </AuthStartLink>
          </>
        )}
      </div>
    </header>
  );
}
