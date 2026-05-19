# Geek SEO — OAuth setup

## 1. geek-OAuth (required)

Client `geekseo` is defined in `geek-OAuth/src/oidc/clients.ts`. Redeploy geek-OAuth after pulling.

| Variable | Example |
|----------|---------|
| `AUTH_SERVER_URL` | `https://auth.geekatyourspot.com` |
| `GEEK_BACKEND_URL` | `https://api.geekatyourspot.com` |

Local: run geek-OAuth on **port 3001** (`PORT=3001 npm run dev`) so Next.js can use 3000.

## 2. Database client row (optional)

If your OIDC adapter reads `auth.oauth_clients`, run:

```bash
psql "$DATABASE_URL" -f scripts/geekseo_oauth_client.sql
```

## 3. GeekAPI

```bash
AUTH_SERVER_URL=https://auth.geekatyourspot.com   # or http://localhost:3001
```

## 4. Vercel / Next.js

See `frontend/DEPLOY.md` — `NEXT_PUBLIC_AUTH_URL`, `NEXT_PUBLIC_CLIENT_ID=geekseo`.

## 5. Verify

1. Open `https://<auth-server>/.well-known/openid-configuration`
2. Sign in at `/auth/login` on the frontend
3. Confirm `GET /api/seo/projects` returns 200 with Bearer token
