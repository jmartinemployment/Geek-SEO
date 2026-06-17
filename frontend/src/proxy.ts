import { NextResponse, type NextRequest } from 'next/server';
import { REFRESH_COOKIE } from '@/lib/auth/cookies';
import { requiresAppAuth } from '@/lib/auth/session-policy';

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID;
  const hasRefreshCookie = Boolean(request.cookies.get(REFRESH_COOKIE)?.value);

  // Next.js RSC prefetch requests must not be redirected cross-origin —
  // Chrome blocks the chain and logs "Unsafe attempt to load URL". Let prefetches
  // pass through; the actual navigation is still protected.
  const isPrefetch = request.headers.get('Next-Router-Prefetch') === '1';
  if (isPrefetch) return NextResponse.next();

  if (requiresAppAuth(pathname, hasRefreshCookie, devUserId)) {
    const login = new URL('/api/auth/start', request.url);
    return NextResponse.redirect(login);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/app/:path*', '/content-writing', '/content-writing/:path*'],
};
