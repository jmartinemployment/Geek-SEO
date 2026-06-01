import { Suspense } from 'react';
import { HomeHero } from '@/components/marketing/home-hero';
import { Skeleton } from '@/components/ui/skeleton';

function HomeHeroFallback() {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] flex-col items-center justify-center px-6 py-16">
      <Skeleton className="h-4 w-48" />
      <Skeleton className="mt-6 h-12 w-full max-w-3xl" />
      <Skeleton className="mt-4 h-12 w-full max-w-xl" />
    </div>
  );
}

export default function Home() {
  return (
    <Suspense fallback={<HomeHeroFallback />}>
      <HomeHero />
    </Suspense>
  );
}
