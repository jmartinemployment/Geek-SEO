import { NextResponse } from 'next/server';
import { PKCE_COOKIE, REFRESH_COOKIE } from '@/lib/auth/oauth-cookies';

export async function POST() {
  const res = NextResponse.json({ ok: true });
  const clear = { httpOnly: true, path: '/', maxAge: 0 };
  res.cookies.set(REFRESH_COOKIE, '', clear);
  res.cookies.set(PKCE_COOKIE, '', clear);
  return res;
}
