# Geek SEO — project status

Last updated: May 30, 2026

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

Platform login for Geek SEO (**OAuth 2.1 + PKCE** via **geek-OAuth**) is **complete** for production: issuer, `geekseo` client, redirect URIs, and JWT validation on GeekSeoBackend (`GEEK_OAUTH_AUTHORITY`). This is unrelated to **Google** OAuth for GSC / GA4 integrations (still not built).

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
| Platform login (GeekOAuth) | ✅ JWT validation | ✅ PKCE via GeekOAuth; Next.js `/api/auth/start` + `/api/auth/token` (local routes, not GeekAPI) | `NEXT_PUBLIC_AUTH_*`, `GEEK_OAUTH_AUTHORITY` |
| Projects CRUD | ✅ | ✅ | `GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, auth |
| Content editor + live score | ✅ | ✅ | DataForSEO, Anthropic, Playwright (optional) |
| SERP-backed briefs | ✅ | ✅ | DataForSEO |
| Keyword research + cluster | ✅ | ✅ Planner, Keywords | DataForSEO |
| Full article job (brief→outline→draft) | ✅ worker | ✅ guided | Anthropic + DataForSEO |
| Bulk article job (up to 20 kw) | ✅ worker | API only | same |
| Competitor crawl + insights | ✅ | ✅ sidebar | Playwright |
| WordPress connect/publish | ✅ | ✅ project | WP app password |
| Brand voice CRUD | ✅ | ✅ /app/brand-voice | Postgres |
| Humanize / AI detect / auto-optimize | ✅ | ✅ toolbar | Anthropic |
| Deep SERP analysis | ✅ `GET /api/seo/serp/deep` | — | DataForSEO |
| Google integrations (GSC + GA4) | ✅ OAuth + data endpoints | partial (`IntegrationRequired` pages not yet wired) | Google OAuth env vars + GeekAPI internal Google routes |
| Internal link suggestions | ✅ | — | sibling docs in project |
| Usage gates + subscription tier read | ✅ | partial | subscription row in DB |

## Honest gaps (not stubbed — blocked or not built)

| Area | Status |
|------|--------|
| Google Search Console OAuth | **Backend implemented in GeekSeoBackend** (`/api/seo/integrations/google/*`, `/api/seo/rankings/{projectId}`) — frontend wiring and GeekAPI internal Google routes must be live |
| Google Analytics 4 | **Backend implemented in GeekSeoBackend** (`/api/seo/analytics/ga4/{projectId}/landing-pages`) — frontend wiring and internal Google routes must be live |
| PayPal billing + webhooks | **Not built** |
| Site-wide technical audit crawl | **Not built** |
| Copyscape / plagiarism | **Not built** |
| Chrome extension, WP plugin, Google Docs | **Not built** |
| Public API keys for agencies | **Not built** |
| E2E Playwright test suite | **Not built** |
| Production Railway deploy checklist | docs only |

## Local run

See `scripts/LOCAL_DEV.md`. Minimum: all three backend services + frontend. **Geek-SEO repo configures only GeekSeoBackend** (`GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, provider keys). `DATABASE_URL` / `REPO_*` belong on Jeff’s GeekAPI + GeekRepository only.

If Postgres previously ran migration `0004` that dropped `geek_seo`, restart GeekRepository so EF recreates the schema.

## Plan reference

Master plan: `plan-documents/GEEKSEO-PLAN.md` (31 features / 34 steps). This repo implements the **Surfer-style content core** end-to-end; GSC/GA4/monetization/integrations remain separate workstreams.

Platform decoupling: `plan-documents/PLATFORM-DECOUPLING.md` — **M3** `GeekSeo.Persistence` (schema in this repo) **implemented**; **M2/M7** next; **O1/O2** optional.
