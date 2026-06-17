import Link from 'next/link';
import { FileText } from 'lucide-react';
import type { RecentDocument } from '@/lib/dashboard-data';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { Separator } from '@/components/ui/separator';

export function DashboardRecentDocuments({ documents }: { documents: RecentDocument[] }) {
  return (
    <section aria-label="Recent documents">
      <Card>
        <CardHeader className="flex-row items-center justify-between space-y-0">
          <CardTitle>Recent documents</CardTitle>
          <Link href="/app/projects" className="text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
            View all →
          </Link>
        </CardHeader>
        <CardContent className="pt-0">
          {documents.length === 0 ? (
            <Empty className="border-none bg-transparent py-8">
              <EmptyHeader>
                <EmptyMedia>
                  <FileText className="size-5" />
                </EmptyMedia>
                <EmptyTitle>No documents yet</EmptyTitle>
                <EmptyDescription>Create content from the optimizer or the content writing workspace.</EmptyDescription>
              </EmptyHeader>
              <EmptyContent>
                <Link
                  href="/content-writing"
                  className="inline-flex h-9 items-center rounded-[var(--radius-button)] bg-[var(--color-accent)] px-5 text-sm font-semibold text-white hover:bg-[var(--color-accent-hover)]"
                >
                  Start content writing
                </Link>
              </EmptyContent>
            </Empty>
          ) : (
            <ul>
              {documents.map((document, index) => (
                <li key={document.id}>
                  {index > 0 ? <Separator className="my-0" /> : null}
                  <Link
                    href={`/content-writing?documentId=${document.id}`}
                    className="flex items-center justify-between gap-4 py-3 transition-colors hover:bg-[var(--color-surface-muted)]"
                  >
                    <div className="min-w-0">
                      <p className="truncate font-medium text-[var(--color-text-primary)]">
                        {document.title || 'Untitled'}
                      </p>
                      <p className="truncate text-xs text-[var(--color-text-secondary)]">
                        {document.projectName} · {document.targetKeyword || 'No keyword'}
                      </p>
                    </div>
                    <Badge variant="score">
                      {document.seoScore > 0 ? `${document.seoScore}` : '—'}
                    </Badge>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </section>
  );
}
