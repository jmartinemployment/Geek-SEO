import { describe, expect, it } from 'vitest';
import { buildTokenExchangeParams, toClientTokenPayload } from '@/lib/auth/token-exchange';

describe('token-exchange helpers', () => {
  it('builds authorization_code grant params', () => {
    const params = buildTokenExchangeParams({
      grantType: 'authorization_code',
      code: 'auth-code',
      codeVerifier: 'verifier',
    });

    expect(params.get('grant_type')).toBe('authorization_code');
    expect(params.get('code')).toBe('auth-code');
    expect(params.get('code_verifier')).toBe('verifier');
    expect(params.get('redirect_uri')).toBe('https://app.example.com/auth/callback');
    expect(params.get('client_id')).toBe('geekseo-test');
  });

  it('builds refresh_token grant params', () => {
    const params = buildTokenExchangeParams({
      grantType: 'refresh_token',
      refreshToken: 'refresh-abc',
    });

    expect(params.get('grant_type')).toBe('refresh_token');
    expect(params.get('refresh_token')).toBe('refresh-abc');
    expect(params.get('client_id')).toBe('geekseo-test');
  });

  it('detects invalid_grant token errors', async () => {
    const { isInvalidGrantError } = await import('@/lib/auth/token-exchange');
    expect(isInvalidGrantError(new Error('{"error":"invalid_grant"}'))).toBe(true);
    expect(isInvalidGrantError(new Error('other'))).toBe(false);
  });

  it('maps provider token payload to client shape', () => {
    const mapped = toClientTokenPayload({
      access_token: 'access',
      refresh_token: 'refresh',
      expires_in: 900,
      token_type: 'Bearer',
    });

    expect(mapped).toEqual({
      accessToken: 'access',
      refreshToken: 'refresh',
      expiresIn: 900,
    });
  });
});
