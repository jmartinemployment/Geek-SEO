'use client';

import Link from 'next/link';
import { Search } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/components/auth/auth-provider';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';

export function AppHeaderSearch() {
  const router = useRouter();
  const [query, setQuery] = useState('');

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = query.trim();
    if (!value) {
      return;
    }

    const isUrl = /^https?:\/\//i.test(value) || value.includes('.');
    if (isUrl) {
      router.push('/app/dashboard');
      return;
    }

    router.push(`/app/keywords?q=${encodeURIComponent(value)}`);
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto hidden w-full max-w-[520px] md:block">
      <div className="relative">
        <Search className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-[var(--color-text-muted)]" />
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Enter your website or keyword"
          className="h-10 w-full rounded-[var(--radius-search)] border-[1.5px] border-[var(--color-border-strong)] bg-white pl-11 pr-4 text-sm text-[var(--color-text-primary)] outline-none transition-[border-color,box-shadow] placeholder:text-[var(--color-text-muted)] focus:border-[var(--color-accent)] focus:shadow-[0_0_0_3px_rgba(59,179,122,0.15)]"
        />
      </div>
    </form>
  );
}

export function AppHeaderActions() {
  const { isAuthenticated, logout } = useAuth();

  if (!isAuthenticated) {
    return (
      <div className="flex items-center gap-3">
        <Link
          href="/api/auth/start"
          className="inline-flex h-8 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-3 text-xs font-semibold text-white hover:bg-[var(--color-accent-hover)]"
        >
          Start free →
        </Link>
        <Link href="/api/auth/start" className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
          Sign in
        </Link>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-3">
      <Button variant="outline" size="sm">
        Upgrade
      </Button>
      <button type="button" onClick={() => void logout()} className="hidden text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] sm:inline">
        Sign out
      </button>
      <Avatar>
        <AvatarFallback>G</AvatarFallback>
      </Avatar>
    </div>
  );
}

export function AppHeader() {
  return (
    <header className="sticky top-0 z-30 flex h-14 items-center gap-4 border-b border-[var(--color-border)] bg-[var(--color-bg)] px-4 md:px-10">
      <Link href="/app/dashboard" className="shrink-0 text-lg font-bold tracking-[-0.02em] text-[var(--color-text-primary)]">
        Geek SEO
      </Link>
      <div className="flex flex-1 justify-center">
        <AppHeaderSearch />
      </div>
      <AppHeaderActions />
    </header>
  );
}
