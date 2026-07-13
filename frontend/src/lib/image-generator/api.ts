const API_BASE_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? "http://localhost:5051";

export interface ImageGeneratorSection {
  sourceType: string;
  headingSlug: string;
  heading: string;
  briefText: string;
  geekApiSlug: string | null;
  relativePath: string | null;
  existsOnDisk: boolean;
  imageUrl: string | null;
  status: string;
}

export interface ImageGeneratorSectionsResponse {
  projectId: string;
  sections: ImageGeneratorSection[];
}

export interface GenerateFromBriefResponse {
  heading: string;
  fileName: string;
  imageBase64: string;
}

type RawSection = Record<string, unknown>;

function str(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function strOrNull(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function normalizeSection(raw: RawSection): ImageGeneratorSection {
  return {
    sourceType: str(raw.sourceType ?? raw.SourceType),
    headingSlug: str(raw.headingSlug ?? raw.HeadingSlug),
    heading: str(raw.heading ?? raw.Heading),
    briefText: str(raw.briefText ?? raw.BriefText),
    geekApiSlug: strOrNull(raw.geekApiSlug ?? raw.GeekApiSlug),
    relativePath: strOrNull(raw.relativePath ?? raw.RelativePath),
    existsOnDisk: Boolean(raw.existsOnDisk ?? raw.ExistsOnDisk),
    imageUrl: strOrNull(raw.imageUrl ?? raw.ImageUrl),
    status: str(raw.status ?? raw.Status) || "Pending",
  };
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let message = response.statusText;
    try {
      const body = (await response.json()) as { detail?: string; title?: string };
      message = body.detail ?? body.title ?? message;
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }

  return response.json() as Promise<T>;
}

export function generateFromBrief(heading: string, briefText: string): Promise<GenerateFromBriefResponse> {
  return request("/api/image-generator/generate-from-brief", {
    method: "POST",
    body: JSON.stringify({ heading, briefText }),
  });
}

export async function listImageGeneratorSections(
  projectId: string
): Promise<ImageGeneratorSectionsResponse> {
  const raw = await request<{
    projectId?: string;
    ProjectId?: string;
    sections?: RawSection[];
    Sections?: RawSection[];
  }>(`/api/image-generator/projects/${projectId}/sections`);

  const sections = (raw.sections ?? raw.Sections ?? []).map(normalizeSection);
  return {
    projectId: str(raw.projectId ?? raw.ProjectId) || projectId,
    sections,
  };
}

export function generateFigureDraft(
  projectId: string,
  sourceType: string,
  headingSlug: string
): Promise<unknown> {
  const encodedSource = encodeURIComponent(sourceType);
  return request(
    `/api/image-generator/projects/${projectId}/${encodedSource}/${encodeURIComponent(headingSlug)}/generate`,
    { method: "POST" }
  );
}
