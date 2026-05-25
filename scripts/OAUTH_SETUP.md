# geek-OAuth setup for Geek SEO

Geek SEO login uses **geek-OAuth only**. GeekAPI is not part of this project.

## DNS

Host **`auth`** → `auth.geekatyourspot.com` → geek-OAuth Railway URL. See `frontend/DEPLOY.md`.

## 1. Register client

Run `scripts/geekseo_oauth_client.sql` and add `geekseo` to `geek-OAuth/src/oidc/clients.ts` with redirect:

- `http://localhost:3000/auth/callback`
- `https://seo.geekatyourspot.com/auth/callback`

## 2. Local geek-OAuth

```bash
cd ../geek-OAuth
PORT=3001 npm run dev
```

## 3. GeekSeoBackend

GeekSeoBackend must validate JWTs from `AUTH_SERVER_URL` (same public URL as `NEXT_PUBLIC_AUTH_URL`). SEO routes are not on geek-OAuth.

## 4. Frontend

```bash
NEXT_PUBLIC_OAUTH_AUTHORITY=http://localhost:3001
NEXT_PUBLIC_OAUTH_CLIENT_ID=geekseo
```

See `frontend/.env.example` and `frontend/DEPLOY.md`.
