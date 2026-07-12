import type {
  ContentFigureDto,
  ContentFiguresListResponse,
  CrawlSummary,
  ExportMarkdownResponse,
  FigureGenerateResponse,
  FigureMergeResponse,
  GeneratedContentSet,
  KeywordSourceCategory,
  KeywordSourceResponse,
  LlmProviderType,
  LmStudioHealthStatus,
  ProjectDetail,
  ProjectSummary,
  PublishToSiteResponse,
} from "./types";

const SEO_API_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? "http://localhost:5051";

/** Content Writer routes are hosted on GeekSeoBackend (same origin as SEO API). */
const API_BASE_URL = SEO_API_URL;

/** True when the UI talks to the hosted Railway API (LM Studio is not available there). */
export function isProductionContentWriterApi(): boolean {
  try {
    const url = new URL(API_BASE_URL);
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

export class FigureArtConflictError extends ApiError {
  constructor(
    message: string,
    public readonly readyCount: number,
    public readonly publishedCount: number,
  ) {
    super(message, 409);
    this.name = "FigureArtConflictError";
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: {
        ...(init?.body && !(init.body instanceof FormData) ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch {
    throw new ApiError(
      `Could not reach the API at ${API_BASE_URL}. Hard-refresh the page and confirm the API is running.`,
      0
    );
  }

  if (!response.ok) {
    const detail = await response.text().catch(() => response.statusText);
    if (response.status === 409) {
      const conflict = tryParseFigureConflict(detail);
      if (conflict) {
        throw new FigureArtConflictError(conflict.detail, conflict.readyCount, conflict.publishedCount);
      }
    }
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

export function generateToolPagesContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/tools`, { method: "POST" });
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

export function generateImagePromptsContent(
  projectId: string,
  options?: { confirmRegenerateWithArt?: boolean },
): Promise<GeneratedContentSet> {
  const query = options?.confirmRegenerateWithArt ? "?confirmRegenerateWithArt=true" : "";
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate/image-prompts${query}`, {
    method: "POST",
  });
}

export function exportMarkdownContent(
  projectId: string,
  department?: string
): Promise<ExportMarkdownResponse> {
  return request<ExportMarkdownResponse>(`/api/projects/${projectId}/export/markdown`, {
    method: "POST",
    body: JSON.stringify(department ? { department } : {}),
  });
}

export function publishToSite(
  projectId: string,
  department?: string
): Promise<PublishToSiteResponse> {
  return request<PublishToSiteResponse>(`/api/projects/${projectId}/publish/site`, {
    method: "POST",
    body: JSON.stringify(department ? { department } : {}),
  });
}

export function listFigures(projectId: string): Promise<ContentFiguresListResponse> {
  return request<ContentFiguresListResponse>(`/api/projects/${projectId}/figures`);
}

export function mergeFigures(
  projectId: string,
  source: "pillar" | "blog"
): Promise<FigureMergeResponse> {
  return request<FigureMergeResponse>(`/api/projects/${projectId}/figures/merge`, {
    method: "POST",
    body: JSON.stringify({ source }),
  });
}

export function attachFigure(
  projectId: string,
  source: string,
  headingSlug: string,
  file: File,
  alt?: string
): Promise<ContentFigureDto> {
  const form = new FormData();
  form.append("file", file);
  const query = alt ? `?alt=${encodeURIComponent(alt)}` : "";
  const encodedSource = encodeURIComponent(source);
  return request<ContentFigureDto>(
    `/api/projects/${projectId}/figures/${encodedSource}/${encodeURIComponent(headingSlug)}/attach${query}`,
    { method: "POST", body: form }
  );
}

export function skipFigure(
  projectId: string,
  source: string,
  headingSlug: string
): Promise<ContentFigureDto> {
  const encodedSource = encodeURIComponent(source);
  return request<ContentFigureDto>(
    `/api/projects/${projectId}/figures/${encodedSource}/${encodeURIComponent(headingSlug)}/skip`,
    { method: "POST" }
  );
}

export function generateFigureImage(
  projectId: string,
  source: string,
  headingSlug: string
): Promise<ContentFigureDto> {
  const encodedSource = encodeURIComponent(source);
  return request<ContentFigureDto>(
    `/api/projects/${projectId}/figures/${encodedSource}/${encodeURIComponent(headingSlug)}/generate`,
    { method: "POST" }
  );
}

export function generatePendingFigures(
  projectId: string,
  source: string
): Promise<FigureGenerateResponse> {
  return request<FigureGenerateResponse>(`/api/projects/${projectId}/figures/generate`, {
    method: "POST",
    body: JSON.stringify({ source }),
  });
}

export function generateAllContent(projectId: string): Promise<GeneratedContentSet> {
  return request<GeneratedContentSet>(`/api/projects/${projectId}/generate`, { method: "POST" });
}

export function getLmStudioStatus(): Promise<LmStudioHealthStatus> {
  return request<LmStudioHealthStatus>("/api/llm/lm-studio/status");
}

function tryParseFigureConflict(raw: string): { detail: string; readyCount: number; publishedCount: number } | null {
  try {
    const parsed = JSON.parse(raw) as {
      detail?: string;
      readyCount?: number;
      publishedCount?: number;
    };
    if (typeof parsed.readyCount !== "number" || typeof parsed.publishedCount !== "number") {
      return null;
    }
    return {
      detail: parsed.detail ?? "Figure art would be affected.",
      readyCount: parsed.readyCount,
      publishedCount: parsed.publishedCount,
    };
  } catch {
    return null;
  }
}

function tryParseProblemDetail(raw: string): string | null {
  try {
    const parsed = JSON.parse(raw) as { detail?: string; title?: string };
    return parsed.detail ?? parsed.title ?? null;
  } catch {
    return null;
  }
}
