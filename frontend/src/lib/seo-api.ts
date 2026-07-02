import { parseSeoApiErrorResponse } from '@/lib/seo-api-errors';
import { buildApiHeaders } from '@/lib/auth/api-headers';
import type { ScoreUpdate } from '@/hooks/useContentScoring';

export { SeoApiError, formatSeoApiErrorMessage } from '@/lib/seo-api-errors';
export type { SeoGateErrorBody } from '@/lib/seo-api-errors';

/** GeekSeoBackend — sole SEO API for this app (see plan-documents/ARCHITECTURE.md). */
const SEO_API_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? 'http://localhost:5051';

const API_URL = SEO_API_URL;

async function seoJson<T>(res: Response): Promise<T> {
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<T>;
}

export function apiHeaders(accessToken?: string | null): HeadersInit {
  return buildApiHeaders(accessToken, process.env.NEXT_PUBLIC_DEV_USER_ID);
}

function hasAuthContext(accessToken?: string | null): boolean {
  return Boolean(accessToken) || Boolean(process.env.NEXT_PUBLIC_DEV_USER_ID);
}

export type SeoProject = {
  id: string;
  name: string;
  url: string;
  defaultLocation: string;
  gscConnected: boolean;
  businessAddress?: string | null;
  serviceRadiusMiles?: number;
  localSeoEnabled?: boolean;
};

export type SiteWritingFocus = {
  siteName: string;
  siteUrl: string;
  primaryNiche?: string;
  nicheDescription?: string;
  nicheTags?: string[];
  businessSummary?: string;
  matchedPillarTopic?: string | null;
  matchedPillarIntent?: string | null;
  matchedPillarAngle?: string | null;
  geoAnchorNodes?: string[];
  serviceAreaDescription?: string;
  gapTopics?: string[];
  competitorDomains?: string[];
  authorityPageUrls?: string[];
  nicheProfileId?: string | null;
  nicheProfileUpdatedAt?: string | null;
  capturedAt?: string;
  writingInstructions?: string;
};

export function parseSiteWritingFocus(json?: string | null): SiteWritingFocus | null {
  if (!json?.trim()) return null;
  try {
    return JSON.parse(json) as SiteWritingFocus;
  } catch {
    return null;
  }
}

export type ContentWriterHeading = {
  level: number;
  text: string;
  sequence: number;
};

export type ContentWriterExportBenchmarks = {
  medianH2CountTop5: number;
  medianWordCountTop5: number;
  competitorDomainCount: number;
  competitorPageCount: number;
};

export type ContentWriterCitationCandidate = {
  url: string;
  title?: string | null;
  domain?: string | null;
  source: string;
};

export type ContentWriterOperatorQuery = {
  bucket: string;
  label: string;
  query: string;
  searchEngine?: string;
};

export type ContentWriterCompetitorExport = {
  domain: string;
  url: string;
  seedRankAbsolute?: number;
  headings?: ContentWriterHeading[];
  schemaTypes?: string[];
  hasFaqSchema?: boolean;
};

export type ContentWriterSerpItem = {
  position: number;
  type: string;
  title?: string | null;
  url?: string | null;
  domain?: string | null;
  snippet?: string | null;
  relatedQuestions?: string[];
};

export type ContentWriterManualResearchLane = {
  lane: string;
  label: string;
  organicCount: number;
  paaCount?: number;
  organicResults?: ContentWriterSerpItem[];
  paaQuestions?: string[];
};

export type ContentWriterSerpExport = {
  runId: string;
  projectId: string;
  keyword: string;
  targetSiteUrl: string;
  status: string;
  serpSeResultsCount: number;
  researchMode?: string;
  topicSlug?: string | null;
  manualResearchLanes?: ContentWriterManualResearchLane[];
  serp: ContentWriterSerpItem[];
  gapTopics?: string[];
  writingInstructions?: string;
  writingRecommendations?: string[];
  sourceHeadings?: ContentWriterHeading[];
  competitors?: ContentWriterCompetitorExport[];
  citationCandidates?: ContentWriterCitationCandidate[];
  operatorQueries?: ContentWriterOperatorQuery[];
  supplementalPaaQuestions?: string[];
  featuredSnippetCandidate?: string | null;
  newsHooks?: string[];
  localAngleHint?: string | null;
  benchmarks?: ContentWriterExportBenchmarks;
};

export function parseContentWriterKeywordBundle(
  json?: string | null,
): ContentWriterSerpExport | null {
  if (!json?.trim()) return null;
  try {
    return JSON.parse(json) as ContentWriterSerpExport;
  } catch {
    return null;
  }
}

export type SeoContentDocument = {
  id: string;
  projectId: string;
  userId: string;
  urlResearchId?: string | null;
  analysisRunId?: string | null;
  siteProfileId?: string | null;
  serpKeyword?: string | null;
  siteFocusJson?: string | null;
  siteFocusCapturedAt?: string | null;
  keywordBundleJson?: string | null;
  keywordBundleCapturedAt?: string | null;
  title: string;
  contentHtml: string;
  featuredImageUrl?: string | null;
  targetKeyword: string;
  targetLocation?: string;
  seoScore: number;
  wordCount: number;
  scoreComponentsJson: string;
  status: string;
  blogSpokeJson?: string | null;
  parentDocumentId?: string | null;
  documentKind?: string | null;
  publishSlug?: string | null;
};

export type ContentBlogSpoke = {
  slug: string;
  primaryKeyword: string;
  spokeType: string;
  title: string;
  contentHtml: string;
  excerpt?: string | null;
  metaDescription?: string | null;
};

export function parseBlogSpokeJson(
  blogSpokeJson?: string | null,
): ContentBlogSpoke | null {
  if (!blogSpokeJson?.trim()) return null;
  try {
    return JSON.parse(blogSpokeJson) as ContentBlogSpoke;
  } catch {
    return null;
  }
}

export type ContentBlogSpokeGetResult = {
  spoke: ContentBlogSpoke;
  clusterDocumentId?: string | null;
};

function normalizeBlogSpoke(body: Record<string, unknown>): ContentBlogSpoke {
  const spoke = (body.spoke ?? body) as Record<string, unknown>;
  return {
    slug: String(spoke.slug ?? spoke.Slug ?? ''),
    primaryKeyword: String(spoke.primaryKeyword ?? spoke.PrimaryKeyword ?? ''),
    spokeType: String(spoke.spokeType ?? spoke.SpokeType ?? 'comparison'),
    title: String(spoke.title ?? spoke.Title ?? ''),
    contentHtml: String(spoke.contentHtml ?? spoke.ContentHtml ?? ''),
    excerpt: (spoke.excerpt ?? spoke.Excerpt ?? null) as string | null,
    metaDescription: (spoke.metaDescription ?? spoke.MetaDescription ?? null) as string | null,
  };
}

export async function getBlogSpoke(
  documentId: string,
  accessToken?: string | null,
): Promise<ContentBlogSpokeGetResult | null> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/blog-spoke`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 404) return null;
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  return {
    spoke: normalizeBlogSpoke(body),
    clusterDocumentId: (body.clusterDocumentId ?? body.ClusterDocumentId ?? null) as string | null,
  };
}

export async function generateBlogSpoke(
  documentId: string,
  body: { spokeType?: string; spokeKeyword?: string },
  accessToken?: string | null,
): Promise<ContentBlogSpoke> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/blog-spoke/generate`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<ContentBlogSpoke>;
}

export async function addBlogSpokeFaqs(
  documentId: string,
  accessToken?: string | null,
): Promise<ContentBlogSpoke> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/blog-spoke/add-faqs`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<ContentBlogSpoke>;
}

export type ContentLinkFaqItem = {
  question: string;
  targetDocumentId?: string | null;
  targetPath?: string | null;
  anchorText?: string | null;
  source?: string | null;
  linkStatus?: string | null;
};

export type ContentLinkBodySlot = {
  insertAfterH2Hint?: string | null;
  targetDocumentId?: string | null;
  targetPath?: string | null;
  anchorText?: string | null;
  priority?: number;
  minStatusRequired?: string | null;
};

export type ContentLinkPlan = {
  faqItems: ContentLinkFaqItem[];
  bodyLinks: ContentLinkBodySlot[];
};

export type ContentClusterCandidate = {
  phrase: string;
  sourceType: string;
  score: number;
  rejectReason?: string | null;
  suggestedQuestion?: string | null;
  suggestedSlug?: string | null;
  plannedSpokeId?: string | null;
};

export type ContentClusterFilteredCandidate = {
  phrase: string;
  sourceType: string;
  rejectReason: string;
};

export type ContentClusterPlanResult = {
  spokeCandidates: ContentClusterCandidate[];
  faqItems: ContentLinkFaqItem[];
  bodyLinks: ContentLinkBodySlot[];
  filteredOut: ContentClusterFilteredCandidate[];
};

function normalizeLinkPlan(body: Record<string, unknown>): ContentLinkPlan {
  const faqRaw = body.faqItems ?? body.FaqItems;
  const bodyRaw = body.bodyLinks ?? body.BodyLinks;
  return {
    faqItems: Array.isArray(faqRaw) ? (faqRaw as ContentLinkFaqItem[]) : [],
    bodyLinks: Array.isArray(bodyRaw) ? (bodyRaw as ContentLinkPlan['bodyLinks']) : [],
  };
}

function normalizeClusterPlanResult(body: Record<string, unknown>): ContentClusterPlanResult {
  const spokeRaw = body.spokeCandidates ?? body.SpokeCandidates;
  const faqRaw = body.faqItems ?? body.FaqItems;
  const bodyRaw = body.bodyLinks ?? body.BodyLinks;
  const filteredRaw = body.filteredOut ?? body.FilteredOut;
  return {
    spokeCandidates: Array.isArray(spokeRaw) ? (spokeRaw as ContentClusterCandidate[]) : [],
    faqItems: Array.isArray(faqRaw) ? (faqRaw as ContentLinkFaqItem[]) : [],
    bodyLinks: Array.isArray(bodyRaw) ? (bodyRaw as ContentLinkBodySlot[]) : [],
    filteredOut: Array.isArray(filteredRaw) ? (filteredRaw as ContentClusterFilteredCandidate[]) : [],
  };
}

export async function getClusterPlan(
  documentId: string,
  accessToken?: string | null,
): Promise<ContentLinkPlan> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/cluster/plan`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  return normalizeLinkPlan(body);
}

export async function buildClusterPlan(
  documentId: string,
  accessToken?: string | null,
): Promise<ContentClusterPlanResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/cluster/plan`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  return normalizeClusterPlanResult(body);
}

export async function saveClusterPlan(
  documentId: string,
  plan: ContentLinkPlan,
  accessToken?: string | null,
): Promise<ContentLinkPlan> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/cluster/plan`, {
    method: 'PUT',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(plan),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  return normalizeLinkPlan(body);
}

