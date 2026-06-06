import type { FusedSiteUnderstanding } from '@/lib/seo-api';

export function existingSchemaTopicNames(fusion: FusedSiteUnderstanding): string[] {
  const names = new Set<string>();
  for (const candidate of fusion.allCandidates) {
    if (!candidate.evidence.some((e) => e.source === 'schema')) continue;
    names.add(candidate.name);
  }
  return [...names].sort((a, b) => a.localeCompare(b));
}

export function buildKnowsAboutSyncSnippet(
  fusion: FusedSiteUnderstanding,
  topicName: string,
): { knowsAbout: string[]; snippet: string } {
  const knowsAbout = existingSchemaTopicNames(fusion);
  const exists = knowsAbout.some((n) => n.toLowerCase() === topicName.toLowerCase());
  if (!exists) knowsAbout.push(topicName);
  knowsAbout.sort((a, b) => a.localeCompare(b));

  const snippet = `"knowsAbout": ${JSON.stringify(knowsAbout, null, 2)}`;

  return { knowsAbout, snippet };
}

export function orphanLinkSuggestions(
  fusion: FusedSiteUnderstanding,
  orphanSlug: string,
): string[] {
  return fusion.selectedPillars
    .filter((p) => p.slug !== orphanSlug)
    .map((p) => p.name)
    .slice(0, 5);
}
