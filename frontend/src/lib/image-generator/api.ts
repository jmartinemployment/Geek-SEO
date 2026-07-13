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

export function listImageGeneratorSections(projectId: string): Promise<ImageGeneratorSectionsResponse> {
  return request(`/api/image-generator/projects/${projectId}/sections`);
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
