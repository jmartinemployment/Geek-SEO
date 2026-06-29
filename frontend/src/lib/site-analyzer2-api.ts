import { apiHeaders } from '@/lib/seo-api';

const SEO_API_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? 'http://localhost:5051';

/** Site Analyzer 2 operator API — hosted in GeekSeoBackend when SA2 is enabled. */
export function getSiteAnalyzer2ApiBase(): string {
  return `${SEO_API_URL.replace(/\/$/, '')}/api/seo/sa2`;
}

export function siteAnalyzer2Headers(accessToken?: string | null): HeadersInit {
  return apiHeaders(accessToken);
}

export async function siteAnalyzer2Fetch(
  path: string,
  accessToken?: string | null,
  init?: RequestInit,
): Promise<Response> {
  const base = getSiteAnalyzer2ApiBase();
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  return fetch(`${base}${normalizedPath}`, {
    ...init,
    headers: {
      ...siteAnalyzer2Headers(accessToken),
      ...(init?.headers ?? {}),
    },
  });
}
