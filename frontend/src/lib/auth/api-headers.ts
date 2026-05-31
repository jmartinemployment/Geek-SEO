export function buildApiHeaders(accessToken?: string | null, devUserId?: string | null): HeadersInit {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
    return headers;
  }

  if (devUserId) {
    headers['X-User-Id'] = devUserId;
  }

  return headers;
}
