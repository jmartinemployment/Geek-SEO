import { authConfig } from '@/lib/auth/config';
import { generateCodeChallenge } from '@/lib/auth/pkce-server';

export function buildAuthorizeUrl(codeVerifier: string): string {
  const params = new URLSearchParams({
    client_id: authConfig.clientId,
    redirect_uri: authConfig.redirectUri,
    response_type: 'code',
    scope: authConfig.scope,
    code_challenge: generateCodeChallenge(codeVerifier),
    code_challenge_method: 'S256',
  });

  return `${authConfig.authorizeUrl}?${params.toString()}`;
}
