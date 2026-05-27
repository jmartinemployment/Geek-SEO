import { parseSeoApiErrorResponse } from '@/lib/seo-api-errors';

export { SeoApiError, formatSeoApiErrorMessage } from '@/lib/seo-api-errors';
export type { SeoGateErrorBody } from '@/lib/seo-api-errors';

/** GeekSeoBackend — sole SEO API for this app (see plan-documents/GEEKSEO-PLAN.md). */
const SEO_API_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? 'http://localhost:5051';

const API_URL = SEO_API_URL;

async function seoJson<T>(res: Response): Promise<T> {
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<T>;
}

export function apiHeaders(accessToken?: string | null): HeadersInit {
  const headers: HeadersInit = { 'Content-Type': 'application/json' };
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
    return headers;
  }
  const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID;
  if (devUserId) headers['X-User-Id'] = devUserId;
  return headers;
}

export type SeoProject = {
  id: string;
  name: string;
  url: string;
  defaultLocation: string;
  gscConnected: boolean;
};

export type SeoContentDocument = {
  id: string;
  projectId: string;
  userId: string;
  title: string;
  contentHtml: string;
  targetKeyword: string;
  targetLocation?: string;
  seoScore: number;
  wordCount: number;
  scoreComponentsJson: string;
  status: string;
};

export async function listProjects(accessToken?: string | null): Promise<SeoProject[]> {
  const res = await fetch(`${API_URL}/api/seo/projects`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  return seoJson<SeoProject[]>(res);
}

export type BackgroundJobStatus = {
  jobId: string;
  jobType: string;
  status: string;
  progressPercent: number;
  resultId?: string;
  errorMessage?: string;
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
  body: { name: string; url: string; defaultLocation?: string },
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
    projectId: string;
    title?: string;
    targetKeyword?: string;
    targetLocation?: string;
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

export async function startFullArticle(
  body: {
    projectId: string;
    keyword: string;
    location?: string;
    title?: string;
  },
  accessToken?: string | null,
): Promise<BackgroundJobStatus> {
  const res = await fetch(`${API_URL}/api/seo/writing/full-article`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BackgroundJobStatus>;
}

export async function getJobStatus(
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
  peopleAlsoAsk: string[];
  benchmarkQuality: string;
};

export type AutoOptimizeResult = {
  contentHtml: string;
  previousScore: number;
  estimatedScore: number;
  changesApplied: string[];
};

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

export async function startBulkArticles(
  body: { projectId: string; keywords: string[]; location?: string },
  accessToken?: string | null,
): Promise<BackgroundJobStatus> {
  const res = await fetch(`${API_URL}/api/seo/writing/bulk`, {
    method: 'POST',
    headers: apiHeaders(accessToken),
    body: JSON.stringify(body),
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<BackgroundJobStatus>;
}

export async function getSubscriptionTier(
  accessToken?: string | null,
): Promise<{ tier: string }> {
  const res = await fetch(`${API_URL}/api/seo/subscription`, {
    headers: apiHeaders(accessToken),
    cache: 'no-store',
  });
  if (!res.ok) throw await parseSeoApiErrorResponse(res);
  return res.json() as Promise<{ tier: string }>;
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
): Promise<{ url: string; expiresAt: string }> {
  const res = await fetch(
    `${API_URL}/api/seo/integrations/google/connect-url?projectId=${projectId}`,
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
