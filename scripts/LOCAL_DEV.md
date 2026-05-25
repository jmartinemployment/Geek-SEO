# Local development

**Plan:** `plan-documents/GEEKSEO-PLAN.md`

## What runs from this repo

| Service | Port | Path |
|---------|------|------|
| Next.js | 3000 | `frontend/` |
| **GeekSeoBackend** | **5051** | **`GeekSeoBackend/`** (add this project — not in GeekBackend) |

## What does not run SEO product code

**GeekAPI** — platform/OIDC and optional **internal DB gateway only**. Never point the Geek SEO frontend at GeekAPI for `/api/seo` or SignalR.

## GeekSeoBackend

```bash
cd GeekSeoBackend
cp .env.example .env
dotnet run
# http://localhost:5051/health
```

Requires GeekRepository on `:5050` and GeekAPI issuer for tokens. In `.env`: `REPO_URL`, `GEEK_OAUTH_AUTHORITY`, `GEEK_SEO_BACKEND_CLIENT_SECRET` (client `geekseo-backend` — not `GEEK_API_CLIENT_SECRET`).

## Frontend

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

`NEXT_PUBLIC_SEO_API_URL=http://localhost:5051` — only valid when GeekSeoBackend in **this repo** is running.

## Auth

geek-OAuth on :3001 — see `scripts/OAUTH_SETUP.md`. Optional `NEXT_PUBLIC_DEV_USER_ID` for dev.

## Data layer (Jeff — GeekBackend)

GeekRepository :5050 with `geek_seo` schema. GeekSeoBackend calls Jeff’s internal HTTP API for persistence — not GeekAPI product routes.
