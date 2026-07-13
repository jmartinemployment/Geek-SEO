/** Minimum delay before proactive refresh (ms). */
export const MIN_REFRESH_DELAY_MS = 30_000;

/** Refresh this many seconds before access token expiry. */
export const REFRESH_BUFFER_SECONDS = 60;

export function computeRefreshDelayMs(expiresInSeconds: number): number {
  return Math.max(MIN_REFRESH_DELAY_MS, (expiresInSeconds - REFRESH_BUFFER_SECONDS) * 1000);
}

export function shouldBootstrapSession(pathname: string, devUserId?: string | null): boolean {
  if (devUserId) return false;
  if (pathname.startsWith('/auth')) return false;
  return isProtectedAppRoute(pathname);
}

/** Refresh an existing session on public routes without blocking first paint. */
export function shouldBackgroundRefreshSession(pathname: string, devUserId?: string | null): boolean {
  if (devUserId) return false;
  if (pathname.startsWith('/auth')) return false;
  if (isProtectedAppRoute(pathname)) return false;
  return true;
}

export function isAuthenticated(accessToken: string | null, devUserId?: string | null): boolean {
  return Boolean(accessToken) || Boolean(devUserId);
}

export function isProtectedAppRoute(pathname: string): boolean {
  return (
    pathname.startsWith('/analytics') ||
    pathname.startsWith('/audit') ||
    pathname.startsWith('/brand-voice') ||
    pathname.startsWith('/briefs') ||
    pathname.startsWith('/bulk') ||
    pathname.startsWith('/calendar') ||
    pathname.startsWith('/cannibalization') ||
    pathname.startsWith('/content-guard') ||
    pathname.startsWith('/dashboard') ||
    pathname.startsWith('/geo') ||
    pathname.startsWith('/guided') ||
    pathname.startsWith('/keywords') ||
    pathname.startsWith('/planner') ||
    pathname.startsWith('/projects') ||
    pathname.startsWith('/rankings') ||
    pathname.startsWith('/serp') ||
    pathname.startsWith('/settings') ||
    pathname.startsWith('/strategy')
  );
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
