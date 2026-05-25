# Geek SEO — Master Implementation Plan

**Date:** May 2026  
**Status:** Canonical. All implementation follows this document.

---

## Product goal

Clone the core workflows of **Surfer SEO**, **Semrush ContentShake AI**, and **Frase.io** into a commercial SaaS at **$29–$149/mo** for small businesses and freelance SEOs.

**Primary loop:**  
`keyword research → content brief → AI draft → real-time SEO scoring → publish → monitor decay → AI visibility tracking`

**Differentiators vs competitors:**
- Transparent 6-component SEO score (competitors use black-box scores)
- Local SERP targeting on every tier
- WordPress-native publish (dogfood on geekatyourspot.com before external launch)
- $29–$149 vs Surfer $99–$219, Frase $49–$299, ContentShake $60+

---

## Clone scope — 31 parity features

| # | Feature | Cloned from |
|---|---------|-------------|
| 1 | Real-time content editor with live SEO score (transparent 6-component, SignalR) | Surfer Content Editor |
| 2 | Content brief generator (SERP → PAA → headings → word count → term targets) | Surfer + Frase |
| 3 | One-click full article (keyword → SERP → brief → outline → draft → score, async &lt; 3 min p95) | Surfer AI + ContentShake |
| 4 | Bulk article generation (up to 100 keywords/job, max 3 parallel pipelines) | Surfer Programmatic SEO + ContentShake |
| 5 | AI humanizer (remove AI detection patterns, preserve brand voice, re-score) | Surfer Humanizer |
| 6 | AI content detection (GPTZero badge, flagged sentences, Humanize CTA) | Surfer + Frase |
| 7 | Auto-optimize (top 5 missing terms, max 1/paragraph, undo) | Surfer Auto-Optimize |
| 8 | Auto internal linking (inventory scan, auto-insert top suggestion on request) | Surfer Smart Internal Linking |
| 9 | Brand voice profiles (samples → tone → prepend in every AI prompt) | Surfer Custom Voice + ContentShake |
| 10 | Content Planner — mode=full: seed → 15–40 clusters | Surfer Keyword Research |
| 11 | Topic Research — mode=quick: seed → 5–15 clusters | Surfer Topic Research |
| 12 | Topical Map (GSC, covered/gap/partial, 14d refresh) | Surfer Sites / Topic Explorer |
| 13 | Deep SERP analyzer (50 results, term matrix, CSV export) | Surfer SERP Analyzer |
| 14 | Keyword cannibalization (GSC conflicts → merge/canonical guidance) | Surfer Sites |
| 15 | WordPress REST publish (Application Password, AES-256-GCM) | ContentShake + Surfer WP |
| 16 | Content calendar (kanban: planned / writing / review / published) | ContentShake Board |
| 17 | Guided SMB wizard (6 steps through publish) | ContentShake flow |
| 18 | Published content audit (GSC decay, weekly, sparkline 30d) | Surfer Content Audit |
| 19 | Content Guard (decay → crawl → patch → WP draft → approve/rollback) | Frase Content Guard |
| 20 | Multi-LLM AI visibility tracker (daily probes, mention rate, citations) | Surfer AI Tracker + Frase GEO |
| 21 | Dual SEO + GEO scores in editor sidebar (one SignalR round-trip) | Frase dual scoring |
| 22 | E-E-A-T advisory layer (6 codes, non-scored, in ScoreUpdate) | Surfer E-E-A-T |
| 23 | SERP feature guidance in ScoreUpdate (actionable copy per feature type) | Surfer |
| 24 | Internal link suggestions panel (max 10, insert-at-cursor; manual pick) | Surfer + Clearscope |
| 25 | Plagiarism check (Copyscape; block wizard publish if &gt; 15% match) | Surfer |
| 26 | Google Analytics 4 (OAuth, landing pages + document scores) | ContentShake |
| 27 | GSC integration (OAuth, rankings, topical map, guard triggers) | Surfer Sites |
| 28 | WordPress plugin (score column, editor sidebar, deep link) | Surfer + ContentShake WP |
| 29 | Chrome extension MV3 (SERP keyword popup, WP editor score overlay) | Surfer Keyword Surfer |
| 30 | Google Docs add-on (sidebar score + top 5 suggestions) | Surfer Google Docs |
| 31 | ChatGPT Actions OpenAPI + public REST API + Agency API keys (120 req/min) | Surfer ChatGPT + NeuronWriter API |

