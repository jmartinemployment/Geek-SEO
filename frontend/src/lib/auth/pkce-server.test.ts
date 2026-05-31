import { describe, expect, it } from 'vitest';
import { generateCodeChallenge, generateCodeVerifier } from '@/lib/auth/pkce-server';

describe('pkce-server', () => {
  it('generates stable S256 challenge for a verifier', () => {
    const verifier = 'test-verifier-value';
    expect(generateCodeChallenge(verifier)).toBe(generateCodeChallenge(verifier));
  });

  it('generates unique verifiers', () => {
    expect(generateCodeVerifier()).not.toBe(generateCodeVerifier());
  });
});
