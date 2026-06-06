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
  const siteStructure = steps.find((s) => s.slug === 'site_structure');
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
  const exclusionReasons = outputStringArray(merging, 'exclusionReasonsSample');
  const fusionVersion = merging.outputs.fusionVersion;
  const peerCandidates = outputNumber(merging, 'candidateCount');
  const signalSources = outputStringArray(merging, 'signalSourcesPresent');
  const entityResolved = schema?.outputs.entityResolved === true;
  const resolvedPlatforms = outputStringArray(schema, 'resolvedEntityPlatforms');
  const fromPage = outputNumber(merging, 'fromPageContent');
  const fromPageVertical = outputNumber(merging, 'fromPageVertical');
  const fromInternalLink = outputNumber(merging, 'fromInternalLink');
  const fromUrlPattern = outputNumber(merging, 'fromUrlPattern');
  const fromSameAs = outputNumber(merging, 'fromSameAs');
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

  if (entityResolved && resolvedPlatforms.length > 0) {
    parts.push(
      `Brand entity resolved via schema.org sameAs (${resolvedPlatforms.join(', ')}), boosting confidence on schema-declared topics.`,
    );
  }

  if (typeof fusionVersion === 'string' && fusionVersion.length > 0 && peerCandidates !== null) {
    const sources =
      signalSources.length > 0
        ? signalSources.join(', ')
        : 'schema, page, sitemap, nav, and headings';
    parts.push(
      `Topic fusion (${fusionVersion}) ranked ${peerCandidates} peer candidate(s) from ${sources} before applying the pillar cap.`,
    );
  }

  if (fromPageVertical !== null && fromPageVertical > 0) {
    parts.push(
      `${fromPageVertical} candidate(s) came from H2/H3 vertical sections on the homepage (e.g. industry or use-case silos like Accounting).`,
    );
  } else if (fromPage !== null && fromPage > 0) {
    parts.push(
      `${fromPage} candidate(s) also came from visible homepage copy (lists and section headings).`,
    );
  }

  if (fromInternalLink !== null && fromInternalLink > 0) {
    parts.push(
      `${fromInternalLink} candidate(s) were confirmed by internal link anchor text across crawled pages.`,
    );
  }

  if (fromUrlPattern !== null && fromUrlPattern > 0) {
    parts.push(
      `${fromUrlPattern} candidate(s) were inferred from URL path patterns (e.g. /services/… slugs).`,
    );
  }

  if (fromSameAs !== null && fromSameAs > 0) {
    parts.push(
      `${fromSameAs} schema topic(s) received a sameAs entity-resolution boost (brand linked to Wikipedia, LinkedIn, or similar).`,
    );
  }

  const gscConnected = merging.outputs.gscConnected === true;
  const gscSkipped = merging.outputs.gscSkipped === true;
  const fromGsc = outputNumber(merging, 'fromGsc');
  const gscMatchedPillars = outputNumber(merging, 'gscMatchedPillars');
  const gscSilentSlugs = outputStringArray(merging, 'gscSilentPillarSlugs');
  if (gscConnected === false) {
    parts.push(
      'Google Search Console is not connected — owner query data was not used. Connect GSC in project settings to confirm pillars with real search demand.',
    );
  } else if (gscSkipped) {
    const reason = merging.outputs.gscSkipReason;
    if (typeof reason === 'string' && reason.length > 0) {
      parts.push(`GSC owner overlay was skipped (${reason}).`);
    }
  } else if (fromGsc !== null && fromGsc > 0) {
    parts.push(
      `${gscMatchedPillars ?? fromGsc} pillar(s) were confirmed by GSC query clusters (${fromGsc} with gsc evidence).`,
    );
    if (gscSilentSlugs.length > 0) {
      parts.push(
        `${gscSilentSlugs.length} selected pillar(s) have no matching GSC queries yet — the site may not rank for those topics.`,
      );
    }
  }

  const keywords = steps.find((s) => s.slug === 'keywords');
  const serpValidation = steps.find((s) => s.slug === 'serp_validation');
  const pillarsEnriched = outputNumber(keywords, 'pillarsEnriched');
  const keywordsSkipped = keywords?.outputs.skipped === true;
  if (keywordsSkipped) {
    const reason = keywords?.outputs.skipReason;
    if (typeof reason === 'string' && reason.length > 0) {
      parts.push(`Keyword volume/difficulty enrichment was skipped (${reason}).`);
    }
  } else if (pillarsEnriched !== null && pillarsEnriched > 0) {
    parts.push(`${pillarsEnriched} pillar(s) were enriched with search volume and keyword difficulty.`);
  }

  const serpSkipped = serpValidation?.outputs.skipped === true;
  const pillarsDemoted = outputNumber(serpValidation, 'pillarsDemoted');
  const competitorCount = outputNumber(serpValidation, 'competitorCount');
  if (serpSkipped) {
    const reason = serpValidation?.outputs.skipReason;
    if (typeof reason === 'string' && reason.length > 0) {
      parts.push(`SERP validation was skipped (${reason}).`);
    }
  } else {
    if (pillarsDemoted !== null && pillarsDemoted > 0) {
      parts.push(
        `${pillarsDemoted} non-schema pillar(s) were demoted — no organic SERP footprint for those topics.`,
      );
    }
    if (competitorCount !== null && competitorCount > 0) {
      parts.push(`${competitorCount} competitor domain(s) were identified from SERP overlap.`);
    }
  }

  const pagesCrawled = outputNumber(siteStructure, 'pagesCrawled');
  const internalLinkCount = outputNumber(siteStructure, 'internalLinkCount');
  if (pagesCrawled !== null && pagesCrawled > 1 && internalLinkCount !== null && internalLinkCount > 0) {
    parts.push(
      `Site structure scan crawled ${pagesCrawled} pages and parsed ${internalLinkCount} internal link(s) for topic confirmation.`,
    );
  }

  const mergedAway = exclusionReasons.filter((line) => line.includes('Merged with similar topic'));
  if (mergedAway.length > 0) {
    parts.push(
      `Similar topics were merged to avoid duplicates (e.g. ${mergedAway.slice(0, 2).join('; ')}).`,
    );
  }

  if (excludedCount !== null && excludedCount > 0 && pillarCap !== null) {
    const heldBack =
      excludedSample.length > 0 ? excludedSample.join(', ') : `${excludedCount} topic(s)`;
    parts.push(
      `${excludedCount} topic(s) were not promoted to pillars because of the ${pillarCap}-pillar strategy cap: ${heldBack}.`,
    );
  }

  return parts.length > 0 ? parts.join(' ') : null;
}

