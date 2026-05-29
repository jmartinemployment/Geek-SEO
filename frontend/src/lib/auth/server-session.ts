import { cookies } from 'next/headers';
import { authConfig } from '@/lib/auth/config';
import { REFRESH_COOKIE } from '@/lib/auth/oauth-cookies';

type TokenResponse = {
  access_token: string;
};

export async function getServerAccessToken(): Promise<string | null> {
  if (process.env.NEXT_PUBLIC_DEV_USER_ID) {
    return null;
  }

  const refresh = (await cookies()).get(REFRESH_COOKIE)?.value;
  if (!refresh) {
    return null;
  }

  const params = new URLSearchParams({
    grant_type: 'refresh_token',
    refresh_token: refresh,
    client_id: authConfig.clientId,
  });

  const response = await fetch(authConfig.tokenUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params.toString(),
    cache: 'no-store',
  });

  if (!response.ok) {
    return null;
  }

  const data = (await response.json()) as TokenResponse;
  return data.access_token;
}
