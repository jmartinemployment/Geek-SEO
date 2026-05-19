export const authConfig = {
  authUrl: (process.env.NEXT_PUBLIC_AUTH_URL ?? 'http://localhost:3001').replace(/\/$/u, ''),
  clientId: process.env.NEXT_PUBLIC_CLIENT_ID ?? 'geekseo',
  redirectUri:
    process.env.NEXT_PUBLIC_REDIRECT_URI ??
    `${(process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3000').replace(/\/$/u, '')}/auth/callback`,
  scope: 'openid profile email',
};
