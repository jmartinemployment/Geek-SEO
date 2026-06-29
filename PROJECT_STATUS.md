# Geek SEO — project status

Last updated: June 14, 2026

**v1 parity #1–27:** checklist complete (June 2026) — matrix below  
**Backlog:** [`plan-documents/TODO.md`](plan-documents/TODO.md) · **Providers:** [`plan-documents/SEO-PROVIDER-STRATEGY.md`](plan-documents/SEO-PROVIDER-STRATEGY.md) · **Index:** [`docs/ROADMAP.md`](docs/ROADMAP.md) · **Niche artifact paradigm:** [`plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md`](plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md)

## Latest on `main`

| Item | Detail |
|------|--------|
| **HEAD** | `110e7cd` — manual niche pipeline fixes (run-step scope, UI poll, Reset hidden) + artifact paradigm doc |
| **New backend APIs** | `GET /api/seo/topical-map/{id}`, `GET/PUT/POST /api/seo/content-guard/*`, `GET/POST/DELETE /api/seo/geo/queries`, `GET /api/seo/geo/queries/{id}/trends` |
| **Workers** | `SeoMaintenanceWorker` — hourly topical refresh, published snapshots, daily GEO probes + Content Guard scan | ✅ `WORKER_SERVICE_USER_ID` set on Railway (see **Identity & worker service account**). **TODO later:** migrate to `@geekatyourspot.com` service account. |
| **GeekRepository** | New `repo/seo/*` controllers + `AddContentGuardTables` EF migration |
| **CI** | `.github/workflows/unit-tests.yml` — Vitest (13) + xUnit (**8**) on push/PR |
| **Unit tests (local)** | ✅ May 31, 2026 — `npm run test` (13) + `dotnet test GeekSEO.slnx` (**8**) + `npm run build` |

## Production (verified May 31, 2026)

| Service | URL | Status |
|---------|-----|--------|
| **GeekSeoBackend** | `seo-api.geekatyourspot.com` | ✅ `/health` OK — `gateway: ok` |
| **Frontend (Vercel)** | `seo.geekatyourspot.com` | ✅ Auto-deploy from `main`; human app audit **0 API 401s** after auth gate |
| **GA4 (live)** | Analytics Data API on GCP `643227070586` | ✅ `GA4_LIVE=1 npm run test:integration:ga4` — landing-pages **200**, 2 rows |
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

