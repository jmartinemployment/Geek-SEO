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

See `plan-documents/GEEKSEO-PLAN.md` for full capability list.

## OAuth troubleshooting

| Symptom | Fix |
|---------|-----|
| Sign-in goes to `localhost:3001` in production | Set all `NEXT_PUBLIC_*` vars on Vercel and redeploy |
| Auth host NXDOMAIN | CNAME `auth` → geek-OAuth Railway URL |
| API errors / CORS | GeekSeoBackend `CORS_ORIGINS` must include `https://seo.geekatyourspot.com` |

`geekseo` client redirect URIs must include `https://seo.geekatyourspot.com/auth/callback` and local `http://localhost:3000/auth/callback`.
