# Geek SEO — project status

Last updated: May 31, 2026 (session — worker env + identity documentation)

## Latest on `main`

| Item | Detail |
|------|--------|
| **HEAD** | `91dec03` — prior Vercel Suspense fix; **local uncommitted** — topical map persist, SERP term matrix cache, published audit snapshots, Content Guard pipeline, GEO tracking, dual GEO scoring, maintenance worker |
| **New backend APIs** | `GET /api/seo/topical-map/{id}`, `GET/PUT/POST /api/seo/content-guard/*`, `GET/POST/DELETE /api/seo/geo/queries`, `GET /api/seo/geo/queries/{id}/trends` |
| **Workers** | `SeoMaintenanceWorker` — hourly topical refresh, published snapshots, daily GEO probes + Content Guard scan | ✅ `WORKER_SERVICE_USER_ID` set on Railway (see **Identity & worker service account**). **TODO later:** migrate to `@geekatyourspot.com` service account. |
| **GeekRepository** | New `repo/seo/*` controllers + `AddContentGuardTables` EF migration |
| **CI** | `.github/workflows/unit-tests.yml` — Vitest (13) + xUnit (**8**) on push/PR |
| **Unit tests (local)** | ✅ May 31, 2026 — `npm run test` (13) + `dotnet test GeekSEO.slnx` (**8**) + `npm run build` |

## Production (verified May 31, 2026)

| Service | URL | Status |
|---------|-----|--------|
| **GeekSeoBackend** | `seo-api.geekatyourspot.com` | ✅ `/health` OK — `gateway: ok` |
| **Frontend (Vercel)** | `seo.geekatyourspot.com` | ✅ Deploy `dpl_6LPEYYSD2e9tvws2qi5fhmVdmiaQ` **READY** (`91dec03`); Vercel edge returns **200** |
| **Frontend (alt)** | `geek-seo.vercel.app` | ✅ **200** — confirms build healthy |
| **DNS** | `seo` CNAME | ✅ Authoritative (`dns1.registrar-servers.com`) → `cname.vercel-dns.com`; ⚠️ some resolvers still cache old `j6ftfgyv.up.railway.app` until TTL expires |
| **Deploy note** | — | Frontend auto-deploys from GitHub `main`. Backend redeploys are **manual** on Railway. Smoke-test authenticated `GET /api/seo/dashboard/overview` when convenient. |

## Platform decoupling (M2–M7 + M4–M6)

| Phase | Status |
|-------|--------|
| M3 `GeekSeo.Persistence` | ✅ Product-owned schema + migrations |
| M2 `GeekSeo.Application` | ✅ No `GeekApplication` on GeekSeoBackend |
| M7 Product Docker | ✅ No GeekBackend clone |
| M4–M6 Legacy platform auth | ✅ Removed from GeekAPI / GeekRepository / GeekApplication |
| Production | ✅ GeekSeoBackend + Vercel frontend healthy (May 31, 2026); DNS propagation may lag on some networks |

