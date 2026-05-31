import { NextResponse } from 'next/server';
import { buildAuthorizeUrl } from '@/lib/auth/authorize-url';
import { PKCE_COOKIE, pkceCookieOptions } from '@/lib/auth/cookies';
import { generateCodeVerifier } from '@/lib/auth/pkce-server';

/** Starts OAuth: stores PKCE verifier in an httpOnly cookie, then redirects to GeekOAuth. */
export async function GET() {
  const verifier = generateCodeVerifier();
  const res = NextResponse.redirect(buildAuthorizeUrl(verifier));
  res.cookies.set(PKCE_COOKIE, verifier, pkceCookieOptions());
  return res;
}
