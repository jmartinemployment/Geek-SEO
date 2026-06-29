import { Suspense } from 'react';
import { DashboardCopilotPanel } from '@/components/dashboard/dashboard-copilot-panel';
import { DashboardFeatureCards } from '@/components/dashboard/dashboard-feature-cards';
import { DashboardRecentDocuments } from '@/components/dashboard/dashboard-recent-documents';
import { DashboardSitesSection } from '@/components/dashboard/dashboard-sites-section';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { getServerAccessToken } from '@/lib/auth/server-session';
import { loadDashboardData, type DashboardData } from '@/lib/dashboard-data';

function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-8">
      <div className="flex gap-3 overflow-hidden">
        {Array.from({ length: 6 }).map((_, index) => (
          <Skeleton key={index} className="h-36 w-40 shrink-0 rounded-[var(--radius-card)]" />
        ))}
      </div>
      <Skeleton className="h-48 w-full rounded-[var(--radius-card)]" />
      <Skeleton className="h-56 w-full rounded-[var(--radius-card)]" />
      <Skeleton className="h-48 w-full rounded-[var(--radius-card)]" />
    </div>
  );
}

function DashboardView({ data }: { data: DashboardData }) {
  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-[-0.02em] text-[var(--color-text-primary)]">
          Dashboard
        </h1>
        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
          Overview of your sites, content scores, and next actions.
        </p>
      </header>

      <DashboardFeatureCards />
      <DashboardCopilotPanel suggestions={data.copilotSuggestions} />
      <DashboardSitesSection projects={data.projects} />
      <DashboardRecentDocuments documents={data.recentDocuments} />
    </div>
  );
}

function DashboardError({ message }: { message: string }) {
  return (
    <Alert>
      <AlertTitle>Dashboard unavailable</AlertTitle>
      <AlertDescription>{message}</AlertDescription>
    </Alert>
  );
}

async function DashboardContent() {
  const accessToken = await getServerAccessToken();
  let data: DashboardData;

  try {
    data = await loadDashboardData(accessToken);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load dashboard';
    return <DashboardError message={message} />;
  }

  return <DashboardView data={data} />;
}

export default function DashboardPage() {
  return (
    <Suspense fallback={<DashboardSkeleton />}>
      <DashboardContent />
    </Suspense>
  );
}