Details: [`plan-documents/PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md). **Platform decoupling complete** (M0–M9, M1, **O2** legacy auth tables dropped). Optional future: **O1** standalone contracts repo.

## Identity (geek-OAuth)

Platform login for Geek SEO (**OAuth 2.1 + PKCE** via **geek-OAuth**) is **complete** for production: issuer, `geekseo` client, redirect URIs, and JWT validation on GeekSeoBackend (`GEEK_OAUTH_AUTHORITY`). **Google** OAuth for GSC / GA4 is separate — backend + frontend connect flow is wired; automated checks in `npm run test:integration:google` (CI: `e2e-google-integration.yml`).

**Auth refactor (May 31):** Testable modules — `cookies`, `session-policy`, `token-exchange`, `authorize-url`, `api-headers`. Routes: `/api/auth/start`, `/api/auth/token`, `/api/auth/logout`. Middleware uses `geekseo_refresh` cookie + dev bypass (`NEXT_PUBLIC_DEV_USER_ID`). Unit tests: `npm run test` (Vitest), `dotnet test GeekSEO.slnx` (`GeekSeoBackend.Tests`).

## Identity & worker service account

GeekOAuth stores users in **Railway Postgres** (`DATABASE_URL` on the GeekOAuth service), table **`asp_net_users`** — **not** Supabase `auth.users` and not the shared OrderStack Supabase project.

**Production today (single user):**

| Field | Value |
|-------|--------|
| `id` (JWT `sub`, use for `WORKER_SERVICE_USER_ID`) | `92b274f5-2fcb-4935-ba2d-cd8c03e1b21b` |
| `email` / `user_name` | `jmartinemployment@gmail.com` |

Lookup on GeekOAuth DB:

```sql
SELECT id, email, user_name, display_name FROM asp_net_users;
```

All `geek_seo.seo_projects."UserId"` rows currently reference this id.

**TODO (later):** Create a dedicated **`@geekatyourspot.com`** GeekOAuth account for production operations (background workers, full-access bypass, future team seats). Then:

1. Register the new user in GeekOAuth (`asp_net_users`).
2. Re-assign or re-create SEO projects under the new `UserId` (or add a migration path).
3. Update Railway GeekSeoBackend: `WORKER_SERVICE_USER_ID`, and `SUBSCRIPTION_FULL_ACCESS_EMAILS` if used.
4. Re-test `SeoMaintenanceWorker` and subscription bypass after cutover.

Schema reference: `GeekOAuth/src/GeekOAuth.Server/Data/identity_tables.sql`.

## Architecture (target state — implemented)

```
Browser → GeekSeoBackend (:5051)  providers, scoring, workers, SignalR
       → GeekAPI /api/seo/internal/*  authorized data pipe
       → GeekRepository repo/seo/*  Postgres geek_seo
```

GeekSeoBackend does **not** use `REPO_URL`. Providers and scoring run on the product host only.

## Core flows — working when env is configured

| Flow | Backend | Frontend | Needs |
|------|---------|----------|-------|
| Platform login (GeekOAuth) | ✅ JWT validation | ✅ PKCE via GeekOAuth; Next.js `/api/auth/start` + `/api/auth/token` | `NEXT_PUBLIC_AUTH_*`, `GEEK_OAUTH_AUTHORITY` |
| Dashboard overview | ✅ `GET /api/seo/dashboard/overview` | ✅ `/app/dashboard` and `/app/content` (both use overview API) | GeekAPI + Postgres |
| Projects CRUD | ✅ | ✅ | `GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, auth |
| Content editor + live score | ✅ | ✅ | DataForSEO, Anthropic, Playwright (optional) |
| SERP-backed briefs | ✅ | ✅ | DataForSEO |
| Keyword research + cluster | ✅ | ✅ Planner, Keywords | DataForSEO |
| Full article job (brief→outline→draft) | ✅ worker | ✅ guided | Anthropic + DataForSEO |
| Bulk article job | ✅ worker | ✅ `/app/bulk` | Professional tier |
| Competitor crawl + insights | ✅ | ✅ sidebar | Playwright |
| WordPress connect/publish | ✅ | ✅ project | WP app password |
| Brand voice CRUD | ✅ | ✅ /app/brand-voice | Postgres |
| Humanize / AI detect / auto-optimize | ✅ | ✅ toolbar | Anthropic |
| Deep SERP analysis | ✅ 50 organic results | ✅ `/app/serp` + CSV export | DataForSEO |
| Google integrations (GSC + GA4) | ✅ OAuth + data endpoints | ✅ connect / rankings / analytics | `GOOGLE_*`; Professional tier for data |
| Internal link suggest + auto-insert | ✅ | ✅ editor panel | sibling docs in project |
| Keyword cannibalization | ✅ GSC analysis | ✅ `/app/cannibalization` | GSC connected |
| Topical map (GSC clusters) | ✅ generate + GET cached | ✅ `/app/strategy/topical-map` | GSC connected; 14d TTL + `SeoMaintenanceWorker` refresh |
| Published content decay audit | ✅ GSC compare + sparkline DB | ✅ `/app/content-guard` | Weekly snapshots via worker when `WORKER_SERVICE_USER_ID` set |
| GEO / AI visibility probe | ✅ probe + query CRUD + 30d trends | ✅ `/app/geo` | DataForSEO; daily worker snapshots enabled queries |
| Content Guard | ✅ decay scan + AI patch + WP draft | ✅ policy, runs, approve/rollback | Requires WP connection for drafts |
| Content calendar kanban | ✅ status PATCH | ✅ `/app/calendar` | Postgres |
| Site-wide technical audit | ✅ Playwright crawl | ✅ `/app/audit`, `/app/audit/[projectId]` | Professional tier |
| Copyscape / plagiarism (optional) | ✅ when `COPYSCAPE_*` set | ✅ editor panel | Optional provider |
| Usage gates + subscription tier read | ✅ | ✅ pricing + settings | PayPal sandbox live |

## Master plan feature matrix (31 parity features)

Honest status against `plan-documents/geekseo-plan.md`. **~27/31 shipped end-to-end** in this repo (May 31, 2026). Features **#28–31** are separate integration products and remain out of scope.

| # | Feature | Backend | Frontend | Notes |
|---|---------|---------|----------|-------|
| 1 | Live content editor + score | ✅ | ✅ | SignalR optional |
| 2 | Content brief generator | ✅ | ✅ | `/app/briefs/new` |
| 3 | One-click full article | ✅ | ✅ | Guided flow |
| 4 | Bulk articles | ✅ | ✅ | `/app/bulk` |
| 5 | AI humanizer | ✅ | ✅ | Editor AI toolbar |
| 6 | AI content detection | ✅ | ✅ | Editor AI toolbar (not GPTZero-branded) |
| 7 | Auto-optimize | ✅ | ✅ | Editor AI toolbar |
| 8 | Auto internal linking | ✅ | ✅ | `POST /api/seo/links/auto-insert` |
| 9 | Brand voice | ✅ | ✅ | `/app/brand-voice` |
| 10–11 | Planner / topic research | ✅ | ✅ | `/app/planner`, `/app/keywords` |
| 12 | Topical map | ✅ | ✅ | GSC clusters; 14d TTL persist + worker refresh |
| 13 | Deep SERP analyzer | ✅ | ✅ | 50 results + CSV + **term matrix heatmap** + 7d cache |
| 14 | Cannibalization | ✅ | ✅ | `/app/cannibalization` |
| 15 | WordPress publish | ✅ | ✅ | Project page + editor |
| 16 | Content calendar | ✅ | ✅ | `/app/calendar` kanban |
| 17 | Guided SMB wizard | ✅ | ✅ | `/app/guided` |
| 18 | Published content audit | ✅ | ✅ | GSC decay + sparkline snapshots in Postgres |
| 19 | Content Guard | ✅ | ✅ | Auto-patch → WP draft; approve/rollback runs |
| 20 | GEO / AI visibility | ✅ | ✅ | Query tracking + daily AIO probe worker + 30d trends |
| 21–23 | Dual GEO + E-E-A-T + SERP features in score | ✅ | ✅ | SEO + GEO rings; 6 E-E-A-T advisories; SERP feature guidance |
| 24 | Internal link suggestions | ✅ | ✅ | Editor panel |
| 25 | Plagiarism (Copyscape) | ✅ | ✅ | Optional provider |
| 26 | GA4 | ✅ | ✅ | `/app/analytics` |
| 27 | GSC | ✅ | ✅ | `/app/rankings` |
| 28–31 | WP plugin, Chrome ext, Docs, Public API | — | — | **Not built** (separate products) |

## Honest gaps (not stubbed — blocked or not built)

| Area | Status |
|------|--------|
| Multi-LLM GEO probes (ChatGPT, Gemini, Perplexity) | Platform status shown; only **Google AIO/organic** probe implemented (DataForSEO) |
| Chrome extension, WP plugin, Google Docs, Public API (#28–31) | Separate repos / not started |
| PayPal production go-live | Sandbox wired — see [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) |
| Production DB migration | ✅ `AddContentGuardTables` applied on GeekRepository Postgres (May 31, 2026) |
| Scheduled workers in production | ✅ `WORKER_SERVICE_USER_ID=92b274f5-2fcb-4935-ba2d-cd8c03e1b21b` on Railway GeekSeoBackend. **TODO later:** `@geekatyourspot.com` service account |
| E2E Playwright | Smoke + Google + SEO API integration CI; auth local `test:e2e:auth:local` |

## Testing

| Layer | Command | CI workflow |
|-------|---------|-------------|
| Frontend unit (auth) | `cd frontend && npm run test` | `unit-tests.yml` (vitest job) |
| Backend unit | `dotnet test GeekSEO.slnx` | `unit-tests.yml` (dotnet job) |
| Production API (plagiarism status) | `npm run test:integration:plagiarism` | `e2e-seo-api-integration.yml` |
| Production API (site audit gate) | `npm run test:integration:site-audit` | `e2e-seo-api-integration.yml` |
| Google OAuth + GSC/GA4 | `npm run test:integration:google` | `e2e-google-integration.yml` |
| Playwright smoke | `npm run test:e2e:smoke` | `e2e-smoke.yml` |
| Playwright authenticated | `npm run test:e2e:auth:local` or `test:e2e:auth` | `e2e-authenticated.yml` |

## Local run

See `scripts/LOCAL_DEV.md`. Minimum: GeekSeoBackend + GeekAPI + GeekRepository + frontend. **Geek-SEO repo configures only GeekSeoBackend** (`GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, provider keys). `DATABASE_URL` / `REPO_*` belong on Jeff’s GeekAPI + GeekRepository only.

**Navigation:** `/app/projects/:projectId` redirects to `/app/content?projectId=…` (`frontend/next.config.ts`). Primary sidebar: dashboard, topical map, content, keywords, cannibalization, rankings, audit, analytics; overflow menu includes bulk, SERP, geo, content-guard, guided, calendar, brand voice, briefs.

## Plan reference

Master plan: `plan-documents/geekseo-plan.md` (31 features / 34 steps). **In-repo plan complete** for features #1–#27 (May 31, 2026 session). Redesign: `plan-documents/REDESIGN-PLAN.md` — Phase 1 shell + Phase 6 audit shipped.

Platform decoupling: `plan-documents/PLATFORM-DECOUPLING.md` — **complete** (M0–M9, M1, O2).

## Next (highest impact)

1. **TODO (later):** Migrate production identity from personal Gmail to a dedicated **`@geekatyourspot.com`** GeekOAuth account; update `WORKER_SERVICE_USER_ID` and project ownership (see **Identity & worker service account** above).
2. Smoke-test Content Guard end-to-end (decay scan → WP draft → approve) on a connected project.
3. Optional: add ChatGPT/Gemini/Perplexity probe providers when API keys are available (#20 stretch).
4. Separate products #28–31 if/when scoped (WP plugin, Chrome extension, Docs add-on, Public API).
