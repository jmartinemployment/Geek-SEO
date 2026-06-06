import type { NichePillarResult } from '@/lib/seo-api';

export type PillarUrlMatch = {
  pillarTopic: string;
  pillarSlug: string;
  coverageStatus: NichePillarResult['coverageStatus'];
  strategicPriority: NichePillarResult['strategicPriority'];
  matchKind: 'pillar_page' | 'subtopic_page' | 'path_slug';
};

export function normalizePagePath(url: string): string {
  const trimmed = url.trim();
  if (!trimmed) return '/';

  try {
    const withScheme = /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`;
    const pathname = new URL(withScheme).pathname.replace(/\/$/, '').toLowerCase();
    return pathname || '/';
  } catch {
    return trimmed.split('?')[0]?.replace(/\/$/, '').toLowerCase() ?? trimmed.toLowerCase();
  }
}

export function matchUrlToNichePillar(
  pageUrl: string,
  pillars: NichePillarResult[],
): PillarUrlMatch | null {
  if (pillars.length === 0) return null;

  const targetPath = normalizePagePath(pageUrl);

  for (const pillar of pillars) {
    for (const subtopic of pillar.subtopics) {
      if (!subtopic.existingUrl) continue;
      const subtopicPath = normalizePagePath(subtopic.existingUrl);
      if (targetPath === subtopicPath || targetPath.startsWith(`${subtopicPath}/`)) {
        return {
          pillarTopic: pillar.pillarTopic,
          pillarSlug: pillar.pillarSlug,
          coverageStatus: pillar.coverageStatus,
          strategicPriority: pillar.strategicPriority,
          matchKind: 'subtopic_page',
        };
      }
    }
  }

  for (const pillar of pillars) {
    if (pillar.pageUrl) {
      const pillarPath = normalizePagePath(pillar.pageUrl);
      if (targetPath === pillarPath || targetPath.startsWith(`${pillarPath}/`)) {
        return {
          pillarTopic: pillar.pillarTopic,
          pillarSlug: pillar.pillarSlug,
          coverageStatus: pillar.coverageStatus,
          strategicPriority: pillar.strategicPriority,
          matchKind: 'pillar_page',
        };
      }
    }
  }

  for (const pillar of pillars) {
    const slug = pillar.pillarSlug.trim().toLowerCase();
    if (!slug) continue;
    if (targetPath.includes(`/${slug}`) || targetPath.endsWith(`/${slug}`)) {
      return {
        pillarTopic: pillar.pillarTopic,
        pillarSlug: pillar.pillarSlug,
        coverageStatus: pillar.coverageStatus,
        strategicPriority: pillar.strategicPriority,
        matchKind: 'path_slug',
      };
    }
  }

  return null;
}

const priorityRank: Record<NichePillarResult['strategicPriority'], number> = {
  must_have: 0,
  high_value: 1,
  expansion: 2,
};

const coverageRank: Record<NichePillarResult['coverageStatus'], number> = {
  gap: 0,
  partial: 1,
  covered: 2,
};

/** Decaying URLs on gap/must-have pillars surface first in Content Guard. */
export function compareDecayingPagesByPillarPriority(
  aUrl: string,
  bUrl: string,
  pillars: NichePillarResult[],
): number {
  const aMatch = matchUrlToNichePillar(aUrl, pillars);
  const bMatch = matchUrlToNichePillar(bUrl, pillars);

  if (!aMatch && !bMatch) return 0;
  if (!aMatch) return 1;
  if (!bMatch) return -1;

  const coverageDiff =
    coverageRank[aMatch.coverageStatus] - coverageRank[bMatch.coverageStatus];
  if (coverageDiff !== 0) return coverageDiff;

  return priorityRank[aMatch.strategicPriority] - priorityRank[bMatch.strategicPriority];
}
