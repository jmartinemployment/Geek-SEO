import { NextResponse } from 'next/server';
import { authConfig } from '@/lib/auth/config';
import { PKCE_COOKIE, pkceCookieOptions } from '@/lib/auth/oauth-cookies';
import { generateCodeChallenge, generateCodeVerifier } from '@/lib/auth/pkce-server';

/** Starts OAuth: stores PKCE verifier in an httpOnly cookie, then redirects to GeekOAuth. */
export async function GET() {
  const verifier = generateCodeVerifier();
  const challenge = generateCodeChallenge(verifier);
  const params = new URLSearchParams({
    client_id: authConfig.clientId,
    redirect_uri: authConfig.redirectUri,
    response_type: 'code',
    scope: authConfig.scope,
    code_challenge: challenge,
    code_challenge_method: 'S256',
  });

  const res = NextResponse.redirect(`${authConfig.authorizeUrl}?${params.toString()}`);
  res.cookies.set(PKCE_COOKIE, verifier, pkceCookieOptions());
  return res;
}