Details: platform decoupling **complete** (M0–M9, M1, **O2** legacy auth tables dropped, June 2026). Optional future: **O1** standalone contracts repo.

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
| WordPress connect/publish | ✅ | ✅ project | **Operator has no WP site** — feature code exists; cannot verify publish/draft in production |
| Brand voice CRUD | ✅ | ✅ /app/brand-voice | Postgres |
| Humanize / AI detect / auto-optimize | ✅ | ✅ toolbar | Anthropic |
| Deep SERP analysis | ✅ 50 organic results | ✅ `/app/serp` + CSV export | DataForSEO |
| Google integrations (GSC + GA4) | ✅ OAuth + data endpoints | ✅ connect / rankings / analytics | `GOOGLE_*`; Professional tier for data |
| Internal link suggest + auto-insert | ✅ | ✅ editor panel | sibling docs in project |
| Keyword cannibalization | ✅ GSC analysis | ✅ `/app/cannibalization` | GSC connected |
| Topical map **#12 + #12b** | ✅ v2 generate + GET cached | ✅ `/app/strategy/topical-map` (table + React Flow map) | GSC + DataForSEO SERP seeds; priority, pillars, competitors |
| Niche analyzer | ✅ manual 14-step pipeline | ✅ `/app/strategy/niche-analyzer` | **Step 1 ready to test** — see [artifact paradigm](plan-documents/NICHE-ANALYZER-ARTIFACT-PARADIGM.md#step-1--testing-status-june-2026); Tier A suggestions not built |
| Published content decay audit | ✅ GSC compare + sparkline DB | ✅ `/app/content-guard` | Weekly snapshots via worker when `WORKER_SERVICE_USER_ID` set |
| GEO / AI visibility probe | ✅ probe + query CRUD + 30d trends | ✅ `/app/geo` | DataForSEO; daily worker snapshots enabled queries |
| Content Guard | ✅ decay scan + AI patch in DB | ✅ policy, runs, approve/rollback | GSC for decay; **WP draft N/A** — operator has no WordPress site |
| Content calendar kanban | ✅ status PATCH | ✅ `/app/calendar` | Postgres |
| Site-wide technical audit | ✅ Playwright crawl | ✅ `/app/audit`, `/app/audit/[projectId]` | Professional tier |
| Copyscape / plagiarism (optional) | ✅ when `COPYSCAPE_*` set | ✅ editor panel | Optional provider |
| Usage gates + subscription tier read | ✅ | ✅ pricing + settings | PayPal sandbox live |

## Master plan feature matrix (31 parity features)

Honest status for v1 parity scope (June 2026). Future work: [`TODO.md`](plan-documents/TODO.md).

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
| 12 / 12b | Topical map | ✅ | ✅ | v2 core shipped; V2.2/V2.4 dashboard/V2.5 E2E → TODO |
| 13 | Deep SERP analyzer | ✅ | ✅ | 50 results + CSV + **term matrix heatmap** + 7d cache |
| 14 | Cannibalization | ✅ | ✅ | `/app/cannibalization` |
| 15 | WordPress publish | ✅ | ✅ | **Operator has no WP site** — not verifiable in production |
| 16 | Content calendar | ✅ | ✅ | `/app/calendar` kanban |
| 17 | Guided SMB wizard | ✅ | ✅ | `/app/guided` |
| 18 | Published content audit | ✅ | ✅ | GSC decay + sparkline snapshots in Postgres |
| 19 | Content Guard | ✅ | ✅ | Decay scan + AI patch + runs; **WP draft N/A** (no WordPress site for operator) |
| 20 | GEO / AI visibility | ✅ | ✅ | Query tracking + daily AIO probe worker + 30d trends |
| 21–23 | Dual GEO + E-E-A-T + SERP features in score | ✅ | ✅ | SEO + GEO rings; 6 E-E-A-T advisories; SERP feature guidance |
| 24 | Internal link suggestions | ✅ | ✅ | Editor panel |
| 25 | Plagiarism (Copyscape) | ✅ | ✅ | Optional provider |
| 26 | GA4 | ✅ | ✅ | `/app/analytics` |
| 27 | GSC | ✅ | ✅ | `/app/rankings` |
| 28–31 | WP plugin, Chrome ext, Docs, Public API | — | — | **Not built** — [`docs/ROADMAP.md`](docs/ROADMAP.md) |

## v1 checklist

Parity **#1–27** shipped per matrix above (June 2026). Waivers: #6, #15, #19, #20 — see [`TODO.md`](plan-documents/TODO.md).

## Future work

[`plan-documents/TODO.md`](plan-documents/TODO.md) — #12b polish (V2.2, V2.4 dashboard, V2.5 E2E), #6/#15/#19/#20, #28–31, REDESIGN 2/2b/7, P4 ops, upgrade plan, security.

## Honest gaps (not stubbed — blocked or not built)

| Area | Status |
|------|--------|
| Multi-LLM GEO probes (ChatGPT, Gemini, Perplexity) | Platform status shown; only **Google AIO/organic** probe implemented (DataForSEO) |
| Chrome extension, WP plugin, Google Docs, Public API (#28–31) | Separate repos / not started |
| PayPal production go-live | Sandbox wired — see [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) |
| Production DB migration | ✅ `AddContentGuardTables` applied on GeekRepository Postgres (May 31, 2026) |
| Scheduled workers in production | ✅ `WORKER_SERVICE_USER_ID` set on Railway. **TODO later:** `@geekatyourspot.com` service account |
| Production API regression (May 31) | ✅ Fixed — `UseSnakeCaseNamingConvention` removed from GeekRepository runtime (PascalCase `geek_seo` columns) |
| WordPress publish + Content Guard WP drafts (#15, #19) | **Not testable** — operator no longer has access to a WordPress site. Decay detection, GSC audit, AI patch HTML, and run approve/rollback still work without WP. |
| Content Guard API wiring | ✅ Verified via `npm run test:integration:content-guard` (401, tier gate, policy CRUD when tier allows) |
| E2E Playwright | Smoke + Google + SEO API integration CI; auth local `test:e2e:auth:local` |

## Testing

| Layer | Command | CI workflow |
|-------|---------|-------------|
| Frontend unit (auth) | `cd frontend && npm run test` | `unit-tests.yml` (vitest job) |
| Backend unit | `dotnet test GeekSEO.slnx` | `unit-tests.yml` (dotnet job) |
| Production API (plagiarism status) | `npm run test:integration:plagiarism` | `e2e-seo-api-integration.yml` |
| Production API (content guard gate + 401) | `npm run test:integration:content-guard` | `e2e-seo-api-integration.yml` |
| Google OAuth + GSC/GA4 | `npm run test:integration:google` | `e2e-google-integration.yml` |
| GA4 landing-pages (live) | `GA4_LIVE=1 npm run test:integration:ga4` | `e2e-seo-api-integration.yml` |
| Human full app audit | `npm run test:human:app-audit` | manual / pre-release |
| Playwright smoke | `npm run test:e2e:smoke` | `e2e-smoke.yml` |
| Playwright authenticated | `npm run test:e2e:auth:local` or `test:e2e:auth` | `e2e-authenticated.yml` |

## Local run

See `scripts/LOCAL_DEV.md`. Minimum: GeekSeoBackend + GeekAPI + GeekRepository + frontend. **Geek-SEO repo configures only GeekSeoBackend** (`GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, provider keys). `DATABASE_URL` / `REPO_*` belong on Jeff’s GeekAPI + GeekRepository only.

**Navigation:** `/app/projects/:projectId` redirects to `/app/content?projectId=…` (`frontend/next.config.ts`). Primary sidebar: dashboard, topical map, content, keywords, cannibalization, rankings, audit, analytics; overflow menu includes bulk, SERP, geo, content-guard, content writing, calendar, brand voice.

## Plan reference

**Backlog:** [`TODO.md`](plan-documents/TODO.md). **Providers:** [`SEO-PROVIDER-STRATEGY.md`](plan-documents/SEO-PROVIDER-STRATEGY.md). **Architecture:** [`ARCHITECTURE.md`](../ARCHITECTURE.md).

Platform decoupling — **complete** (M0–M9, M1, O2).

## Session plan closure (May 31, 2026)

| Thread | Status |
|--------|--------|
| Site audit (worker context, detail load, live crawl) | ✅ |
| Topical map v2 (SERP clusters, table/map UI) | ✅ |
| Topical map V2.2 / V2.4 dashboard / V2.5 E2E | → [`TODO.md`](plan-documents/TODO.md) |
| Production auth / SEO API 401 on navigation | ✅ `3b1d98e` |
| GA4 403 (Analytics Data API) | ✅ live test + GCP `643227070586` |
| Human app audit | ✅ 19/19 pages clean (dashboard test-score alerts ignored) |

**v1 master plan:** 100% complete. **Future:** [`plan-documents/TODO.md`](plan-documents/TODO.md).

## Next (prioritized — see roadmap)

Full table and build order: [`docs/ROADMAP.md`](docs/ROADMAP.md).

1. **Before PayPal live:** P0 security fixes — [`docs/CODE-REVIEW.md`](docs/CODE-REVIEW.md).
2. PayPal production go-live — [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md).
3. Migrate worker identity to **`@geekatyourspot.com`** (see **Identity & worker service account** above).
4. Content Guard live smoke without WP (`CONTENT_GUARD_LIVE=1 npm run test:integration:content-guard`).
5. Integration products **#28–31** when scoped (#31 public API first).
6. WP publish / Guard draft E2E when a staging or operator WP host exists.
7. Optional: ChatGPT/Gemini/Perplexity GEO probes (#20 stretch).
