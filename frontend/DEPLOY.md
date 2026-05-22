# Deploy Geek SEO frontend (Vercel)

WordPress is **optional**. Users can write, score, and export HTML without a connected site.

## Prerequisites

- GeekAPI on Railway with `AUTH_SERVER_URL` pointing at geek-OAuth
- `geekseo` OAuth client registered (`scripts/geekseo_oauth_client.sql` + `geek-OAuth/src/oidc/clients.ts`)
- DNS: `seo.geekatyourspot.com` → Vercel (A `76.76.21.21` or CNAME)

## Namecheap DNS (two different Railway services)

Geek SEO uses **two** backends. Do not point both hostnames at GeekAPI.

| Host (Namecheap) | FQDN | Points to (CNAME value) | Service |
|------------------|------|---------------------------|---------|
| `api` | `api.geekatyourspot.com` | `geekbackend-production-41f7.up.railway.app` | **GeekAPI** only |
| `auth` | `auth.geekatyourspot.com` | **geek-OAuth** Railway hostname (not GeekAPI) | **geek-OAuth** OIDC |

Common mistakes:

- **Host `oauth`** creates `oauth.geekatyourspot.com` — the app expects **`auth`**, not `oauth`.
- Pointing `auth` or `oauth` at `geekbackend-production-41f7` sends login traffic to GeekAPI; OIDC lives on geek-OAuth (`/oauth/authorize`, `/.well-known/openid-configuration`).
- CNAME at Namecheap is not enough: in each Railway service → **Settings → Networking → Custom Domain**, add `api.geekatyourspot.com` (GeekAPI) and `auth.geekatyourspot.com` (geek-OAuth) so TLS certificates match.

Verify:

```bash
curl -s https://api.geekatyourspot.com/health          # {"service":"GeekAPI",...}
curl -s https://auth.geekatyourspot.com/health         # {"status":"ok"}  (geek-OAuth)
curl -s https://auth.geekatyourspot.com/.well-known/openid-configuration | head
```

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

After changing any `NEXT_PUBLIC_*` variable, **redeploy** — values are baked in at build time.

### OAuth sign-in troubleshooting

| Symptom | Cause | Fix |
|---------|--------|-----|
| Clicking sign-in goes to `localhost:3001` on `seo.geekatyourspot.com` | Vercel env vars missing; build used defaults | Set all five `NEXT_PUBLIC_*` vars above, then `vercel deploy --prod` |
| `Could not resolve host: auth.geekatyourspot.com` | No DNS for auth subdomain | Add CNAME (or A) for `auth` → your live geek-OAuth host (Railway public URL) |
| `Application not found` from Railway | geek-OAuth service stopped or wrong domain | Redeploy geek-OAuth; set `AUTH_SERVER_URL` to the **same** public URL the browser uses |
| Local: connection refused on :3001 | geek-OAuth not running or on wrong port | `cd ../geek-OAuth && PORT=3001 npm run dev` (see `scripts/OAUTH_SETUP.md`) |

`geekseo` redirect URIs in `geek-OAuth/src/oidc/clients.ts` must include your exact callback URL (production + `http://localhost:3000/auth/callback` for local).

## GeekAPI (Railway)

```
AUTH_SERVER_URL=https://auth.geekatyourspot.com
REPO_URL=...
REPO_API_KEY=...
CORS_ORIGINS=https://seo.geekatyourspot.com,http://localhost:3000
```

`CORS_ORIGINS` is comma-separated (no spaces required). Include every frontend origin that calls the API or SignalR hub — production domain, `localhost:3000` for local dev, and Vercel preview URLs if you use them.

If unset, GeekAPI defaults to `http://localhost:3000` and `https://seo.geekatyourspot.com`.

## GeekRepository (Playwright)

Railway **Geek-Repository** must build with a Playwright Dockerfile (not `Dockerfile.repository`’s old `aspnet` image):

- `GeekBackend/Dockerfile.repository`, or
- `GeekBackend/GeekRepository/Dockerfile`

Both use `mcr.microsoft.com/playwright/dotnet:v1.51.0-noble` and run `playwright.ps1 install chromium` at image build time.

**Quick unblock (no competitor crawl):** `DISABLE_PLAYWRIGHT=true` on Railway — service starts; SERP-only scoring.

**Verify Dockerfile in Railway:** Service → Settings → Build → Dockerfile path = `Dockerfile.repository` (repo root `GeekBackend`).

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
