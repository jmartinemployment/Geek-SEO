# Geek SEO — project status

Last updated: May 25, 2026

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
| Internal link suggestions | ✅ | — | sibling docs in project |
| Usage gates + subscription tier read | ✅ | partial | subscription row in DB |

## Honest gaps (not stubbed — blocked or not built)

| Area | Status |
|------|--------|
| Google Search Console OAuth | **Not built** — rankings, topical map, content guard UI show integration-required |
| Google Analytics 4 | **Not built** |
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
