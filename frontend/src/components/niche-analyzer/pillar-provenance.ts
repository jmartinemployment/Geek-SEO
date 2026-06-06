import type { NicheAnalysisStepLogEntry } from '@/lib/seo-api';

function outputNumber(step: NicheAnalysisStepLogEntry | undefined, key: string): number | null {
  const value = step?.outputs[key];
  return typeof value === 'number' ? value : null;
}

function outputStringArray(step: NicheAnalysisStepLogEntry | undefined, key: string): string[] {
  const value = step?.outputs[key];
  if (!Array.isArray(value)) return [];
  return value.map((item) => String(item));
}

/** Human-readable explanation of why the pillar table looks the way it does. */
export function buildPillarProvenanceSummary(
  steps: NicheAnalysisStepLogEntry[],
  pillarCount: number,
): string | null {
  const schema = steps.find((s) => s.slug === 'schema');
  const siteUrls = steps.find((s) => s.slug === 'site_urls');
  const merging = steps.find((s) => s.slug === 'merging');

  if (!merging || pillarCount === 0) return null;

  const mergedCount = outputNumber(merging, 'mergedCount') ?? pillarCount;
  const fromSchema = outputNumber(merging, 'fromSchema');
  const fromSitemap = outputNumber(merging, 'fromSitemap');
  const fromNav = outputNumber(merging, 'fromNav');
  const fromHeadings = outputNumber(merging, 'fromHeadings');
  const primarySource = merging.outputs.primarySource;
  const topics =
    outputStringArray(schema, 'knowsAboutTopics').length > 0
      ? outputStringArray(schema, 'knowsAboutTopics')
      : outputStringArray(schema, 'allSchemaTopics').length > 0
        ? outputStringArray(schema, 'allSchemaTopics')
        : outputStringArray(schema, 'serviceNames');
  const offerTopics = outputStringArray(schema, 'offerCatalogTopics');
  const excludedCount = outputNumber(merging, 'excludedByCapCount');
  const pillarCap = outputNumber(merging, 'pillarCap');
  const excludedSample = outputStringArray(merging, 'excludedSampleNames');
  const totalUrls = outputNumber(siteUrls, 'totalUrls');
  const sitemapPillars = outputNumber(siteUrls, 'pillarCount');

  const parts: string[] = [];

  if (primarySource === 'schema' && fromSchema === mergedCount && topics.length > 0 && offerTopics.length === 0) {
    parts.push(
      `All ${mergedCount} pillars come from schema.org JSON-LD on your homepage — the \`knowsAbout\` list (${topics.join(', ')}).`,
    );
  } else if (topics.length > 0 || offerTopics.length > 0) {
    const schemaParts: string[] = [];
    if (topics.length > 0) {
      schemaParts.push(`knowsAbout: ${topics.join(', ')}`);
    }
    if (offerTopics.length > 0) {
      schemaParts.push(`offer catalog / serviceType: ${offerTopics.join(', ')}`);
    }
    parts.push(
      `Schema.org contributed ${fromSchema ?? mergedCount} pillar candidate(s) — ${schemaParts.join('; ')}.`,
    );
  } else if (typeof primarySource === 'string' && primarySource.startsWith('mixed')) {
    parts.push(
      `Pillars were merged from multiple signals (${primarySource}). Schema contributed ${fromSchema ?? 0}, sitemap ${fromSitemap ?? 0}, navigation ${fromNav ?? 0}, headings ${fromHeadings ?? 0}.`,
    );
  } else if (fromSchema && fromSchema > 0) {
    parts.push(`${fromSchema} of ${mergedCount} pillars came from schema.org on the homepage.`);
  }

  if (totalUrls !== null && sitemapPillars !== null) {
    if (totalUrls <= 1 && sitemapPillars === 0) {
      parts.push(
        'Your sitemap lists only the homepage, so no URL path silos (e.g. /services/…) were inferred — that is expected for single-page sites.',
      );
    } else if (sitemapPillars > 0) {
      parts.push(`${sitemapPillars} pillar(s) were grouped from ${totalUrls} sitemap URL(s).`);
    }
  }

  if (fromNav === 0 && fromHeadings === 0 && parts.length > 0) {
    parts.push('Navigation and homepage headings did not add extra pillar candidates this run.');
  }

  if (excludedCount !== null && excludedCount > 0 && pillarCap !== null) {
    const heldBack =
      excludedSample.length > 0 ? excludedSample.join(', ') : `${excludedCount} topic(s)`;
    parts.push(
      `${excludedCount} schema topic(s) were not promoted to pillars because of the ${pillarCap}-pillar strategy cap: ${heldBack}.`,
    );
  }

  return parts.length > 0 ? parts.join(' ') : null;
}

export const OUTPUT_LABELS: Record<string, string> = {
  knowsAboutTopics: 'knowsAbout topics',
  offerCatalogTopics: 'Offer catalog / serviceType topics',
  allSchemaTopics: 'All schema topics (merged list)',
  serviceNames: 'Schema topics (legacy field)',
  areaServed: 'areaServed (geo tags, not pillars when ≥3 topics)',
  description: 'Business description',
  brandName: 'Brand name',
  becomesPillars: 'Topics become pillar candidates',
  totalUrls: 'Sitemap URLs scanned',
  pillarCount: 'Pillars from URL paths',
  sampleUrls: 'Sample URLs',
  fromSchema: 'Candidates from schema',
  fromSitemap: 'Candidates from sitemap paths',
  fromNav: 'Candidates from navigation',
  fromHeadings: 'Candidates from headings',
  fromPageContent: 'Candidates from page body',
  fusionVersion: 'Fusion engine version',
  exclusionReasonsSample: 'Excluded topics (sample reasons)',
  primarySource: 'Winning source after merge',
  mergedCount: 'Final pillar count',
  candidateCount: 'Total candidates before merge',
  pillarCap: 'Pillar strategy cap',
  excludedByCapCount: 'Topics held back by cap',
  excludedSampleNames: 'Held-back topic names (sample)',
  samplePillarNames: 'Final pillar names',
  pillarSources: 'Pillar → source',
};
