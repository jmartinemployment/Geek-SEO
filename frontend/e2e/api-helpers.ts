import type { APIRequestContext } from '@playwright/test';

export function getSeoApiBaseUrl(): string {
  return (
    process.env.PLAYWRIGHT_API_URL ??
    process.env.NEXT_PUBLIC_SEO_API_URL ??
    'http://localhost:5051'
  ).replace(/\/$/u, '');
}

export function getIntegrationUserId(): string {
  return (
    process.env.INTEGRATION_USER_ID ??
    process.env.NEXT_PUBLIC_DEV_USER_ID ??
    '00000000-0000-0000-0000-000000000001'
  );
}

export function devApiHeaders(): Record<string, string> {
  return {
    'X-User-Id': getIntegrationUserId(),
    Accept: 'application/json',
    'Content-Type': 'application/json',
  };
}

export type EphemeralContentFixture = {
  projectId: string;
  documentId: string;
};

/** Creates a project + content document via GeekSeoBackend (X-User-Id). Caller must delete project. */
export async function createEphemeralContentDocument(
  request: APIRequestContext,
): Promise<EphemeralContentFixture> {
  const apiBase = getSeoApiBaseUrl();
  const headers = devApiHeaders();
  const stamp = Date.now();

  const projectRes = await request.post(`${apiBase}/api/seo/projects`, {
    headers,
    data: {
      name: `E2E content ${stamp}`,
      url: 'https://example.com',
      defaultLocation: 'United States',
    },
  });
  if (!projectRes.ok()) {
    throw new Error(`create project failed: ${projectRes.status()} ${await projectRes.text()}`);
  }
  const project = (await projectRes.json()) as { id?: string };
  if (!project.id) {
    throw new Error('create project missing id');
  }

  const contentRes = await request.post(`${apiBase}/api/seo/content`, {
    headers,
    data: {
      projectId: project.id,
      title: `E2E document ${stamp}`,
      targetKeyword: 'integration test',
    },
  });
  if (!contentRes.ok()) {
    throw new Error(`create content failed: ${contentRes.status()} ${await contentRes.text()}`);
  }
  const doc = (await contentRes.json()) as { id?: string };
  if (!doc.id) {
    throw new Error('create content missing id');
  }

  return { projectId: project.id, documentId: doc.id };
}

export async function deleteProject(request: APIRequestContext, projectId: string): Promise<void> {
  const apiBase = getSeoApiBaseUrl();
  await request.delete(`${apiBase}/api/seo/projects/${projectId}`, {
    headers: devApiHeaders(),
  });
}