export type GenerateLinkedFaqsResponse = {
  contentHtml: string;
  linkedCount: number;
  plainTextOnlyCount: number;
  skipped: string[];
};

export async function generateLinkedFaqs(
  documentId: string,
  accessToken?: string | null,
): Promise<GenerateLinkedFaqsResponse> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/faqs/generate-linked`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  return {
    contentHtml: String(body.contentHtml ?? body.ContentHtml ?? ''),
    linkedCount: Number(body.linkedCount ?? body.LinkedCount ?? 0),
    plainTextOnlyCount: Number(body.plainTextOnlyCount ?? body.PlainTextOnlyCount ?? 0),
    skipped: Array.isArray(body.skipped ?? body.Skipped)
      ? ((body.skipped ?? body.Skipped) as string[])
      : [],
  };
}

export type ApplyBodyLinksResponse = {
  contentHtml: string;
  appliedCount: number;
  pendingCount: number;
  changed: boolean;
};

export async function applyBodyLinks(
  documentId: string,
  accessToken?: string | null,
): Promise<ApplyBodyLinksResponse> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/body-links/apply`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ instructions: [] }),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  return {
    contentHtml: String(body.contentHtml ?? body.ContentHtml ?? ''),
    appliedCount: Number(body.appliedCount ?? body.AppliedCount ?? 0),
    pendingCount: Number(body.pendingCount ?? body.PendingCount ?? 0),
    changed: Boolean(body.changed ?? body.Changed ?? false),
  };
}

export type ContentSpokeSummary = {
  id: string;
  title: string;
  targetKeyword: string;
  publishSlug?: string | null;
  spokeSourcePhrase?: string | null;
  spokeSourceType?: string | null;
  status: string;
  wordCount: number;
  contentPreview?: string | null;
  updatedAt: string;
};

export type GenerateAllContentSpokesResult = {
  generatedCount: number;
  skippedCount: number;
  failures: string[];
  spokes: ContentSpokeSummary[];
};

function normalizeSpokeSummary(body: Record<string, unknown>): ContentSpokeSummary {
  return {
    id: String(body.id ?? body.Id ?? ''),
    title: String(body.title ?? body.Title ?? ''),
    targetKeyword: String(body.targetKeyword ?? body.TargetKeyword ?? ''),
    publishSlug: (body.publishSlug ?? body.PublishSlug ?? null) as string | null,
    spokeSourcePhrase: (body.spokeSourcePhrase ?? body.SpokeSourcePhrase ?? null) as string | null,
    spokeSourceType: (body.spokeSourceType ?? body.SpokeSourceType ?? null) as string | null,
    status: String(body.status ?? body.Status ?? ''),
    wordCount: Number(body.wordCount ?? body.WordCount ?? 0),
    contentPreview: (body.contentPreview ?? body.ContentPreview ?? null) as string | null,
    updatedAt: String(body.updatedAt ?? body.UpdatedAt ?? ''),
  };
}

export async function listContentSpokes(
  pillarDocumentId: string,
  accessToken?: string | null,
): Promise<ContentSpokeSummary[]> {
  const res = await fetch(`${API_URL}/api/seo/content/${pillarDocumentId}/spokes`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as unknown;
  if (!Array.isArray(body)) return [];
  return body.map((item) => normalizeSpokeSummary(item as Record<string, unknown>));
}

export async function createContentSpoke(
  pillarDocumentId: string,
  body: {
    phrase: string;
    sourceType?: string;
    title?: string;
    targetKeyword?: string;
    publishSlug?: string;
  },
  accessToken?: string | null,
): Promise<ContentSpokeSummary> {
  const res = await fetch(`${API_URL}/api/seo/content/${pillarDocumentId}/spokes`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const payload = (await res.json()) as Record<string, unknown>;
  return normalizeSpokeSummary(payload);
}

export async function generateContentSpoke(
  pillarDocumentId: string,
  spokeDocumentId: string,
  accessToken?: string | null,
  body?: { spokeType?: string },
): Promise<ContentSpokeSummary> {
  const res = await fetch(
    `${API_URL}/api/seo/content/${pillarDocumentId}/spokes/${spokeDocumentId}/generate`,
    {
      method: 'POST',
      headers: apiHeaders(accessToken),
      body: JSON.stringify(body ?? {}),
    },
  );
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const payload = (await res.json()) as Record<string, unknown>;
  return normalizeSpokeSummary(payload);
}

export async function generateAllContentSpokes(
  pillarDocumentId: string,
  accessToken?: string | null,
): Promise<GenerateAllContentSpokesResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${pillarDocumentId}/spokes/generate-all`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({}),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const body = (await res.json()) as Record<string, unknown>;
  const failuresRaw = body.failures ?? body.Failures;
  const spokesRaw = body.spokes ?? body.Spokes;
  return {
    generatedCount: Number(body.generatedCount ?? body.GeneratedCount ?? 0),
    skippedCount: Number(body.skippedCount ?? body.SkippedCount ?? 0),
    failures: Array.isArray(failuresRaw) ? failuresRaw.map(String) : [],
    spokes: Array.isArray(spokesRaw)
      ? spokesRaw.map((item) => normalizeSpokeSummary(item as Record<string, unknown>))
      : [],
  };
}

export type ContentSocialPostResult = {
  facebookPost: string;
  linkedInPost: string;
  keyword: string;
  blogPostSlug?: string | null;
};

export async function generateSocialPosts(
  documentId: string,
  body: { blogPostTitle?: string; blogPostSlug?: string },
  accessToken?: string | null,
): Promise<ContentSocialPostResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/social/generate`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const payload = (await res.json()) as Record<string, unknown>;
  return {
    facebookPost: String(payload.facebookPost ?? payload.FacebookPost ?? ''),
    linkedInPost: String(payload.linkedInPost ?? payload.LinkedInPost ?? ''),
    keyword: String(payload.keyword ?? payload.Keyword ?? ''),
    blogPostSlug: (payload.blogPostSlug ?? payload.BlogPostSlug ?? null) as string | null,
  };
}

export async function listProjects(accessToken?: string | null): Promise<SeoProject[]> {
  if (!hasAuthContext(accessToken)) return [];
  const res = await fetch(`${API_URL}/api/seo/projects`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<SeoProject[]>(res);
}

export async function getProject(
  projectId: string,
  accessToken?: string | null,
): Promise<SeoProject> {
  const res = await fetch(`${API_URL}/api/seo/projects/${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<SeoProject>(res);
}

export type UpdateProjectBody = {
  name?: string;
  url?: string;
  defaultLocation?: string;
  businessAddress?: string | null;
  serviceRadiusMiles?: number;
  localSeoEnabled?: boolean;
};

export async function updateProject(
  projectId: string,
  body: UpdateProjectBody,
  accessToken?: string | null,
): Promise<SeoProject> {
  const res = await fetch(`${API_URL}/api/seo/projects/${projectId}`, {
    method: 'PUT',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  return seoJson<SeoProject>(res);
}

export type BackgroundJobStatus = {
  jobId: string;
  jobType: string;
  status: string;
  progressPercent: number;
  resultId?: string;
  errorMessage?: string;
  keyword?: string;
  keywordIndex?: number;
  keywordTotal?: number;
  documentId?: string;
};

export type CompetitorPageInsight = {
  url: string;
  domain?: string;
  position: number;
  wordCount: number;
  metaTitle?: string;
  crawledAt?: string;
};

export type CompetitorInsights = {
  keyword: string;
  location: string;
  pages: CompetitorPageInsight[];
  benchmarkQuality: string;
  crawlStatus?: string;
};

export async function createProject(
  body: { name: string; url: string; defaultLocation: string },
  accessToken?: string | null,
): Promise<SeoProject> {
  const res = await fetch(`${API_URL}/api/seo/projects`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoProject>;
}

export async function listContent(
  projectId: string,
  accessToken?: string | null,
): Promise<SeoContentDocument[]> {
  const res = await fetch(`${API_URL}/api/seo/content?projectId=${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoContentDocument[]>;
}

export async function createContent(
  body: {
    projectId?: string;
    title?: string;
    targetKeyword?: string;
    targetLocation?: string;
    analysisRunId?: string;
    siteProfileId?: string;
  },
  accessToken?: string | null,
): Promise<SeoContentDocument> {
  const res = await fetch(`${API_URL}/api/seo/content`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoContentDocument>;
}

export async function getContent(
  id: string,
  accessToken?: string | null,
): Promise<SeoContentDocument> {
  const res = await fetch(`${API_URL}/api/seo/content/${id}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoContentDocument>;
}

export async function getResearchPack(
  documentId: string,
  accessToken?: string | null,
): Promise<ContentWriterSerpExport | null> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/research-pack`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) return null;
  return res.json() as Promise<ContentWriterSerpExport>;
}

export async function getRenderedContentHtml(
  id: string,
  accessToken?: string | null,
): Promise<RenderedArticleResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${id}/rendered-html`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<RenderedArticleResult>;
}

/** Article body + JSON-LD script tags for CMS paste (Copy HTML). */
export function formatRenderedArticleForClipboard(result: RenderedArticleResult): string {
  const body = result.bodyHtml || result.renderedHtml;
  if (result.schemaScripts.length === 0) return body;
  if (result.renderedHtml.includes('application/ld+json')) return result.renderedHtml;
  return `${body.trimEnd()}\n${result.schemaScripts.join('\n')}`;
}

export async function updateContent(
  id: string,
  body: {
    contentHtml: string;
    title?: string;
    targetKeyword?: string;
    targetLocation?: string;
  },
  accessToken?: string | null,
): Promise<SeoContentDocument> {
  const res = await fetch(`${API_URL}/api/seo/content/${id}/content`, {
    method: 'PUT',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoContentDocument>;
}

export async function updateContentStatus(
  id: string,
  status: string,
  accessToken?: string | null,
): Promise<SeoContentDocument> {
  const res = await fetch(`${API_URL}/api/seo/content/${id}/status`, {
    method: 'PATCH',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ status }),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoContentDocument>;
}

export async function attachUrlResearch(
  documentId: string,
  analysisRunId: string,
  accessToken?: string | null,
  targetKeyword?: string,
): Promise<SeoContentDocument> {
  return attachAnalysisRun(documentId, { analysisRunId, targetKeyword }, accessToken);
}

export type AnalysisRunSummary = {
  id: string;
  projectId: string;
  keyword: string;
  targetSiteUrl: string;
  status: string;
  serpSeResultsCount: number;
  organicResultCount: number;
  createdAt: string;
  contentWritingReady: boolean;
};

export async function listAnalysisRuns(
  projectId: string,
  accessToken?: string | null,
): Promise<AnalysisRunSummary[]> {
  const res = await fetch(
    `${API_URL}/api/seo/analysis-runs?projectId=${encodeURIComponent(projectId)}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<AnalysisRunSummary[]>;
}

export async function attachAnalysisRun(
  documentId: string,
  body: { analysisRunId: string; targetKeyword?: string; siteProfileId?: string },
  accessToken?: string | null,
): Promise<SeoContentDocument> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/analysis-run`, {
    method: 'PATCH',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<SeoContentDocument>;
}

/** SERP-only readiness — does not require Site Analyzer / site crawl data. */
export function analysisRunBlockReason(
  runs: AnalysisRunSummary[] | null | undefined,
  preferredRunId?: string,
): string | null {
  if (!runs) return null;
  const ready = runs.filter((run) => run.contentWritingReady);
  if (ready.length === 0) {
    return 'No analysis run with organic SERP results is ready for Content Writing yet.';
  }
  if (preferredRunId) {
    const targeted = runs.find((run) => run.id === preferredRunId);
    if (targeted && !targeted.contentWritingReady) {
      if (targeted.status?.toLowerCase() === 'failed') {
        return 'Selected analysis run failed — SERP data is not available.';
      }
      return 'Selected analysis run has no organic SERP results yet.';
    }
  }
  return null;
}

export async function draftContentFromResearch(
  documentId: string,
  accessToken?: string | null,
): Promise<WritingTextResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/draft`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<WritingTextResult>;
}

export type DraftJobProgressOptions = {
  onProgress?: (status: BackgroundJobStatus, elapsedMs: number) => void;
};

export async function getBackgroundJob(
  jobId: string,
  accessToken?: string | null,
): Promise<BackgroundJobStatus> {
  const res = await fetch(`${API_URL}/api/seo/jobs/${jobId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BackgroundJobStatus>;
}

export async function enqueueKeywordContentDraft(
  documentId: string,
  body: { keyword: string; location?: string; title?: string },
  accessToken?: string | null,
): Promise<BackgroundJobStatus> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/draft-job/keyword`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
    cache: 'no-store',
  });
  if (res.status !== 202) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BackgroundJobStatus>;
}

export async function enqueueResearchContentDraft(
  documentId: string,
  accessToken?: string | null,
): Promise<BackgroundJobStatus> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/draft-job/research`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status !== 202) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BackgroundJobStatus>;
}

export async function enqueueBulkArticles(
  body: { projectId: string; keywords: string[]; location?: string },
  accessToken?: string | null,
): Promise<BackgroundJobStatus> {
  const res = await fetch(`${API_URL}/api/seo/writing/bulk`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
    cache: 'no-store',
  });
  if (res.status !== 202) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BackgroundJobStatus>;
}

export type DraftJobProgressMode = 'keyword' | 'research';

export function describeDraftJobProgress(
  status: BackgroundJobStatus,
  mode: DraftJobProgressMode = 'keyword',
): string {
  if (status.status === 'failed') return status.errorMessage || 'Draft failed';
  if (status.status === 'completed' || status.status === 'complete') return 'Draft complete';
  if (status.progressPercent >= 90) return 'Saving draft…';

  if (mode === 'research') {
    if (status.progressPercent >= 20) return 'Drafting article from Site Analyzer pack…';
    if (status.progressPercent >= 5) return 'Loading research pack…';
    if (status.status === 'pending' || status.status === 'queued') return 'Queued…';
    return 'Preparing draft…';
  }

  if (status.progressPercent >= 55) return 'Drafting article…';
  if (status.progressPercent >= 25) return 'Writing outline…';
  if (status.progressPercent >= 5) return 'Researching SERP and building brief…';
  if (status.status === 'pending' || status.status === 'queued') return 'Queued…';
  return 'Draft in progress…';
}

export type FeaturedImageResult = {
  dataUrl: string;
  prompt: string;
  mimeType: string;
};

export async function generateFeaturedImage(
  documentId: string,
  options?: { regenerate?: boolean },
  accessToken?: string | null,
): Promise<FeaturedImageResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/featured-image`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ regenerate: options?.regenerate ?? false }),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<FeaturedImageResult>;
}

