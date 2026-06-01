# Local development

**Backlog:** `plan-documents/TODO.md`  
**Decoupling:** `plan-documents/PLATFORM-DECOUPLING.md` (M2 = in-repo contracts; M7 = no Docker GeekBackend clone)

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
| `GOOGLE_PSI_API_KEY` | Optional — PageSpeed scores on `GET /api/public/scan` (homepage free scan) |
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` / `GOOGLE_REDIRECT_URI` | Google OAuth for GSC + GA4 integration endpoints |

**GeekRepository** must run with `DATABASE_URL` so `geek_seo` migrations apply. GeekAPI needs `REPO_URL` + `REPO_API_KEY` to proxy to the repo.

If `geek_seo` was dropped by an older migration, restart GeekRepository once — EF `InitialSeoSchema` recreates it.

**Schema changes (product-owned):** run `dotnet ef` from Geek-SEO against `GeekSeo.Persistence` only (see `GeekSeo.Persistence/CLAUDE.md` — no `--startup-project GeekSeoBackend`). GeekRepository applies the same migrations at startup.

**GeekBackend Railway:** GeekAPI uses `./Dockerfile`; GeekRepository uses `./Dockerfile.repository` + `railway.geekrepository.toml` — do not add a root `railway.toml` that applies to both.

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

**GeekOAuth** (issuer) — see `scripts/OAUTH_SETUP.md`. Frontend PKCE uses Next.js routes `/api/auth/start` and `/api/auth/token` (not GeekAPI `/api/auth/*`). Optional `NEXT_PUBLIC_DEV_USER_ID` for dev.

## Billing (PayPal)

Production is on **sandbox** checkout (test payments only). Full runbook:

- [`docs/PAYPAL-BILLING.md`](../docs/PAYPAL-BILLING.md) — sandbox test steps, operator email, **going live later** checklist

One-time setup per PayPal environment (from `GeekSeoBackend/` with Railway linked):

```bash
railway run -- node ../scripts/paypal-create-subscription-plans.mjs
railway run -- node ../scripts/paypal-create-webhook.mjs
```

Or: `bash scripts/paypal-setup-sandbox.sh` (same commands).

GeekSeoBackend env: `PAYPAL_*`, `SUBSCRIPTION_FULL_ACCESS_EMAILS` (your login = full access without subscribing).

**Plagiarism (Copyscape):** Optional — `COPYSCAPE_USERNAME`, `COPYSCAPE_API_KEY` on GeekSeoBackend. Optional `COPYSCAPE_SPEND_LIMIT_USD` (default `0.50` per check). Without credentials the editor shows “not configured” and publishing is unaffected. Verify: `npm run test:integration:plagiarism`. Optional live API call: `COPYSCAPE_LIVE_CHECK=1 npm run test:integration:plagiarism`.

**Site audit:** `/app/audit` (Professional tier). Requires Playwright on GeekSeoBackend (not `DISABLE_PLAYWRIGHT=true`). Verify: `npm run test:integration:site-audit`. Live crawl: `SITE_AUDIT_LIVE=1 npm run test:integration:site-audit` (requires Professional+ tier for `INTEGRATION_USER_ID` — add dev user to `SUBSCRIPTION_FULL_ACCESS_USER_IDS` on Railway or use operator email in `SUBSCRIPTION_FULL_ACCESS_EMAILS`).

## Unit tests

```bash
# From repo root
dotnet test GeekSEO.slnx

cd frontend
npm run test    # Vitest — auth modules (13 tests)
```

CI: `.github/workflows/unit-tests.yml` on push/PR to `main`.

## E2E (frontend)

```bash
cd frontend
npm run test:e2e:smoke
npm run test:integration:plagiarism
npm run test:integration:site-audit
npm run test:e2e:auth:local   # needs GeekSeoBackend :5051 + GEEK_API_URL + GEEK_BACKEND_API_KEY for content/plagiarism test
```

Without a working data gateway locally, `test:e2e:auth:local` still passes but **skips** the content-editor plagiarism panel test (gateway probe returns 400). Production integration scripts hit Railway directly and do not need local GeekAPI.

## GeekBackend (sibling repo — E2E only)

**Runtime E2E:** GeekAPI + GeekRepository must run for persistence (GeekSeoBackend has no `DATABASE_URL`).

**Product build:** `dotnet build GeekSEO.slnx` from repo root — in-repo `GeekSeo.Application` + `GeekSeo.Persistence` only.

**Product Railway deploy:** root `Dockerfile` builds from Geek-SEO only (PLATFORM-DECOUPLING M7). GeekRepository deploy pins **`Geek-SEO.commit`** in the GeekBackend repo when `GeekSeo.Persistence` changes.

## GeekBackend platform roles

| Service | Role |
|---------|------|
| **GeekOAuth** | Login + JWT issuance (`GEEK_OAUTH_AUTHORITY`) |
| **GeekAPI** | `/api/seo/internal/*` gateway; holds `REPO_URL` — **not** the issuer |
| **GeekRepository** | Only service with DB access to schema **`geek_seo`** |
