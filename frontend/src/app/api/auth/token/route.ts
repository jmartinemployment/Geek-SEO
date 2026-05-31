import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';
import {
  PKCE_COOKIE,
  REFRESH_COOKIE,
  clearAuthCookieOptions,
  refreshCookieOptions,
} from '@/lib/auth/cookies';
import {
  buildTokenExchangeParams,
  exchangeOAuthToken,
  isAuthorizationCodeExpiredError,
  isRefreshSessionExpiredError,
  toClientTokenPayload,
} from '@/lib/auth/token-exchange';

function sessionExpiredResponse(): NextResponse {
  const res = NextResponse.json(
    { accessToken: null, expiresIn: 0, sessionExpired: true },
    { status: 401 },
  );
  res.cookies.set(REFRESH_COOKIE, '', clearAuthCookieOptions());
  return res;
}

export async function POST(request: Request) {
  let grantType: string = 'unknown';
  try {
    const json = (await request.json()) as {
      grantType: 'authorization_code' | 'refresh_token';
      code?: string;
      codeVerifier?: string;
    };
    grantType = json.grantType ?? 'unknown';

    if (grantType !== 'authorization_code' && grantType !== 'refresh_token') {
      return NextResponse.json({ error: 'Unsupported grant type' }, { status: 400 });
    }

    if (json.grantType === 'authorization_code') {
      if (!json.code) return NextResponse.json({ error: 'code required' }, { status: 400 });

      const jar = await cookies();
      const codeVerifier = jar.get(PKCE_COOKIE)?.value ?? json.codeVerifier?.trim() ?? '';
      if (!codeVerifier) {
        return NextResponse.json(
          { error: 'Sign-in session expired. Go to Log in and try again.' },
          { status: 400 },
        );
      }

      const tokens = await exchangeOAuthToken(
        buildTokenExchangeParams({ grantType: 'authorization_code', code: json.code, codeVerifier }),
      );
      const payload = toClientTokenPayload(tokens);
      const res = NextResponse.json({
        accessToken: payload.accessToken,
        expiresIn: payload.expiresIn,
      });

      if (payload.refreshToken) {
        res.cookies.set(REFRESH_COOKIE, payload.refreshToken, refreshCookieOptions());
      }
      res.cookies.set(PKCE_COOKIE, '', clearAuthCookieOptions());
      return res;
    }

    const refresh = (await cookies()).get(REFRESH_COOKIE)?.value;
    if (!refresh) {
      return NextResponse.json({ accessToken: null, expiresIn: 0 });
    }

    const tokens = await exchangeOAuthToken(
      buildTokenExchangeParams({ grantType: 'refresh_token', refreshToken: refresh }),
    );
    const payload = toClientTokenPayload(tokens);
    const res = NextResponse.json({
      accessToken: payload.accessToken,
      expiresIn: payload.expiresIn,
    });

    if (payload.refreshToken) {
      res.cookies.set(REFRESH_COOKIE, payload.refreshToken, refreshCookieOptions());
    }

    return res;
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Token error';

    if (grantType === 'refresh_token' && isRefreshSessionExpiredError(error)) {
      return sessionExpiredResponse();
    }

    if (grantType === 'authorization_code' && isAuthorizationCodeExpiredError(error)) {
      return NextResponse.json(
        { error: 'Sign-in code expired or already used. Go to Log in and try again.' },
        { status: 400 },
      );
    }

    return NextResponse.json({ error: message }, { status: 400 });
  }
}
