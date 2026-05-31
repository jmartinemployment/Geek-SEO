export const PKCE_COOKIE = 'geekseo_pkce_verifier';
export const REFRESH_COOKIE = 'geekseo_refresh';

type CookieOptions = {
  httpOnly: boolean;
  secure: boolean;
  sameSite: 'lax';
  path: string;
  maxAge: number;
};

export function pkceCookieOptions(isProduction = process.env.NODE_ENV === 'production'): CookieOptions {
  return {
    httpOnly: true,
    secure: isProduction,
    sameSite: 'lax',
    path: '/',
    maxAge: 600,
  };
}

export function refreshCookieOptions(isProduction = process.env.NODE_ENV === 'production'): CookieOptions {
  return {
    httpOnly: true,
    secure: isProduction,
    sameSite: 'lax',
    path: '/',
    maxAge: 60 * 60 * 24 * 30,
  };
}

export function clearAuthCookieOptions(): Pick<CookieOptions, 'httpOnly' | 'path' | 'maxAge'> {
  return { httpOnly: true, path: '/', maxAge: 0 };
}
