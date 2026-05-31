import { NextResponse } from 'next/server';
import { PKCE_COOKIE, REFRESH_COOKIE, clearAuthCookieOptions } from '@/lib/auth/cookies';

export async function POST() {
  const res = NextResponse.json({ ok: true });
  const clear = clearAuthCookieOptions();
  res.cookies.set(REFRESH_COOKIE, '', clear);
  res.cookies.set(PKCE_COOKIE, '', clear);
  return res;
}
