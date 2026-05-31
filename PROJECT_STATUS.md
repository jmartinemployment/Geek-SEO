# Geek SEO — project status

Last updated: May 31, 2026 (post `2497bfc` on `main`)

## Latest release

| Item | Detail |
|------|--------|
| **Git** | `2497bfc` — `feat(seo): dashboard overview, GSC tools, auth refactor, and unit tests` (pushed to `origin/main`) |
| **New app routes** | `/app/content`, `/app/bulk`, `/app/serp`, `/app/cannibalization`, `/app/content-guard`, `/app/geo`, `/app/audit/[projectId]` |
| **New backend APIs** | `GET /api/seo/dashboard/overview`, topical map, cannibalization, content-audit, geo probe, links auto-insert |
| **Auth** | OAuth modules split under `frontend/src/lib/auth/*`; `UserIdResolver` on backend |
| **CI** | `.github/workflows/unit-tests.yml` — Vitest (13) + xUnit (5) on push/PR |

## Platform decoupling (M2–M7 + M4–M6)

| Phase | Status |
|-------|--------|
| M3 `GeekSeo.Persistence` | ✅ Product-owned schema + migrations |
| M2 `GeekSeo.Application` | ✅ No `GeekApplication` on GeekSeoBackend |
| M7 Product Docker | ✅ No GeekBackend clone |
| M4–M6 Legacy platform auth | ✅ Removed from GeekAPI / GeekRepository / GeekApplication |
| Production | ✅ GeekSeoBackend, GeekAPI, GeekRepository `/health` OK; GeekSeoBackend `gateway: ok` |

Details: [`plan-documents/PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md). **Platform decoupling complete** (M0–M9, M1, **O2** legacy auth tables dropped). Optional future: **O1** standalone contracts repo.

## Identity (geek-OAuth)

Platform login for Geek SEO (**OAuth 2.1 + PKCE** via **geek-OAuth**) is **complete** for production: issuer, `geekseo` client, redirect URIs, and JWT validation on GeekSeoBackend (`GEEK_OAUTH_AUTHORITY`). **Google** OAuth for GSC / GA4 is separate — backend + frontend connect flow is wired; automated checks in `npm run test:integration:google` (CI: `e2e-google-integration.yml`).

**Auth refactor (May 31):** Testable modules — `cookies`, `session-policy`, `token-exchange`, `authorize-url`, `api-headers`. Routes: `/api/auth/start`, `/api/auth/token`, `/api/auth/logout`. Middleware uses `geekseo_refresh` cookie + dev bypass (`NEXT_PUBLIC_DEV_USER_ID`). Unit tests: `npm run test` (Vitest), `dotnet test GeekSEO.slnx` (`GeekSeoBackend.Tests`).

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
| Dashboard overview | ✅ `GET /api/seo/dashboard/overview` | ✅ dashboard + `/app/content` | GeekAPI + Postgres |
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
| Topical map (GSC clusters) | ✅ on-demand generate | ✅ `/app/strategy/topical-map` | GSC connected; no 14d persisted worker yet |
| Published content decay audit | ✅ GSC period compare | ✅ `/app/content-guard` | GSC connected; on-demand (no weekly worker) |
| GEO / AI visibility probe | ✅ on-demand SERP probe | ✅ `/app/geo` | DataForSEO; no daily multi-LLM worker |
| Content calendar kanban | ✅ status PATCH | ✅ `/app/calendar` | Postgres |
| Site-wide technical audit | ✅ Playwright crawl | ✅ `/app/audit`, `/app/audit/[projectId]` | Professional tier |
| Copyscape / plagiarism (optional) | ✅ when `COPYSCAPE_*` set | ✅ editor panel | Optional provider |
| Usage gates + subscription tier read | ✅ | ✅ pricing + settings | PayPal sandbox live |

## Master plan feature matrix (31 parity features)

Honest status against `plan-documents/geekseo-plan.md`. **~22/31 shipped end-to-end** in this repo (May 31, 2026 session).

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
| 12 | Topical map | ✅ | ✅ | GSC on-demand; no 14d TTL worker / DB persist |
| 13 | Deep SERP analyzer | ✅ | ✅ | 50 results + CSV; no term matrix heatmap |
| 14 | Cannibalization | ✅ | ✅ | `/app/cannibalization` |
| 15 | WordPress publish | ✅ | ✅ | Project page + editor |
| 16 | Content calendar | ✅ | ✅ | `/app/calendar` kanban |
| 17 | Guided SMB wizard | ✅ | ✅ | `/app/guided` |
| 18 | Published content audit | ✅ | ✅ | On-demand GSC decay in Content Guard; no weekly worker / sparkline DB |
| 19 | Content Guard | ⚠️ | ⚠️ | Decay scan + recommendations; no auto-patch / WP draft pipeline |
| 20 | GEO / AI visibility | ⚠️ | ⚠️ | On-demand Google AIO probe; no daily multi-LLM worker |
| 21–23 | Dual GEO + E-E-A-T + SERP features in score | ⚠️ | — | Partial scoring only |
| 24 | Internal link suggestions | ✅ | ✅ | Editor panel |
| 25 | Plagiarism (Copyscape) | ✅ | ✅ | Optional provider |
| 26 | GA4 | ✅ | ✅ | `/app/analytics` |
| 27 | GSC | ✅ | ✅ | `/app/rankings` |
| 28–31 | WP plugin, Chrome ext, Docs, Public API | — | — | **Not built** (separate products) |

## Honest gaps (not stubbed — blocked or not built)

| Area | Status |
|------|--------|
| Published content decay audit (#18) | On-demand GSC compare shipped; weekly worker + sparkline DB persist not built |
| Content Guard (#19) | Decay detection shipped; crawl + Claude patch + WP draft flow not built |
| GEO tracker (#20) | On-demand Google AIO/organic probe; daily multi-LLM worker + DB snapshots not built |
| Term matrix / SERP heatmap (#13 full) | Not built |
| Topical map 14d refresh + DB persist | On-demand only today |
| Chrome extension, WP plugin, Google Docs, Public API (#28–31) | Separate repos / not started |
| PayPal production go-live | Sandbox wired — see [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) |
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

See `scripts/LOCAL_DEV.md`. Minimum: all three backend services + frontend. **Geek-SEO repo configures only GeekSeoBackend** (`GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, provider keys). `DATABASE_URL` / `REPO_*` belong on Jeff’s GeekAPI + GeekRepository only.

## Plan reference

Master plan: `plan-documents/geekseo-plan.md` (31 features / 34 steps). Redesign: `plan-documents/REDESIGN-PLAN.md` — Phase 1 shell + Phase 6 audit shipped; dashboard overview + content list + SERP/cannibalization/GEO/content-guard UIs shipped in-repo (on-demand backends; workers/DB persist still open).

Platform decoupling: `plan-documents/PLATFORM-DECOUPLING.md` — **complete** (M0–M9, M1, O2).

## Next (in-repo, highest impact)

1. Deploy **GeekSeoBackend** + **frontend** so `2497bfc` APIs are live in production.
2. Topical map **14d worker + DB persist** (GeekAPI/GeekRepository internal routes if not already deployed).
3. Content Guard **auto-patch + WP draft** pipeline (#19 full).
4. GEO **daily multi-LLM worker + snapshots** (#20 full).
5. Dual SEO+GEO scoring rings in editor (#21–23).
