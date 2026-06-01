'use client';

import { Suspense } from 'react';
import { useAuth } from '@/lib/auth/auth-context';
import { useRouter } from 'next/navigation';
import RankTrackerWorkspace from '@/components/rank-tracker/rank-tracker-workspace';
import { Card } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Alert } from '@/components/ui/alert';

export default function RankTrackerPage() {
  const { user, accessToken, authReady } = useAuth();
  const router = useRouter();

  if (!authReady) {
    return (
      <div className="space-y-4 p-6">
        <Skeleton className="h-10 w-48" />
        <Card className="p-6">
          <Skeleton className="h-64 w-full" />
        </Card>
      </div>
    );
  }

  if (!user) {
    router.push('/auth/login');
    return null;
  }

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-3xl font-bold">Rank Tracker</h1>
        <p className="text-sm text-muted-foreground mt-2">Monitor your keyword rankings over time</p>
      </div>

      <Suspense
        fallback={
          <Card className="p-6">
            <Skeleton className="h-64 w-full" />
          </Card>
        }
      >
        <RankTrackerWorkspace accessToken={accessToken} />
      </Suspense>
    </div>
  );
}
