import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';
import { authConfig } from '@/lib/auth/config';
import { PKCE_COOKIE, REFRESH_COOKIE } from '@/lib/auth/oauth-cookies';

type TokenResponse = {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
  token_type: string;
};

async function exchangeToken(body: URLSearchParams): Promise<TokenResponse> {
  const response = await fetch(authConfig.tokenUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: body.toString(),
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || 'Token exchange failed');
  }

  return response.json() as Promise<TokenResponse>;
}

export async function POST(request: Request) {
  try {
    const json = (await request.json()) as {
      grantType: 'authorization_code' | 'refresh_token';
      code?: string;
      codeVerifier?: string;
    };

    const params = new URLSearchParams({
      client_id: authConfig.clientId,
    });

    if (json.grantType === 'authorization_code') {
      if (!json.code) return NextResponse.json({ error: 'code required' }, { status: 400 });

      const jar = await cookies();
      const codeVerifier =
        jar.get(PKCE_COOKIE)?.value ?? json.codeVerifier?.trim() ?? '';
      if (!codeVerifier)
        return NextResponse.json(
          { error: 'Sign-in session expired. Go to Log in and try again.' },
          { status: 400 },
        );

      params.set('grant_type', 'authorization_code');
      params.set('code', json.code);
      params.set('code_verifier', codeVerifier);
      params.set('redirect_uri', authConfig.redirectUri);
    } else {
      const refresh = (await cookies()).get(REFRESH_COOKIE)?.value;
      if (!refresh)
        return NextResponse.json({ accessToken: null, expiresIn: 0 });
      params.set('grant_type', 'refresh_token');
      params.set('refresh_token', refresh);
    }

    const tokens = await exchangeToken(params);
    const res = NextResponse.json({
      accessToken: tokens.access_token,
      expiresIn: tokens.expires_in,
    });

    if (tokens.refresh_token) {
      res.cookies.set(REFRESH_COOKIE, tokens.refresh_token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        path: '/',
        maxAge: 60 * 60 * 24 * 30,
      });
    }

    res.cookies.set(PKCE_COOKIE, '', { httpOnly: true, path: '/', maxAge: 0 });

    return res;
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Token error';
    return NextResponse.json({ error: message }, { status: 400 });
  }
}
