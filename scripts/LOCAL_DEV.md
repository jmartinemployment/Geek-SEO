# Local development

**Plan:** `plan-documents/GEEKSEO-PLAN.md`

## What runs from this repo

| Service | Port | Path |
|---------|------|------|
| Next.js | 3000 | `frontend/` |
| **GeekSeoBackend** | **5051** | **`GeekSeoBackend/`** |

## GeekSeoBackend

```bash
cd GeekSeoBackend
cp .env.example .env
dotnet run
# http://localhost:5051/health
```

**Do not set `REPO_URL`** on GeekSeoBackend. Data goes through GeekAPI internal routes only.

| Variable | Purpose |
|----------|---------|
| `GEEK_API_URL` | GeekAPI base URL (e.g. `http://localhost:5272` locally) |
| `GEEK_BACKEND_API_KEY` | Same key as GeekAPI `GEEK_BACKEND_API_KEY` (dev internal proxy) |
| `GEEK_OAUTH_AUTHORITY` | Token issuer (production) |
| `DATAFORSEO_LOGIN` / `DATAFORSEO_PASSWORD` | Live SERP (required for real scoring) |
| `ANTHROPIC_API_KEY` | Term extraction + AI writing |
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` / `GOOGLE_REDIRECT_URI` | Google OAuth for GSC + GA4 integration endpoints |

**GeekRepository** must run with `DATABASE_URL` so `geek_seo` migrations apply. GeekAPI needs `REPO_URL` + `REPO_API_KEY` to proxy to the repo.

If `geek_seo` was dropped by an older migration, restart GeekRepository once — EF `InitialSeoSchema` recreates it.

**Background jobs:** `FullArticleJobWorker` runs inside GeekSeoBackend (polls every 5s). `POST /api/seo/writing/full-article` enqueues; poll `GET /api/seo/jobs/{id}`.

## Frontend

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

`NEXT_PUBLIC_SEO_API_URL=http://localhost:5051`

## Auth

geek-OAuth — see `scripts/OAUTH_SETUP.md`. Optional `NEXT_PUBLIC_DEV_USER_ID` for dev.

## GeekBackend (Jeff)

- **GeekAPI:** issuer + `/api/seo/internal/*` gateway; holds `REPO_URL`
- **GeekRepository:** `geek_seo` schema — not called directly from this repo
