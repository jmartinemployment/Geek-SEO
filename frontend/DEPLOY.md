# Deploy Geek SEO frontend (Vercel)

WordPress is **optional**. Users can write, score, and export HTML without a connected site.

## Prerequisites

- **GeekSeoBackend** deployed and reachable (SEO REST + SignalR)
- **geek-OAuth** deployed for login (`scripts/geekseo_oauth_client.sql` + `geek-OAuth/src/oidc/clients.ts`)
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

Configure on the **GeekSeoBackend** service (not in this repo), including at minimum:

- `GEEK_SEO_DATABASE_URL` or equivalent Postgres connection
- `AUTH_SERVER_URL=https://auth.geekatyourspot.com`
- `CORS_ORIGINS=https://seo.geekatyourspot.com,http://localhost:3000`
- Provider keys: `DATAFORSEO_*`, `ANTHROPIC_API_KEY`, Playwright as required

See `plan-documents/GEEKSEO-PLAN.md` for full capability list.

## OAuth troubleshooting

| Symptom | Fix |
|---------|-----|
| Sign-in goes to `localhost:3001` in production | Set all `NEXT_PUBLIC_*` vars on Vercel and redeploy |
| Auth host NXDOMAIN | CNAME `auth` → geek-OAuth Railway URL |
| API errors / CORS | GeekSeoBackend `CORS_ORIGINS` must include `https://seo.geekatyourspot.com` |

`geekseo` client redirect URIs must include `https://seo.geekatyourspot.com/auth/callback` and local `http://localhost:3000/auth/callback`.
