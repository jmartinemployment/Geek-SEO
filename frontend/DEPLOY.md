# Deploy Geek SEO frontend (Vercel)

WordPress is **optional**. Users can write, score, and export HTML without a connected site.

## Prerequisites

- GeekAPI on Railway with `AUTH_SERVER_URL` pointing at geek-OAuth
- `geekseo` OAuth client registered (`scripts/geekseo_oauth_client.sql` + `geek-OAuth/src/oidc/clients.ts`)
- DNS: `seo.geekatyourspot.com` → Vercel (A `76.76.21.21` or CNAME)

## Vercel project

1. Import repo root: `Geek-SEO/frontend`
2. Framework: Next.js (auto-detected)
3. Production domain: `seo.geekatyourspot.com`

## Environment variables (Production)

| Variable | Value |
|----------|--------|
| `NEXT_PUBLIC_API_URL` | `https://api.geekatyourspot.com` |
| `NEXT_PUBLIC_AUTH_URL` | `https://auth.geekatyourspot.com` (geek-OAuth) |
| `NEXT_PUBLIC_APP_URL` | `https://seo.geekatyourspot.com` |
| `NEXT_PUBLIC_CLIENT_ID` | `geekseo` |
| `NEXT_PUBLIC_REDIRECT_URI` | `https://seo.geekatyourspot.com/auth/callback` |

Do **not** set `NEXT_PUBLIC_DEV_USER_ID` in production.

## GeekAPI (Railway)

```
AUTH_SERVER_URL=https://auth.geekatyourspot.com
REPO_URL=...
REPO_API_KEY=...
```

CORS must include `https://seo.geekatyourspot.com`.

## GeekRepository (Playwright)

Dockerfile: `GeekBackend/GeekRepository/Dockerfile` uses `mcr.microsoft.com/playwright/dotnet` (includes Chromium).

Railway: point the GeekRepository service at that Dockerfile, or set `DISABLE_PLAYWRIGHT=true` until the image is deployed.

Local install:

```bash
cd GeekBackend/GeekRepository && dotnet build
pwsh bin/Debug/net*/playwright.ps1 install chromium
```

## AI article generation (Guided Mode)

Set on **GeekRepository**:

```
ANTHROPIC_API_KEY=sk-ant-...
```
