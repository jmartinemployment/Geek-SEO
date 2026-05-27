export const PKCE_COOKIE = 'geekseo_pkce_verifier';
export const REFRESH_COOKIE = 'geekseo_refresh';

export function pkceCookieOptions() {
  return {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax' as const,
    path: '/',
    maxAge: 600,
  };
}
