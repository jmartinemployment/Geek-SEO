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

export async function deleteProject(request: APIRequestContext, projectId: string): Promise<void> {
  const apiBase = getSeoApiBaseUrl();
  await request.delete(`${apiBase}/api/seo/projects/${projectId}`, {
    headers: devApiHeaders(),
  });
}
