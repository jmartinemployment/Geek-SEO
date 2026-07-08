import type {
  CrawlSummary,
  GeneratedContentSet,
  KeywordSourceCategory,
  KeywordSourceResponse,
  LlmProviderType,
  LmStudioHealthStatus,
  ProjectDetail,
  ProjectSummary,
} from "./types";

const DEFAULT_SEO_API_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? "http://localhost:5051";
const PRODUCTION_SEO_API_URL = "https://seo-api.geekatyourspot.com";

/** Content Writer routes are hosted on GeekSeoBackend (same origin as SEO API). */
function resolveApiBaseUrl(): string {
  if (typeof window !== "undefined") {
    const host = window.location.hostname;
    if (host === "seo.geekatyourspot.com" || host === "geek-seo.vercel.app") {
      return PRODUCTION_SEO_API_URL;
    }
  }

  if (DEFAULT_SEO_API_URL.includes("contentwriter-production")) {
    return PRODUCTION_SEO_API_URL;
  }

  return DEFAULT_SEO_API_URL;
}

/** True when the UI talks to the hosted Railway API (LM Studio is not available there). */
export function isProductionContentWriterApi(): boolean {
  try {
    const url = new URL(resolveApiBaseUrl());
    return url.hostname !== "localhost" && url.hostname !== "127.0.0.1";
  } catch {
    return false;
  }
}

export function defaultLlmProvider(): LlmProviderType {
  return isProductionContentWriterApi() ? "OpenAi" : "LmStudio";
}

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${resolveApiBaseUrl()}${path}`, {
      ...init,
      headers: {
        ...(init?.body && !(init.body instanceof FormData) ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch {
    throw new ApiError(
      `Could not reach the API at ${resolveApiBaseUrl()}. Hard-refresh the page and confirm the API is running.`,
      0
    );
  }

  if (!response.ok) {
    const detail = await response.text().catch(() => response.statusText);
    const problemDetail = tryParseProblemDetail(detail);
    throw new ApiError(problemDetail || detail || response.statusText, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function createProject(input: {
  name: string;
  projectUrl: string;
  targetKeyword: string;
  preferredProvider: LlmProviderType;
}): Promise<ProjectSummary> {
  return request<ProjectSummary>("/api/projects", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function getRecentProjects(): Promise<ProjectSummary[]> {
  return request<ProjectSummary[]>("/api/projects");
}

export function getProject(projectId: string): Promise<ProjectDetail> {
  return request<ProjectDetail>(`/api/projects/${projectId}`);
}

export function crawlProject(projectId: string, maxPages = 50): Promise<CrawlSummary> {
  return request<CrawlSummary>(`/api/projects/${projectId}/crawl?maxPages=${maxPages}`, {
    method: "POST",
  });
}

export function uploadKeywordSource(
  projectId: string,
  category: KeywordSourceCategory,
  file: File
): Promise<KeywordSourceResponse> {
  const formData = new FormData();
  formData.append("category", category);
  formData.append("file", file);

  return request<KeywordSourceResponse>(`/api/projects/${projectId}/keyword-sources`, {
    method: "POST",
    body: formData,
  });
}

export function deleteKeywordSource(projectId: string, keywordSourceId: string): Promise<void> {
  return request<void>(`/api/projects/${projectId}/keyword-sources/${keywordSourceId}`, {
    method: "DELETE",
  });
}

export function generatePillarPlanContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/pillar/plan`, { method: "POST" });
}

export function generatePillarBodyContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/pillar/body`, { method: "POST" });
}

export function generatePillarContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/pillar`, { method: "POST" });
}

export function generateBlogContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/blog`, { method: "POST" });
}

export function generateSocialContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/social`, { method: "POST" });
}

export function generateColdOutreachContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(
    `/api/projects/${projectId}/generate/email-cold-outreach`,
    { method: "POST" },
  );
}

export function generateAllContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate`, { method: "POST" });
}

export function getLmStudioStatus(): Promise<LmStudioHealthStatus> {
  return request<LmStudioHealthStatus>("/api/llm/lm-studio/status");
}

function tryParseProblemDetail(raw: string): string | null {
  try {
    const parsed = JSON.parse(raw) as { detail?: string; title?: string };
    return parsed.detail ?? parsed.title ?? null;
  } catch {
    return null;
  }
}