---

## Out of scope (not in this product)

| Capability | Reason |
|------------|--------|
| Surfer True Density | Transparent term coverage + heading score instead |
| Semrush/Ahrefs/Moz proprietary keyword DB | DataForSEO covers the same data |
| Frase 80-skill agent / MCP mesh | ChatGPT Actions (#31) is the integration surface |
| Multi-seat RBAC, SSO, team invites | Solo operator; Team/Agency = volume caps |
| Zapier, Mailchimp, Social Poster, Shopify | WordPress-first |
| 170+ languages | English-first SMB scope |
| MarketMuse inventory / Rankability coaching | Enterprise scope |
| Surfer Sites full PM (time tracking, calendar sync) | ContentShake kanban is sufficient |

---

## Architecture rules

**This repo (Geek-SEO)** owns the product: **frontend** + **GeekSeoBackend** (not in GeekBackend).

**GeekAPI is never the SEO product host.** Planning rule from day one: GeekAPI is platform/OIDC and (if used at all) an **internal data gateway** to Postgres — not `/api/seo` for browsers, not SignalR scoring, not SERP/AI.

```
Browser → GeekSeoBackend (this repo, :5051) → [internal data HTTP] → GeekRepository → Postgres (geek_seo)
```

GeekAPI may sit between GeekSeoBackend and GeekRepository **only for DB access** (Jeff’s gateway). It is **not** part of the Geek SEO product surface.

| Rule | Violation |
|------|-----------|
| All SEO product logic in **GeekSeoBackend** under **Geek-SEO** | SEO controllers/hubs on GeekAPI |
| Browser calls **GeekSeoBackend** only | Frontend `fetch` to GeekAPI for SEO |
| GeekAPI used for **data persistence gateway** only (optional internal routes) | GeekAPI hosts scoring, SERP, editor APIs |
| Real-time scores via SignalR on **GeekSeoBackend** | SignalR on GeekAPI |
| geek-OAuth for login; GeekSeoBackend validates JWT | OAuth business logic duplicated in GeekAPI for SEO |

**Local ports:** geek-OAuth 3001 · GeekSeoBackend 5051 (this repo, when built) · Next.js 3000 · GeekRepository 5050 (GeekBackend, data only)

**Scoring formulas:** [`geekseo-content-scoring-spec.md`](geekseo-content-scoring-spec.md)

---

## Pricing tiers

| Tier | Price | Key limits |
|------|-------|------------|
| Starter | $29/mo | 20 docs, 3 full articles, 5 deep SERP, 10 plagiarism, 1 brand voice, 10 humanize, 20 auto-optimize, no GSC/GA4 |
| Professional | $59/mo | 60 docs, 15 full articles, 30 deep SERP, 50 plagiarism, 3 brand voices, GSC + GA4, topical map 2/mo, 5 GEO queries, content audit |
| Team | $89/mo | 150 docs, 40 full articles, 100 deep SERP, bulk 3/mo, content guard 30 runs, 20 GEO queries |
| Agency | $149/mo | Unlimited caps, public API + API keys, white-label PDF reports |

Caps enforced by `UsageLimits.cs` + `SeoUsageGateMiddleware`. COGS target ≥ 70% gross margin on Professional at 20 full-article equivalents/mo.

---

## Database — schema `geek_seo` (25 tables)

**Instance:** Supabase `mpnruwauxsqbrxvlksnf` · **Access:** GeekRepository only (Jeff, GeekBackend) · **Tenancy:** `user_id` on every row · GeekSeoBackend never opens a DB connection string

Core: `seo_projects`, `seo_content_documents`, `seo_content_briefs`, `seo_background_jobs`  
SERP: `seo_serp_results`, `seo_competitor_pages`, `seo_serp_deep_cache`  
Planning: `seo_keyword_clusters`, `seo_topical_maps`, `seo_topical_map_clusters`  
GSC/GA4: `seo_gsc_connections`, `seo_gsc_rankings`, `seo_ga4_connections`  
Monitoring: `seo_published_page_audits`, `seo_geo_tracking_queries`, `seo_geo_mention_snapshots`  
Guard: `seo_content_guard_policies`, `seo_content_guard_runs`  
Publish: `seo_wordpress_connections`, `seo_site_page_inventory`  
Other: `seo_brand_voices`, `seo_subscriptions`, `seo_usage_meters`, `seo_api_keys`, `seo_reports`

Full column definitions are in the `geek_seo` schema migration (`InitialSeoSchema`); new tables follow the same snake_case + `user_id` pattern.

---

## API surface (GeekSeoBackend)

**Envelope:** `{ success, data }` or `{ success: false, error: { code, message, details } }`  
**Statuses:** 401 auth · 402 tier · 403 ownership · 429 cap · 503 provider down

**Routes:** `/api/seo/projects`, `/content`, `/writing/*`, `/briefs/generate`, `/keywords/*`, `/serp`, `/serp/deep`, `/topical-map/*`, `/content-audit/*`, `/content-guard/*`, `/geo/*`, `/cannibalization/*`, `/links/suggest`, `/plagiarism/check`, `/brand-voice`, `/gsc/*`, `/rankings/*`, `/analytics/ga4/*`, `/wordpress/*`, `/subscription`, `/jobs/{id}`, `/reports/{id}`, `/openapi.json`, `/api-keys`, `/api/seo/v1/*` (Agency + `X-Api-Key`)

**SignalR** `/hubs/seo-scoring`: `JoinDocument`, `ContentChanged`, `KeywordChanged` → `ScoreUpdate`, `BenchmarkRefreshing`, `Error`

---

## Environment variables

**GeekSeoBackend** (deployed separately — not configured in this repo): provider keys, DB URL, OAuth authority, `CORS_ORIGINS` including `https://seo.geekatyourspot.com`.

**Frontend (this repo):** `NEXT_PUBLIC_SEO_API_URL`, `NEXT_PUBLIC_SEO_SIGNALR_URL`, OAuth vars (`NEXT_PUBLIC_OAUTH_*` / auth config), optional `NEXT_PUBLIC_DEV_USER_ID` for local dev only.

---

## Implementation steps

Complete steps in order. Each step is **done** only when every **Done when** bullet is verified (manual or automated). No step is complete with placeholder UI, fake API responses, or in-memory data stores.

---

### Step 1 — Parity #1: Real-time content editor with live SEO score

**Cloned from:** Surfer Content Editor

**Done when:**
- User signs in, creates a project and content document; rows persist in `geek_seo` and reload after refresh
- Editor page loads HTML; typing triggers `ScoreUpdate` over SignalR within p95 &lt; 2.5s on warm SERP cache
- First keyword on a document fetches SERP + competitor crawls, caches 24h/72h, shows loading copy until benchmarks exist
- Score shows 6 components + grade; suggestions sorted by point value
- Tier and usage gates return 402/429 with `requiredTier` / meter details
- Frontend uses `NEXT_PUBLIC_SEO_API_URL` only (GeekSeoBackend)

**Build — GeekSeoBackend (this repo, `GeekSeoBackend/`):** .NET host :5051, `/api/seo/*`, `/hubs/seo-scoring`, scoring/SERP/Playwright/AI, persistence via internal HTTP to Jeff’s data gateway (not public SEO on GeekAPI).

**Build — GeekBackend (Jeff only):** GeekRepository `geek_seo` schema/migrations; optional thin GeekAPI **internal** routes for CRUD — **no** product SEO.

**Build — frontend (this repo):** wired to GeekSeoBackend only; Step 1 UI exists but **blocked** until `GeekSeoBackend/` exists.

### Session notes

**May 24, 2026:** Planning always excluded GeekAPI as SEO product host. Agent incorrectly documented/ran against GeekAPI :8080/:5272 and claimed Step 1 progress without `GeekSeoBackend/` in this repo. Frontend shell only until backend is added here.

---

### Step 2 — Parity #2: Content brief generator

**Cloned from:** Surfer + Frase brief flow

**Done when:**
- `POST /api/seo/briefs/generate` returns PAA, heading targets, word count, top-20 terms from live SERP data
- Brief saves to `seo_content_briefs` and opens in editor with targets applied
- Usage meter `content_brief` increments; cap enforced per tier

**Build:** `ContentBriefService`, brief controller, brief UI (`/app/briefs/new`), wire from editor

---

### Step 3 — Parity #3: One-click full article

**Cloned from:** Surfer AI + ContentShake generate article

**Done when:**
- `POST /api/seo/writing/full-article` enqueues job; poll `GET /api/seo/jobs/{id}` until `completed`
- Pipeline: brief → outline → draft → score; p95 &lt; 3 minutes on warm cache
- Result document opens in editor with score ≥ 40 or explicit failure `error_message` on job row

**Build:** `AIWritingService`, `ClaudeProvider`, `BackgroundJobWorker` (continuous poll), job UI progress

---

### Step 4 — Parity #4: Bulk article generation

**Cloned from:** Surfer Programmatic SEO + ContentShake bulk

**Done when:**
- `POST /api/seo/writing/bulk` accepts up to 100 keywords; max 3 documents generating in parallel
- Each child job visible in jobs API; failures isolated per keyword

**Build:** bulk job type in worker, bulk entry UI, tier cap `bulk_job`

---

### Step 5 — Parity #5: AI humanizer

**Cloned from:** Surfer Humanizer

**Done when:**
- `POST /api/seo/writing/humanize` rewrites content, re-scores, pushes `ScoreUpdate`
- Brand voice from step 9 prepended when default voice exists

**Build:** `HumanizeAsync` in `AIWritingService`, editor toolbar action, meter `humanize`

---

### Step 6 — Parity #6: AI content detection

**Cloned from:** Surfer + Frase detection

**Done when:**
- `POST /api/seo/writing/detect` returns GPTZero probability and flagged spans
- Editor shows badge + Humanize CTA

**Build:** `GPTZeroProvider`, `AiDetectionService`, detection UI

---

### Step 7 — Parity #7: Auto-optimize

**Cloned from:** Surfer Auto-Optimize

**Done when:**
- `POST /api/seo/content/{id}/auto-optimize` inserts top 5 missing terms (max 1 per paragraph), returns before/after score and change list
- Undo restores prior HTML in editor

**Build:** `AutoOptimizeAsync`, toolbar button, meter `auto_optimize`

---

### Step 8 — Parity #8: Auto internal linking

**Cloned from:** Surfer Smart Internal Linking

**Done when:**
- On user request, service scans `seo_site_page_inventory`, ranks by topic similarity, auto-inserts top link into HTML
- Distinct from #24 manual panel

**Build:** `InternalLinkService` auto-insert endpoint, editor action

---

### Step 9 — Parity #9: Brand voice profiles

**Cloned from:** Surfer Custom Voice + ContentShake

**Done when:**
- CRUD `seo_brand_voices`; one default per user; all AI writes prepend voice block
- Tier limits on voice count enforced

**Build:** `BrandVoiceService`, `/api/seo/brand-voice`, settings UI

---

### Step 10 — Parity #10–11: Content Planner and Topic Research

**Cloned from:** Surfer Keyword Research + Topic Research

**Done when:**
- `POST /api/seo/keywords/clusters` with `mode=full` returns 15–40 clusters; `mode=quick` returns 5–15
- `/app/planner` shows grid, filters, sort; row action creates document + brief and opens editor

**Build:** `KeywordClusteringService`, `ContentPlannerService`, `DataForSEOKeywordProvider`, planner page

---

### Step 11 — Parity #27: GSC integration

**Cloned from:** Surfer Sites (search console)

**Done when:**
- OAuth connect per project; tokens encrypted in `seo_gsc_connections`
- `POST /api/seo/rankings/{projectId}/sync` populates `seo_gsc_rankings`
- `/app/rankings` table with 90d trend per keyword

**Build:** `GscService`, `GscSyncWorker` (daily 05:00 UTC), GSC controllers, rankings page

---

### Step 12 — Parity #12: Topical Map

**Cloned from:** Surfer Domain Map / Topic Explorer

**Done when:**
- Requires connected GSC; `POST /api/seo/topical-map/generate` produces covered/gap/partial clusters; 14d TTL; 1 refresh/24h/project
- `/app/strategy/topical-map` visualization + “Write this” creates document

**Build:** `TopicalMapService`, topical map worker job type, topical map page

---

### Step 13 — Parity #13: Deep SERP analyzer

**Cloned from:** Surfer SERP Analyzer

**Done when:**
- `GET /api/seo/serp/deep` returns 50 results + term matrix; 7d cache in `seo_serp_deep_cache`
- `/app/serp/[keyword]` table, heatmap, CSV export; no auto-navigate from editor

**Build:** `SerpDeepAnalyzerService`, deep SERP page, meter `deep_serp`

---

### Step 14 — Parity #14: Keyword cannibalization

**Cloned from:** Surfer Sites cannibalization

**Done when:**
- `GET /api/seo/cannibalization/{projectId}` lists GSC queries with 2+ URLs (impressions &gt; 10) and canonical/merge guidance

**Build:** `CannibalizationService`, strategy UI section

---

### Step 15 — Parity #15: WordPress REST publish

**Cloned from:** ContentShake + Surfer WP publish

**Done when:**
- Connect with Application Password (encrypted); publish creates/updates WP post; `seo_site_page_inventory` updated; `published_url` on document
- 401 returns `WP_AUTH_FAILED` without retrying bad credentials

**Build:** `WordPressService`, connect/publish UI in settings + editor

---

### Step 16 — Parity #16: Content calendar

**Cloned from:** ContentShake Board

**Done when:**
- `/app/calendar` kanban with drag-drop across planned / writing / review / published
- Card shows live `seo_score`; PATCH status persists

**Build:** calendar board UI, status PATCH wired

---

### Step 17 — Parity #17: Guided SMB wizard

**Cloned from:** ContentShake guided flow

**Done when:**
- Six steps: business context → keyword → full article job → review → score checklist (pass/fail per component) → publish
- Publish blocked if plagiarism &gt; 15% (after step 25)

**Build:** complete `/app/guided` wizard screens

---

### Step 18 — Parity #18: Published content audit

**Cloned from:** Surfer Content Audit

**Done when:**
- Register published URLs; weekly worker updates recommendations; `/app/audit` shows sparkline and refresh/merge/canonical badges

**Build:** `ContentAuditService`, `PublishedAuditWorker` (Sun 06:00 UTC), audit page

---

### Step 19 — Parity #19: Content Guard

**Cloned from:** Frase Content Guard

**Done when:**
- Policy CRUD; enable per document; daily worker detects decay (position −5 or clicks −25% vs 28d avg); fix job crawls live URL, Claude patch, WP draft
- User approves or rolls back from `/app/content-guard`; `pre_patch_html` restored on rollback

**Build:** `ContentGuardService`, `ContentGuardWorker`, guard-fix job pipeline, content-guard page

---

### Step 20 — Parity #20: Multi-LLM AI visibility tracker

**Cloned from:** Surfer AI Tracker + Frase GEO

**Done when:**
- User adds queries; daily worker probes configured platforms (ChatGPT, Gemini, Google AIO, Perplexity, Claude); snapshots in `seo_geo_mention_snapshots`
- `/app/geo` shows mention rate and 30d trends; platforms without API keys show “not configured” — not fabricated scores

**Build:** `IGeoVisibilityProvider` implementations, `GeoVisibilityWorker`, geo page

---

### Step 21 — Parity #21–23: Dual SEO + GEO scores, E-E-A-T, SERP features

**Cloned from:** Frase dual scoring + Surfer E-E-A-T + SERP guidance

**Done when:**
- Single `ScoreUpdate` includes SEO + GEO rings, 5 GEO dimensions, `eeatAdvisories[]`, `serpFeatures[]`
- Editor sidebar: dual rings, component accordions, advisory list, SERP feature panel

**Build:** `GeoScoringService`, extend `ScoringOrchestrator` + hub payload, sidebar UI (step 50 editor work)

---

### Step 22 — Parity #24: Internal link suggestions panel

**Cloned from:** Surfer + Clearscope manual links

**Done when:**
- `GET /api/seo/links/suggest` returns max 10 links with relevance; insert-at-cursor in editor
- Distinct from #8 auto-insert

**Build:** panel UI wired to `InternalLinkService` suggest path

---

### Step 23 — Parity #25: Plagiarism check

**Cloned from:** Surfer plagiarism

**Done when:**
- `POST /api/seo/plagiarism/check` calls Copyscape; caches 24h on document; wizard publish blocked if &gt; 15%
- Provider down returns 503, never 0% fake match

**Build:** `CopyscapeProvider`, `PlagiarismService`, check UI + wizard gate

---

### Step 24 — Parity #26: Google Analytics 4

**Cloned from:** ContentShake GA4

**Done when:**
- GA4 OAuth per project; `GET /api/seo/analytics/ga4/{projectId}/landing-pages` with date range
- `/app/analytics` joins landing URLs to documents by `published_url`

**Build:** `Ga4Service`, analytics page, Professional+ tier gate

---

### Step 25 — Parity #28: WordPress plugin

**Cloned from:** Surfer + ContentShake WP plugin

**Done when:**
- Plugin in `integrations/wordpress-plugin/geek-seo-wp/` installs on WP 6.5+; posts list score column; editor sidebar with deep link; API key in `wp_options` encrypted; nonces on AJAX

**Build:** PHP plugin complete per route inventory

---

### Step 26 — Parity #29: Chrome extension MV3

**Cloned from:** Surfer Keyword Surfer

**Done when:**
- Extension loads on Google SERP (keyword popup) and WP block editor (score overlay); Agency API key or OAuth identity auth

**Build:** `integrations/chrome-extension/` shipped to Chrome Web Store–ready manifest

---

### Step 27 — Parity #30: Google Docs add-on

**Cloned from:** Surfer Google Docs integration

**Done when:**
- Apps Script sidebar pulls doc text, scores via API, shows top 5 suggestions with point values

**Build:** `integrations/google-docs-addon/`

---

### Step 28 — Parity #31: Public API + ChatGPT Actions

**Cloned from:** Surfer ChatGPT + NeuronWriter API

**Done when:**
- `GET /api/seo/openapi.json` valid OpenAPI 3.1
- Agency tier: API key CRUD; `/api/seo/v1/*` mirrors content, full-article, clusters, jobs, reports; 120 req/min per key
- `SeoApiKeyMiddleware` validates before JWT on v1 routes

**Build:** OpenAPI generator, v1 route map, key management UI, `/pricing` subscription flow (PayPal JS SDK + webhook activates `seo_subscriptions`)

---

### Step 29 — Billing and pricing page

**Done when:**
- `/pricing` tier cards; PayPal subscription per tier; webhook sets active tier; cancel flow works
- All gated routes respect live subscription row

**Build:** `PaymentService`, PayPal provider, `SubscriptionController` webhook, pricing page (completes monetization for clone launch)

---

### Step 30 — Playwright E2E: all clone flows

**Done when:**
- Automated tests pass: OAuth login; guided wizard through publish; editor SignalR score; planner → editor; topical map with GSC test project; content guard approve; calendar drag-drop; auto-optimize undo; detect + plagiarism gate

**Build:** E2E suite in repo per `scripts/E2E_SMOKE.md`, CI wired

---

### Step 31 — Unit tests: scoring and gates

**Done when:**
- Tests cover all 6 SEO components, 5 GEO dimensions, `NlpExtractor`, term benchmarks, feature gate matrix, usage cap enforcement

**Build:** test project in GeekBackend solution

---

### Step 32 — Production deploy

**Done when:**
- `geek_seo` migration applied; GeekSeoBackend `/health` green in production
- Vercel frontend on `seo.geekatyourspot.com`; SignalR WebSocket succeeds; cold-keyword score round-trip &lt; 40s, warm &lt; 3s

**Build:** Railway services + env vars; Vercel env; smoke checklist signed off

---

### Step 33 — Dogfood and launch proof

**Done when:**
- geekatyourspot.com GSC connected; 5 local-keyword articles published via WP REST; 2–4 weeks rankings/decay data in audit
- Marketing landing page uses real screenshots from dogfood

**Build:** landing page content, internal dogfood log

---

### Step 34 — Multi-instance SignalR (scale-out)

**Done when:**
- Second GeekSeoBackend instance on Railway shares SignalR groups via Redis backplane; score broadcast reaches clients on either instance

**Build:** `AddStackExchangeRedis()` on SignalR, Railway Redis provisioned

---

## Step completion checklist

Before marking any step done:

- [ ] What data source does it use, and did you verify read/write against production-like Postgres?
- [ ] Does every user-visible action hit real providers (or return explicit 503 when keys missing)?
- [ ] Did unit/E2E tests for this step pass?
- [ ] Is `ISeoDataClient` the only persistence path from GeekSeoBackend?
- [ ] Does the UI match commercial quality (loading states, errors, empty states)?
