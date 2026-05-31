import { NextResponse, type NextRequest } from 'next/server';
import { REFRESH_COOKIE } from '@/lib/auth/cookies';
import { requiresAppAuth } from '@/lib/auth/session-policy';

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID;
  const hasRefreshCookie = Boolean(request.cookies.get(REFRESH_COOKIE)?.value);

  if (requiresAppAuth(pathname, hasRefreshCookie, devUserId)) {
    const login = new URL('/api/auth/start', request.url);
    return NextResponse.redirect(login);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/app/:path*'],
};
