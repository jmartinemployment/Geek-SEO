import { cookies } from 'next/headers';
import { REFRESH_COOKIE } from '@/lib/auth/cookies';
import {
  buildTokenExchangeParams,
  exchangeOAuthToken,
} from '@/lib/auth/token-exchange';

export async function getServerAccessToken(): Promise<string | null> {
  if (process.env.NEXT_PUBLIC_DEV_USER_ID) {
    return null;
  }

  const refresh = (await cookies()).get(REFRESH_COOKIE)?.value;
  if (!refresh) {
    return null;
  }

  try {
    const tokens = await exchangeOAuthToken(
      buildTokenExchangeParams({ grantType: 'refresh_token', refreshToken: refresh }),
    );
    return tokens.access_token;
  } catch {
    return null;
  }
}
