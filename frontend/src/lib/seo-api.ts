import { parseSeoApiErrorResponse } from '@/lib/seo-api-errors';
import { buildApiHeaders } from '@/lib/auth/api-headers';

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

export function getApiUrl(): string {
  return API_URL;
}

export function getSeoApiUrl(): string {
  return SEO_API_URL;
}

export function getHubUrl(): string {
  const signalrBase =
    process.env.NEXT_PUBLIC_SEO_SIGNALR_URL?.replace(/\/$/, '') ?? SEO_API_URL;
  return `${signalrBase}/hubs/seo-realtime`;
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
  latestAuditScore: number | null;
  latestAuditAt: string | null;
};

export type DashboardOverviewResponse = {
  projects: DashboardOverviewProject[];
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
