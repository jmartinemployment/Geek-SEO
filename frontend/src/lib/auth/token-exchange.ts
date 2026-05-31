import { authConfig } from '@/lib/auth/config';

export type OAuthTokenResponse = {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
  token_type: string;
};

export type TokenGrantType = 'authorization_code' | 'refresh_token';

export type TokenExchangeInput =
  | { grantType: 'authorization_code'; code: string; codeVerifier: string }
  | { grantType: 'refresh_token'; refreshToken: string };

export function buildTokenExchangeParams(input: TokenExchangeInput): URLSearchParams {
  const params = new URLSearchParams({ client_id: authConfig.clientId });

  if (input.grantType === 'authorization_code') {
    params.set('grant_type', 'authorization_code');
    params.set('code', input.code);
    params.set('code_verifier', input.codeVerifier);
    params.set('redirect_uri', authConfig.redirectUri);
    return params;
  }

  params.set('grant_type', 'refresh_token');
  params.set('refresh_token', input.refreshToken);
  return params;
}

export async function exchangeOAuthToken(params: URLSearchParams): Promise<OAuthTokenResponse> {
  const response = await fetch(authConfig.tokenUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params.toString(),
    cache: 'no-store',
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || 'Token exchange failed');
  }

  return response.json() as Promise<OAuthTokenResponse>;
}

export function isInvalidGrantError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error);
  return message.includes('invalid_grant');
}

export function toClientTokenPayload(tokens: OAuthTokenResponse) {
  return {
    accessToken: tokens.access_token,
    expiresIn: tokens.expires_in,
    refreshToken: tokens.refresh_token,
  };
}
