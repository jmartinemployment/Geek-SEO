import { describe, expect, it } from 'vitest';
import {
  computeRefreshDelayMs,
  isAuthenticated,
  requiresAppAuth,
  shouldBootstrapSession,
} from '@/lib/auth/session-policy';

describe('session-policy', () => {
  it('computes refresh delay with minimum floor', () => {
    expect(computeRefreshDelayMs(900)).toBe(840_000);
    expect(computeRefreshDelayMs(30)).toBe(30_000);
  });

  it('skips bootstrap on auth routes and dev mode', () => {
    expect(shouldBootstrapSession('/auth/callback', null)).toBe(false);
    expect(shouldBootstrapSession('/app/dashboard', 'dev-user')).toBe(false);
    expect(shouldBootstrapSession('/app/dashboard', null)).toBe(true);
  });

  it('detects authenticated state from token or dev user', () => {
    expect(isAuthenticated('token', null)).toBe(true);
    expect(isAuthenticated(null, 'dev-user')).toBe(true);
    expect(isAuthenticated(null, null)).toBe(false);
  });

  it('requires refresh cookie for protected app routes', () => {
    expect(requiresAppAuth('/app/dashboard', false, null)).toBe(true);
    expect(requiresAppAuth('/app/dashboard', true, null)).toBe(false);
    expect(requiresAppAuth('/app/dashboard', false, 'dev-user')).toBe(false);
    expect(requiresAppAuth('/content-writing', false, null)).toBe(true);
    expect(requiresAppAuth('/content-writing', true, null)).toBe(false);
    expect(requiresAppAuth('/pricing', false, null)).toBe(false);
  });
});
