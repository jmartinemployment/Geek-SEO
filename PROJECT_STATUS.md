# Geek SEO — project status

Last updated: May 31, 2026

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
| Google integrations (GSC + GA4) | ✅ OAuth + data endpoints | ✅ connect on project / rankings / analytics | `GOOGLE_*` on GeekSeoBackend; GSC/GA4 data needs user OAuth + Professional tier |
| Internal link suggestions | ✅ | — | sibling docs in project |
| Site-wide technical audit | ✅ Playwright crawl | ✅ `/app/audit` | Professional tier; Playwright on GeekSeoBackend |
| Copyscape / plagiarism (optional) | ✅ when `COPYSCAPE_*` set | ✅ editor panel | Optional provider; app works without it |
| Usage gates + subscription tier read | ✅ | ✅ pricing + settings | `GET /api/seo/subscription`; operator full access via `SUBSCRIPTION_FULL_ACCESS_EMAILS` |

## Honest gaps (not stubbed — blocked or not built)

| Area | Status |
|------|--------|
| Google Search Console OAuth | **Backend + frontend wired** — connect on project page or Rankings; needs `GOOGLE_*` env on GeekSeoBackend + GeekAPI internal Google routes |
| Google Analytics 4 | **Backend + frontend wired** — same connect flow; data on `/app/analytics` |
| PayPal billing + webhooks | **Sandbox live** — see [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) (sandbox test steps + **Going live later** checklist). Operator: `SUBSCRIPTION_FULL_ACCESS_EMAILS=jmartinemployment@gmail.com` |
| Chrome extension, WP plugin, Google Docs | **Not built** |
| Public API keys for agencies | **Not built** |
| E2E Playwright | **Smoke:** `test:e2e:smoke` + CI. **Google:** `test:integration:google` + `test:e2e:google` + CI. **SEO API:** `test:integration:plagiarism`, `test:integration:site-audit` + CI (`e2e-seo-api-integration.yml`). **Auth local:** `test:e2e:auth:local`. **Auth prod:** `test:e2e:auth` when `PLAYWRIGHT_TEST_*` secrets set |
| Production Railway deploy checklist | docs only |

## Local run

See `scripts/LOCAL_DEV.md`. Minimum: all three backend services + frontend. **Geek-SEO repo configures only GeekSeoBackend** (`GEEK_API_URL`, `GEEK_BACKEND_API_KEY`, provider keys). `DATABASE_URL` / `REPO_*` belong on Jeff’s GeekAPI + GeekRepository only.

If Postgres previously ran migration `0004` that dropped `geek_seo`, restart GeekRepository so EF recreates the schema.

## Plan reference

Master plan: `plan-documents/GEEKSEO-PLAN.md` (31 features / 34 steps). This repo implements the **Surfer-style content core** end-to-end; GSC/GA4/monetization/integrations remain separate workstreams.

Platform decoupling: `plan-documents/PLATFORM-DECOUPLING.md` — **complete** (M0–M9, M1, O2). Optional future: **O1** standalone contracts repo.