/** Resolve GSC-silent pillar slugs to display names using the fusion snapshot. */
export function resolveGscSilentPillars(
  steps: NicheAnalysisStepLogEntry[],
  fusion?: { selectedPillars?: { slug: string; name: string }[] } | null,
): { slug: string; name: string }[] {
  const merging = steps.find((s) => s.slug === 'merging');
  const slugs = outputStringArray(merging, 'gscSilentPillarSlugs');
  if (slugs.length === 0) return [];

  const nameBySlug = new Map(
    (fusion?.selectedPillars ?? []).map((p) => [p.slug.toLowerCase(), p.name]),
  );

  return slugs.map((slug) => ({
    slug,
    name: nameBySlug.get(slug.toLowerCase()) ?? slug.replaceAll('-', ' '),
  }));
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
  fromPageVertical: 'Candidates from H2/H3 vertical sections',
  entityResolved: 'Brand entity resolved via sameAs',
  sameAsUrls: 'sameAs URLs in schema',
  resolvedEntityPlatforms: 'Entity authority platforms matched',
  fromSameAs: 'Schema topics with sameAs boost',
  pillarsEnriched: 'Pillars with keyword metrics',
  pillarsAttempted: 'Pillars checked for keyword metrics',
  pillarsValidated: 'Pillars checked in SERP',
  pillarsWithFootprint: 'Pillars with organic SERP results',
  pillarsDemoted: 'Pillars demoted (no SERP footprint)',
  competitorCount: 'Competitor domains from SERP',
  sampleCompetitors: 'Sample competitors',
  siteRanksCount: 'Pillars where your site ranks',
  pagesCrawled: 'Pages crawled for structure signals',
  internalLinkCount: 'Internal links parsed',
  urlPatternTopicCount: 'Topics from URL path patterns',
  sampleInternalAnchors: 'Internal link anchor text (sample)',
  sampleUrlPatterns: 'URL pattern topics (sample)',
  fromInternalLink: 'Candidates from internal links',
  fromUrlPattern: 'Candidates from URL patterns',
  verticalTopicCount: 'H2/H3 vertical section count',
  servicePhraseCount: 'Body phrase count',
  sampleVerticalTopics: 'H2/H3 vertical topics (sample)',
  sampleServicePhrases: 'Body phrases (sample)',
  fusionVersion: 'Fusion engine version',
  signalSourcesPresent: 'Signals present in fusion pool',
  exclusionReasonsSample: 'Excluded topics (sample reasons)',
  primarySource: 'Winning source after merge',
  mergedCount: 'Final pillar count',
  candidateCount: 'Total candidates before merge',
  pillarCap: 'Pillar strategy cap',
  excludedByCapCount: 'Topics held back by cap',
  excludedSampleNames: 'Held-back topic names (sample)',
  samplePillarNames: 'Final pillar names',
  pillarSources: 'Pillar → source',
  normalizedTopicalitySample: 'Normalized topicality (selected pillars)',
  entityThinCount: 'Entity-thin pillars (SERP gap)',
  linkGraphEdgeCount: 'Internal link graph edges',
  orphanPillarCount: 'Orphan pillars (no cross-links)',
  recommendedActionCount: 'Recommended actions (Phase E)',
  enabled: 'Step enabled',
  pillarsCovered: 'Pillars fully covered',
  pillarsPartial: 'Pillars partially covered',
  pillarsGap: 'Pillar gaps',
  subtopicsCovered: 'Subtopics matched to URLs',
  subtopicsTotal: 'Subtopics evaluated',
  samplePartialPillars: 'Partial pillars (sample)',
  fromGsc: 'Candidates with GSC query confirmation',
  gscConnected: 'GSC connected for this project',
  gscSkipped: 'GSC overlay skipped',
  gscSkipReason: 'GSC skip reason',
  gscQueryRowCount: 'GSC query rows analyzed',
  gscMatchedPillars: 'Pillars matched to GSC clusters',
  gscSilentPillarSlugs: 'Selected pillars with no GSC match',
  isLocalBusiness: 'Local business signals detected',
  locationPageCount: 'Location landing pages found',
  sampleLocationPages: 'Location pages (sample)',
  localGapCount: 'Service areas missing location pages',
  sampleLocalGaps: 'Missing location areas (sample)',
};
