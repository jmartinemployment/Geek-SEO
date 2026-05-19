import { NextResponse, type NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  if (!pathname.startsWith('/app')) return NextResponse.next();

  const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID;
  if (devUserId) return NextResponse.next();

  if (!request.cookies.get('geekseo_refresh')) {
    const login = new URL('/auth/login', request.url);
    login.searchParams.set('next', pathname);
    return NextResponse.redirect(login);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/app/:path*'],
};
