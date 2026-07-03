import type { ContentSpokeSummary, SeoContentDocument } from '@/lib/seo-api';

export const SPOKE_SHELL_MARKER = 'Spoke draft shell';

export function isSpokeShellHtml(html?: string | null): boolean {
  return html?.includes(SPOKE_SHELL_MARKER) ?? false;
}

export function isSpokeShellDocument(doc: Pick<SeoContentDocument, 'documentKind' | 'status' | 'contentHtml'>): boolean {
  if (doc.documentKind !== 'spoke') return false;
  if (doc.status === 'body_generated') return false;
  if (doc.status === 'shell_created') return true;
  return isSpokeShellHtml(doc.contentHtml);
}

export function isSpokeGenerated(spoke: Pick<ContentSpokeSummary, 'status'>): boolean {
  return spoke.status === 'body_generated';
}

export function isSpokeShellSummary(spoke: Pick<ContentSpokeSummary, 'status'>): boolean {
  return spoke.status === 'shell_created' || (!isSpokeGenerated(spoke) && spoke.status !== 'body_generated');
}