export function getApiUrl(): string {
  return API_URL;
}

export function getSeoApiUrl(): string {
  return SEO_API_URL;
}

export function getHubUrl(): string {
  const signalrBase =
    process.env.NEXT_PUBLIC_SEO_SIGNALR_URL?.replace(/\/$/, '') ?? SEO_API_URL;
  return `${signalrBase}/hubs/seo-scoring`;
}

export async function getCompetitors(
  documentId: string,
  accessToken?: string | null,
): Promise<CompetitorInsights> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/competitors`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<CompetitorInsights>;
}

export async function refreshCompetitorCrawl(
  documentId: string,
  accessToken?: string | null,
): Promise<CompetitorInsights> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/competitors/crawl`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<CompetitorInsights>;
}

export type KeywordResult = {
  keyword: string;
  searchVolume: number;
  keywordDifficulty: number;
  cpcUsd: number;
  competition: string;
};

export async function researchKeywords(
  body: {
    projectId: string;
    seedKeyword: string;
    location?: string;
    resultCount?: number;
  },
  accessToken?: string | null,
): Promise<KeywordResult[]> {
  const res = await fetch(`${API_URL}/api/seo/keywords/research`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<KeywordResult[]>;
}

export type WordPressConnectionStatus = {
  connected: boolean;
  siteUrl?: string;
  username?: string;
  defaultPostStatus: string;
};

export type WordPressPublishResult = {
  postId: number;
  url: string;
  status: string;
};

export async function getWordPressStatus(
  projectId: string,
  accessToken?: string | null,
): Promise<WordPressConnectionStatus> {
  const res = await fetch(`${API_URL}/api/seo/wordpress/${projectId}/status`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<WordPressConnectionStatus>;
}

export async function connectWordPress(
  projectId: string,
  body: {
    siteUrl: string;
    username: string;
    applicationPassword: string;
    defaultPostStatus?: string;
  },
  accessToken?: string | null,
): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/wordpress/connect?projectId=${projectId}`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export type ContentBrief = {
  keyword: string;
  location: string;
  targetWordCount: number;
  avgTitleLength: number;
  recommendedTerms: string[];
  suggestedHeadings: string[];
  topCompetitors: { position: number; url: string; title?: string; wordCount: number }[];
  competitorDomains: string[];
  competitorHeadingHighlights: string[];
  competitorSchemaTypes: string[];
  peopleAlsoAsk: string[];
  closingFaqQuestions: string[];
  methodology: {
    name: string;
    phases: string[];
    phaseDefinitions: {
      id: string;
      label: string;
      intent: string;
      headingFamilies: string[];
    }[];
  };
  directAnswerBlocks: {
    label: string;
    instruction: string;
  }[];
  technicalEvidenceRequirements: string[];
  geoAnchorNodes: string[];
  schemaBlueprint: {
    primaryType: string;
    additionalTypes: string[];
    softwareEntities: string[];
    aboutEntities: string[];
  };
  reviewChecklist: string[];
  nicheContext: {
    primaryNiche?: string | null;
    matchedPillar?: string | null;
    gapTopics: string[];
  };
  serpIntelligence: {
    peopleAlsoAsk: string[];
    relatedSearches: string[];
    featureFlags: string[];
    featuredSnippet?: string | null;
  };
  authorOrganizationName?: string | null;
  authorOrganizationUrl?: string | null;
  benchmarkQuality: string;
};

export type WritingTextResult = {
  content: string;
};

export type RenderedArticleResult = {
  bodyHtml: string;
  renderedHtml: string;
  schemaScripts: string[];
  schemaTypes: string[];
};

export type AutoOptimizeResult = {
  contentHtml: string;
  previousScore: number;
  estimatedScore: number;
  changesApplied: string[];
};

export type ApplySuggestionResult = {
  contentHtml: string;
  appliedChange: string;
  scoreUpdate?: ScoreUpdate | null;
};

export type ApplySuggestionOutcome =
  | { kind: 'completed'; result: ApplySuggestionResult }
  | { kind: 'queued'; job: BackgroundJobStatus };

export type AiDetectionResult = {
  aiProbability: number;
  summary: string;
};

export async function generateBrief(
  body: { projectId: string; keyword: string; location?: string },
  accessToken?: string | null,
): Promise<ContentBrief> {
  const res = await fetch(`${API_URL}/api/seo/briefs/generate`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<ContentBrief>;
}

export async function generateOutline(
  body: { keyword: string; brief: ContentBrief; title?: string },
  accessToken?: string | null,
): Promise<WritingTextResult> {
  const res = await fetch(`${API_URL}/api/seo/writing/outline`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<WritingTextResult>;
}

export async function generateDraft(
  body: {
    keyword: string;
    brief: ContentBrief;
    outline: string;
    targetWordCount?: number;
    title?: string;
  },
  accessToken?: string | null,
): Promise<WritingTextResult> {
  const res = await fetch(`${API_URL}/api/seo/writing/draft`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<WritingTextResult>;
}

/** Legacy keyword path — brief and outline run internally; not exposed in Content Writing UI. */
export async function draftFromKeyword(
  body: { projectId: string; keyword: string; location?: string; title?: string },
  accessToken?: string | null,
): Promise<WritingTextResult> {
  const brief = await generateBrief(
    { projectId: body.projectId, keyword: body.keyword, location: body.location },
    accessToken,
  );
  const outline = await generateOutline(
    { keyword: body.keyword, brief, title: body.title },
    accessToken,
  );
  return generateDraft(
    {
      keyword: body.keyword,
      brief,
      outline: outline.content,
      targetWordCount: brief.targetWordCount,
      title: body.title,
    },
    accessToken,
  );
}

export async function humanizeContent(
  body: { documentId: string; contentHtml: string },
  accessToken?: string | null,
): Promise<{ content: string }> {
  const res = await fetch(`${API_URL}/api/seo/writing/humanize`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<{ content: string }>;
}

export async function detectAiContent(
  body: { documentId: string; contentHtml: string },
  accessToken?: string | null,
): Promise<AiDetectionResult> {
  const res = await fetch(`${API_URL}/api/seo/writing/detect`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<AiDetectionResult>;
}

export async function autoOptimizeContent(
  documentId: string,
  accessToken?: string | null,
): Promise<AutoOptimizeResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/auto-optimize`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<AutoOptimizeResult>;
}

export async function applyScoreSuggestion(
  documentId: string,
  suggestionId: string,
  accessToken?: string | null,
  contentHtml?: string,
): Promise<ApplySuggestionOutcome> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/apply-suggestion`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ suggestionId, contentHtml }),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const result = (await res.json()) as ApplySuggestionResult;
  return { kind: 'completed', result };
}

export async function insertResearchCitation(
  documentId: string,
  body: { url: string; title?: string; contentHtml?: string },
  accessToken?: string | null,
): Promise<ApplySuggestionResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/insert-citation`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<ApplySuggestionResult>;
}

