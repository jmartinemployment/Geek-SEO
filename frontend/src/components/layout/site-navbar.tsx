'use client';

import Image from 'next/image';
import Link from 'next/link';
import { AppHeaderActions } from '@/components/app/app-header';

export function SiteNavbar() {
  return (
    <header className="sticky top-0 z-[60] bg-white shadow-sm">
      <div className="container">
        <nav className="w-full" aria-label="Site">
          <div className="flex h-16 w-full items-center justify-between gap-4">
            <Link href="/" className="flex items-center">
              <Image
                src="/images/GeekAtYourSpot.svg"
                alt="Geek @ Your Spot logo"
                width={116}
                height={48}
                priority
              />
            </Link>
            <AppHeaderActions />
          </div>
        </nav>
      </div>
    </header>
  );
}
