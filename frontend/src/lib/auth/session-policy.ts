/** Minimum delay before proactive refresh (ms). */
export const MIN_REFRESH_DELAY_MS = 30_000;

/** Refresh this many seconds before access token expiry. */
export const REFRESH_BUFFER_SECONDS = 60;

export function computeRefreshDelayMs(expiresInSeconds: number): number {
  return Math.max(MIN_REFRESH_DELAY_MS, (expiresInSeconds - REFRESH_BUFFER_SECONDS) * 1000);
}

export function shouldBootstrapSession(pathname: string, devUserId?: string | null): boolean {
  if (devUserId) return false;
  return !pathname.startsWith('/auth');
}

export function isAuthenticated(accessToken: string | null, devUserId?: string | null): boolean {
  return Boolean(accessToken) || Boolean(devUserId);
}

export function isProtectedAppRoute(pathname: string): boolean {
  return pathname.startsWith('/app') || pathname.startsWith('/content-writing');
}

export function requiresAppAuth(
  pathname: string,
  hasRefreshCookie: boolean,
  devUserId?: string | null,
): boolean {
  if (!isProtectedAppRoute(pathname)) return false;
  if (devUserId) return false;
  return !hasRefreshCookie;
}
