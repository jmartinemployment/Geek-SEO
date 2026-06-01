# Deploy Geek SEO frontend (Vercel)

WordPress is **optional**. Users can write, score, and export HTML without a connected site.

## Prerequisites

- **GeekSeoBackend** deployed and reachable (SEO REST + SignalR)
- **geek-OAuth** deployed for login (`scripts/geekseo_oauth_client.sql` + `geek-OAuth/src/oidc/clients.ts`) — platform identity (OAuth 2.1 + PKCE) is **live** for Geek SEO once DNS and env vars match your public issuer
- DNS: `seo.geekatyourspot.com` → Vercel

## DNS for this product

| Host | FQDN | Purpose |
|------|------|---------|
| `seo` | `seo.geekatyourspot.com` | This Next.js app (Vercel) |
| `auth` | `auth.geekatyourspot.com` | geek-OAuth (login only) |
| `seo-api` | `seo-api.geekatyourspot.com` | **GeekSeoBackend** (example hostname — use your Railway custom domain) |

Geek SEO does **not** use `api.geekatyourspot.com` or GeekAPI.

Verify:

```bash
curl -s https://seo-api.geekatyourspot.com/health    # GeekSeoBackend
curl -s https://auth.geekatyourspot.com/health       # geek-OAuth
```

## E2E in CI

- **Smoke (every PR):** `.github/workflows/e2e-smoke.yml` — no secrets.
- **Authenticated (weekly / manual):** `.github/workflows/e2e-authenticated.yml` — add repo secrets `PLAYWRIGHT_TEST_EMAIL` and `PLAYWRIGHT_TEST_PASSWORD` (GeekOAuth user, no 2FA).
- **Local (developer):** `npm run test:e2e:auth:local` — uses `NEXT_PUBLIC_DEV_USER_ID` in `.env.local`; no OAuth password. See `e2e/README.md`.

## Vercel

1. Import `Geek-SEO/frontend`
2. Framework: Next.js
3. Domain: `seo.geekatyourspot.com`

## Environment variables (production)

| Variable | Value |
|----------|--------|
| `NEXT_PUBLIC_SEO_API_URL` | `https://seo-api.geekatyourspot.com` (your GeekSeoBackend URL) |
| `NEXT_PUBLIC_SEO_SIGNALR_URL` | Same as SEO API URL unless split |
| `NEXT_PUBLIC_AUTH_URL` | `https://auth.geekatyourspot.com` |
| `NEXT_PUBLIC_APP_URL` | `https://seo.geekatyourspot.com` |
| `NEXT_PUBLIC_CLIENT_ID` | `geekseo` |
| `NEXT_PUBLIC_REDIRECT_URI` | `https://seo.geekatyourspot.com/auth/callback` |

Do **not** set `NEXT_PUBLIC_DEV_USER_ID` in production.  
Do **not** set `NEXT_PUBLIC_API_URL` to GeekAPI.

Redeploy after any `NEXT_PUBLIC_*` change (build-time embed).

## GeekSeoBackend (Railway / hosting)

Configure on the **GeekSeoBackend** service only. **Do not** set `DATABASE_URL`, `REPO_URL`, or any GeekRepository URL — persistence goes through GeekAPI (`GEEK_API_URL`).

| Variable | Example |
|----------|---------|
| `GEEK_API_URL` | `https://api.geekatyourspot.com` |
| `GEEK_BACKEND_API_KEY` | Same key as on GeekAPI |
| `GEEK_OAUTH_AUTHORITY` | `https://api.geekatyourspot.com` (or your issuer URL) |
| `JWT_AUDIENCE` | `geekseo` |
| `CORS_ORIGINS` | `https://seo.geekatyourspot.com,http://localhost:3000` |
| `DATAFORSEO_LOGIN` / `DATAFORSEO_PASSWORD` | DataForSEO account |
| `ANTHROPIC_API_KEY` | Claude API key |
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` / `GOOGLE_REDIRECT_URI` | Google OAuth for GSC + GA4 backend routes |

See `PROJECT_STATUS.md` and `plan-documents/TODO.md` for capability list and backlog.

## OAuth troubleshooting

| Symptom | Fix |
|---------|-----|
| Sign-in goes to `localhost:3001` in production | Set all `NEXT_PUBLIC_*` vars on Vercel and redeploy |
| OpenIddict `client_id is invalid` (ID2052) | **geek-OAuth** has no `geekseo` client — redeploy **GeekOAuth** (not GeekSeoBackend). Verify `curl` below. Do **not** put `GOOGLE_CLIENT_ID` in `NEXT_PUBLIC_CLIENT_ID`. |
| Auth host NXDOMAIN | CNAME `auth` → geek-OAuth Railway URL |
| API errors / CORS | GeekSeoBackend `CORS_ORIGINS` must include `https://seo.geekatyourspot.com` |

Verify `geekseo` is registered on auth:

```bash
curl -sS "https://auth.geekatyourspot.com/connect/authorize?client_id=geekseo&redirect_uri=https://seo.geekatyourspot.com/auth/callback&response_type=code&scope=openid&code_challenge=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa&code_challenge_method=S256" | head -1
```

If you see `client_id is invalid`, redeploy **GeekOAuth** from `main` and ensure Railway has `CLIENT_SECRET_GEEKAPI` and `CLIENT_SECRET_GEEKWEBSITE` set.

`geekseo` client redirect URIs must include `https://seo.geekatyourspot.com/auth/callback` and local `http://localhost:3000/auth/callback`.
