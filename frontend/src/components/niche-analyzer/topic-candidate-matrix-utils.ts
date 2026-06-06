import type { TopicCandidate, TopicEvidence } from '@/lib/seo-api';

export const SOURCE_LABELS: Record<string, string> = {
  schema: 'Schema',
  same_as: 'sameAs',
  sitemap: 'Sitemap',
  nav: 'Navigation',
  page: 'Body copy',
  page_vertical: 'H2/H3 vertical',
  heading: 'Heading',
  internal_link: 'Internal link',
  url_pattern: 'URL pattern',
  gsc: 'Search Console',
};

export const SOURCE_COLORS: Record<string, string> = {
  schema: 'bg-violet-100 text-violet-800',
  same_as: 'bg-indigo-100 text-indigo-800',
  sitemap: 'bg-sky-100 text-sky-800',
  nav: 'bg-cyan-100 text-cyan-800',
  page: 'bg-stone-100 text-stone-700',
  page_vertical: 'bg-amber-100 text-amber-800',
  heading: 'bg-orange-100 text-orange-800',
  internal_link: 'bg-teal-100 text-teal-800',
  url_pattern: 'bg-blue-100 text-blue-800',
  gsc: 'bg-emerald-100 text-emerald-800',
};

export function sourceLabel(source: string): string {
  return SOURCE_LABELS[source] ?? source.replaceAll('_', ' ');
}

export function uniqueSources(candidate: TopicCandidate): string[] {
  return [...new Set(candidate.evidence.map((e) => e.source))];
}

export function candidateHasSource(candidate: TopicCandidate, source: string): boolean {
  return candidate.evidence.some((e) => e.source === source);
}

export function formatExclusionReason(reason: string | undefined): string | null {
  if (!reason) return null;
  return reason
    .replace(/^Merged with similar topic:\s*/i, 'Merged: ')
    .replace(/^Below pillar cap \(\d+\):\s*/i, 'Cap: ')
    .replace(/^No corroboration:\s*/i, 'Single source: ');
}

export function evidenceTotalWeight(evidence: TopicEvidence[]): number {
  return evidence.reduce((sum, e) => sum + e.weight, 0);
}
