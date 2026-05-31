import { describe, expect, it } from 'vitest';
import { buildApiHeaders } from '@/lib/auth/api-headers';

describe('buildApiHeaders', () => {
  it('uses bearer token when access token is present', () => {
    const headers = buildApiHeaders('token-123', 'dev-user') as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer token-123');
    expect(headers['X-User-Id']).toBeUndefined();
  });

  it('falls back to dev user header without access token', () => {
    const headers = buildApiHeaders(null, '00000000-0000-0000-0000-000000000001') as Record<
      string,
      string
    >;
    expect(headers.Authorization).toBeUndefined();
    expect(headers['X-User-Id']).toBe('00000000-0000-0000-0000-000000000001');
  });

  it('returns json content type only when unauthenticated', () => {
    const headers = buildApiHeaders(null, null) as Record<string, string>;
    expect(headers['Content-Type']).toBe('application/json');
    expect(headers.Authorization).toBeUndefined();
    expect(headers['X-User-Id']).toBeUndefined();
  });
});
