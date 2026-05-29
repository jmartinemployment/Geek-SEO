import Link from 'next/link';
import { ExternalLink, Globe, Plus } from 'lucide-react';
import { SITE_METRIC_COLUMNS } from '@/components/dashboard/dashboard.constants';
import type { ProjectWithDocuments } from '@/lib/dashboard-data';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { Separator } from '@/components/ui/separator';

function formatDomain(url: string) {
  try {
    return new URL(url.startsWith('http') ? url : `https://${url}`).hostname.replace(/^www\./, '');
  } catch {
    return url;
  }
}

function SiteMetricCell({ label }: { label: string }) {
  return (
    <div className="min-w-24 flex-1">
      <p className="text-[11px] font-medium uppercase tracking-[0.08em] text-[var(--color-text-secondary)]">
        {label}
      </p>
      <p className="mt-1 text-[22px] font-bold text-[var(--color-metric-blue)]">—</p>
    </div>
  );
}

function SiteRow({ project }: { project: ProjectWithDocuments }) {
  const domain = formatDomain(project.url);

  return (
    <Card className="shadow-none hover:shadow-[var(--shadow-card-hover)]">
      <CardContent className="flex flex-col gap-4 py-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-3">
            <div className="flex size-9 items-center justify-center rounded-full bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]">
              <Globe className="size-4" />
            </div>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <p className="truncate text-sm font-semibold text-[var(--color-text-primary)]">@{domain}</p>
                <Link
                  href={project.url.startsWith('http') ? project.url : `https://${project.url}`}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
                >
                  {domain}
                  <ExternalLink className="size-3.5" />
                </Link>
              </div>
              <p className="text-xs text-[var(--color-text-secondary)]">
                {project.documents.length} documents
                {project.gscConnected ? ' · GSC connected' : ''}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="accent">{project.name}</Badge>
            <Link
              href={`/app/projects/${project.id}`}
              className="inline-flex h-8 items-center rounded-[var(--radius-button)] border border-[var(--color-border-strong)] bg-white px-3 text-xs font-semibold text-[var(--color-text-primary)] hover:bg-[var(--color-surface-muted)]"
            >
              Open content
            </Link>
          </div>
        </div>
        <Separator />
        <div className="flex gap-4 overflow-x-auto pb-1">
          {SITE_METRIC_COLUMNS.map((label) => (
            <SiteMetricCell key={label} label={label} />
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

export function DashboardSitesSection({ projects }: { projects: ProjectWithDocuments[] }) {
  return (
    <section aria-label="Your sites">
      <Card>
        <CardHeader className="flex-row items-center justify-between space-y-0">
          <CardTitle>Your sites</CardTitle>
          <Button variant="outline" size="sm">
            <Plus />
            Add site
          </Button>
        </CardHeader>
        <CardContent className="flex flex-col gap-3 pt-0">
          {projects.length === 0 ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia>
                  <Globe className="size-5" />
                </EmptyMedia>
                <EmptyTitle>No sites yet</EmptyTitle>
                <EmptyDescription>
                  Add your first domain to start scoring content and planning your topical map.
                </EmptyDescription>
              </EmptyHeader>
              <EmptyContent>
                <Link
                  href="/app/projects"
                  className="inline-flex h-9 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-5 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]"
                >
                  Add your first site
                </Link>
              </EmptyContent>
            </Empty>
          ) : (
            projects.map((project) => <SiteRow key={project.id} project={project} />)
          )}
        </CardContent>
      </Card>
    </section>
  );
}
