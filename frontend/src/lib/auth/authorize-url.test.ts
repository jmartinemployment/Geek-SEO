import { describe, expect, it } from 'vitest';
import { buildAuthorizeUrl } from '@/lib/auth/authorize-url';
import { generateCodeChallenge } from '@/lib/auth/pkce-server';

describe('buildAuthorizeUrl', () => {
  it('includes PKCE and OAuth params', () => {
    const verifier = 'verifier-abc';
    const url = new URL(buildAuthorizeUrl(verifier));

    expect(url.origin + url.pathname).toBe('https://auth.example.com/connect/authorize');
    expect(url.searchParams.get('client_id')).toBe('geekseo-test');
    expect(url.searchParams.get('redirect_uri')).toBe('https://app.example.com/auth/callback');
    expect(url.searchParams.get('response_type')).toBe('code');
    expect(url.searchParams.get('scope')).toContain('offline_access');
    expect(url.searchParams.get('code_challenge_method')).toBe('S256');
    expect(url.searchParams.get('code_challenge')).toBe(generateCodeChallenge(verifier));
  });
});