/** Below SignalR default 32 KB — large drafts must score over HTTP. */
export const SIGNALR_SCORE_HTML_MAX_CHARS = 28_000;

export type ScoreContentResult = {
  scoreUpdate?: ScoreUpdate | null;
  pendingReason?: string | null;
};

export async function scoreContentDocument(
  documentId: string,
  body: { contentHtml?: string; targetKeyword?: string },
  accessToken?: string | null,
): Promise<ScoreContentResult> {
  const res = await fetch(`${API_URL}/api/seo/content/${documentId}/score`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (res.status === 202) {
    const pending = (await res.json()) as { pendingReason?: string };
    return { pendingReason: pending.pendingReason ?? 'benchmark_refreshing' };
  }
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  const data = (await res.json()) as { scoreUpdate?: ScoreUpdate; pendingReason?: string };
  return {
    scoreUpdate: data.scoreUpdate ?? null,
    pendingReason: data.pendingReason ?? null,
  };
}

export async function publishToWordPress(
  documentId: string,
  body: { postStatus?: string; slug?: string },
  accessToken?: string | null,
): Promise<WordPressPublishResult> {
  const res = await fetch(`${API_URL}/api/seo/wordpress/publish?documentId=${documentId}`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<WordPressPublishResult>;
}

export type KeywordCluster = {
  clusterName: string;
  pillarKeyword: string;
  keywords: string[];
  averageVolume: number;
  averageDifficulty: number;
};

export async function clusterKeywords(
  body: { projectId: string; keywords: string[]; location?: string },
  accessToken?: string | null,
): Promise<KeywordCluster[]> {
  const res = await fetch(`${API_URL}/api/seo/keywords/cluster`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<KeywordCluster[]>;
}

export type BrandVoice = {
  id: string;
  name: string;
  sampleText: string;
  styleInstructions?: string;
  createdAt: string;
};

export async function listBrandVoices(accessToken?: string | null): Promise<BrandVoice[]> {
  if (!hasAuthContext(accessToken)) return [];
  const res = await fetch(`${API_URL}/api/seo/brand-voices`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BrandVoice[]>;
}

export async function createBrandVoice(
  body: { name: string; sampleText: string; styleInstructions?: string },
  accessToken?: string | null,
): Promise<BrandVoice> {
  const res = await fetch(`${API_URL}/api/seo/brand-voices`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BrandVoice>;
}

export async function deleteBrandVoice(id: string, accessToken?: string | null): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/brand-voices/${id}`, {
    method: 'DELETE',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function getSubscriptionTier(
  accessToken?: string | null,
): Promise<{ tier: string }> {
  const summary = await getSubscription(accessToken);
  return { tier: summary.tier };
}

export type SubscriptionSummary = {
  tier: string;
  status: string;
  paypalSubscriptionId?: string | null;
  currentPeriodEnd?: string | null;
};

export type BillingCatalogTier = {
  key: string;
  name: string;
  priceLabel: string;
  priceMonthly: number;
  highlights: string[];
};

export type BillingPlansResponse = {
  tiers: BillingCatalogTier[];
  checkout: {
    available: boolean;
    provider: string;
    deferred: boolean;
    clientId?: string;
    planIds?: Record<string, string>;
    environment?: 'sandbox' | 'live';
    missing: string[];
    plansSetupHint?: string;
  };
  manualTierChangeEnabled: boolean;
};

export async function getSubscription(
  accessToken?: string | null,
): Promise<SubscriptionSummary> {
  const res = await fetch(`${API_URL}/api/seo/subscription`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<SubscriptionSummary>(res);
}

export async function getBillingPlans(): Promise<BillingPlansResponse> {
  const res = await fetch(`${API_URL}/api/seo/subscription/plans`, {
    cache: 'no-store',
  });
  return seoJson<BillingPlansResponse>(res);
}

export async function setSubscriptionTier(
  tier: string,
  accessToken?: string | null,
): Promise<SubscriptionSummary> {
  const res = await fetch(`${API_URL}/api/seo/subscription/tier`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ tier }),
  });
  return seoJson<SubscriptionSummary>(res);
}

export async function cancelSubscription(accessToken?: string | null): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/subscription/cancel`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function deleteSerpCache(
  keyword: string,
  location: string,
  accessToken?: string | null,
  languageCode = 'en',
): Promise<void> {
  const params = new URLSearchParams({ keyword, location, languageCode });
  const res = await fetch(`${API_URL}/api/seo/serp-cache?${params}`, {
    method: 'DELETE',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export type GoogleIntegrationStatus = {
  connected: boolean;
  gscConnected: boolean;
  ga4Connected: boolean;
  siteUrl?: string;
  propertyId?: string;
  connectedAt?: string;
};

export type GoogleRankingRow = {
  query: string;
  page: string;
  impressions: number;
  clicks: number;
  ctr: number;
  position: number;
};

export type GoogleRankingsResponse = {
  projectId: string;
  siteUrl: string;
  startDate: string;
  endDate: string;
  rows: GoogleRankingRow[];
};

export type Ga4LandingPageRow = {
  landingPage: string;
  sessions: number;
  users: number;
  conversions: number;
};

export type Ga4LandingPagesResponse = {
  projectId: string;
  propertyId: string;
  startDate: string;
  endDate: string;
  rows: Ga4LandingPageRow[];
};

export async function getGoogleIntegrationStatus(
  projectId: string,
  accessToken?: string | null,
): Promise<GoogleIntegrationStatus> {
  const res = await fetch(
    `${API_URL}/api/seo/integrations/google/status?projectId=${projectId}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson<GoogleIntegrationStatus>(res);
}

export async function getGoogleConnectUrl(
  projectId: string,
  accessToken?: string | null,
  options?: { siteUrl?: string; propertyId?: string },
): Promise<{ url: string; expiresAt: string }> {
  const params = new URLSearchParams({ projectId });
  if (options?.siteUrl) params.set('siteUrl', options.siteUrl);
  if (options?.propertyId) params.set('propertyId', options.propertyId);
  const res = await fetch(
    `${API_URL}/api/seo/integrations/google/connect-url?${params.toString()}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson<{ url: string; expiresAt: string }>(res);
}

export async function disconnectGoogle(
  projectId: string,
  accessToken?: string | null,
): Promise<void> {
  const res = await fetch(
    `${API_URL}/api/seo/integrations/google/disconnect?projectId=${projectId}`,
    { method: 'DELETE', headers: apiHeaders(accessToken) },
  );
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function getGoogleRankings(
  projectId: string,
  accessToken?: string | null,
  startDate?: string,
  endDate?: string,
): Promise<GoogleRankingsResponse> {
  const params = new URLSearchParams();
  if (startDate) params.set('startDate', startDate);
  if (endDate) params.set('endDate', endDate);
  const qs = params.toString();
  const res = await fetch(
    `${API_URL}/api/seo/rankings/${projectId}${qs ? `?${qs}` : ''}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson<GoogleRankingsResponse>(res);
}

export async function getGa4LandingPages(
  projectId: string,
  accessToken?: string | null,
  startDate?: string,
  endDate?: string,
): Promise<Ga4LandingPagesResponse> {
  const params = new URLSearchParams();
  if (startDate) params.set('startDate', startDate);
  if (endDate) params.set('endDate', endDate);
  const qs = params.toString();
  const res = await fetch(
    `${API_URL}/api/seo/analytics/ga4/${projectId}/landing-pages${qs ? `?${qs}` : ''}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson<Ga4LandingPagesResponse>(res);
}

export type SiteAuditIssue = {
  code: string;
  severity: string;
  message: string;
  field?: string;
};

export type SiteAuditSummary = {
  id: string;
  projectId: string;
  status: string;
  pagesCrawled: number;
  overallScore: number | null;
  errorMessage: string | null;
  startedAt: string;
  completedAt: string | null;
};

export type SiteAuditPage = {
  id: string;
  url: string;
  score: number;
  issues: SiteAuditIssue[];
  crawledAt: string;
};

export type SiteAuditDetail = SiteAuditSummary & {
  pages: SiteAuditPage[];
};

export async function listSiteAudits(
  projectId: string,
  accessToken?: string | null,
): Promise<SiteAuditSummary[]> {
  const res = await fetch(`${API_URL}/api/seo/audit/site?projectId=${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<SiteAuditSummary[]>(res);
}

export async function getSiteAudit(
  auditId: string,
  accessToken?: string | null,
): Promise<SiteAuditDetail> {
  const res = await fetch(`${API_URL}/api/seo/audit/site/${auditId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<SiteAuditDetail>(res);
}

export async function startSiteAudit(
  projectId: string,
  accessToken?: string | null,
): Promise<SiteAuditSummary> {
  const res = await fetch(`${API_URL}/api/seo/audit/site`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ projectId }),
  });
  return seoJson<SiteAuditSummary>(res);
}

export type PlagiarismMatch = {
  url: string;
  title?: string;
  matchPercent: number;
  wordsMatched: number;
  viewUrl?: string;
};

export type PlagiarismCheckResult = {
  id: string;
  documentId: string;
  matchPercent: number;
  publishBlocked: boolean;
  cached: boolean;
  checkedAt: string;
  matches: PlagiarismMatch[];
};

export type PlagiarismStatus = {
  configured: boolean;
  provider: string;
};

export async function getPlagiarismStatus(
  accessToken?: string | null,
): Promise<PlagiarismStatus> {
  const res = await fetch(`${API_URL}/api/seo/plagiarism/status`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<PlagiarismStatus>(res);
}

export async function getLatestPlagiarismCheck(
  documentId: string,
  accessToken?: string | null,
): Promise<PlagiarismCheckResult | null> {
  const res = await fetch(`${API_URL}/api/seo/plagiarism/check/${documentId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 204) return null;
  return seoJson<PlagiarismCheckResult>(res);
}

export async function checkPlagiarism(
  documentId: string,
  accessToken?: string | null,
  forceRefresh = false,
): Promise<PlagiarismCheckResult> {
  const res = await fetch(`${API_URL}/api/seo/plagiarism/check`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify({ documentId, forceRefresh }),
  });
  return seoJson<PlagiarismCheckResult>(res);
}

export type InternalLinkSuggestion = {
  targetDocumentId: string;
  anchorText: string;
  targetUrl: string;
  publishPath?: string | null;
  linkType: 'spoke' | 'pillar' | 'sibling';
  reason: string;
  relevanceScore: number;
};

function normalizeInternalLinkSuggestion(body: Record<string, unknown>): InternalLinkSuggestion {
  const linkTypeRaw = String(body.linkType ?? body.LinkType ?? 'sibling').toLowerCase();
  const linkType =
    linkTypeRaw === 'spoke' || linkTypeRaw === 'pillar' || linkTypeRaw === 'sibling'
      ? linkTypeRaw
      : 'sibling';

  return {
    targetDocumentId: String(body.targetDocumentId ?? body.TargetDocumentId ?? ''),
    anchorText: String(body.anchorText ?? body.AnchorText ?? ''),
    targetUrl: String(body.targetUrl ?? body.TargetUrl ?? ''),
    publishPath: (body.publishPath ?? body.PublishPath) as string | null | undefined,
    linkType,
    reason: String(body.reason ?? body.Reason ?? ''),
    relevanceScore: Number(body.relevanceScore ?? body.RelevanceScore ?? 0),
  };
}

export async function suggestInternalLinks(
  body: { projectId: string; documentId: string; maxSuggestions?: number },
  accessToken?: string | null,
): Promise<InternalLinkSuggestion[]> {
  const res = await fetch(`${API_URL}/api/seo/links/suggest`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  const raw = await seoJson<Record<string, unknown>[]>(res);
  return raw.map(normalizeInternalLinkSuggestion);
}

export type DeepSerpOrganic = {
  position: number;
  url: string;
  title?: string;
  snippet?: string;
  domain?: string;
};

export type SerpIntentSummary = {
  primaryIntent: string;
  contentFormats: string[];
  avgSnippetLength: number;
};

export type DeepSerpResult = {
  keyword: string;
  location: string;
  provider: string;
  organic: DeepSerpOrganic[];
  peopleAlsoAsk: string[];
  relatedSearches: string[];
  intent: SerpIntentSummary;
  termMatrix?: SerpTermMatrix;
  cachedAt?: string;
};

export type SerpTermMatrix = {
  terms: string[];
  rows: SerpTermMatrixRow[];
};

export type SerpTermMatrixRow = {
  position: number;
  url: string;
  title?: string;
  counts: number[];
};

export async function analyzeDeepSerp(
  params: { keyword: string; location?: string; languageCode?: string },
  accessToken?: string | null,
): Promise<DeepSerpResult> {
  const qs = new URLSearchParams({ keyword: params.keyword });
  if (params.location) qs.set('location', params.location);
  if (params.languageCode) qs.set('languageCode', params.languageCode);
  const res = await fetch(`${API_URL}/api/seo/serp/deep?${qs}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<DeepSerpResult>(res);
}

export type InternalLinkAutoInsertResult = {
  inserted: boolean;
  contentHtml: string;
  anchorText?: string;
  targetUrl?: string;
  message?: string;
};

export async function autoInsertInternalLink(
  body: { projectId: string; documentId: string },
  accessToken?: string | null,
): Promise<InternalLinkAutoInsertResult> {
  const res = await fetch(`${API_URL}/api/seo/links/auto-insert`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  return seoJson<InternalLinkAutoInsertResult>(res);
}

export type CannibalizationPage = {
  url: string;
  impressions: number;
  clicks: number;
  position: number;
};

export type CannibalizationIssue = {
  query: string;
  pages: CannibalizationPage[];
  severity: string;
  recommendation: string;
  totalImpressions: number;
};

export type CannibalizationReport = {
  projectId: string;
  startDate: string;
  endDate: string;
  gscRowCount: number;
  uniqueQueryCount: number;
  multiUrlQueryCount: number;
  competingQueryCount: number;
  issues: CannibalizationIssue[];
};

export async function getCannibalizationReport(
  projectId: string,
  accessToken?: string | null,
): Promise<CannibalizationReport> {
  const res = await fetch(`${API_URL}/api/seo/cannibalization/${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  const raw = await seoJson<Record<string, unknown>>(res);
  const issuesRaw = raw.issues ?? raw.Issues;
  const issues = Array.isArray(issuesRaw)
    ? issuesRaw.map((item) => normalizeCannibalizationIssue(item as Record<string, unknown>))
    : [];
  return {
    projectId: String(raw.projectId ?? raw.ProjectId ?? projectId),
    startDate: String(raw.startDate ?? raw.StartDate ?? ''),
    endDate: String(raw.endDate ?? raw.EndDate ?? ''),
    gscRowCount: Number(raw.gscRowCount ?? raw.GscRowCount ?? 0),
    uniqueQueryCount: Number(raw.uniqueQueryCount ?? raw.UniqueQueryCount ?? 0),
    multiUrlQueryCount: Number(raw.multiUrlQueryCount ?? raw.MultiUrlQueryCount ?? 0),
    competingQueryCount: Number(raw.competingQueryCount ?? raw.CompetingQueryCount ?? issues.length),
    issues,
  };
}

function normalizeCannibalizationIssue(raw: Record<string, unknown>): CannibalizationIssue {
  const pagesRaw = raw.pages ?? raw.Pages;
  const pages = Array.isArray(pagesRaw)
    ? pagesRaw.map((p) => {
        const page = p as Record<string, unknown>;
        return {
          url: String(page.url ?? page.Url ?? ''),
          impressions: Number(page.impressions ?? page.Impressions ?? 0),
          clicks: Number(page.clicks ?? page.Clicks ?? 0),
          position: Number(page.position ?? page.Position ?? 0),
        };
      })
    : [];
  return {
    query: String(raw.query ?? raw.Query ?? ''),
    pages,
    severity: String(raw.severity ?? raw.Severity ?? 'low'),
    recommendation: String(raw.recommendation ?? raw.Recommendation ?? ''),
    totalImpressions: Number(raw.totalImpressions ?? raw.TotalImpressions ?? 0),
  };
}

export type DashboardOverviewProject = {
  project: SeoProject;
  documents: SeoContentDocument[];
  latestAuditScore: number | null;
  latestAuditAt: string | null;
};

export type DashboardOverviewResponse = {
  projects: DashboardOverviewProject[];
  recentDocuments: SeoContentDocument[];
};

export async function getDashboardOverview(
  accessToken?: string | null,
): Promise<DashboardOverviewResponse> {
  const res = await fetch(`${API_URL}/api/seo/dashboard/overview`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<DashboardOverviewResponse>(res);
}


export type TopicalMapCoverage = 'covered' | 'partial' | 'gap' | 'opportunity';

export type TopicalTier = 'Pillar' | 'Cluster' | 'Article';

export type QuickWin = {
  topicName: string;
  reason: string;
  intent?: string;
  searchVolume?: number;
  keywordDifficulty?: number;
};

export type SemanticEntity = {
  name: string;
  type: string;
  pillarRefs?: string[];
  reason?: string;
};

export type ContentSequenceItem = {
  order: number;
  topicId: string;
  topicName: string;
  tier: TopicalTier;
  reason?: string;
};

export type LinkGraphEdge = {
  sourceTopicId: string;
  targetTopicId: string;
  anchorText: string;
  priority?: 'high' | 'medium' | 'low';
};

export type InternalLinkingBlueprint = {
  sequences: ContentSequenceItem[];
  linkGraph: LinkGraphEdge[];
};

export type TopicalMapTopic = {
  name: string;
  queries: string[];
  coverage: TopicalMapCoverage;
  matchedDocumentId?: string;
  matchedDocumentTitle?: string;
  matchedPageUrl?: string;
  matchSource?: 'gsc' | 'document';
  totalImpressions: number;
  mainKeyword?: string;
  pillarName?: string;
  searchVolume?: number;
  keywordDifficulty?: number;
  intent?: string;
  averagePosition?: number;
  priorityScore?: number;
  clusterMethod?: 'gsc_page' | 'serp' | 'token' | string;
  competitorDomains?: string[];
  tier?: TopicalTier;
  pillarId?: string;
  parentClusterId?: string;
  entityGaps?: string[];
  entityCoverage?: number;
  linkFrom?: string[];
  linkTo?: string[];
  contentSequence?: number;
  suggestedWordCount?: number;
  suggestedTitle?: string;
  suggestedSlug?: string;
  contentType?: string;
  isDuplicate?: boolean;
  duplicateOf?: string;
  strategicPriority?: 'Must-have' | 'High-value' | 'Expansion';
};

export type TopicalMapResult = {
  version?: number;
  projectId: string;
  generatedAt: string;
  expiresAt?: string;
  topics: TopicalMapTopic[];
  coveredCount: number;
  gapCount: number;
  partialCount: number;
  opportunityCount?: number;
  recommendations?: TopicalMapTopic[];
  mode?: 'gsc' | 'seed';
  seedKeyword?: string;
  pillarCount?: number;
  clusterCount?: number;
  articleCount?: number;
  quickWins?: QuickWin[];
  semanticEntities?: SemanticEntity[];
  duplicateCount?: number;
  linkingBlueprint?: InternalLinkingBlueprint;
};

export async function generateTopicalMap(
  projectId: string,
  accessToken?: string | null,
  options?: { force?: boolean; seedKeyword?: string; fromNiche?: boolean },
): Promise<TopicalMapResult> {
  const params = new URLSearchParams();
  if (options?.force) params.set('force', 'true');
  if (options?.seedKeyword) params.set('seedKeyword', options.seedKeyword);
  if (options?.fromNiche) params.set('fromNiche', 'true');
  const query = params.toString() ? `?${params.toString()}` : '';
  const res = await fetch(`${API_URL}/api/seo/topical-map/${projectId}/generate${query}`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  return seoJson<TopicalMapResult>(res);
}

export async function getTopicalMap(
  projectId: string,
  accessToken?: string | null,
): Promise<TopicalMapResult | null> {
  const res = await fetch(`${API_URL}/api/seo/topical-map/${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 404 || res.status === 204) return null;
  return seoJson<TopicalMapResult>(res);
}

export async function getLinksBlueprint(
  projectId: string,
  accessToken?: string | null,
): Promise<InternalLinkingBlueprint | null> {
  const res = await fetch(`${API_URL}/api/seo/topical-map/${projectId}/linking-blueprint`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 404 || res.status === 204) return null;
  return seoJson<InternalLinkingBlueprint>(res);
}

export type EntityGapAnalysis = {
  name: string;
  mainKeyword?: string;
  tier?: TopicalTier;
  entityCoverage: number;
  entityGaps: string[];
  gapCount: number;
};

export async function getEntityGaps(
  projectId: string,
  accessToken?: string | null,
): Promise<EntityGapAnalysis[] | null> {
  const res = await fetch(`${API_URL}/api/seo/topical-map/${projectId}/entity-gaps`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 404 || res.status === 204) return null;
  return seoJson<EntityGapAnalysis[]>(res);
}

export type PublishedPageMetrics = {
  url: string;
  publishedPageId?: string;
  recentClicks: number;
  baselineClicks: number;
  recentImpressions: number;
  baselineImpressions: number;
  recentPosition: number;
  baselinePosition: number;
  clicksChangePercent: number;
  positionChange: number;
  status: 'stable' | 'decaying' | 'critical';
  recommendation: string;
  sparkline?: PerformanceSnapshotPoint[];
};

export type PerformanceSnapshotPoint = {
  date: string;
  clicks: number;
  impressions: number;
  position?: number;
};

export type PublishedContentAuditReport = {
  projectId: string;
  recentStartDate: string;
  recentEndDate: string;
  baselineStartDate: string;
  baselineEndDate: string;
  pages: PublishedPageMetrics[];
  decayingCount: number;
};

export async function getPublishedContentAudit(
  projectId: string,
  accessToken?: string | null,
): Promise<PublishedContentAuditReport> {
  const res = await fetch(`${API_URL}/api/seo/content-audit/${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<PublishedContentAuditReport>(res);
}

export type GeoPlatformStatus = {
  id: string;
  name: string;
  configured: boolean;
  provider?: string;
  note?: string;
};

export type GeoProbeResult = {
  projectId: string;
  query: string;
  platform: string;
  mentioned: boolean;
  hasAiOverview: boolean;
  organicPosition?: number;
  snippet?: string;
  checkedAt: string;
  note?: string;
};

export async function getGeoPlatforms(
  accessToken?: string | null,
): Promise<{ platforms: GeoPlatformStatus[] }> {
  if (!hasAuthContext(accessToken)) return { platforms: [] };
  const res = await fetch(`${API_URL}/api/seo/geo/platforms`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<{ platforms: GeoPlatformStatus[] }>(res);
}

export async function probeGeoVisibility(
  body: { projectId: string; query: string; location?: string },
  accessToken?: string | null,
): Promise<GeoProbeResult> {
  const res = await fetch(`${API_URL}/api/seo/geo/probe`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  return seoJson<GeoProbeResult>(res);
}

export type GeoTrackingQuery = {
  id: string;
  projectId: string;
  queryText: string;
  platforms: string[];
  enabled: boolean;
};

export type GeoTrendPoint = {
  date: string;
  platform: string;
  mentioned: boolean;
};

export type GeoTrendsResponse = {
  queryId: string;
  queryText: string;
  points: GeoTrendPoint[];
  mentionRate30d: number;
};

export async function listGeoQueries(
  projectId: string,
  accessToken?: string | null,
): Promise<GeoTrackingQuery[]> {
  const res = await fetch(`${API_URL}/api/seo/geo/queries?projectId=${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<GeoTrackingQuery[]>(res);
}

export async function createGeoQuery(
  body: { projectId: string; queryText: string; platforms?: string[] },
  accessToken?: string | null,
): Promise<GeoTrackingQuery> {
  const res = await fetch(`${API_URL}/api/seo/geo/queries`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  return seoJson<GeoTrackingQuery>(res);
}

export async function deleteGeoQuery(queryId: string, accessToken?: string | null): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/geo/queries/${queryId}`, {
    method: 'DELETE',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function getGeoTrends(
  queryId: string,
  accessToken?: string | null,
): Promise<GeoTrendsResponse> {
  const res = await fetch(`${API_URL}/api/seo/geo/queries/${queryId}/trends`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<GeoTrendsResponse>(res);
}

export type ContentGuardPolicy = {
  projectId: string;
  enabled: boolean;
  autoPatch: boolean;
};

export type ContentGuardRun = {
  id: string;
  projectId: string;
  documentId?: string;
  url: string;
  status: string;
  recommendation?: string;
  wordPressDraftPostId?: number;
  detectedAt: string;
  completedAt?: string;
};

export async function getContentGuardPolicy(
  projectId: string,
  accessToken?: string | null,
): Promise<ContentGuardPolicy | null> {
  const res = await fetch(`${API_URL}/api/seo/content-guard/${projectId}/policy`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 404) return null;
  return seoJson<ContentGuardPolicy>(res);
}

export async function upsertContentGuardPolicy(
  projectId: string,
  body: { enabled: boolean; autoPatch: boolean },
  accessToken?: string | null,
): Promise<ContentGuardPolicy> {
  const res = await fetch(`${API_URL}/api/seo/content-guard/${projectId}/policy`, {
    method: 'PUT',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  return seoJson<ContentGuardPolicy>(res);
}

export async function listContentGuardRuns(
  projectId: string,
  accessToken?: string | null,
): Promise<ContentGuardRun[]> {
  const res = await fetch(`${API_URL}/api/seo/content-guard/${projectId}/runs`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<ContentGuardRun[]>(res);
}

export async function scanContentGuard(projectId: string, accessToken?: string | null): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/content-guard/${projectId}/scan`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function approveContentGuardRun(runId: string, accessToken?: string | null): Promise<ContentGuardRun> {
  const res = await fetch(`${API_URL}/api/seo/content-guard/runs/${runId}/approve`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  return seoJson<ContentGuardRun>(res);
}

export async function rollbackContentGuardRun(runId: string, accessToken?: string | null): Promise<ContentGuardRun> {
  const res = await fetch(`${API_URL}/api/seo/content-guard/runs/${runId}/rollback`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  return seoJson<ContentGuardRun>(res);
}

export type TrackedKeyword = {
  id: string;
  projectId: string;
  keyword: string;
  location: string;
  device: string;
  enabled: boolean;
  addedAt: string;
};

export type RankHistoryPoint = {
  date: string;
  position: number | null;
  pageUrl?: string;
};

export async function getRankTrackerKeywords(
  projectId: string,
  accessToken?: string | null,
): Promise<TrackedKeyword[]> {
  if (!hasAuthContext(accessToken)) return [];
  const res = await fetch(`${API_URL}/api/seo/rank-tracker/${projectId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<TrackedKeyword[]>(res);
}

export async function addTrackedKeyword(
  projectId: string,
  request: { keyword: string; location?: string; device?: string },
  accessToken?: string | null,
): Promise<TrackedKeyword> {
  const res = await fetch(`${API_URL}/api/seo/rank-tracker/${projectId}`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(request),
  });
  return seoJson<TrackedKeyword>(res);
}

export async function deleteTrackedKeyword(keywordId: string, accessToken?: string | null): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/rank-tracker/keyword/${keywordId}`, {
    method: 'DELETE',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function getRankHistory(
  projectId: string,
  keyword: string,
  days: number = 30,
  accessToken?: string | null,
): Promise<RankHistoryPoint[]> {
  if (!hasAuthContext(accessToken)) return [];
  const res = await fetch(
    `${API_URL}/api/seo/rank-tracker/${projectId}/history?keyword=${encodeURIComponent(keyword)}&days=${days}`,
    {
      headers: apiHeaders(accessToken),
      cache: 'no-store',
    },
  );
  return seoJson<RankHistoryPoint[]>(res);
}

// ─── Page URL research (async analyze + SignalR progress) ───────────────────

export type UrlResearchSummary = {
  id: string;
  projectId: string;
  sourceUrl: string;
  derivedKeyword: string;
  status: string;
  dataQuality?: string | null;
  researchedAt?: string | null;
  createdAt: string;
};

export type UrlResearchFull = UrlResearchSummary & {
  searchLocation: string;
  businessContext?: string;
  errorMessage?: string | null;
  dataQualityNotes?: string | null;
  intentPrimary: string;
  intentJustification: string;
  directAnswerInstruction?: string;
  pafText?: string;
  pafType?: string;
  medianWordCountTop5: number;
  medianTitleLengthTop10: number;
  medianH2CountTop5: number;
  dominantContentFormat: string;
  organicResults?: Array<{
    position: number;
    url: string;
    domain: string;
    title: string;
    snippet: string;
    contentType: string;
  }>;
  peopleAlsoAsk?: Array<{ question: string; serpAnswerPreview: string; depth: number }>;
  relatedSearches?: Array<{ searchText: string }>;
  competitors?: Array<{
    url: string;
    position: number;
    h1: string;
    estimatedWordCount: number;
    headings?: Array<{ level: number; text: string }>;
  }>;
  recommendedTerms?: Array<{ term: string }>;
  closingFaqs?: Array<{ question: string; source: string }>;
  sectionHints?: Array<{
    suggestedH2: string;
    label: string;
    subtopicsFromSerp: string[];
  }>;
};

export type UrlResearchAnalyzeResponse = {
  urlResearchId: string;
  status: string;
};

export async function analyzeUrlResearch(
  body: { projectId: string; pageUrl: string },
  accessToken?: string | null,
): Promise<UrlResearchAnalyzeResponse> {
  const res = await fetch(`${API_URL}/api/seo/url-research/analyze`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
    cache: 'no-store',
  });
  return seoJson<UrlResearchAnalyzeResponse>(res);
}

export async function getUrlResearch(
  id: string,
  accessToken?: string | null,
): Promise<UrlResearchFull> {
  const res = await fetch(`${API_URL}/api/seo/url-research/${id}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<UrlResearchFull>(res);
}

export async function listUrlResearch(
  projectId: string,
  accessToken?: string | null,
): Promise<UrlResearchSummary[]> {
  const res = await fetch(
    `${API_URL}/api/seo/url-research?projectId=${encodeURIComponent(projectId)}`,
    {
      headers: apiHeaders(accessToken),
      cache: 'no-store',
    },
  );
  return seoJson<UrlResearchSummary[]>(res);
}

// ─── Site Analyzer (manual step wizard) ─────────────────────────────────────

export type SiteAnalyzerStepStatus = 'pending' | 'running' | 'green' | 'red';

export type SiteAnalyzerStepState = {
  stepNumber: number;
  title: string;
  status: SiteAnalyzerStepStatus;
  message: string;
  validationMessage?: string | null;
  log?: string | null;
  counts?: Record<string, unknown> | null;
  updatedAt?: string | null;
};

export type SiteAnalyzerPackSummary = {
  urlResearchId: string;
  keyword: string;
  location: string;
  dataQuality?: string | null;
  status: string;
  steps: SiteAnalyzerStepState[];
  firstRedStep?: number | null;
  handoffReady: boolean;
  researchedAt?: string | null;
  createdAt: string;
};

export type SiteAnalyzerProjectState = {
  projectId: string;
  siteUrl: string;
  siteResearchId?: string | null;
  siteIndexSteps: SiteAnalyzerStepState[];
  siteIndexComplete: boolean;
  firstRedSiteIndexStep?: number | null;
  packs: SiteAnalyzerPackSummary[];
};

export type SiteAnalyzerStepRunResponse = {
  stepNumber: number;
  status: SiteAnalyzerStepStatus;
  message: string;
  validationMessage?: string | null;
  log?: string | null;
  counts?: Record<string, unknown> | null;
};

function normalizeStepRunResponse(raw: SiteAnalyzerStepRunResponse): SiteAnalyzerStepRunResponse {
  const validationMessage =
    raw.validationMessage ??
    (raw.status === 'red' ? raw.message : null);
  return { ...raw, validationMessage };
}

export type SiteAnalyzerCreatePackResponse = {
  urlResearchId: string;
  keyword: string;
  location: string;
};

type SiteAnalyzerStepApi = {
  stepNumber: number;
  status: SiteAnalyzerStepStatus;
  message?: string;
  validationMessage?: string | null;
  log?: string | null;
  counts?: Record<string, unknown> | null;
  updatedAt?: string | null;
};

/** Legacy flat state shape (pre-packs array). */
type SiteAnalyzerStateApiLegacy = {
  siteResearchId?: string | null;
  siteUrl?: string;
  siteIndexSteps?: SiteAnalyzerStepApi[];
  activeUrlResearchId?: string | null;
  keyword?: string | null;
  keywordPackSteps?: SiteAnalyzerStepApi[];
  handoffEnabled?: boolean;
};

function mapStepApi(step: SiteAnalyzerStepApi, title: string): SiteAnalyzerStepState {
  const validationMessage =
    step.validationMessage ??
    (step.status === 'red' && step.message ? step.message : null);
  return {
    stepNumber: step.stepNumber,
    title,
    status: step.status ?? 'pending',
    message: step.message ?? '',
    validationMessage,
    log: step.log ?? null,
    counts: step.counts ?? null,
    updatedAt: step.updatedAt ?? null,
  };
}

function normalizeSiteAnalyzerProjectState(
  raw: SiteAnalyzerProjectState & SiteAnalyzerStateApiLegacy,
  projectId: string,
): SiteAnalyzerProjectState {
  if (Array.isArray(raw.packs)) {
    return {
      projectId: raw.projectId ?? projectId,
      siteUrl: raw.siteUrl ?? '',
      siteResearchId: raw.siteResearchId ?? null,
      siteIndexSteps: raw.siteIndexSteps ?? [],
      siteIndexComplete: raw.siteIndexComplete ?? false,
      firstRedSiteIndexStep: raw.firstRedSiteIndexStep ?? null,
      packs: raw.packs,
    };
  }

  const siteIndexSteps = raw.siteIndexSteps ?? [];
  const siteIndexComplete =
    raw.siteIndexComplete ??
    (siteIndexSteps.length >= 4 && siteIndexSteps.every((s) => s.status === 'green'));
  const packs: SiteAnalyzerPackSummary[] = raw.activeUrlResearchId
    ? [
        {
          urlResearchId: raw.activeUrlResearchId,
          keyword: raw.keyword ?? '',
          location: 'United States',
          status: 'queued',
          steps: (raw.keywordPackSteps ?? []).map((step) =>
            mapStepApi(step, `Step ${step.stepNumber}`),
          ),
          handoffReady: raw.handoffEnabled ?? false,
          createdAt: new Date().toISOString(),
        },
      ]
    : [];

  return {
    projectId,
    siteUrl: raw.siteUrl ?? '',
    siteResearchId: raw.siteResearchId ?? null,
    siteIndexSteps,
    siteIndexComplete,
    firstRedSiteIndexStep:
      raw.firstRedSiteIndexStep ??
      siteIndexSteps.find((s) => s.status === 'red')?.stepNumber ??
      null,
    packs,
  };
}

export async function getSiteAnalyzerProjectState(
  projectId: string,
  accessToken?: string | null,
): Promise<SiteAnalyzerProjectState> {
  const res = await fetch(
    `${API_URL}/api/seo/site-analyzer/projects/${encodeURIComponent(projectId)}/state`,
    {
      headers: apiHeaders(accessToken),
      cache: 'no-store',
    },
  );
  const raw = await seoJson<SiteAnalyzerProjectState & SiteAnalyzerStateApiLegacy>(res);
  return normalizeSiteAnalyzerProjectState(raw, projectId);
}

export async function runSiteIndexStep(
  projectId: string,
  step: number,
  accessToken?: string | null,
): Promise<SiteAnalyzerStepRunResponse> {
  const res = await fetch(
    `${API_URL}/api/seo/site-analyzer/projects/${encodeURIComponent(projectId)}/site-index/steps/${step}/run`,
    {
      method: 'POST',
      headers: apiHeaders(accessToken),
      cache: 'no-store',
    },
  );
  return normalizeStepRunResponse(await seoJson<SiteAnalyzerStepRunResponse>(res));
}

export async function createSiteAnalyzerPack(
  projectId: string,
  body: { keyword: string; location?: string },
  accessToken?: string | null,
): Promise<SiteAnalyzerCreatePackResponse> {
  const res = await fetch(
    `${API_URL}/api/seo/site-analyzer/projects/${encodeURIComponent(projectId)}/packs`,
    {
      method: 'POST',
      headers: apiHeaders(accessToken),
      body: JSON.stringify(body),
      cache: 'no-store',
    },
  );
  return seoJson<SiteAnalyzerCreatePackResponse>(res);
}

export async function runSiteAnalyzerPackStep(
  urlResearchId: string,
  step: number,
  accessToken?: string | null,
): Promise<SiteAnalyzerStepRunResponse> {
  const res = await fetch(
    `${API_URL}/api/seo/site-analyzer/packs/${encodeURIComponent(urlResearchId)}/steps/${step}/run`,
    {
      method: 'POST',
      headers: apiHeaders(accessToken),
      cache: 'no-store',
    },
  );
  return normalizeStepRunResponse(await seoJson<SiteAnalyzerStepRunResponse>(res));
}

/** First failing step message for Content Writing gate UI. */
export function siteAnalyzerBlockReason(
  state: SiteAnalyzerProjectState | null,
  preferredUrlResearchId?: string | null,
): string | null {
  if (!state) return 'Site Analyzer state could not be loaded.';

  const packs = state.packs ?? [];
  const siteIndexSteps = state.siteIndexSteps ?? [];

  if (!state.siteIndexComplete) {
    const red = siteIndexSteps.find((s) => s.status === 'red');
    if (red?.validationMessage) return red.validationMessage;
    if (red?.message) return red.message;
    const pending = siteIndexSteps.find((s) => s.status !== 'green');
    if (pending) {
      const label = pending.title ? ` (${pending.title})` : '';
      return `Complete Site Analyzer step ${pending.stepNumber}${label} first.`;
    }
    return 'Complete the site index (steps 1–4) in Site Analyzer first.';
  }

  const completePack = packs.find((p) => p.handoffReady || p.dataQuality === 'full');
  if (completePack) return null;

  if (preferredUrlResearchId) {
    const targeted = packs.find((p) => p.urlResearchId === preferredUrlResearchId);
    if (targeted && !targeted.handoffReady && targeted.dataQuality !== 'full') {
      const step10 = targeted.steps.find((s) => s.stepNumber === 10);
      if (step10?.status === 'green') {
        return 'This keyword pack was updated after the last finalize. Re-run step 10 in Site Analyzer.';
      }
      if (step10?.status === 'pending' || step10?.status === 'running') {
        return `Run step 10 for “${targeted.keyword}” in Site Analyzer to finalize the pack for Content Writing.`;
      }
      if (step10?.validationMessage) return step10.validationMessage;
      if (step10?.message) return step10.message;
      return 'Finish keyword pack steps 5–10 in Site Analyzer, then run step 10 to finalize.';
    }
  }

  const packWithRed = packs.find((p) => p.firstRedStep);
  if (packWithRed?.firstRedStep) {
    const step = packWithRed.steps.find((s) => s.stepNumber === packWithRed.firstRedStep);
    if (step?.validationMessage) return step.validationMessage;
    if (step?.message) return step.message;
    return `Keyword pack incomplete — step ${packWithRed.firstRedStep} is still red.`;
  }

  if (packs.length === 0) {
    return 'Create a keyword research pack in Site Analyzer (steps 5–10).';
  }

  return 'No keyword pack has passed all gates yet. Finish steps 5–10 in Site Analyzer.';
}

// ─── Niche Analyzer (legacy API — UI removed) ────────────────────────────────

export type StepStatus = 'pending' | 'running' | 'complete' | 'skipped' | 'error';

export type NicheAnalysisStatus = {
  profileId: string;
  status: 'pending' | 'queued' | 'processing' | 'complete' | 'failed';
  step?: string;
  stepNumber?: number;
  totalSteps?: number;
  errorMessage?: string;
  createdAt?: string;
  progressAt?: string;
  structureStatus?: string;
  enrichmentStatus?: string;
  persistStage?: string;
  stepStatuses?: Record<string, StepStatus>;
  stepSummaries?: Record<string, string>;
  stepErrors?: Record<string, string>;
  stepWarnings?: Record<string, string>;
};

export type NicheAnalysisStepLogEntry = {
  stepNumber: number;
  slug: string;
  title: string;
  status: string;
  summary: string;
  outputs: Record<string, unknown>;
};

export type NicheStepDefinition = {
  stepNumber: number;
  slug: string;
  title: string;
  phase: string;
  dependencies: string[];
  isOptional: boolean;
  isTerminal: boolean;
};

export type TopicEvidence = {
  source: string;
  snippet?: string;
  url?: string;
  weight: number;
};

export type TopicCandidate = {
  name: string;
  slug: string;
  evidence: TopicEvidence[];
  confidence: number;
  dedicatedPageUrl?: string;
  internalLinkCount: number;
};

export type SiteTopicProfile = {
  allCandidates: TopicCandidate[];
  selectedPillars: TopicCandidate[];
  excludedCandidates: TopicCandidate[];
  exclusionReasons: Record<string, string>;
  sulVersion: string;
  signalSourcesPresent: string[];
  normalizedTopicalityBySlug?: Record<string, number>;
  entityCoverageBySlug?: Record<string, PillarEntityCoverage>;
  internalLinkGraph?: InternalLinkGraph | null;
  recommendedActions?: PillarRecommendedAction[];
  localGeography?: LocalGeographyAnalysis | null;
};

export type LocalGeographyAnalysis = {
  areasServed: string[];
  locationPagesFound: LocalLocationPage[];
  gaps: LocalGeographyGap[];
  isLocalBusiness: boolean;
};

export type LocalLocationPage = {
  name: string;
  slug: string;
  url: string;
  matchSource: string;
};

export type LocalGeographyGap = {
  areaName: string;
  suggestedSlug: string;
  suggestedTitle: string;
  reason: string;
};

export type PillarRecommendedAction = {
  actionType:
    | 'suggest_pillar_page'
    | 'suggest_local_page'
    | 'schema_sync'
    | 'entity_thin_content'
    | 'link_orphan_pillar'
    | string;
  topicSlug: string;
  topicName: string;
  summary: string;
  priority: number;
};

export type PillarEntityCoverage = {
  slug: string;
  name: string;
  coverageScore: number;
  expectedEntityCount: number;
  matchedEntityCount: number;
  missingEntities: string[];
  isEntityThin: boolean;
};

export type InternalLinkGraphEdge = {
  fromSlug: string;
  toSlug: string;
  linkCount: number;
  sampleAnchors: string[];
};

export type InternalLinkGraph = {
  edges: InternalLinkGraphEdge[];
  orphanSlugs: string[];
};

export type NicheAnalysisDetails = {
  stepLogVersion: number;
  steps: NicheAnalysisStepLogEntry[];
  fusionSnapshot?: SiteTopicProfile | null;
  stepDefinitions?: NicheStepDefinition[] | null;
};

export type NicheTopicCandidateRow = {
  id: string;
  nicheProfileId: string;
  slug: string;
  name: string;
  confidence: number;
  isSelected: boolean;
  exclusionReason?: string | null;
  dedicatedPageUrl?: string | null;
  internalLinkCount: number;
  contentDepthScore: number;
  displayOrder: number;
  evidence?: TopicEvidence[] | null;
};

export type NicheTopicCandidateList = {
  items: NicheTopicCandidateRow[];
  total: number;
  page: number;
  pageSize: number;
};

export type NicheSubtopicResult = {
  id: string;
  subtopicTitle: string;
  targetKeyword: string;
  searchIntent: string;
  searchVolume: number;
  keywordDifficulty: number;
  coverageStatus: 'covered' | 'partial' | 'gap';
  existingUrl?: string;
  recommendedFormat: string;
  recommendedWordCount: number;
  fixEffort: string;
  isQuickWin: boolean;
};

export type PaaQuestionItem = {
  question: string;
  answer?: string;
  sourceUrl?: string;
  sourceTitle?: string;
};

export type CompetitorSiteInsight = {
  domain: string;
  pagesCrawled: number;
  avgWordCount: number;
  topHeadings: string[];
  hasFaqSchema: boolean;
  scope: 'national' | 'local' | 'both';
  services?: string[];
  knowsAbout?: string[];
  areaServed?: string[];
  sameAs?: string[];
  description?: string;
  brandName?: string;
};

export type NichePillarResult = {
  id: string;
  pillarTopic: string;
  pillarSlug: string;
  primaryKeyword: string;
  pageUrl?: string;
  searchIntent: string;
  searchVolume: number;
  keywordDifficulty: number;
  coverageStatus: 'covered' | 'partial' | 'gap';
  coverageScore: number;
  existingPageCount: number;
  requiredSubtopicCount: number;
  coveredSubtopicCount: number;
  strategicPriority: 'must_have' | 'high_value' | 'expansion';
  contentAngle?: string;
  source: string;
  displayOrder: number;
  subtopics: NicheSubtopicResult[];
  paaQuestions: PaaQuestionItem[];
  relatedSearches: string[];
  localPaaQuestions: PaaQuestionItem[];
  localRelatedSearches: string[];
};

export type NicheProfileResult = {
  id: string;
  projectId: string;
  domain: string;
  primaryNiche: string;
  nicheDescription: string;
  nicheTags: string[];
  audienceType: string;
  competitionLevel: string;
  topicalAuthorityScore: number;
  totalPillarsIdentified: number;
  pillarsCovered: number;
  pillarsPartial: number;
  pillarsGap: number;
  analyzedAt?: string;
  nextAnalysisDue?: string;
  createdAt?: string;
  status: string;
  structureStatus?: string;
  enrichmentStatus?: string;
  pillars: NichePillarResult[];
  competitors: NicheCompetitorResult[];
  entities: NicheEntityResult[];
};

export type CompetitorPillarResult = {
  name: string;
  slug: string;
  source: string;
  confidence: number;
};

export type NicheCompetitorResult = {
  id: string;
  domain: string;
  serpPresence: number;
  estimatedAuthorityScore: number;
  pillarsRanking: number;
  strengthAssessment: string;
  scope: 'national' | 'local' | 'both';
  pagesCrawled: number;
  avgWordCount: number;
  hasFaqSchema: boolean;
  services?: string[];
  knowsAbout?: string[];
  areaServed?: string[];
  sameAs?: string[];
  description?: string;
  brandName?: string;
  pillars?: CompetitorPillarResult[];
  competitorAnalyzedAt?: string;
};

export type NicheEntityResult = {
  id: string;
  entityName: string;
  entityType: string;
  mentionFrequency: number;
  presentOnDomain: boolean;
};

export type PillarCoverageMatrix = {
  pillarId: string;
  pillarTopic: string;
  primaryKeyword: string;
  searchVolume: number;
  keywordDifficulty: number;
  coverageScore: number;
  coveredSubtopics: number;
  totalSubtopics: number;
  gapSubtopics: number;
  coverageStatus: string;
  strategicPriority: string;
  hasQuickWins: boolean;
};

export type TopicalGapSummary = {
  subtopicId: string;
  pillarTopic: string;
  subtopicTitle: string;
  targetKeyword: string;
  searchVolume: number;
  keywordDifficulty: number;
  isQuickWin: boolean;
  recommendedFormat: string;
  fixEffort: string;
};

export type AuthorityProgressPoint = {
  snapshotDate: string;
  topicalAuthorityScore: number;
  pillarsCovered: number;
  totalSubtopicsCovered: number;
  totalGaps: number;
};

export type NicheProfileSummary = {
  id: string;
  domain: string;
  primaryNiche: string;
  topicalAuthorityScore: number;
  totalPillars: number;
  pillarsCovered: number;
  pillarsGap: number;
  competitionLevel: string;
  analyzedAt?: string;
  status: string;
};

export async function analyzeNiche(
  projectId: string,
  domain: string,
  accessToken?: string | null,
  seedTopic?: string,
): Promise<{ profileId: string; status: string }> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/analyze`, {
    method: 'POST',
    headers: { ...apiHeaders(accessToken), 'Content-Type': 'application/json' },
    body: JSON.stringify({ projectId, domain, seedTopic }),
  });
  return seoJson(res);
}

export async function runNicheStep(
  profileId: string,
  slug: string,
  accessToken?: string | null,
): Promise<void> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}/run-step/${slug}`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
}

export async function getNicheProfileCompetitors(
  profileId: string,
  accessToken?: string | null,
): Promise<NicheCompetitorResult[]> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}/niche-competitors`, {
    headers: apiHeaders(accessToken),
  });
  if (res.status === 404) return [];
  return seoJson(res);
}

export async function analyzeCompetitors(
  profileId: string,
  accessToken?: string | null,
): Promise<{ profileId: string; message: string }> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}/analyze-competitors`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
  });
  return seoJson(res);
}

export async function getNicheAnalysisStatus(
  profileId: string,
  accessToken?: string | null,
): Promise<NicheAnalysisStatus> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}/status`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson(res);
}

export async function getNicheAnalysisDetails(
  profileId: string,
  accessToken?: string | null,
): Promise<NicheAnalysisDetails> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}/analysis-details`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) {
    // 404 = not ready yet; 5xx = GeekRepository transient error during analysis
    return { stepLogVersion: 1, steps: [], fusionSnapshot: null };
  }
  return res.json() as Promise<NicheAnalysisDetails>;
}

export async function getNicheTopicCandidates(
  profileId: string,
  accessToken?: string | null,
  options?: { page?: number; pageSize?: number; selectedOnly?: boolean },
): Promise<NicheTopicCandidateList> {
  const page = options?.page ?? 1;
  const pageSize = options?.pageSize ?? 200;
  const selected =
    options?.selectedOnly === true
      ? '&selectedOnly=true'
      : options?.selectedOnly === false
        ? '&selectedOnly=false'
        : '';
  const res = await fetch(
    `${API_URL}/api/seo/niche-analyzer/${profileId}/topic-candidates?page=${page}&pageSize=${pageSize}${selected}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson(res);
}

/** Fetch full candidate inventory (paginated server-side). */
export async function getAllNicheTopicCandidates(
  profileId: string,
  accessToken?: string | null,
): Promise<NicheTopicCandidateRow[]> {
  const pageSize = 200;
  const first = await getNicheTopicCandidates(profileId, accessToken, { page: 1, pageSize });
  const items = [...first.items];
  const pages = Math.ceil(first.total / pageSize);
  for (let page = 2; page <= pages; page++) {
    const next = await getNicheTopicCandidates(profileId, accessToken, { page, pageSize });
    items.push(...next.items);
  }
  return items;
}

export async function getNicheProfile(
  profileId: string,
  accessToken?: string | null,
): Promise<NicheProfileResult> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson(res);
}

export async function getLatestNicheProfile(
  projectId: string,
  accessToken?: string | null,
): Promise<NicheProfileResult | null> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/project/${projectId}/latest`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (res.status === 204) return null;
  return seoJson(res);
}

export async function getNicheCoverageMatrix(
  profileId: string,
  accessToken?: string | null,
): Promise<PillarCoverageMatrix[]> {
  const res = await fetch(`${API_URL}/api/seo/niche-analyzer/${profileId}/coverage-matrix`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson(res);
}

export async function getNicheGaps(
  profileId: string,
  quickWinsOnly: boolean,
  accessToken?: string | null,
): Promise<TopicalGapSummary[]> {
  const res = await fetch(
    `${API_URL}/api/seo/niche-analyzer/${profileId}/gaps?quickWinsOnly=${quickWinsOnly}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson(res);
}

export async function getNicheProgress(
  projectId: string,
  accessToken?: string | null,
  months = 12,
): Promise<AuthorityProgressPoint[]> {
  const res = await fetch(
    `${API_URL}/api/seo/niche-analyzer/project/${projectId}/progress?months=${months}`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  if (res.ok) return seoJson(res);
  if (res.status === 404) return [];
  return [];
}

export async function getNicheHistory(
  projectId: string,
  accessToken?: string | null,
): Promise<NicheProfileSummary[]> {
  const res = await fetch(
    `${API_URL}/api/seo/niche-analyzer/project/${projectId}/history`,
    { headers: apiHeaders(accessToken), cache: 'no-store' },
  );
  return seoJson(res);
}
