# Geek SEO — Master Product Plan

**Version:** 1.5  
**Date:** May 2026  
**Product goal:** Functional clone of **Surfer SEO** + **Semrush ContentShake AI**, differentiated by transparent scoring, local SERP, honest billing, and WordPress-native workflow for Geek At Your Spot clients.  
**Changelog (1.5):** Critique fixes — no RLS in migrations; unified `seo_background_jobs`; complete route/tier/meter matrix; E-E-A-T advisory layer in scoring spec; GEO v1 scoped to Google AI Overviews only; tenancy on all services; `ISubscriptionService` owns webhooks; COGS table; internal-link inventory pipeline; Serp feature flags; implementation steps renumbered; integrations auth spec; SignalR/Vercel notes.  
**Changelog (1.4):** Clone strategy, Surfer/ContentShake module maps, guided mode UX, extended DB/API/frontend/implementation for all clone features.  
**Changelog (1.3):** Full Surfer SEO + Semrush ContentShake competitive parity — WordPress publish, one-click full article, topical map, published content audit, internal linking, plagiarism, deep SERP, GA4, GEO visibility, brand voice, humanizer, bulk generation, cannibalization, Google Docs add-on, Chrome extension, ChatGPT connector, public API.  
**Changelog (1.2):** Enforced three-tier boundaries (GeekAPI → GeekApplication → GeekRepository → DB), dedicated DB principal, `IContentDocumentService` + hub orchestration in Application, `ISeoStorageRepository`, EF-only migrations, Playwright-only PDF, fixed section numbering and implementation steps.  
**Changelog (1.1):** Security model, usage metering, OAuth reuse, no Prisma. Full feature set — no phases, deferrals, or stubs.
**Author:** Jeff Martin, Geek At Your Spot  
**Domain:** seo.geekatyourspot.com (subject to change)  
**Backend:** GeekBackend (.NET 10) — new `GeekSEO` module  
**Database:** Supabase instance `mpnruwauxsqbrxvlksnf` — new `geek_seo` schema  

---

## 1. Product Overview

### What It Is

Geek SEO is an AI-powered SEO content optimization SaaS that helps business owners and content writers rank higher on Google and appear in AI-generated answers (ChatGPT, Perplexity, Google AI Overviews). It provides a real-time content editor that scores documents against live SERP competitors as you type, generates structured content briefs from SERP analysis, writes and humanizes AI drafts, publishes directly to WordPress, plans site-wide content strategy from GSC topical maps, monitors published posts for decay, and tracks visibility across search and AI answer engines — with a fully transparent scoring formula Surfer does not offer.

### Clone Strategy

Geek SEO is built to **replicate the core workflows of Surfer SEO and Semrush ContentShake AI** — not merely “inspired by” them. A user migrating from either product should recognize the same mental model: keyword → SERP research → brief → write → score → publish → monitor.

**What we clone (functional parity):**

| Source product | Clone target in Geek SEO |
|---|---|
| **Surfer SEO** | Content Editor, Content Score, SERP Analyzer (50 deep), Topical Map, Content Audit, Surfer AI one-click article, internal links, plagiarism, AI Visibility Tracker, keyword clustering, site audit |
| **Semrush ContentShake AI** | AI article generation, keyword ideas, “ready to publish” guided flow, **push to WordPress**, GA4 performance view |

**What we do differently (why not a literal white-label):**

| Differentiator | Reason |
|---|---|
| Transparent 6-part score | Surfer’s black-box score is the #1 trust complaint |
| Local SERP on every plan | Underserved SMB wedge |
| $29–$149 vs Surfer $99–$299+ / ContentShake + Semrush bundle | Pricing story |
| GeekBackend OAuth + no separate user store | Shared identity with geekatyourspot.com |
| Next.js app (not Surfer’s stack) | Your stack — same UX patterns, different codebase |

**Dual UX modes (clone both audiences):**

| Mode | Clones | UI |
|---|---|---|
| **Expert Mode** | Surfer SEO power users | Full editor, SERP deep dive, topical map, all metrics visible |
| **Guided Mode** | ContentShake SMB flow | Wizard: business context → keyword pick → one-click article → score checklist → Publish to WordPress |

Default for new accounts: **Guided Mode**. Toggle in settings restores Expert layout.

### Who It Is For

Primary: Small business owners who want Surfer/ContentShake outcomes without SEO jargon — **Guided Mode** is the ContentShake clone path.

Secondary: Freelance writers and SEOs who want **Surfer-class tooling** at lower cost — **Expert Mode**.

The product is dogfooded on geekatyourspot.com (WordPress) before external sales — proving the ContentShake publish path on a real site.

### Positioning Table

| Dimension | Surfer SEO | Frase.io | NeuronWriter | Clearscope | Geek SEO |
|---|---|---|---|---|---|
| Entry price | $49/mo (annual) | $15/mo | $23/mo | $129/mo | $29/mo |
| Target user | SEO professionals | Content teams | Budget SEOs | Enterprise | Small business owners |
| Content scoring | Correlation-based, opaque | SEO + GEO dual score | NLP + AI score | F-A++ letter grade | Transparent 6-component breakdown |
| AI provider | Proprietary | Proprietary | GPT-based | Watson + Google NLP + OpenAI | IAIProvider (Claude primary, swappable) |
| Local SEO support | None | None | None | None | Native local SERP targeting |
| Transparent formula | No | No | No | No | Yes — exact point breakdown per component |
| Billing trust | Complained about on Trustpilot | Clean | Clean | Clean | No-gotchas guarantee |
| GSC integration | Yes (Topical Map required) | Yes | No | Yes | Yes — topical map + rank tracking + content audit |
| WordPress publish | Plugin | Direct push | No | Plugin | Yes — REST publish (Application Password) |
| One-click full article AI | Yes (Surfer AI) | Yes | Partial | No | Yes — brief + draft + score in one job |
| Topical map / content gaps | Yes | No | No | No | Yes — GSC-powered |
| Published content audit | Yes | No | No | No | Yes — GSC decay monitoring |
| Plagiarism check | Yes | Yes | Partial | No | Yes |
| Internal link suggestions | Yes | No | Yes | Yes | Yes — in-editor |
| Deep SERP (50 results) | Yes | No | No | No | Yes |
| AI visibility (GEO) tracking | Yes | No | No | Yes (GEO) | Yes |
| Google Analytics 4 | No | Yes | No | No | Yes |
| Brand voice | No | N | Y | N | Yes |
| Bulk article generation | Partial | N | N | N | Yes |
| Real-time scoring | Yes | Yes | Yes | Yes | Yes via SignalR WebSocket |
| API access | $299/mo plan | All plans | $93/mo plan | None | Agency ($149) — REST + API keys |
| Google Docs / Chrome / ChatGPT | Docs, ChatGPT | No | Chrome | MCP | Docs add-on + Chrome ext + GPT Actions |

### Key Differentiators from Market Research

**Transparent scoring formula.** Every competitor produces a score number without explaining the exact weighting. Geek SEO shows users the precise point breakdown: "your term coverage is 18/35 points because you're missing these 7 terms." Users who understand the scoring stay (lower churn) and trust the product (higher NPS).

**Native local SERP targeting.** No competitor has meaningful local SEO content features. Geek SEO passes a location parameter on every SERP request from day one, enabling content optimization for "plumber in Boca Raton" vs. "best plumber" — directly serving the small-business wedge.

**Provider-interface architecture.** IAIProvider, ISerpProvider, IKeywordProvider, ICrawlerProvider, IRichTextProvider, IPdfProvider, and IPaymentProvider are all interface-based. Claude can be swapped for OpenAI. DataForSEO can be swapped for Serper. PayPal can be swapped for Stripe. No business logic is coupled to any vendor. This is a maintainability and vendor-risk hedge.

**No-gotchas pricing.** Surfer's Trustpilot page is dominated by billing complaints. Geek SEO commits to monthly price equals what you pay, instant cancellation, and stable feature sets per plan.

**Honest billing as a lead generation strategy.** There are measurable search queries for "Surfer SEO cancel" and "Surfer SEO alternatives." This frustration is a direct acquisition channel.

### Dogfood GTM Strategy

**GTM sequence step 1** is internal use on geekatyourspot.com. Every content page on the site is created and optimized through Geek SEO before any external sales. This produces:

1. Real performance data — content scores correlated with actual GSC ranking improvements
2. Bug discovery in a real small-business context
3. Screenshot-worthy before/after proof for the product landing page
4. A demo account with real data, not seeded test data

Once five to ten content pieces have documented ranking improvements, sales outreach begins with Geek At Your Spot clients and referrals. The product story is "we built this tool to grow our own site and it worked — now we're offering it to our clients."

### Scope Commitment

The entire feature set in this document ships complete. There are no MVP phases, no deferred features, and no stubbed data layers. Every service connects to real providers (DataForSEO, Anthropic, Google GSC, PayPal, Supabase, Playwright). If a provider credential is missing, the feature fails with a clear error — it does not fall back to hardcoded or in-memory data.

### Tier Naming (Team / Agency)

**Team** and **Agency** are **volume tiers** (higher monthly limits), not multi-seat collaboration plans. Architecture decision: solo operator per account — no shared workspaces, invites, or role-based access in v1. Do not implement seat licensing; tier names reflect usage caps only.

### Relationship to RankPilot

`rankpilot/` in the monorepo is a separate Angular SEO auditor product. Geek SEO is the **content optimization SaaS** (editor, scoring, briefs, writing). RankPilot is not merged into Geek SEO; shared code lives in `@geek/*` packages and GeekBackend auth only. Avoid duplicating SERP/scoring logic in both codebases — Geek SEO owns the content editor product.

### Rank Tracking Expectations

"Rank tracking" means **Google Search Console Search Analytics data** (impressions, clicks, average position, CTR) synced daily — not third-party SERP position scraping. UI copy must say "GSC performance" where precision matters. Users without GSC connected see an empty state with connect CTA, not fabricated ranks.

### Competitive Parity Features (Surfer SEO + Semrush ContentShake)

All items below ship in v1 — same scope rules as the rest of this document (real providers, no stubs). Each feature follows **GeekAPI → Application Service → Repository/Provider → external API or DB**.

| # | Feature | Surfer / ContentShake reference | Geek SEO capability |
|---|---|---|---|
| 1 | **One-click full article** | Surfer AI | `POST /api/seo/writing/full-article` → `seo_background_jobs` (poll `GET /api/seo/jobs/{id}`) |
| 2 | **WordPress publish** | ContentShake push | `POST /api/seo/wordpress/publish` — draft or publish via WP REST API + Application Password |
| 3 | **Topical map** | Surfer Topical Map | GSC queries + keyword clustering → content gap cards + “create document” CTA |
| 4 | **Published content audit** | Surfer Content Audit | Track mapped URLs; weekly GSC snapshots; refresh recommendations |
| 5 | **Internal linking (editor)** | Surfer internal links | Sidebar suggests anchor + target URL from `seo_site_page_inventory` |
| 6 | **Plagiarism check** | Both | `IPlagiarismProvider` (Copyscape API) on demand per document |
| 7 | **Deep SERP analyzer** | Surfer SERP Analyzer | Top **50** organic results + TF-IDF term matrix (not just top 10 for scoring) |
| 8 | **Keyword cannibalization** | Surfer Pro+ | GSC: multiple pages competing for same query → warning + merge/canonical guidance |
| 9 | **Google Analytics 4** | ContentShake | GA4 Data API — landing page performance alongside GSC |
| 10 | **AI visibility (GEO)** | Surfer AI Tracker, Frase GEO | **v1:** Google AI Overviews only (DataForSEO). Domain/brand mention detection on SERP AI blocks. **v2 (labeled):** ChatGPT/Perplexity via `IAIProvider` probes when stable APIs exist |
| 11 | **Brand voice** | Frase (leader) | User uploads samples → `seo_brand_voices` → injected into all `IAIProvider` prompts |
| 12 | **AI humanizer** | Surfer Surfy | `POST /api/seo/writing/humanize` — rewrite to reduce AI-detection patterns while preserving score |
| 13 | **Bulk / autopilot articles** | Surfer partial, Sight AI | `POST /api/seo/writing/bulk` — queue N keywords → background jobs → documents |
| 14 | **Google Docs add-on** | Surfer | Separate Apps Script add-on calls Geek SEO REST API with OAuth token (same `geekseo` client) |
| 15 | **Chrome extension** | NeuronWriter | MV3 extension: highlight page, send selection to Geek SEO API for score/snippet |
| 16 | **ChatGPT connector** | Surfer ChatGPT | OpenAPI spec published at `/api/seo/openapi.json` + GPT Actions for brief/draft/score |
| 17 | **Public REST API** | Surfer high tier | Agency tier: API keys in `seo_api_keys` — same routes as UI, rate-limited |
| 18 | **Readability + E-E-A-T hints** | market-gaps research | **Advisory layer** (not scored): `EeatAdvisory[]` in `ScoreUpdate` — citations, author byline, experience sections, schema markup (see scoring spec §7) |
| 19 | **Auto-Optimize** | Surfer (most-used in-editor feature) | `POST /api/seo/content/{id}/auto-optimize` — service splices top missing NLP terms into appropriate paragraphs; returns patched `content_html`; frontend applies as single TipTap transaction |
| 20 | **AI detection score** | Surfer (pre-humanize) | `POST /api/seo/writing/detect` — `IAiDetectionProvider` (GPTZero API primary) returns `{ aiProbability: 0.87, sentences: [...] }`; shown in editor toolbar before user decides to humanize |
| 21 | **Content calendar** | ContentShake planning board | `status` column on `seo_content_documents` (`planned` → `writing` → `review` → `published`). `/app/calendar` — kanban across all project documents; drag card to change status via `PATCH /api/seo/content/{id}/status` |
| 22 | **SERP feature capture guidance** | gap across all competitors | DataForSEO already returns `featured_snippet`, `people_also_ask`, `local_pack` flags per SERP result. `SerpFeatureGuidanceService` maps present features → actionable copy: "Featured snippet detected — add a 40–60 word direct answer in a `<p>` immediately after your first H2." Shown in `ScoreSidebar` above suggestions. |

**Integrations architecture:** Google Docs, Chrome, and ChatGPT are **thin clients** of GeekBackend — no duplicate business logic.

| Integration | Auth | Rate limits |
|---|---|---|
| **Google Docs add-on** | GeekBackend OAuth PKCE (`geekseo` client); Apps Script stores refresh via user paste or secure property | Same tier as UI — calls `/api/seo/*` with Bearer |
| **Chrome extension** | Agency **API key** in extension options (`X-Api-Key`) OR OAuth redirect for logged-in users | 60 req/min per key |
| **ChatGPT GPT Actions** | Agency API key per user; OpenAPI at `/api/seo/openapi.json` | Same as `PublicApiController` |
| **Public REST API** | Agency only — `seo_api_keys` hashed with bcrypt | 120 req/min per key |

No integration bypasses `SeoFeatureGateMiddleware` / `SeoUsageGateMiddleware`.

### Surfer SEO → Geek SEO Module Map

| Surfer feature | Geek SEO module | Route / entry |
|---|---|---|
| Content Editor + Score | `ContentEditorPage` + `SeoContentScoringHub` | `/app/content/[id]` |
| SERP Analyzer | `SerpDeepAnalysisPage` | `/app/serp/[keyword]` + `GET /api/seo/serp/deep` |
| Content Brief | `ContentBriefService` | `/app/briefs/new` |
| Surfer AI (full article) | `AIWritingService.EnqueueFullArticleAsync` | `POST /api/seo/writing/full-article` → `seo_background_jobs` |
| Outline / Draft / Optimize | `AIWritingService` | `/app/write` |
| Topical Map | `TopicalMapService` | `/app/strategy/topical-map` |
| Content Audit (published) | `PublishedContentAuditService` | `/app/content-audit` |
| Keyword Research | `KeywordResearchService` | `/app/keywords` |
| Keyword Clustering | `KeywordResearchService.ClusterAsync` | same |
| Cannibalization | `KeywordCannibalizationService` | `/app/strategy/cannibalization` |
| Site Audit | `SiteAuditService` | `/app/audit` |
| GSC / Rank tracking | `RankTrackingService` | `/app/rankings` |
| Internal links | `InternalLinkingService` | Editor sidebar + `GET /api/seo/links/suggest` |
| Plagiarism | `PlagiarismService` | Editor toolbar + `POST /api/seo/plagiarism/check` |
| AI Visibility Tracker | `GeoVisibilityService` | `/app/geo` |
| Humanizer (Surfy) | `AIWritingService.HumanizeAsync` | `POST /api/seo/writing/humanize` |
| Bulk generation | `BulkWritingService` | `/app/bulk` |
| Brand voice | `BrandVoiceService` | `/app/settings/brand-voice` |
| Google Docs | `integrations/google-docs/` Apps Script | Uses REST API |
| ChatGPT | `GET /api/seo/openapi.json` | GPT Actions |
| API (Peace of Mind) | `PublicApiController` + `seo_api_keys` | Agency tier |
| Auto-Optimize | `ContentScoringService.AutoOptimizeAsync` | `POST /api/seo/content/{id}/auto-optimize` |
| AI Detection | `AIWritingService.DetectAsync` via `IAiDetectionProvider` | `POST /api/seo/writing/detect` |
| SERP Feature Guidance | `ContentScoringService.GetSerpFeatureGuidanceAsync` | `ScoreSidebar` — included in `ScoreUpdate` payload |

### ContentShake → Geek SEO Module Map

| ContentShake feature | Geek SEO module | Route / entry |
|---|---|---|
| AI SEO article | `EnqueueFullArticleAsync` + Guided wizard | `/app/guided` step 4 — poll `GET /api/seo/jobs/{id}` |
| Keyword ideas (Semrush DB) | `KeywordResearchService` (DataForSEO — same UX, different provider) | Guided step 2 |
| SEO score while writing | Real-time SignalR scoring | Editor (same as Surfer clone) |
| Ready to publish | Guided checklist: score ≥ target, plagiarism pass, meta filled | Guided step 5 |
| Push to WordPress | `WordPressPublishService` | `POST /api/seo/wordpress/publish` + Guided “Publish” button |
| GA4 stats | `GoogleAnalyticsService` | `/app/analytics` |
| GSC data | `RankTrackingService` | `/app/rankings` |
| Content calendar / planning board | `ContentCalendarPage` | `/app/calendar` — kanban by status |

---

## 2. System Architecture

### Full System Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         BROWSER / CLIENT                                │
│                                                                         │
│  Next.js 16 / React 19 (Vercel)                                         │
│  ┌─────────────────────┐  ┌─────────────────────┐                      │
│  │  App Router Pages   │  │  TipTap Editor      │                      │
│  │  /app/content/[id]  │  │  + ScoreSidebar     │                      │
│  │  /app/keywords      │  │  (WebSocket client) │                      │
│  │  /app/audit/[id]    │  └──────────┬──────────┘                      │
│  │  /app/rankings      │             │ SignalR WS                       │
│  └──────────┬──────────┘             │                                  │
│             │ HTTPS REST             │                                  │
└─────────────┼──────────────────────┬─┘                                  │
              │                      │                                     │
              ▼                      ▼                                     │
┌─────────────────────────────────────────────────────────────────────────┤
│                      GEEKAPI  (Railway .NET 10)                         │
│                                                                         │
│  Controllers/Seo/           Hubs/                       Middleware/       │
│  ├── ProjectsController     └── SeoContentScoringHub  SeoFeatureGate    │
│  ├── ContentController          (SignalR — services     SeoUsageGate      │
│  ├── BriefController             only, no repos)                        │
│  ├── SerpController                                                     │
│  ├── WritingController                                                  │
│  ├── KeywordsController                                                 │
│  ├── AuditController                                                    │
│  ├── RankController                                                     │
│  ├── ReportsController                                                  │
│  ├── SubscriptionController   ├── WordPressController                   │
│  ├── GscController            ├── TopicalMapController                    │
│  ├── PublishedContentAuditCtrl├── PlagiarismController                  │
│  ├── IntegrationsController   ├── GeoVisibilityController               │
│  ├── AnalyticsController      └── PublicApiController (Agency keys)     │
│                                                                         │
│  Injects I*Service only — NEVER I*Repository, SeoDbContext, providers   │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   GEEKAPPLICATION  (in-process class library)           │
│                                                                         │
│  Interfaces/Seo/            Services/Seo/                              │
│  ├── IContentScoringService ├── ContentScoringService (+ hub methods)  │
│  ├── IContentDocumentService├── ContentDocumentService                 │
│  ├── IContentBriefService   ├── ContentBriefService                    │
│  ├── IAIWritingService      ├── AIWritingService                       │
│  ├── IKeywordResearch...    ├── KeywordResearchService                 │
│  ├── ISiteAuditService      ├── SiteAuditService                       │
│  ├── IPageAuditService      ├── PageAuditService                       │
│  ├── IRankTrackingService   ├── RankTrackingService                    │
│  ├── IReportService         ├── ReportService                          │
│  ├── ISubscriptionService   ├── SubscriptionService                    │
│  ├── IProjectService          ├── ProjectService                       │
│  ├── IUsageMeteringService    ├── UsageMeteringService                   │
│  ├── IWordPressPublishService ├── WordPressPublishService                │
│  ├── ITopicalMapService       ├── TopicalMapService                      │
│  ├── IPublishedContentAudit.. ├── PublishedContentAuditService           │
│  ├── IInternalLinkingService  ├── InternalLinkingService                 │
│  ├── IPlagiarismService       ├── PlagiarismService                      │
│  ├── IGoogleAnalyticsService  ├── GoogleAnalyticsService                 │
│  ├── IGeoVisibilityService    ├── GeoVisibilityService                   │
│  ├── IBrandVoiceService       ├── BrandVoiceService                      │
│  ├── IBulkWritingService      ├── BulkWritingService                     │
│  ├── IKeywordCannibalization. ├── KeywordCannibalizationService        │
│  └── IGscService              └── GscService                             │
│                                                                         │
│  Provider interfaces defined here; implementations in GeekRepository  │
│  IAIProvider / ISerpProvider / IKeywordProvider / ICrawlerProvider       │
│  IRichTextProvider / IPdfProvider / IPaymentProvider                    │
│  IPlagiarismProvider / IWordPressProvider / IAnalyticsProvider            │
│  IGeoVisibilityProvider / IAiDetectionProvider                            │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                  GEEKREPOSITORY  (in-process class library)             │
│                                                                         │
│  Repositories/Seo/          Providers/Seo/                             │
│  ├── ContentDocumentRepo    ├── ClaudeProvider          → Anthropic API │
│  ├── KeywordRepository      ├── DataForSEOSerpProvider  → DataForSEO   │
│  ├── SerpCacheRepository    ├── DataForSEOKeywordProv.  → DataForSEO   │
│  ├── AuditRepository        ├── PlaywrightCrawlerProv.  → Playwright   │
│  ├── RankRepository         ├── TipTapProvider          → HtmlAgilityPack│
│  ├── SubscriptionRepository ├── PlaywrightPdfProvider   → Playwright     │
│  ├── UsageRepository        ├── SeoStorageRepository  → Supabase Storage │
│  ├── WordPressConnectionRepo├── CopyscapePlagiarismProvider             │
│  ├── TopicalMapRepository   ├── WordPressRestProvider → WP REST API      │
│  ├── PublishedPageRepository├── GoogleAnalytics4Provider → GA4 Data API   │
│  ├── SitePageInventoryRepo  ├── GeoVisibilityProvider → AI Overview APIs  │
│  ├── BrandVoiceRepository   │   + IAIProvider probes for LLM mentions    │
│  ├── BulkJobRepository      │                                          │
│  └── GscRepository / Ga4Repository                                       │
│                             PlaywrightPdfProvider (shared Chromium pool) │
│                                                                         │
│  SeoDbContext (EF Core 10) — ONLY type that opens PostgreSQL connections  │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │  Connection: GEEK_SEO_DATABASE_URL
                           │  Principal: geekseo_app (schema geek_seo only)
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│              SUPABASE PostgreSQL  (instance mpnruwauxsqbrxvlksnf)      │
│              Schema: geek_seo  (31 tables — see Section 5)                │
└─────────────────────────────────────────────────────────────────────────┘
```

### Layer Enforcement (NON-NEGOTIABLE)

Matches `GeekBackend/CLAUDE.md` — enforced by `.csproj` references and code review:

```
GeekAPI  →  GeekApplication  →  GeekRepository  →  PostgreSQL / Supabase Storage
   ↑              ↑                    ↑
Controllers    I*Service           I*Repository + Provider impls
Hubs           (business logic)    SeoDbContext (DB only)
```

| Rule | Detail |
|---|---|
| **GeekAPI `.csproj`** | References `GeekApplication` only. **Must not** reference `GeekRepository`. |
| **GeekApplication `.csproj`** | References `GeekRepository` (for DI registration at startup — interfaces still live in Application). Services inject `I*Repository` and provider interfaces — **never** `SeoDbContext`. |
| **GeekRepository `.csproj`** | References PostgreSQL/Npgsql/EF Core/Playwright SDKs. **Only** `Repositories/Seo/*` and `SeoDbContext` open DB connections. |
| **Controllers & Hubs** | Inject `I*Service` only. Forbidden: `IContentDocumentRepository`, `SeoDbContext`, `IAIProvider`, any concrete `*Repository` or `*Provider`. |
| **Background workers** (`GscSyncService`, `SiteAuditWorker`) | Resolve `I*Service` from `IServiceScopeFactory` — **never** repositories directly. |
| **Next.js** | HTTPS to GeekAPI only. No database drivers, no Supabase client, no Prisma. |

**Analyzer enforcement (recommended):** Add a `GeekAPI` architecture test or Roslyn analyzer that fails the build if any `GeekAPI` type references `GeekRepository` namespaces or `Npgsql`/`SeoDbContext`.

### Data Access & Security Model

Geek SEO data lives in Supabase schema `geek_seo` on instance `mpnruwauxsqbrxvlksnf` (shared with OrderStack / GeekQuote). The Next.js frontend **never** connects to Supabase.

| Layer | Responsibility |
|---|---|
| **PostgreSQL** | Accepts connections **only** from `geekseo_app` DB user with `USAGE` on schema `geek_seo` (and `public` if required for extensions). No anon/authenticated Supabase roles for app traffic. |
| **`SeoDbContext` (GeekRepository)** | Sole owner of PostgreSQL connections for SEO. Uses `GEEK_SEO_DATABASE_URL` when set; otherwise falls back to `DATABASE_URL` (same Supabase **instance**, schema `geek_seo`). Separate env var = optional **different DB role** (`geekseo_app` with grants on `geek_seo` only), not a second database server. |
| **`ISeoStorageRepository` (GeekRepository)** | Sole owner of Supabase Storage API calls (`seo-reports` bucket). Application services call the interface only. |
| **Repositories** | Called **only** from `GeekApplication/Services/Seo/*`. Every query includes `userId` ownership (or project-scoped join). |
| **GeekAPI + SignalR** | Authorize via existing GeekBackend OAuth JWT (`sub` = `users.id`). Call `I*Service` methods; services enforce tenancy. |
| **EF Core global filters** | `SeoDbContext` applies query filters on tenant entities (`UserId == currentUserId`) where practical; services still pass `userId` explicitly. |
| **Supabase RLS** | **Disabled** in `InitialSeoSchema` migration. GeekBackend uses `geekseo_app`, not Supabase Auth JWTs — `auth.uid()` policies never apply. Tenancy = Application services + EF global query filters only. |
| **GSC tokens** | AES-256-GCM encrypted at rest in `seo_gsc_connections`. Never returned to clients. |

**Database user setup (run once on Supabase):**

```sql
CREATE ROLE geekseo_app LOGIN PASSWORD '...';
GRANT USAGE ON SCHEMA geek_seo TO geekseo_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA geek_seo TO geekseo_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA geek_seo TO geekseo_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA geek_seo
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO geekseo_app;
-- Do NOT grant geekseo_app access to auth, orderstack, or other schemas
```

**Migrations:** `SeoDbContext` only. Add with `--context SeoDbContext`. Apply on deploy via `MigrateAsync()` in `GeekAPI/Program.cs` (same as existing content tables). **Do not** run hand-written DDL from Section 5 in production — Section 5 SQL is reference documentation only.

### Railway Infrastructure (Browser Workloads)

Playwright (crawl + PDF) runs inside GeekAPI on Railway. Mitigations required in implementation:

- **Singleton browser pool** — one `IBrowser` instance per process, reuse contexts; do not launch Chromium per request.
- **Bounded concurrency** — `SemaphoreSlim` max 2 concurrent crawls on API instances; site audits run on `IBackgroundTaskQueue` only.
- **Timeouts** — 30s navigation timeout, 60s max per page crawl.
- **Memory** — Railway service plan must allow ≥2 GB RAM for Playwright; monitor OOM in production logs.
- **Alternative path** — `ICrawlerProvider` interface allows swapping to DataForSEO On-Page API if self-hosted Chromium becomes unstable (implement interface, not a runtime fallback stub).

### WebSocket Content Scoring Flow

```
TipTap onUpdate event fires
    │
    ▼
Frontend debounce: 800ms (clears on next keystroke, fires only when user pauses)
    │
    ▼
SignalR emit: hub.sendAsync("ContentChanged", { documentId, contentHtml, targetKeyword })
    │
    ▼
SeoContentScoringHub.ContentChanged(documentId, contentHtml, targetKeyword)
    │   (Hub is thin — no repositories)
    ▼
IContentScoringService.ProcessContentChangedAsync(userId, documentId, contentHtml, targetKeyword)
    │   (all logic in Application layer)
    ├── IContentDocumentService.EnsureAccessAsync(userId, documentId) — sole ownership gate
    ├── Rate limit: max 1 benchmark refresh per document per 60s
    ├── ISerpCacheRepository cache check; on MISS → IBackgroundTaskQueue refresh
    ├── ScoreAsync against warm benchmarks; persist via repository inside service
    └── Returns ContentScoreHubResult { ScoreUpdate | PendingReason | BenchmarkRefreshing }
    │
    ▼
Hub maps result → SignalR events (ScoreUpdate | ScorePending | BenchmarkReady)
    │
    └── group(documentId):
        ScoreUpdate {
            score: 72,
            grade: "C",
            components: { termCoverage: 18, wordCount: 15, headingStructure: 10, ... },
            suggestions: [ { component, pointValue, actionText, currentValue, targetValue } ],
            timestamp: "2026-05-18T14:32:00Z"
        }
    │
    ▼
Frontend ScoreSidebar receives ScoreUpdate → re-renders score ring + suggestion list
```

### GSC OAuth Flow

```
User clicks "Connect Google Search Console" in /app/settings
    │
    ▼
Frontend: GET /api/seo/gsc/auth-url?projectId={id}
    │
    ▼
GscController → GscService.GetAuthorizationUrlAsync(userId, projectId)
    → Build Google OAuth 2.0 URL:
      https://accounts.google.com/o/oauth2/v2/auth
      ?client_id={GOOGLE_CLIENT_ID}
      &redirect_uri={BASE_URL}/api/seo/gsc/callback
      &response_type=code
      &scope=https://www.googleapis.com/auth/webmasters.readonly
      &access_type=offline
      &prompt=consent
      &state={base64(userId:projectId:csrfToken)}
    │
    ▼
Browser redirects to Google consent screen
    │
    ▼
User approves → Google redirects to /api/seo/gsc/callback?code=...&state=...
    │
    ▼
GscController.Callback(code, state)
    → Validate state (csrf token, userId match)
    → POST to https://oauth2.googleapis.com/token (exchange code for tokens)
    → Receive: access_token, refresh_token, expires_in
    → AES-256-GCM encrypt refresh_token with ENCRYPTION_KEY env var
    → Store in seo_gsc_connections (encrypted_refresh_token, site_url, user_id, project_id)
    → Redirect to /app/settings?gsc=connected
    │
    ▼
Daily background job (IHostedService in GeekAPI):
    → For each row in seo_gsc_connections:
        → Decrypt refresh_token
        → GET new access_token via refresh grant
        → Call Google Search Console searchanalytics.query API
          dimensions: ["query", "page", "date"]
          startDate: yesterday, endDate: yesterday
        → Upsert results into seo_rank_tracking
```

### PayPal Webhook Flow

```
User clicks "Subscribe" on pricing page
    │
    ▼
Frontend loads PayPal JS SDK (client-side)
    → PayPal.Buttons({ createSubscription, onApprove })
    → createSubscription calls PayPal with planId for selected tier
    → PayPal returns subscriptionId in onApprove callback
    │
    ▼
Frontend: POST /api/seo/subscription/confirm
    body: { subscriptionId, tier }
    │
    ▼
SubscriptionController → SubscriptionService.ConfirmAsync(userId, subscriptionId, tier)
    → GET https://api.paypal.com/v1/billing/subscriptions/{subscriptionId}
    → Verify status = "ACTIVE", plan_id matches expected plan for tier
    → Insert into seo_subscriptions (status: "pending_webhook")
    → Return 200 OK
    │
    ▼
PayPal fires webhook to /api/seo/subscription/webhook
    (Raw request body preserved for signature verification — see Section 9)
    BILLING.SUBSCRIPTION.ACTIVATED  → UPDATE seo_subscriptions SET status = 'active'
    BILLING.SUBSCRIPTION.CANCELLED  → UPDATE seo_subscriptions SET status = 'cancelled'
    BILLING.SUBSCRIPTION.SUSPENDED  → UPDATE seo_subscriptions SET status = 'suspended'
    PAYMENT.SALE.COMPLETED          → UPDATE seo_subscriptions SET current_period_end (calculate from cycle)
    PAYMENT.SALE.DENIED             → UPDATE seo_subscriptions SET status = 'payment_failed'
    │
    ▼
SeoFeatureGateMiddleware (on every authenticated request):
    → ISubscriptionService.GetActiveTierAsync(userId)
    → Check route against feature gate table
    → 402 Payment Required if feature not in tier
```

### Site Audit Flow

```
User enters site URL on /app/audit → clicks "Start Audit"
    │
    ▼
POST /api/seo/audit/site  body: { projectId, siteUrl }
    │
    ▼
AuditController → ISiteAuditService.StartAuditAsync(userId, projectId, siteUrl)
    → INSERT INTO seo_site_audits (status: "running", started_at: now)
    → Enqueue background job (IBackgroundTaskQueue)
    → Return 202 Accepted with auditId
    │
    ▼
Background job: SiteAuditWorker.ExecuteAsync(auditId)
    → ICrawlerProvider.CrawlSiteAsync(siteUrl, maxPages: 100)
        → Playwright: fetch sitemap.xml or crawl from root
        → For each page: extract HTML, meta, headings, links, word count, status code
        → Respect robots.txt (check Disallow rules before fetching each URL)
        → Store each page as ICrawlResult
    │
    ├── For each crawled page:
    │   → IPageAuditService.AuditPageAsync(url, crawlResult)
    │   → Run 20-point on-page analysis:
    │     title tag present + keyword-like terms, meta description, H1 count,
    │     canonical tag, broken internal links, image alt text missing,
    │     page speed indicators (large inline scripts, render-blocking CSS),
    │     structured data presence, duplicate title detection
    │   → INSERT INTO seo_site_audit_pages (auditId, url, score, issues jsonb)
    │
    ├── Aggregate overall_score = AVG(page scores)
    │
    └── UPDATE seo_site_audits SET status = "complete", overall_score, pages_crawled, completed_at
    │
    ▼
Frontend polls GET /api/seo/audit/{auditId}/status every 5 seconds
    → When status = "complete", redirect to /app/audit/{auditId}
    → AuditResultsTable renders issues grouped by severity
```

---

## 3. Authentication Strategy

### Overview

Geek SEO reuses GeekBackend's existing OAuth 2.1 + PKCE + TOTP infrastructure. No separate auth system. Geek SEO registers as a new OAuth client in GeekBackend. The Next.js frontend runs the PKCE flow. GeekBackend issues JWTs; the `sub` claim equals the user's `users.id` UUID in the GeekBackend auth tables.

**Do not implement:** NextAuth, Auth.js, Supabase Auth, or any separate user store. All authentication is delegated to GeekBackend.

---

### Registration and Login Flow

```
User clicks "Sign Up" on seo.geekatyourspot.com
    │
    ▼
GET /auth/signup → SignupPage (Next.js)
    Form: name, email, password
    POST to GeekBackend: POST /api/auth/register
        body: { name, email, password, clientId: "geekseo" }
    GeekBackend creates user record, sends verification email via Resend
    Redirect to /auth/verify-email (show "check your inbox" screen)
    │
    ▼
User clicks email verification link → GET /api/auth/verify?token=...
    GeekBackend marks user verified
    Redirects to seo.geekatyourspot.com/login
    │
    ▼
Login: PKCE flow
    Frontend generates code_verifier (random 64 bytes, base64url)
    Derives code_challenge = base64url(SHA-256(code_verifier))
    GET /api/auth/authorize
        ?client_id=geekseo
        &redirect_uri={APP_URL}/auth/callback
        &response_type=code
        &code_challenge={challenge}
        &code_challenge_method=S256
    GeekBackend validates client_id, shows login form (or returns auth code if session exists)
    User enters email + password → GeekBackend validates credentials
    If TOTP enabled: redirect through TOTP challenge page
    On success: redirect to {APP_URL}/auth/callback?code={authCode}
    │
    ▼
Frontend: GET /auth/callback?code={authCode}
    POST to GeekBackend: POST /api/auth/token
        body: { grant_type: "authorization_code", code, code_verifier, client_id: "geekseo", redirect_uri }
    GeekBackend returns:
        { access_token, token_type: "Bearer", expires_in: 900, refresh_token }
    Store access_token in memory (React context, never localStorage)
    POST /api/auth/token/cookie: GeekBackend sets httpOnly Secure SameSite=Strict cookie
        containing encrypted refresh_token (GeekBackend owns the cookie)
    Redirect to /app/dashboard
```

---

### Token Lifecycle

| Token | Storage | Lifetime | Refresh |
|---|---|---|---|
| `access_token` | React memory (AuthContext) | 15 minutes | Auto-refresh 60s before expiry |
| `refresh_token` | httpOnly Secure cookie (GeekBackend-set) | 30 days | Sliding window on use |

**Silent refresh:** `useAuth` hook sets a `setTimeout` to fire 60 seconds before the `access_token` expiry. On fire, call `POST /api/auth/token/refresh` (the cookie is sent automatically by the browser). On success, update `access_token` in context. On failure (cookie expired or revoked), redirect to `/login`.

**On page load:** Before rendering authenticated routes, call `GET /api/auth/me` with the stored `access_token`. On 401, attempt silent refresh. If refresh fails, redirect to `/login`.

---

### SignalR Authentication

The `useContentScoring` hook's `accessTokenFactory` returns the in-memory `access_token` from `AuthContext`:

```typescript
// hooks/useContentScoring.ts — connection builder
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/seo-scoring`, {
    accessTokenFactory: () => authContext.accessToken ?? '',
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();
```

If the access token expires mid-session (unlikely at 15-min lifetime + 800ms debounce), `withAutomaticReconnect` triggers a reconnect. The frontend refreshes the token before the reconnect attempt via the `onreconnecting` event.

### Next.js (Vercel) + SignalR (Railway)

| Concern | Decision |
|---|---|
| **Hub host** | `SeoContentScoringHub` on GeekAPI (Railway), path `/hubs/seo-scoring` |
| **Frontend** | Vercel — browser WebSocket to `NEXT_PUBLIC_API_URL` (not server-side SignalR from Next) |
| **CORS** | GeekAPI allows `https://seo.geekatyourspot.com` + `http://localhost:3000` with credentials |
| **Scale-out** | Dogfood: single Railway instance. Before multi-instance: add **Redis backplane** for SignalR or sticky sessions |
| **Auth** | `accessTokenFactory` passes Bearer JWT; hub validates same as REST |

---

### GeekSEO OAuth Client Registration in GeekBackend

Register a new client in GeekBackend's `oauth_clients` table:

```sql
INSERT INTO oauth_clients (client_id, client_name, redirect_uris, allowed_scopes, pkce_required)
VALUES (
    'geekseo',
    'Geek SEO',
    ARRAY['https://seo.geekatyourspot.com/auth/callback', 'http://localhost:3000/auth/callback'],
    ARRAY['profile', 'email'],
    true
);
```

No client secret — PKCE-only client (public client). GeekBackend validates the `code_verifier` instead of a secret.

---

### Environment Variables

```bash
# GeekBackend (Railway)
# No new vars — geekseo client registered in DB, not env

# Next.js frontend (Vercel)
NEXT_PUBLIC_API_URL=https://api.geekatyourspot.com        # GeekBackend base URL
NEXT_PUBLIC_AUTH_URL=https://api.geekatyourspot.com       # Same — GeekBackend issues auth
NEXT_PUBLIC_CLIENT_ID=geekseo
NEXT_PUBLIC_REDIRECT_URI=https://seo.geekatyourspot.com/auth/callback
NEXT_PUBLIC_APP_URL=https://seo.geekatyourspot.com
```

---

### JWT Claims Used by GeekBackend

Every GeekBackend JWT carries:

| Claim | Type | Example | Used By |
|---|---|---|---|
| `sub` | UUID string | `"f47ac10b-..."` | All EF Core `.Where(x => x.UserId == userId)` queries |
| `email` | string | `"jeff@..."` | UI display only |
| `name` | string | `"Jeff Martin"` | UI display only |
| `exp` | Unix timestamp | `1748000000` | Token expiry check in middleware |

**Subscription tier** is **not** read from JWT. `SeoFeatureGateMiddleware` and `SeoUsageGateMiddleware` call `ISubscriptionService.GetActiveTierAsync(userId)` → `seo_subscriptions` table only.

`SeoContentScoringHub` and all SEO controllers read `sub` from `context.User` and pass `userId` into Application services.

---

### Teams: v1 Solo, Schema Designed for v2 Multi-Seat

**v1 (shipping):** Each account is a solo operator. "Team" and "Agency" tiers are volume limits (more content reports, more AI drafts per month), not multi-seat. No shared workspaces, no invites, no role-based access.

**Why name them Team/Agency if solo?** Volume buyers — small agencies with one operator account — need higher limits and API access. The tier name signals the intended buyer, not the seat model.

**v2 design (not in scope for v1, but schema accommodates it):**

Organization tables (`seo_organizations`, `seo_organization_members`) are defined **once** in Section 5 (end of reference SQL). Do not duplicate DDL elsewhere.

`seo_projects` has a nullable `org_id` column in Section 5. In v1 it is always NULL. In v2, team members access projects where `org_id` matches their membership. Metering aggregates across the org's `user_id`s.

**Metering for v2:** When `org_id IS NOT NULL` on a project, `IUsageMeteringService.IncrementAsync` writes to the org owner's usage counter. Team members share the org's monthly limit, not individual limits.

---

## 4. Provider Interface Contracts

### IAIProvider

```csharp
// GeekApplication/Interfaces/Seo/IAIProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface IAIProvider
{
    Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default);
    Task<Result<AIResponse>> CompleteWithSystemAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record AIRequest
{
    public required string SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
    public string Model { get; init; } = "claude-sonnet-4-5";
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.7;
    public bool EnableCaching { get; init; } = true;
}

public sealed record AIResponse
{
    public required string Content { get; init; }
    public required string Model { get; init; }
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required string StopReason { get; init; }
    public bool CacheHit { get; init; }
}
```

### ISerpProvider

```csharp
// GeekApplication/Interfaces/Seo/ISerpProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface ISerpProvider
{
    Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record SerpRequest
{
    public required string Keyword { get; init; }
    public string Location { get; init; } = "United States";
    public string LanguageCode { get; init; } = "en";
    public string CountryCode { get; init; } = "US";
    public int ResultCount { get; init; } = 10;
    public string Device { get; init; } = "desktop";
}

public sealed record SerpResult
{
    public required string Keyword { get; init; }
    public required string Location { get; init; }
    public required IReadOnlyList<SerpOrganicResult> OrganicResults { get; init; }
    public IReadOnlyList<PeopleAlsoAskResult> PeopleAlsoAsk { get; init; } = [];
    public IReadOnlyList<string> RelatedSearches { get; init; } = [];
    public string? FeaturedSnippetText { get; init; }
    public required SerpFeatures Features { get; init; }
    public required DateTimeOffset FetchedAt { get; init; }
}

/// <summary>SERP-level feature flags from DataForSEO — drives SerpFeatureGuidance in ScoreUpdate.</summary>
public sealed record SerpFeatures
{
    public bool HasFeaturedSnippet { get; init; }
    public bool HasPeopleAlsoAsk { get; init; }
    public bool HasLocalPack { get; init; }
    public bool HasImagePack { get; init; }
    public bool HasVideoCarousel { get; init; }
    public bool HasKnowledgePanel { get; init; }
}

public sealed record SerpOrganicResult
{
    public required int Position { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public string? Domain { get; init; }
}

public sealed record PeopleAlsoAskResult
{
    public required string Question { get; init; }
    public string? Answer { get; init; }
}
```

### IKeywordProvider

```csharp
// GeekApplication/Interfaces/Seo/IKeywordProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface IKeywordProvider
{
    Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordDataAsync(KeywordRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(string seedKeyword, string location, int count, CancellationToken ct = default);
    Task<Result<IReadOnlyList<KeywordCluster>>> ClusterKeywordsAsync(IReadOnlyList<string> keywords, string location, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record KeywordRequest
{
    public required IReadOnlyList<string> Keywords { get; init; }
    public string Location { get; init; } = "United States";
    public string LanguageCode { get; init; } = "en";
}

public sealed record KeywordResult
{
    public required string Keyword { get; init; }
    public required int SearchVolume { get; init; }
    public required double KeywordDifficulty { get; init; }
    public required double CpcUsd { get; init; }
    public required string Competition { get; init; }
    public IReadOnlyList<MonthlySearchVolume> MonthlyTrend { get; init; } = [];
    public IReadOnlyList<string> SerpFeatures { get; init; } = [];
}

public sealed record MonthlySearchVolume
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required int Volume { get; init; }
}

public sealed record KeywordCluster
{
    public required string ClusterName { get; init; }
    public required string PillarKeyword { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
    public required double AverageVolume { get; init; }
    public required double AverageDifficulty { get; init; }
}
```

### ICrawlerProvider

```csharp
// GeekApplication/Interfaces/Seo/ICrawlerProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface ICrawlerProvider
{
    Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PageContent>>> CrawlSiteAsync(string siteUrl, int maxPages, CancellationToken ct = default);
    Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record PageContent
{
    public required string Url { get; init; }
    public required string FullText { get; init; }
    public required string? MetaTitle { get; init; }
    public required string? MetaDescription { get; init; }
    public required string? CanonicalUrl { get; init; }
    public required int WordCount { get; init; }
    public required int HttpStatusCode { get; init; }
    public required IReadOnlyList<Heading> Headings { get; init; }
    public required IReadOnlyList<string> InternalLinks { get; init; }
    public required IReadOnlyList<string> ExternalLinks { get; init; }
    public required IReadOnlyList<ImageAltData> Images { get; init; }
    public required bool HasStructuredData { get; init; }
    public required IReadOnlyList<string> StructuredDataTypes { get; init; }
    public required DateTimeOffset CrawledAt { get; init; }
}

public sealed record Heading
{
    public required string Level { get; init; }   // "h1", "h2", "h3", etc.
    public required string Text { get; init; }
}

public sealed record ImageAltData
{
    public required string Src { get; init; }
    public string? AltText { get; init; }
    public bool HasAlt => !string.IsNullOrWhiteSpace(AltText);
}

public sealed record CrawlRequest
{
    public required string Url { get; init; }
    public bool FollowLinks { get; init; } = false;
    public int MaxPages { get; init; } = 1;
    public bool RespectRobotsTxt { get; init; } = true;
}
```

### IRichTextProvider

```csharp
// GeekApplication/Interfaces/Seo/IRichTextProvider.cs

namespace GeekApplication.Interfaces.Seo;

// Minimal interface — TipTap is primarily a frontend concern.
// Backend uses this to extract plain text and structure from HTML.
public interface IRichTextProvider
{
    string ExtractPlainText(string html);
    IReadOnlyList<Heading> ExtractHeadings(string html);
    int CountWords(string html);
    string? ExtractFirstH1(string html);
    string ProviderName { get; }
}
```

### IPdfProvider

```csharp
// GeekApplication/Interfaces/Seo/IPdfProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface IPdfProvider
{
    Task<Result<PdfResult>> GeneratePdfAsync(PdfRequest request, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record PdfRequest
{
    public required string HtmlContent { get; init; }
    public required string FileName { get; init; }
    public PdfPageFormat PageFormat { get; init; } = PdfPageFormat.A4;
    public string? HeaderHtml { get; init; }
    public string? FooterHtml { get; init; }
    public PdfMargins Margins { get; init; } = new();
}

public sealed record PdfResult
{
    public required byte[] Bytes { get; init; }
    public required string FileName { get; init; }
    public required long FileSizeBytes { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}

public sealed record PdfMargins
{
    public string Top { get; init; } = "20mm";
    public string Bottom { get; init; } = "20mm";
    public string Left { get; init; } = "15mm";
    public string Right { get; init; } = "15mm";
}

public enum PdfPageFormat { A4, Letter, Legal }
```

### IPaymentProvider

```csharp
// GeekApplication/Interfaces/Seo/IPaymentProvider.cs

namespace GeekApplication.Interfaces.Seo;

/// <summary>
/// Abstracts the subscription billing processor. Primary implementation: PayPal.
/// Swap at the DI layer to change processors without touching ISubscriptionService.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Verify a subscription exists and is in an expected state after onApprove callback.</summary>
    Task<Result<PaymentSubscriptionDetail>> GetSubscriptionAsync(string subscriptionId, CancellationToken ct = default);

    /// <summary>Cancel an active subscription immediately.</summary>
    Task<Result> CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken ct = default);

    /// <summary>Validate that an incoming webhook payload came from the processor, not a spoof.</summary>
    Task<Result> VerifyWebhookSignatureAsync(WebhookVerificationRequest request, CancellationToken ct = default);

    /// <summary>Parse the raw webhook payload into a provider-agnostic event.</summary>
    Result<PaymentWebhookEvent> ParseWebhookEvent(string rawBody);

    string ProviderName { get; }
}

public sealed record PaymentSubscriptionDetail
{
    public required string SubscriptionId { get; init; }
    public required string PlanId { get; init; }
    public required PaymentSubscriptionStatus Status { get; init; }
    public DateTimeOffset? CurrentPeriodStart { get; init; }
    public DateTimeOffset? CurrentPeriodEnd { get; init; }
}

public enum PaymentSubscriptionStatus
{
    Active,
    PendingActivation,
    Cancelled,
    Suspended,
    PaymentFailed,
    Unknown
}

public sealed record WebhookVerificationRequest
{
    public required string RawBody { get; init; }
    public required string WebhookId { get; init; }
    public required IReadOnlyDictionary<string, string> Headers { get; init; }
}

public sealed record PaymentWebhookEvent
{
    public required string EventType { get; init; }       // normalized: "subscription.activated", "subscription.cancelled", etc.
    public required string SubscriptionId { get; init; }
    public required string PlanId { get; init; }
    public DateTimeOffset? PeriodEnd { get; init; }
    public required string RawEventType { get; init; }    // original processor event string for logging
}
```

**Primary implementation:** `PayPalPaymentProvider` in `GeekRepository/Providers/Seo/PayPalPaymentProvider.cs`.

PayPal-specific webhook headers it reads: `PAYPAL-TRANSMISSION-ID`, `PAYPAL-TRANSMISSION-TIME`, `PAYPAL-CERT-URL`, `PAYPAL-AUTH-ALGO`, `PAYPAL-TRANSMISSION-SIG`.

PayPal event-type mapping to normalized `PaymentWebhookEvent.EventType`:

| PayPal event | Normalized |
|---|---|
| `BILLING.SUBSCRIPTION.ACTIVATED` | `subscription.activated` |
| `BILLING.SUBSCRIPTION.CANCELLED` | `subscription.cancelled` |
| `BILLING.SUBSCRIPTION.SUSPENDED` | `subscription.suspended` |
| `PAYMENT.SALE.COMPLETED` | `payment.completed` |
| `PAYMENT.SALE.DENIED` | `payment.failed` |

`ISubscriptionService` depends on `IPaymentProvider`, never on `PayPalPaymentProvider` directly. All PayPal API credentials (`PAYPAL_CLIENT_ID`, `PAYPAL_CLIENT_SECRET`, `PAYPAL_WEBHOOK_ID`) are read inside `PayPalPaymentProvider` from environment — never passed through the interface.

### Clone provider interfaces (Section 4)

`ISerpProvider.GetSerpResultsAsync` accepts `ResultCount` up to **50** for deep SERP (stored in `seo_serp_deep_cache`). Default **10** for scoring (stored in `seo_serp_results`).

```csharp
// IPlagiarismProvider.cs
public interface IPlagiarismProvider
{
    Task<Result<PlagiarismProviderResult>> CheckAsync(string plainText, CancellationToken ct = default);
    string ProviderName { get; }
}

// IWordPressProvider.cs
public interface IWordPressProvider
{
    Task<Result<WordPressConnectionTestResult>> TestConnectionAsync(WordPressCredentials credentials, CancellationToken ct = default);
    Task<Result<WordPressPublishProviderResult>> PublishPostAsync(WordPressCredentials credentials, WordPressPostPayload post, CancellationToken ct = default);
    string ProviderName { get; }
}

// IAnalyticsProvider.cs (GA4)
public interface IAnalyticsProvider
{
    Task<Result<string>> GetOAuthAuthorizationUrlAsync(string redirectUri, string state, CancellationToken ct = default);
    Task<Result<OAuthTokens>> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<Result<Ga4LandingPageReport>> GetLandingPagesAsync(string propertyId, string refreshToken, DateOnly from, DateOnly to, CancellationToken ct = default);
    string ProviderName { get; }
}

// IGeoVisibilityProvider.cs — v1: google_aio only
public interface IGeoVisibilityProvider
{
    Task<Result<GeoMentionCheckResult>> CheckGoogleAiOverviewAsync(string queryText, string domain, string location, CancellationToken ct = default);
    string ProviderName { get; }
}

// IAiDetectionProvider.cs
public interface IAiDetectionProvider
{
    Task<Result<AiDetectionResult>> DetectAsync(string plainText, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record AiDetectionResult
{
    public required double AiProbability { get; init; }
    public IReadOnlyList<AiDetectionSentence> FlaggedSentences { get; init; } = [];
}
```

---

## 5. Database Schema

**Source of truth:** EF Core migrations on `SeoDbContext` (`GeekRepository/Migrations/Seo/`). The SQL below is **reference documentation** showing the intended shape — generate production schema via `dotnet ef migrations add`, not by pasting this script.

**Table count:** 31 tables in `geek_seo` — 15 core + 14 clone-feature tables + 1 unified jobs table + 2 v2 org tables (v1 leaves `org_id` NULL).

**RLS:** Do **not** enable Row Level Security in EF migrations. Reference SQL below has no `ENABLE ROW LEVEL SECURITY` blocks. Tenancy is enforced in **Application services** (every method takes `userId`) + EF Core global query filters on `SeoDbContext`.

### SERP cache tables (when to use which)

| Table | Purpose | TTL | Used by |
|---|---|---|---|
| `seo_serp_results` | Top **10** organic + `serp_features` JSON for scoring benchmarks and SERP feature guidance | 24h | Real-time scoring, briefs, `GetSerpOverviewAsync` |
| `seo_serp_deep_cache` | Top **50** organic + TF-IDF term matrix for Surfer SERP Analyzer clone | 7 days | `GetSerpDeepAnalysisAsync` only — never on every keystroke |
| `seo_competitor_pages` | Crawled HTML/text for benchmark URLs | 72h | Scoring engine crawl cache |

### Internal link inventory pipeline

`seo_site_page_inventory` is populated by (in order of trigger):

1. **Site audit completion** — `SiteAuditWorker` upserts every crawled URL with title, H1, word count after a site-wide audit finishes.
2. **WordPress publish** — `WordPressPublishService` upserts the published URL + post title after successful REST publish.
3. **GSC URL import (optional)** — `RankTrackingService.SyncGscDataAsync` can upsert URLs from GSC Search Analytics top pages (deduped by URL).
4. **Manual register** — `POST /api/seo/content-audit/register` adds a published page URL to inventory.

`InternalLinkingService.SuggestLinksAsync` reads inventory for the project only — never suggests links to uncrawled external domains.

```sql
-- Enable schema
CREATE SCHEMA IF NOT EXISTS geek_seo;

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- seo_projects
-- ============================================================
CREATE TABLE geek_seo.seo_projects (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID NOT NULL,
    org_id              UUID,
    name                TEXT NOT NULL,
    url                 TEXT NOT NULL,
    gsc_connected       BOOLEAN NOT NULL DEFAULT FALSE,
    default_location    TEXT NOT NULL DEFAULT 'United States',
    default_language    TEXT NOT NULL DEFAULT 'en',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
-- v1: org_id is always NULL. v2: set when project belongs to an organization.

CREATE INDEX idx_seo_projects_user_id ON geek_seo.seo_projects (user_id);


-- ============================================================
-- seo_content_documents
-- ============================================================
CREATE TABLE geek_seo.seo_content_documents (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    title               TEXT NOT NULL DEFAULT 'Untitled Document',
    content_html        TEXT NOT NULL DEFAULT '',
    target_keyword      TEXT NOT NULL DEFAULT '',
    target_location     TEXT NOT NULL DEFAULT 'United States',
    seo_score           INTEGER NOT NULL DEFAULT 0,
    word_count          INTEGER NOT NULL DEFAULT 0,
    score_components    JSONB NOT NULL DEFAULT '{}',
    last_scored_at      TIMESTAMPTZ,
    status              TEXT NOT NULL DEFAULT 'planned'
                            CHECK (status IN ('planned', 'writing', 'review', 'published')),
    published_score     INTEGER,           -- score at time of publish (for GSC correlation)
    published_word_count INTEGER,
    published_at        TIMESTAMPTZ,
    ai_detection_score  NUMERIC(5, 4),     -- 0.0–1.0 probability from IAiDetectionProvider
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_content_docs_project ON geek_seo.seo_content_documents (project_id);
CREATE INDEX idx_seo_content_docs_user    ON geek_seo.seo_content_documents (user_id);
CREATE INDEX idx_seo_content_docs_score   ON geek_seo.seo_content_documents (seo_score DESC);
CREATE INDEX idx_seo_content_docs_status  ON geek_seo.seo_content_documents (status);


-- ============================================================
-- seo_keyword_clusters (must exist before seo_keywords FK)
-- ============================================================
CREATE TABLE geek_seo.seo_keyword_clusters (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    name                TEXT NOT NULL,
    pillar_keyword      TEXT NOT NULL,
    keywords            JSONB NOT NULL DEFAULT '[]',
    average_volume      INTEGER NOT NULL DEFAULT 0,
    average_difficulty  NUMERIC(5, 2) NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_keyword_clusters_project ON geek_seo.seo_keyword_clusters (project_id);


-- ============================================================
-- seo_keywords
-- ============================================================
CREATE TABLE geek_seo.seo_keywords (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    keyword             TEXT NOT NULL,
    location            TEXT NOT NULL DEFAULT 'United States',
    search_volume       INTEGER NOT NULL DEFAULT 0,
    keyword_difficulty  NUMERIC(5, 2) NOT NULL DEFAULT 0,
    cpc_usd             NUMERIC(10, 4) NOT NULL DEFAULT 0,
    competition         TEXT,
    serp_features       JSONB NOT NULL DEFAULT '[]',
    monthly_trend       JSONB NOT NULL DEFAULT '[]',
    cluster_id          UUID REFERENCES geek_seo.seo_keyword_clusters(id) ON DELETE SET NULL,
    cached_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '7 days'),
    UNIQUE (project_id, keyword, location)
);

CREATE INDEX idx_seo_keywords_project    ON geek_seo.seo_keywords (project_id);
CREATE INDEX idx_seo_keywords_keyword    ON geek_seo.seo_keywords (keyword);
CREATE INDEX idx_seo_keywords_expires    ON geek_seo.seo_keywords (expires_at);
CREATE INDEX idx_seo_keywords_difficulty ON geek_seo.seo_keywords (keyword_difficulty);


-- ============================================================
-- seo_serp_results (cache table — no RLS, project-scoped access via service layer)
-- ============================================================
CREATE TABLE geek_seo.seo_serp_results (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    keyword             TEXT NOT NULL,
    location            TEXT NOT NULL,
    language_code       TEXT NOT NULL DEFAULT 'en',
    results             JSONB NOT NULL,
    people_also_ask     JSONB NOT NULL DEFAULT '[]',
    related_searches    JSONB NOT NULL DEFAULT '[]',
    featured_snippet    TEXT,
    serp_features       JSONB NOT NULL DEFAULT '{}',
    fetched_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '24 hours'),
    UNIQUE (keyword, location, language_code)
);

-- serp_features JSON shape (stored from DataForSEO):
-- { "has_featured_snippet": bool, "has_people_also_ask": bool, "has_local_pack": bool,
--   "has_image_pack": bool, "has_video_carousel": bool, "has_knowledge_panel": bool }

CREATE INDEX idx_seo_serp_keyword  ON geek_seo.seo_serp_results (keyword, location);
CREATE INDEX idx_seo_serp_expires  ON geek_seo.seo_serp_results (expires_at);

-- ============================================================
-- seo_competitor_pages (cache table — 72h TTL for crawled competitor HTML)
-- ============================================================
CREATE TABLE geek_seo.seo_competitor_pages (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    serp_result_id      UUID NOT NULL REFERENCES geek_seo.seo_serp_results(id) ON DELETE CASCADE,
    url                 TEXT NOT NULL,
    domain              TEXT,
    meta_title          TEXT,
    meta_description    TEXT,
    content_text        TEXT NOT NULL DEFAULT '',
    word_count          INTEGER NOT NULL DEFAULT 0,
    headings            JSONB NOT NULL DEFAULT '[]',
    terms               JSONB NOT NULL DEFAULT '{}',
    internal_link_count INTEGER NOT NULL DEFAULT 0,
    external_link_count INTEGER NOT NULL DEFAULT 0,
    image_count         INTEGER NOT NULL DEFAULT 0,
    images_missing_alt  INTEGER NOT NULL DEFAULT 0,
    has_structured_data BOOLEAN NOT NULL DEFAULT FALSE,
    structured_data_types JSONB NOT NULL DEFAULT '[]',
    http_status         INTEGER NOT NULL DEFAULT 200,
    crawled_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '72 hours'),
    UNIQUE (serp_result_id, url)
);

CREATE INDEX idx_seo_competitor_serp    ON geek_seo.seo_competitor_pages (serp_result_id);
CREATE INDEX idx_seo_competitor_url     ON geek_seo.seo_competitor_pages (url);
CREATE INDEX idx_seo_competitor_expires ON geek_seo.seo_competitor_pages (expires_at);

-- ============================================================
-- seo_page_audits
-- ============================================================
CREATE TABLE geek_seo.seo_page_audits (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    url                 TEXT NOT NULL,
    score               INTEGER NOT NULL DEFAULT 0,
    issues              JSONB NOT NULL DEFAULT '[]',
    metadata            JSONB NOT NULL DEFAULT '{}',
    audited_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_page_audits_project ON geek_seo.seo_page_audits (project_id);
CREATE INDEX idx_seo_page_audits_url     ON geek_seo.seo_page_audits (url);
CREATE INDEX idx_seo_page_audits_user    ON geek_seo.seo_page_audits (user_id);


-- ============================================================
-- seo_site_audits
-- ============================================================
CREATE TABLE geek_seo.seo_site_audits (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    status              TEXT NOT NULL DEFAULT 'pending'
                            CHECK (status IN ('pending', 'running', 'complete', 'failed')),
    pages_crawled       INTEGER NOT NULL DEFAULT 0,
    overall_score       NUMERIC(5, 2),
    error_message       TEXT,
    started_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);

CREATE INDEX idx_seo_site_audits_project ON geek_seo.seo_site_audits (project_id);
CREATE INDEX idx_seo_site_audits_status  ON geek_seo.seo_site_audits (status);


-- ============================================================
-- seo_site_audit_pages
-- ============================================================
CREATE TABLE geek_seo.seo_site_audit_pages (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    audit_id            UUID NOT NULL REFERENCES geek_seo.seo_site_audits(id) ON DELETE CASCADE,
    url                 TEXT NOT NULL,
    score               INTEGER NOT NULL DEFAULT 0,
    http_status         INTEGER NOT NULL DEFAULT 200,
    word_count          INTEGER NOT NULL DEFAULT 0,
    issues              JSONB NOT NULL DEFAULT '[]',
    metadata            JSONB NOT NULL DEFAULT '{}'
);

CREATE INDEX idx_seo_site_audit_pages_audit ON geek_seo.seo_site_audit_pages (audit_id);
CREATE INDEX idx_seo_site_audit_pages_score ON geek_seo.seo_site_audit_pages (score);

-- ============================================================
-- seo_rank_tracking
-- ============================================================
CREATE TABLE geek_seo.seo_rank_tracking (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    keyword             TEXT NOT NULL,
    page_url            TEXT,
    position            NUMERIC(6, 2),
    impressions         INTEGER NOT NULL DEFAULT 0,
    clicks              INTEGER NOT NULL DEFAULT 0,
    ctr                 NUMERIC(6, 4),
    date                DATE NOT NULL,
    source              TEXT NOT NULL DEFAULT 'gsc',
    UNIQUE (project_id, keyword, date)
);

CREATE INDEX idx_seo_rank_tracking_project ON geek_seo.seo_rank_tracking (project_id);
CREATE INDEX idx_seo_rank_tracking_keyword ON geek_seo.seo_rank_tracking (keyword);
CREATE INDEX idx_seo_rank_tracking_date    ON geek_seo.seo_rank_tracking (date DESC);
CREATE INDEX idx_seo_rank_tracking_pos     ON geek_seo.seo_rank_tracking (position);


-- ============================================================
-- seo_gsc_connections
-- ============================================================
CREATE TABLE geek_seo.seo_gsc_connections (
    id                       UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                  UUID NOT NULL,
    project_id               UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    encrypted_refresh_token  BYTEA NOT NULL,
    encryption_iv            BYTEA NOT NULL,
    encryption_tag           BYTEA NOT NULL,
    encrypted_access_token   BYTEA,
    access_token_iv          BYTEA,
    access_token_tag         BYTEA,
    access_token_expires_at  TIMESTAMPTZ,
    site_url                 TEXT NOT NULL,
    google_email             TEXT,
    connected_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_synced_at           TIMESTAMPTZ,
    UNIQUE (project_id)
);

CREATE INDEX idx_seo_gsc_connections_user    ON geek_seo.seo_gsc_connections (user_id);
CREATE INDEX idx_seo_gsc_connections_project ON geek_seo.seo_gsc_connections (project_id);


-- ============================================================
-- seo_subscriptions
-- ============================================================
CREATE TABLE geek_seo.seo_subscriptions (
    id                       UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                  UUID NOT NULL,
    paypal_subscription_id   TEXT NOT NULL UNIQUE,
    paypal_plan_id           TEXT NOT NULL,
    tier                     TEXT NOT NULL CHECK (tier IN ('starter', 'professional', 'team', 'agency')),
    status                   TEXT NOT NULL DEFAULT 'pending_webhook'
                                 CHECK (status IN ('pending_webhook', 'active', 'cancelled', 'suspended', 'payment_failed')),
    current_period_start     TIMESTAMPTZ,
    current_period_end       TIMESTAMPTZ,
    cancelled_at             TIMESTAMPTZ,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_subscriptions_user   ON geek_seo.seo_subscriptions (user_id);
CREATE INDEX idx_seo_subscriptions_status ON geek_seo.seo_subscriptions (status);
CREATE INDEX idx_seo_subscriptions_paypal ON geek_seo.seo_subscriptions (paypal_subscription_id);


-- ============================================================
-- seo_reports
-- ============================================================
CREATE TABLE geek_seo.seo_reports (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    type                TEXT NOT NULL CHECK (type IN ('site_audit', 'page_audit', 'rank_tracking', 'content', 'keyword')),
    title               TEXT NOT NULL,
    file_path           TEXT,
    file_size_bytes     BIGINT,
    status              TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'generating', 'complete', 'failed')),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_reports_project ON geek_seo.seo_reports (project_id);
CREATE INDEX idx_seo_reports_user    ON geek_seo.seo_reports (user_id);


-- ============================================================
-- seo_alerts
-- ============================================================
CREATE TABLE geek_seo.seo_alerts (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    type                TEXT NOT NULL CHECK (type IN ('rank_drop', 'audit_complete', 'recrawl_due', 'score_change')),
    threshold           JSONB NOT NULL DEFAULT '{}',
    enabled             BOOLEAN NOT NULL DEFAULT TRUE,
    last_sent_at        TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_alerts_project ON geek_seo.seo_alerts (project_id);
CREATE INDEX idx_seo_alerts_user    ON geek_seo.seo_alerts (user_id);
CREATE INDEX idx_seo_alerts_type    ON geek_seo.seo_alerts (type);


-- ============================================================
-- seo_usage_counters (monthly metered limits per tier — Section 9)
-- ============================================================
CREATE TABLE geek_seo.seo_usage_counters (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL,
    period_start    DATE NOT NULL,
    feature         TEXT NOT NULL,
    count           INTEGER NOT NULL DEFAULT 0,
  UNIQUE (user_id, period_start, feature)
);

CREATE INDEX idx_seo_usage_user_period ON geek_seo.seo_usage_counters (user_id, period_start);
CREATE INDEX idx_seo_usage_feature     ON geek_seo.seo_usage_counters (feature);


-- ============================================================
-- Clone-feature tables (Surfer + ContentShake parity)
-- ============================================================
CREATE TABLE geek_seo.seo_wordpress_connections (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    site_url            TEXT NOT NULL,
    username            TEXT NOT NULL,
    encrypted_app_password BYTEA NOT NULL,
    encryption_iv       BYTEA NOT NULL,
    encryption_tag      BYTEA NOT NULL,
    default_post_status TEXT NOT NULL DEFAULT 'draft',
    connected_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (project_id)
);

CREATE TABLE geek_seo.seo_published_pages (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    document_id         UUID REFERENCES geek_seo.seo_content_documents(id) ON DELETE SET NULL,
    url                 TEXT NOT NULL,
    wordpress_post_id   INTEGER,
    target_keyword      TEXT,
    last_audit_at       TIMESTAMPTZ,
    UNIQUE (project_id, url)
);

CREATE TABLE geek_seo.seo_content_performance_snapshots (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    published_page_id   UUID NOT NULL REFERENCES geek_seo.seo_published_pages(id) ON DELETE CASCADE,
    date                DATE NOT NULL,
    position            NUMERIC(6, 2),
    impressions         INTEGER NOT NULL DEFAULT 0,
    clicks              INTEGER NOT NULL DEFAULT 0,
    ctr                 NUMERIC(6, 4),
    UNIQUE (published_page_id, date)
);

CREATE TABLE geek_seo.seo_topical_maps (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    status              TEXT NOT NULL DEFAULT 'pending',
    clusters            JSONB NOT NULL DEFAULT '[]',
    content_gaps        JSONB NOT NULL DEFAULT '[]',
    generated_at        TIMESTAMPTZ,
    expires_at          TIMESTAMPTZ
);

CREATE TABLE geek_seo.seo_site_page_inventory (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    url                 TEXT NOT NULL,
    title               TEXT,
    h1                  TEXT,
    word_count          INTEGER NOT NULL DEFAULT 0,
    crawled_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (project_id, url)
);

CREATE TABLE geek_seo.seo_brand_voices (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID NOT NULL,
    name                TEXT NOT NULL,
    sample_text         TEXT NOT NULL,
    style_instructions  TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geek_seo.seo_bulk_jobs (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    status              TEXT NOT NULL DEFAULT 'pending',
    keywords            JSONB NOT NULL,
    completed_count     INTEGER NOT NULL DEFAULT 0,
    total_count         INTEGER NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);

CREATE TABLE geek_seo.seo_plagiarism_checks (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id         UUID NOT NULL REFERENCES geek_seo.seo_content_documents(id) ON DELETE CASCADE,
    match_percent       NUMERIC(5, 2) NOT NULL,
    matches             JSONB NOT NULL DEFAULT '[]',
    checked_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geek_seo.seo_ga4_connections (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    property_id         TEXT NOT NULL,
    encrypted_refresh_token BYTEA NOT NULL,
    encryption_iv       BYTEA NOT NULL,
    encryption_tag      BYTEA NOT NULL,
    connected_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (project_id)
);

CREATE TABLE geek_seo.seo_geo_tracking_queries (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    query_text          TEXT NOT NULL,
    platforms           JSONB NOT NULL DEFAULT '["google_aio"]',
    enabled             BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE geek_seo.seo_geo_mention_snapshots (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    query_id            UUID NOT NULL REFERENCES geek_seo.seo_geo_tracking_queries(id) ON DELETE CASCADE,
    platform            TEXT NOT NULL,
    mentioned           BOOLEAN NOT NULL,
    snippet             TEXT,
    checked_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geek_seo.seo_cannibalization_issues (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    keyword             TEXT NOT NULL,
    competing_urls      JSONB NOT NULL,
    severity            TEXT NOT NULL,
    detected_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geek_seo.seo_api_keys (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID NOT NULL,
    key_hash            TEXT NOT NULL,
    key_prefix          TEXT NOT NULL,
    name                TEXT NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at          TIMESTAMPTZ
);

-- Unified async job queue (full-article, bulk, topical map, site audit enqueue)
CREATE TABLE geek_seo.seo_background_jobs (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID NOT NULL,
    project_id          UUID REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    job_type            TEXT NOT NULL
                            CHECK (job_type IN (
                                'full_article', 'bulk_writing', 'topical_map',
                                'site_audit', 'published_audit_weekly'
                            )),
    status              TEXT NOT NULL DEFAULT 'pending'
                            CHECK (status IN ('pending', 'running', 'complete', 'failed')),
    payload             JSONB NOT NULL DEFAULT '{}',
    result_id           UUID,
    progress_percent    INTEGER NOT NULL DEFAULT 0,
    error_message       TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    started_at          TIMESTAMPTZ,
    completed_at        TIMESTAMPTZ
);

CREATE INDEX idx_seo_background_jobs_user ON geek_seo.seo_background_jobs (user_id, status);
CREATE INDEX idx_seo_background_jobs_type ON geek_seo.seo_background_jobs (job_type, status);

CREATE TABLE geek_seo.seo_serp_deep_cache (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    keyword             TEXT NOT NULL,
    location            TEXT NOT NULL,
    result_count        INTEGER NOT NULL DEFAULT 50,
    results             JSONB NOT NULL,
    term_matrix         JSONB NOT NULL DEFAULT '{}',
    fetched_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL,
    UNIQUE (keyword, location, result_count)
);

-- ============================================================
-- v2 organization tables (created in InitialSeoSchema migration; unused in v1 code)
-- ============================================================
CREATE TABLE geek_seo.seo_organizations (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        TEXT NOT NULL,
    owner_id    UUID NOT NULL,
    slug        TEXT NOT NULL UNIQUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE geek_seo.seo_organization_members (
    org_id      UUID NOT NULL REFERENCES geek_seo.seo_organizations(id) ON DELETE CASCADE,
    user_id     UUID NOT NULL,
    role        TEXT NOT NULL DEFAULT 'writer' CHECK (role IN ('owner', 'admin', 'writer')),
    invited_by  UUID,
    joined_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (org_id, user_id)
);

ALTER TABLE geek_seo.seo_projects
    ADD CONSTRAINT fk_seo_projects_org
    FOREIGN KEY (org_id) REFERENCES geek_seo.seo_organizations(id) ON DELETE SET NULL;
```

---

## 6. Backend Module Spec

### GeekApplication/Interfaces/Seo/ — Service Interfaces

```csharp
// IContentScoringService.cs
public interface IContentScoringService
{
    Task<Result<ContentScoreResult>> ScoreAsync(
        string contentHtml, string targetKeyword, string location,
        SerpBenchmarks benchmarks, CancellationToken ct = default);

    Task<Result<SerpBenchmarks>> GetOrFetchBenchmarksAsync(
        string keyword, string location, CancellationToken ct = default);

    // Hub orchestration — all document/SERP/cache access stays inside this service
    Task<Result<ContentScoreHubResult>> ProcessContentChangedAsync(
        Guid userId, Guid documentId, string contentHtml, string targetKeyword,
        CancellationToken ct = default);

    Task<Result> ProcessKeywordChangedAsync(
        Guid userId, Guid documentId, string newKeyword, string location,
        CancellationToken ct = default);

    // SerpController + editor sidebar (top 10)
    Task<Result<SerpOverviewResult>> GetSerpOverviewAsync(
        Guid userId, Guid projectId, string keyword, string location,
        CancellationToken ct = default);

    // Surfer SERP Analyzer clone (top 50 + term matrix) — uses seo_serp_deep_cache
    Task<Result<SerpDeepAnalysisResult>> GetSerpDeepAnalysisAsync(
        Guid userId, string keyword, string location, CancellationToken ct = default);

    // Surfer Auto-Optimize clone — splices missing terms, re-scores, returns patched HTML
    Task<Result<AutoOptimizeResult>> AutoOptimizeAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);

    // SERP feature guidance for ScoreSidebar (from cached serp_features, not per keystroke)
    Task<Result<IReadOnlyList<SerpFeatureGuidance>>> GetSerpFeatureGuidanceAsync(
        string keyword, string location, CancellationToken ct = default);
}

// IContentDocumentService.cs — sole entry for content CRUD + tenancy from GeekAPI
public interface IContentDocumentService
{
    /// <summary>Throws/returns Failure if user does not own document. Used by scoring hub and other services.</summary>
    Task<Result<ContentDocument>> EnsureAccessAsync(Guid userId, Guid documentId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ContentDocument>>> ListByProjectAsync(
        Guid userId, Guid projectId, CancellationToken ct = default);
    Task<Result<ContentDocument>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default);
    Task<Result<ContentDocument>> CreateAsync(
        Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default);
    Task<Result<ContentDocument>> UpdateContentAsync(
        Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default);
    Task<Result<ContentDocument>> UpdateStatusAsync(
        Guid userId, Guid documentId, ContentDocumentStatus status, CancellationToken ct = default);
    Task<Result<ContentScoreResult>> ScoreNowAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default);
}

// IContentBriefService.cs
public interface IContentBriefService
{
    Task<Result<ContentBrief>> GenerateBriefAsync(
        Guid userId, Guid projectId, string keyword, string location, int competitorCount,
        CancellationToken ct = default);
}

// IAIWritingService.cs
public interface IAIWritingService
{
    Task<Result<string>> GenerateOutlineAsync(
        string keyword, ContentBrief brief, CancellationToken ct = default);

    Task<Result<string>> GenerateDraftAsync(
        string keyword, string outline, ContentBrief brief,
        int targetWordCount, CancellationToken ct = default);

    Task<Result<string>> OptimizeContentAsync(
        string contentHtml, string targetKeyword,
        ContentScoreResult currentScore, CancellationToken ct = default);

    // Surfer AI + ContentShake one-click article clone — returns seo_background_jobs.id
    Task<Result<Guid>> EnqueueFullArticleAsync(
        Guid userId, Guid projectId, string keyword, string location,
        int targetWordCount, CancellationToken ct = default);

    Task<Result<string>> HumanizeAsync(
        Guid userId, Guid documentId, string contentHtml, CancellationToken ct = default);

    Task<Result<AiDetectionResult>> DetectAsync(
        Guid userId, Guid documentId, string contentHtml, CancellationToken ct = default);
}

// IWordPressPublishService.cs — ContentShake publish clone
public interface IWordPressPublishService
{
    Task<Result> ConnectAsync(Guid userId, Guid projectId, WordPressConnectRequest request, CancellationToken ct);
    Task<Result<WordPressPublishResult>> PublishDocumentAsync(
        Guid userId, Guid documentId, WordPressPublishOptions options, CancellationToken ct);
    Task<Result> DisconnectAsync(Guid userId, Guid projectId, CancellationToken ct);
}

// ITopicalMapService.cs — Surfer Topical Map clone
public interface ITopicalMapService
{
    Task<Result<Guid>> GenerateMapAsync(Guid userId, Guid projectId, CancellationToken ct);
    Task<Result<TopicalMapResult>> GetMapAsync(Guid userId, Guid mapId, CancellationToken ct);
}

// IPublishedContentAuditService.cs — Surfer Content Audit clone
public interface IPublishedContentAuditService
{
    Task<Result> RegisterPublishedPageAsync(Guid userId, Guid projectId, RegisterPublishedPageRequest request, CancellationToken ct);
    Task<Result<IReadOnlyList<PublishedPageAuditRow>>> GetAuditDashboardAsync(Guid userId, Guid projectId, CancellationToken ct);
    Task<Result> RunWeeklyAuditAsync(CancellationToken ct);
}

// IInternalLinkingService.cs
public interface IInternalLinkingService
{
    Task<Result<IReadOnlyList<InternalLinkSuggestion>>> SuggestLinksAsync(
        Guid userId, Guid projectId, Guid documentId, string contentHtml, CancellationToken ct);
}

// IPlagiarismService.cs
public interface IPlagiarismService
{
    Task<Result<PlagiarismCheckResult>> CheckDocumentAsync(Guid userId, Guid documentId, CancellationToken ct);
}

// IGoogleAnalyticsService.cs — ContentShake GA4 clone
public interface IGoogleAnalyticsService
{
    Task<Result<string>> GetAuthorizationUrlAsync(Guid userId, Guid projectId, CancellationToken ct);
    Task<Result> HandleCallbackAsync(Guid userId, string code, string state, CancellationToken ct);
    Task<Result<Ga4PerformanceReport>> GetLandingPageReportAsync(
        Guid userId, Guid projectId, DateOnly from, DateOnly to, CancellationToken ct);
}

// IGeoVisibilityService.cs — v1: Google AI Overviews only (DataForSEO)
public interface IGeoVisibilityService
{
    Task<Result<IReadOnlyList<GeoTrackingQuery>>> ListQueriesAsync(Guid userId, Guid projectId, CancellationToken ct);
    Task<Result<Guid>> AddQueryAsync(Guid userId, Guid projectId, string queryText, CancellationToken ct);
    Task<Result<IReadOnlyList<GeoMentionSnapshot>>> GetMentionHistoryAsync(Guid userId, Guid queryId, CancellationToken ct);
    Task<Result> RunDailyGeoScanAsync(CancellationToken ct);
}

// IBrandVoiceService.cs
public interface IBrandVoiceService
{
    Task<Result<BrandVoice>> CreateAsync(Guid userId, CreateBrandVoiceRequest request, CancellationToken ct);
    Task<Result<IReadOnlyList<BrandVoice>>> ListAsync(Guid userId, CancellationToken ct);
    Task<Result> SetDefaultAsync(Guid userId, Guid brandVoiceId, CancellationToken ct);
}

// IBulkWritingService.cs — job id = seo_background_jobs.id (job_type = bulk_writing)
public interface IBulkWritingService
{
    Task<Result<Guid>> StartBulkJobAsync(Guid userId, Guid projectId, IReadOnlyList<string> keywords, CancellationToken ct);
    Task<Result<BulkJobStatus>> GetJobStatusAsync(Guid userId, Guid jobId, CancellationToken ct);
}

// IBackgroundJobService.cs — poll status for any async job type
public interface IBackgroundJobService
{
    Task<Result<BackgroundJobStatus>> GetJobAsync(Guid userId, Guid jobId, CancellationToken ct = default);
}

// IKeywordCannibalizationService.cs
public interface IKeywordCannibalizationService
{
    Task<Result<IReadOnlyList<CannibalizationIssue>>> DetectAsync(Guid userId, Guid projectId, CancellationToken ct);
}

// IKeywordResearchService.cs
public interface IKeywordResearchService
{
    Task<Result<IReadOnlyList<KeywordResult>>> ResearchAsync(
        Guid userId, Guid projectId, string seedKeyword, string location, int resultCount,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<KeywordCluster>>> ClusterAsync(
        Guid userId, Guid projectId, IReadOnlyList<string> keywords,
        string location, CancellationToken ct = default);
}

// ISiteAuditService.cs — audit id stored in seo_background_jobs.result_id when complete
public interface ISiteAuditService
{
    Task<Result<Guid>> StartAuditAsync(Guid userId, Guid projectId, string siteUrl, CancellationToken ct = default);
    Task<Result<SiteAuditStatus>> GetStatusAsync(Guid userId, Guid auditId, CancellationToken ct = default);
    Task<Result<SiteAuditReport>> GetReportAsync(Guid userId, Guid auditId, CancellationToken ct = default);
}

// IPageAuditService.cs
public interface IPageAuditService
{
    Task<Result<PageAuditResult>> AuditPageAsync(
        Guid userId, Guid projectId, string url, CancellationToken ct = default);
    Task<Result<PageAuditResult>> AuditPageFromContentAsync(
        Guid userId, Guid projectId, string url, PageContent crawlResult,
        CancellationToken ct = default);
}

// IRankTrackingService.cs
public interface IRankTrackingService
{
    Task<Result<IReadOnlyList<RankSnapshot>>> GetRankHistoryAsync(
        Guid userId, Guid projectId, string keyword, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<RankSnapshot>>> GetTopKeywordsAsync(
        Guid userId, Guid projectId, int count, CancellationToken ct = default);

    Task<Result> SyncGscDataAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<Result> SyncAllConnectedProjectsAsync(CancellationToken ct = default);
}

// IReportService.cs
public interface IReportService
{
    Task<Result<Guid>> GenerateSiteAuditReportAsync(
        Guid auditId, Guid userId, CancellationToken ct = default);

    Task<Result<Guid>> GenerateRankTrackingReportAsync(
        Guid projectId, Guid userId, DateRange range,
        CancellationToken ct = default);

    Task<Result<string>> GetReportDownloadUrlAsync(
        Guid reportId, CancellationToken ct = default);
}

// ISubscriptionService.cs
public interface ISubscriptionService
{
    Task<Result<SubscriptionTier>> GetActiveTierAsync(Guid userId, CancellationToken ct = default);
    Task<Result> ConfirmSubscriptionAsync(Guid userId, string paypalSubscriptionId, SubscriptionTier tier, CancellationToken ct = default);
    /// <summary>Verify PayPal signature via IPaymentProvider, parse event, update subscription. Called by controller with raw body.</summary>
    Task<Result> ProcessWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
    Task<Result> HandleWebhookEventAsync(PaymentWebhookEvent webhookEvent, CancellationToken ct = default);
    Task<Result> CancelSubscriptionAsync(Guid userId, CancellationToken ct = default);
    bool IsFeatureAllowed(SubscriptionTier tier, string featureName);
}

// IGscService.cs
public interface IGscService
{
    Task<Result<string>> GetAuthorizationUrlAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<Result> HandleCallbackAsync(string code, string state, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GscDataPoint>>> FetchSearchAnalyticsAsync(Guid projectId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<Result> DisconnectAsync(Guid projectId, CancellationToken ct = default);
}

// IProjectService.cs
public interface IProjectService
{
    Task<Result<IReadOnlyList<SeoProject>>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<Result<SeoProject>> GetAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default);
    Task<Result<SeoProject>> UpdateAsync(Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid userId, Guid projectId, CancellationToken ct = default);
}

// IUsageMeteringService.cs
public interface IUsageMeteringService
{
    Task<Result<int>> GetUsageAsync(Guid userId, string feature, CancellationToken ct = default);
    Task<Result<int>> GetLimitAsync(SubscriptionTier tier, string feature, CancellationToken ct = default);
    Task<Result> IncrementAsync(Guid userId, string feature, int amount = 1, CancellationToken ct = default);
    Task<Result> EnsureWithinLimitAsync(Guid userId, SubscriptionTier tier, string feature, CancellationToken ct = default);
}
```

### GeekApplication/Interfaces/Seo/ — Repository Interfaces

```csharp
// IContentDocumentRepository.cs
public interface IContentDocumentRepository
{
    Task<Result<ContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ContentDocument>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<ContentDocument>> CreateAsync(CreateContentDocumentRequest request, CancellationToken ct = default);
    Task<Result> UpdateContentAsync(Guid documentId, string contentHtml, int wordCount, CancellationToken ct = default);
    Task<Result> UpdateScoreAsync(Guid documentId, int score, JsonDocument components, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default);
}

// IKeywordRepository.cs
public interface IKeywordRepository
{
    Task<Result<IReadOnlyList<CachedKeyword>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<CachedKeyword?>> GetCachedAsync(Guid projectId, string keyword, string location, CancellationToken ct = default);
    Task<Result> UpsertAsync(UpsertKeywordRequest request, CancellationToken ct = default);
    Task<Result> BulkUpsertAsync(IReadOnlyList<UpsertKeywordRequest> requests, CancellationToken ct = default);
}

// ISerpCacheRepository.cs
public interface ISerpCacheRepository
{
    Task<Result<SerpCacheEntry?>> GetAsync(string keyword, string location, string languageCode, CancellationToken ct = default);
    Task<Result> UpsertAsync(UpsertSerpCacheRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CachedCompetitorPage>>> GetCompetitorPagesAsync(Guid serpResultId, CancellationToken ct = default);
    Task<Result> UpsertCompetitorPageAsync(UpsertCompetitorPageRequest request, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}

// IAuditRepository.cs
public interface IAuditRepository
{
    Task<Result<SiteAuditRecord>> GetSiteAuditAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<Guid>> CreateSiteAuditAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpdateSiteAuditStatusAsync(Guid auditId, string status, int pagesCrawled, decimal? overallScore, string? error, CancellationToken ct = default);
    Task<Result> InsertSiteAuditPageAsync(InsertAuditPageRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAuditPageRecord>>> GetSiteAuditPagesAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<PageAuditRecord>> GetPageAuditAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<Guid>> CreatePageAuditAsync(CreatePageAuditRequest request, CancellationToken ct = default);
}

// IRankRepository.cs
public interface IRankRepository
{
    Task<Result<IReadOnlyList<RankSnapshot>>> GetHistoryAsync(Guid projectId, string keyword, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RankSnapshot>>> GetTopKeywordsAsync(Guid projectId, int count, CancellationToken ct = default);
    Task<Result> BulkUpsertAsync(IReadOnlyList<RankSnapshot> snapshots, CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> GetTrackedKeywordsAsync(Guid projectId, CancellationToken ct = default);
}

// ISubscriptionRepository.cs
public interface ISubscriptionRepository
{
    Task<Result<SubscriptionRecord?>> GetActiveAsync(Guid userId, CancellationToken ct = default);
    Task<Result> InsertAsync(InsertSubscriptionRequest request, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(string paypalSubscriptionId, string status, DateTimeOffset? periodEnd, CancellationToken ct = default);
    Task<Result> CancelAsync(Guid userId, CancellationToken ct = default);
}

// IGscRepository.cs
public interface IGscRepository
{
    Task<Result<GscConnection?>> GetConnectionAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpsertConnectionAsync(UpsertGscConnectionRequest request, CancellationToken ct = default);
    Task<Result> UpdateLastSyncedAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> DeleteConnectionAsync(Guid projectId, CancellationToken ct = default);
}

// IProjectRepository.cs
public interface IProjectRepository
{
    Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Result<SeoProject?>> GetByIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<SeoProject>> CreateAsync(CreateProjectDbRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid projectId, UpdateProjectDbRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid projectId, Guid userId, CancellationToken ct = default);
}

// IUsageRepository.cs
public interface IUsageRepository
{
    Task<Result<int>> GetCountAsync(Guid userId, DateOnly periodStart, string feature, CancellationToken ct = default);
    Task<Result> IncrementAsync(Guid userId, DateOnly periodStart, string feature, int amount, CancellationToken ct = default);
}

// IBackgroundJobRepository.cs — unified async queue
public interface IBackgroundJobRepository
{
    Task<Result<BackgroundJob>> CreateAsync(CreateBackgroundJobRequest request, CancellationToken ct = default);
    Task<Result<BackgroundJob>> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task<Result> UpdateProgressAsync(Guid jobId, int progressPercent, CancellationToken ct = default);
    Task<Result> MarkCompleteAsync(Guid jobId, Guid? resultId, CancellationToken ct = default);
    Task<Result> MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BackgroundJob>>> GetPendingAsync(string jobType, int limit, CancellationToken ct = default);
}

// ISeoStorageRepository.cs — Supabase Storage; only GeekRepository talks to the Storage API
public interface ISeoStorageRepository
{
    Task<Result<string>> UploadReportAsync(
        string objectPath, byte[] bytes, string contentType, CancellationToken ct = default);
    Task<Result<string>> GetSignedDownloadUrlAsync(
        string objectPath, TimeSpan expiry, CancellationToken ct = default);
    Task<Result> DeleteObjectAsync(string objectPath, CancellationToken ct = default);
}
```

### GeekAPI/Hubs/SeoContentScoringHub.cs

```csharp
// GeekAPI/Hubs/SeoContentScoringHub.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GeekApplication.Interfaces.Seo;

namespace GeekAPI.Hubs;

[Authorize]
public sealed class SeoContentScoringHub(IContentScoringService scoringService) : Hub
{
    public async Task JoinDocument(string documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"doc:{documentId}");
    }

    public async Task LeaveDocument(string documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"doc:{documentId}");
    }

    // Thin hub — all business logic in IContentScoringService (Application layer)
    public async Task ContentChanged(string documentId, string contentHtml, string targetKeyword)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return;

        var result = await scoringService.ProcessContentChangedAsync(
            userId, Guid.Parse(documentId), contentHtml, targetKeyword, Context.ConnectionAborted);

        if (!result.IsSuccess) return;

        var hubResult = result.Value!;
        if (hubResult.PendingReason is not null)
        {
            await Clients.Caller.SendAsync("ScorePending", new { documentId, reason = hubResult.PendingReason });
            return;
        }

        if (hubResult.ScoreUpdate is not null)
        {
            await Clients.Group($"doc:{documentId}").SendAsync("ScoreUpdate", hubResult.ScoreUpdate);
        }
    }

    public async Task KeywordChanged(string documentId, string newKeyword, string location)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return;

        await Clients.Group($"doc:{documentId}")
            .SendAsync("BenchmarkRefreshing", new { documentId, keyword = newKeyword });

        var result = await scoringService.ProcessKeywordChangedAsync(
            userId, Guid.Parse(documentId), newKeyword, location, Context.ConnectionAborted);

        if (result.IsSuccess)
        {
            await Clients.Group($"doc:{documentId}")
                .SendAsync("BenchmarkReady", new { documentId, keyword = newKeyword });
        }
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record ScoreUpdateMessage
{
    public required string DocumentId { get; init; }
    public required int Score { get; init; }
    public required string Grade { get; init; }
    public required object Components { get; init; }
    public required IReadOnlyList<object> Suggestions { get; init; }
    public IReadOnlyList<SerpFeatureGuidance> SerpFeatures { get; init; } = [];
    public IReadOnlyList<EeatAdvisory> EeatAdvisories { get; init; } = [];
    public required DateTimeOffset Timestamp { get; init; }
}
```

### GeekAPI/Controllers/Seo/ — Route Signatures

```csharp
// ProjectsController.cs
[ApiController, Route("api/seo/projects"), Authorize]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]             public Task<IActionResult> List(CancellationToken ct);
    [HttpGet("{id}")]     public Task<IActionResult> Get(Guid id, CancellationToken ct);
    [HttpPost]            public Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken ct);
    [HttpPut("{id}")]     public Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req, CancellationToken ct);
    [HttpDelete("{id}")]  public Task<IActionResult> Delete(Guid id, CancellationToken ct);
}

// ContentController.cs — IContentDocumentService only (no repository injection)
[ApiController, Route("api/seo/content"), Authorize]
public sealed class ContentController(IContentDocumentService contentService) : ControllerBase
{
    [HttpGet]                public Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct);
    [HttpGet("{id}")]        public Task<IActionResult> Get(Guid id, CancellationToken ct);
    [HttpPost]               public Task<IActionResult> Create([FromBody] CreateContentDocumentRequest req, CancellationToken ct);
    [HttpPut("{id}/content")] public Task<IActionResult> UpdateContent(Guid id, [FromBody] UpdateContentRequest req, CancellationToken ct);
    [HttpPost("{id}/score")]  public Task<IActionResult> ScoreNow(Guid id, CancellationToken ct);
    [HttpDelete("{id}")]     public Task<IActionResult> Delete(Guid id, CancellationToken ct);
}

// BriefController.cs
[ApiController, Route("api/seo/briefs"), Authorize]
public sealed class BriefController(IContentBriefService briefService) : ControllerBase
{
    [HttpPost("generate")] public Task<IActionResult> Generate([FromBody] GenerateBriefRequest req, CancellationToken ct);
}

// WritingController.cs
[ApiController, Route("api/seo/writing"), Authorize]
public sealed class WritingController(IAIWritingService writingService) : ControllerBase
{
    [HttpPost("outline")]  public Task<IActionResult> GenerateOutline([FromBody] GenerateOutlineRequest req, CancellationToken ct);
    [HttpPost("draft")]    public Task<IActionResult> GenerateDraft([FromBody] GenerateDraftRequest req, CancellationToken ct);
    [HttpPost("optimize")] public Task<IActionResult> OptimizeContent([FromBody] OptimizeContentRequest req, CancellationToken ct);
}

// KeywordsController.cs
[ApiController, Route("api/seo/keywords"), Authorize]
public sealed class KeywordsController(IKeywordResearchService keywordService) : ControllerBase
{
    [HttpPost("research")]   public Task<IActionResult> Research([FromBody] KeywordResearchRequest req, CancellationToken ct);
    [HttpPost("cluster")]    public Task<IActionResult> Cluster([FromBody] ClusterKeywordsRequest req, CancellationToken ct);
    [HttpGet("project/{projectId}")] public Task<IActionResult> GetProjectKeywords(Guid projectId, CancellationToken ct);
}

// AuditController.cs
[ApiController, Route("api/seo/audit"), Authorize]
public sealed class AuditController(ISiteAuditService siteAudit, IPageAuditService pageAudit) : ControllerBase
{
    [HttpPost("site")]              public Task<IActionResult> StartSiteAudit([FromBody] StartSiteAuditRequest req, CancellationToken ct);
    [HttpGet("site/{auditId}")]     public Task<IActionResult> GetSiteAudit(Guid auditId, CancellationToken ct);
    [HttpGet("site/{auditId}/status")] public Task<IActionResult> GetSiteAuditStatus(Guid auditId, CancellationToken ct);
    [HttpGet("site/{auditId}/pages")] public Task<IActionResult> GetSiteAuditPages(Guid auditId, [FromQuery] string? severity, CancellationToken ct);
    [HttpPost("page")]              public Task<IActionResult> AuditPage([FromBody] AuditPageRequest req, CancellationToken ct);
}

// RankController.cs
[ApiController, Route("api/seo/rankings"), Authorize]
public sealed class RankController(IRankTrackingService rankService) : ControllerBase
{
    [HttpGet("{projectId}/keywords")]    public Task<IActionResult> GetKeywords(Guid projectId, CancellationToken ct);
    [HttpGet("{projectId}/history")]     public Task<IActionResult> GetHistory(Guid projectId, [FromQuery] string keyword, [FromQuery] string from, [FromQuery] string to, CancellationToken ct);
    [HttpGet("{projectId}/top")]         public Task<IActionResult> GetTopKeywords(Guid projectId, [FromQuery] int count, CancellationToken ct);
    [HttpPost("{projectId}/sync")]       public Task<IActionResult> SyncGsc(Guid projectId, CancellationToken ct);
}

// ReportsController.cs
[ApiController, Route("api/seo/reports"), Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("{projectId}")]        public Task<IActionResult> List(Guid projectId, CancellationToken ct);
    [HttpPost("site-audit")]        public Task<IActionResult> GenerateSiteAuditReport([FromBody] GenerateReportRequest req, CancellationToken ct);
    [HttpPost("rank-tracking")]     public Task<IActionResult> GenerateRankReport([FromBody] GenerateRankReportRequest req, CancellationToken ct);
    [HttpGet("{reportId}/download") public Task<IActionResult> Download(Guid reportId, CancellationToken ct);
}

// SubscriptionController.cs — webhook reads raw body (no [FromBody] on webhook action)
[ApiController, Route("api/seo/subscription")]
public sealed class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet, Authorize]            public Task<IActionResult> GetCurrentTier(CancellationToken ct);
    [HttpPost("confirm"), Authorize] public Task<IActionResult> ConfirmSubscription([FromBody] ConfirmSubscriptionRequest req, CancellationToken ct);
    [HttpDelete, Authorize]         public Task<IActionResult> Cancel(CancellationToken ct);
    [HttpPost("webhook"), AllowAnonymous] public Task<IActionResult> Webhook(CancellationToken ct);
}

// GscController.cs
[ApiController, Route("api/seo/gsc"), Authorize]
public sealed class GscController(IGscService gscService) : ControllerBase
{
    [HttpGet("auth-url")]           public Task<IActionResult> GetAuthUrl([FromQuery] Guid projectId, CancellationToken ct);
    [HttpGet("callback"), AllowAnonymous] public Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken ct);
    [HttpDelete("{projectId}")]     public Task<IActionResult> Disconnect(Guid projectId, CancellationToken ct);
    [HttpGet("{projectId}/status")] public Task<IActionResult> GetStatus(Guid projectId, CancellationToken ct);
}

// SerpController.cs
[ApiController, Route("api/seo/serp"), Authorize]
public sealed class SerpController(IContentScoringService scoringService) : ControllerBase
{
    [HttpGet] public Task<IActionResult> GetOverview(...);
    [HttpGet("deep")] public Task<IActionResult> GetDeepAnalysis(...);
}

// WritingController.cs — extend existing
[HttpPost("full-article")] public Task<IActionResult> GenerateFullArticle(...);
[HttpPost("humanize")]    public Task<IActionResult> Humanize(...);
[HttpPost("bulk")]        public Task<IActionResult> StartBulkJob(...);
[HttpGet("bulk/{jobId}")] public Task<IActionResult> GetBulkJobStatus(...);

// WordPressController.cs — ContentShake clone
[ApiController, Route("api/seo/wordpress"), Authorize]
public sealed class WordPressController(IWordPressPublishService wpService) : ControllerBase
{
    [HttpPost("connect")]    public Task<IActionResult> Connect(...);
    [HttpPost("publish")]    public Task<IActionResult> Publish(...);
    [HttpDelete("{projectId}")] public Task<IActionResult> Disconnect(...);
}

// TopicalMapController.cs
[ApiController, Route("api/seo/topical-map"), Authorize]
public sealed class TopicalMapController(ITopicalMapService topicalMapService) : ControllerBase
{
    [HttpPost("generate")] public Task<IActionResult> Generate(...);
    [HttpGet("{mapId}")]   public Task<IActionResult> Get(...);
}

// PublishedContentAuditController.cs (not Site AuditController)
[ApiController, Route("api/seo/content-audit"), Authorize]
public sealed class PublishedContentAuditController(IPublishedContentAuditService auditService) : ControllerBase
{
    [HttpGet]              public Task<IActionResult> Dashboard(...);
    [HttpPost("register")] public Task<IActionResult> RegisterPage(...);
}

// InternalLinksController.cs
[ApiController, Route("api/seo/links"), Authorize]
public sealed class InternalLinksController(IInternalLinkingService linkService) : ControllerBase
{
    [HttpGet("suggest")] public Task<IActionResult> Suggest(...);
}

// PlagiarismController.cs
[ApiController, Route("api/seo/plagiarism"), Authorize]
public sealed class PlagiarismController(IPlagiarismService plagiarismService) : ControllerBase
{
    [HttpPost("check")] public Task<IActionResult> Check(...);
}

// AnalyticsController.cs — GA4 clone
[ApiController, Route("api/seo/analytics"), Authorize]
public sealed class AnalyticsController(IGoogleAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("ga4/auth-url")] public Task<IActionResult> GetGa4AuthUrl(...);
    [HttpGet("ga4/callback"), AllowAnonymous] public Task<IActionResult> Ga4Callback(...);
    [HttpGet("ga4/{projectId}/landing-pages")] public Task<IActionResult> LandingPages(...);
}

// GeoVisibilityController.cs
[ApiController, Route("api/seo/geo"), Authorize]
public sealed class GeoVisibilityController(IGeoVisibilityService geoService) : ControllerBase
{
    [HttpGet("{projectId}")]  public Task<IActionResult> ListQueries(...);
    [HttpPost]                public Task<IActionResult> AddQuery(...);
    [HttpGet("history/{queryId}")] public Task<IActionResult> History(...);
}

// BrandVoiceController.cs
[ApiController, Route("api/seo/brand-voice"), Authorize]
public sealed class BrandVoiceController(IBrandVoiceService brandVoiceService) : ControllerBase
{
    [HttpGet]  public Task<IActionResult> List(...);
    [HttpPost] public Task<IActionResult> Create(...);
    [HttpPut("default/{id}")] public Task<IActionResult> SetDefault(...);
}

// CannibalizationController.cs
[ApiController, Route("api/seo/cannibalization"), Authorize]
public sealed class CannibalizationController(IKeywordCannibalizationService service) : ControllerBase
{
    [HttpGet("{projectId}")] public Task<IActionResult> Detect(...);
}

// ContentController.cs — extend existing (auto-optimize + status)
[HttpPost("{id}/auto-optimize")] public Task<IActionResult> AutoOptimize(Guid id, CancellationToken ct);
[HttpPatch("{id}/status")]       public Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct);

// WritingController.cs — extend existing (AI detection)
[HttpPost("detect")] public Task<IActionResult> Detect([FromBody] DetectAiRequest req, CancellationToken ct);

// JobsController.cs — poll any seo_background_jobs row
[ApiController, Route("api/seo/jobs"), Authorize]
public sealed class JobsController(IBackgroundJobService jobService) : ControllerBase
{
    [HttpGet("{jobId}")] public Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct);
}

// PublicApiController.cs — Agency tier API keys
[ApiController, Route("api/seo/v1")]
public sealed class PublicApiController(...) : ControllerBase
{
    // Same surface as UI routes; authenticates via X-Api-Key header (Agency tier)
}

// IntegrationsController.cs
[HttpGet("openapi.json"), AllowAnonymous] public Task<IActionResult> OpenApiSpec(...);
```

### Clone User Flows

**Guided Mode (ContentShake clone):**
```
/app/guided
  Step 1: Business context (industry, location, site URL) → saves to seo_projects
  Step 2: Keyword ideas (KeywordResearchService) → user picks one
  Step 3: Optional brief preview
  Step 4: "Generate article" → POST /api/seo/writing/full-article → poll GET /api/seo/jobs/{jobId}
  Step 5: Opens ContentEditor with score sidebar; checklist (score ≥ 70, meta, plagiarism)
  Step 6: "Publish to WordPress" → POST /api/seo/wordpress/publish → success URL
```

**Expert Mode — Surfer AI clone:**
```
/app/content → New → Enter keyword → "Generate with AI" (full-article) → editor
```

**Surfer Topical Map clone:**
```
/app/strategy/topical-map → Generate (requires GSC) → cluster cards → "Write this" → new document
```

### Route → tier → meter matrix (complete)

Implement in `GeekApplication/Constants/Seo/FeatureGates.cs` and `UsageLimits.cs`. Middleware matches longest path prefix.

**Minimum tier gates** (`SeoFeatureGateMiddleware` — 402 if below tier):

| Path prefix | Min tier |
|---|---|
| `/api/seo/gsc` | Professional |
| `/api/seo/rankings` | Professional |
| `/api/seo/audit/site` | Professional |
| `/api/seo/reports` | Professional |
| `/api/seo/analytics/ga4` | Professional |
| `/api/seo/content-audit` | Professional |
| `/api/seo/topical-map` | Professional |
| `/api/seo/cannibalization` | Professional |
| `/api/seo/geo` | Professional |
| `/api/seo/serp/deep` | Starter (metered) |
| `/api/seo/writing/bulk` | Professional |
| `/api/seo/v1` | Agency (public API + `X-Api-Key`) |
| `/api/seo/writing/full-article` | Starter |
| `/api/seo/writing/humanize` | Starter |
| `/api/seo/writing/detect` | Starter |
| `/api/seo/plagiarism` | Starter |
| `/api/seo/content/` + `/auto-optimize` | Starter |
| `/api/seo/wordpress` | Starter |
| `/api/seo/links` | Starter |
| `/api/seo/keywords/cluster` | Starter |
| `/api/seo/writing/outline`, `/draft`, `/optimize` | Starter |

**Metered routes** (`SeoUsageGateMiddleware` — 429 if over monthly cap; increment after success):

| Route key | Usage feature key |
|---|---|
| `POST:/api/seo/content` | `content_document` |
| `POST:/api/seo/briefs/generate` | `content_brief` |
| `POST:/api/seo/writing/draft` | `ai_draft` |
| `POST:/api/seo/writing/full-article` | `full_article` |
| `POST:/api/seo/writing/humanize` | `humanize` |
| `POST:/api/seo/writing/detect` | `ai_detect` |
| `POST:/api/seo/writing/bulk` | `bulk_job` |
| `POST:/api/seo/keywords/research` | `keyword_lookup` |
| `POST:/api/seo/audit/page` | `page_audit` |
| `POST:/api/seo/audit/site` | `site_audit` |
| `GET:/api/seo/serp/deep` | `deep_serp` |
| `POST:/api/seo/plagiarism/check` | `plagiarism_check` |
| `POST:/api/seo/content/` + `/auto-optimize` | `auto_optimize` |
| `POST:/api/seo/topical-map/generate` | `topical_map_refresh` |

Monthly caps per tier: Section 9 feature gate table. `UsageLimits.cs` is the single source of truth.

### Editor workflow: humanize, auto-optimize, publish

1. **AI detect** (`POST /writing/detect`) — optional; caches `ai_detection_score` on document.
2. **Humanize** — rewrites content; **always triggers re-score** via SignalR; Guided Mode blocks publish if score drops >5 points from pre-humanize snapshot without user confirm.
3. **Auto-optimize** — returns patched `content_html` + `ContentScoreResult`; frontend applies as one TipTap transaction; show delta ("+8 pts estimated").
4. **Publish checklist (Guided)** — score ≥ target, plagiarism pass, meta filled, optional humanize if `ai_detection_score` > 0.7.

### GeekAPI/Middleware/SeoFeatureGateMiddleware.cs

Controllers inject `ISubscriptionService` only. Middleware resolves tier from JWT `sub` and compares to `FeatureGates.cs` (table above). Returns 402 JSON with `requiredTier` and `upgradeUrl`.

### GeekAPI/Middleware/SeoUsageGateMiddleware.cs

Runs after feature gate. Pre-check `IUsageMeteringService.EnsureWithinLimitAsync`; post-handler `IncrementAsync` via `IAsyncResultFilter` or middleware `finally` on 2xx responses.

---

## 7. Frontend Page Inventory

All pages use Next.js 16 App Router with React 19. Authenticated pages live under `(app)` route group with layout that validates session via GeekBackend OAuth 2.1 PKCE.

### Frontend Data Access (NON-NEGOTIABLE)

The Next.js app is a **pure API client** to GeekBackend. Do not add:

| Forbidden | Use instead |
|---|---|
| Prisma | GeekBackend REST (`/api/seo/*`) |
| Supabase JS client | GeekBackend REST + SignalR |
| NextAuth / Auth.js | GeekBackend OAuth 2.1 PKCE (same pattern as geekatyourspot-r) |
| Vercel AI SDK / direct Anthropic calls from Next.js | `/api/seo/writing/*`, `/api/seo/briefs/*` on GeekBackend |
| Next.js Server Actions that touch the database | Server Components may call GeekBackend with the user's Bearer token only |

All persistence, provider keys, and business logic live in GeekBackend. The frontend holds UI state, TipTap document HTML, and the OAuth access token (memory + httpOnly refresh cookie via GeekBackend).

### Public Routes

| Route | Component | Description |
|---|---|---|
| `/` | `LandingPage` | Hero, feature overview, testimonials (dogfood proof), pricing teaser, CTA |
| `/pricing` | `PricingPage` | Four-tier table, PayPal checkout buttons, feature comparison table |
| `/login` | `LoginPage` | GeekBackend OAuth PKCE flow |
| `/signup` | `SignupPage` | Registration + immediate OAuth |

### Authenticated Routes (`/app/*`)

| Route | Primary Component | Data Sources |
|---|---|---|
| `/app/dashboard` | `DashboardPage` | seo_projects, seo_content_documents, seo_rank_tracking |
| `/app/content` | `ContentListPage` | seo_content_documents (by project) |
| `/app/content/[id]` | `ContentEditorPage` | SignalR WebSocket + REST content API |
| `/app/briefs/new` | `ContentBriefPage` | /api/seo/briefs/generate |
| `/app/write` | `AIWritingPage` | /api/seo/writing/outline, /draft, /optimize |
| `/app/keywords` | `KeywordResearchPage` | /api/seo/keywords/research, /cluster |
| `/app/audit` | `SiteAuditListPage` | seo_site_audits by project |
| `/app/audit/[id]` | `SiteAuditResultsPage` | seo_site_audit_pages |
| `/app/audit/page` | `PageAuditPage` | /api/seo/audit/page |
| `/app/rankings` | `RankTrackingPage` | seo_rank_tracking |
| `/app/reports` | `ReportsPage` | seo_reports |
| `/app/settings` | `SettingsPage` | subscription, GSC, GA4, WordPress, brand voice, mode toggle |
| `/app/guided` | `GuidedWizardPage` | ContentShake clone — 6-step publish flow |
| `/app/strategy/topical-map` | `TopicalMapPage` | Surfer Topical Map clone |
| `/app/strategy/cannibalization` | `CannibalizationPage` | Surfer cannibalization clone |
| `/app/content-audit` | `PublishedContentAuditPage` | Surfer Content Audit clone |
| `/app/serp/[keyword]` | `SerpDeepAnalysisPage` | Surfer SERP Analyzer (50 results) |
| `/app/geo` | `GeoVisibilityPage` | Surfer AI Visibility clone |
| `/app/analytics` | `AnalyticsPage` | ContentShake GA4 clone |
| `/app/bulk` | `BulkGenerationPage` | Bulk article queue |
| `/app/calendar` | `ContentCalendarPage` | Kanban: Planned → Writing → Review → Published. Drag card calls `PATCH /api/seo/content/{id}/status`. ContentShake planning board clone. |

**Editor enhancements (clone):** `InternalLinksPanel`, `PlagiarismButton`, `HumanizeButton`, `AiDetectionBadge`, `AutoOptimizeButton`, `PublishToWordPressButton`, `GenerateFullArticleButton`, `SerpFeatureGuidancePanel`.

**Separate repos (thin clients, same API):**
- `integrations/google-docs-addon/` — Apps Script
- `integrations/chrome-extension/` — MV3
- `integrations/chatgpt-openapi/` — spec + GPT Action config

### Key Component Interfaces

```typescript
// ContentEditor — TipTap + WebSocket score updates
interface ContentEditorProps {
  documentId: string;
  initialContent: string;
  targetKeyword: string;
  location: string;
  onSave: (content: string) => Promise<void>;
}

// ScoreSidebar — receives ScoreUpdate from SignalR
interface ScoreSidebarProps {
  score: number;                          // 0-100
  grade: 'A' | 'B' | 'C' | 'D' | 'F';
  components: ScoreComponents;
  suggestions: SuggestionItem[];
  isLoading: boolean;
}

interface ScoreComponents {
  termCoverage:     { score: number; maxScore: 35; terms: TermSuggestion[] };
  wordCount:        { score: number; maxScore: 20; current: number; min: number; max: number };
  headingStructure: { score: number; maxScore: 15; h1: boolean; h2Count: number; h3Count: number };
  titleTag:         { score: number; maxScore: 10; hasKeyword: boolean; length: number };
  metaDescription:  { score: number; maxScore: 10; hasKeyword: boolean; length: number };
  readability:      { score: number; maxScore: 10; fleschKincaid: number; benchmarkGrade: number };
}

interface SuggestionItem {
  component: string;
  pointValue: number;
  actionText: string;
  currentValue: string | number;
  targetValue: string | number;
}

// SerpCompetitorPanel
interface SerpCompetitorPanelProps {
  keyword: string;
  location: string;
  competitors: CompetitorRow[];
  isLoading: boolean;
}

interface CompetitorRow {
  position: number;
  url: string;
  domain: string;
  wordCount: number;
  score: number | null;
  h2Count: number;
  h3Count: number;
}

// RankHistoryChart
interface RankHistoryChartProps {
  data: RankDataPoint[];
  keyword: string;
}

interface RankDataPoint {
  date: string;
  position: number | null;
  impressions: number;
  clicks: number;
}

// SubscriptionGate — wraps any feature that requires a paid tier
interface SubscriptionGateProps {
  requiredTier: 'starter' | 'professional' | 'team' | 'agency';
  currentTier: 'none' | 'starter' | 'professional' | 'team' | 'agency';
  children: React.ReactNode;
  featureName: string;
}

// GscConnectButton
interface GscConnectButtonProps {
  projectId: string;
  isConnected: boolean;
  onConnected: () => void;
}
```

### SignalR WebSocket Client Hook

```typescript
// hooks/useContentScoring.ts

import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { ScoreUpdateMessage } from '@/types/scoring';

export function useContentScoring(
  documentId: string,
  onScoreUpdate: (update: ScoreUpdateMessage) => void
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/seo-scoring`, {
        accessTokenFactory: () => getAccessToken(),
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ScoreUpdate', (update: ScoreUpdateMessage) => {
      onScoreUpdate(update);
    });

    connection.on('BenchmarkRefreshing', () => {
      // Show "refreshing benchmarks" indicator in ScoreSidebar
    });

    connection.start()
      .then(() => connection.invoke('JoinDocument', documentId))
      .catch(console.error);

    connectionRef.current = connection;

    return () => {
      connection.invoke('LeaveDocument', documentId).catch(() => {});
      connection.stop();
    };
  }, [documentId, onScoreUpdate]);

  const notifyContentChanged = useCallback(
    (contentHtml: string, targetKeyword: string) => {
      if (debounceRef.current) clearTimeout(debounceRef.current);

      debounceRef.current = setTimeout(() => {
        connectionRef.current?.invoke(
          'ContentChanged', documentId, contentHtml, targetKeyword
        ).catch(console.error);
      }, 800);
    },
    [documentId]
  );

  const notifyKeywordChanged = useCallback(
    (newKeyword: string, location: string) => {
      connectionRef.current?.invoke(
        'KeywordChanged', documentId, newKeyword, location
      ).catch(console.error);
    },
    [documentId]
  );

  return { notifyContentChanged, notifyKeywordChanged };
}
```

---

## 8. Content Scoring Algorithm Summary

Full specification is in `geekseo-content-scoring-spec.md`. Summary:

**Input:** HTML content string + target keyword + location string

**Process:**
1. Fetch SERP results for keyword + location via ISerpProvider (cache key: `{keyword}:{location}:{languageCode}`, TTL 24 hours in `seo_serp_results`)
2. For each organic result URL, check `seo_competitor_pages` cache (TTL 72 hours). On cache miss, call ICrawlerProvider.CrawlPageAsync. Store result.
3. IAIProvider extracts semantic terms from each competitor page text
4. Calculate benchmarks: avg word count, avg H2 count, term frequency ranges across top 10 competitors
5. Run 6-component scoring against benchmarks (see spec for formula)
6. Generate prioritized suggestion list (sorted by point value descending)

**Output:** `ContentScoreResult` with total score 0-100, letter grade (A-F), per-component breakdown, and ordered suggestion array. **E-E-A-T advisories** and **SERP feature guidance** are separate (not scored) — see `geekseo-content-scoring-spec.md` §7.

**Caching:** SERP results cached 24 hours. Competitor pages cached 72 hours. During a writing session, only the first content score request triggers external API calls. All subsequent scoring during the same session uses cached benchmarks.

---

## 9. PayPal Integration

### Subscription Plans

| Tier | Monthly Price | PayPal Plan ID (env var) |
|---|---|---|
| Starter | $29/month | `PAYPAL_PLAN_ID_STARTER` |
| Professional | $59/month | `PAYPAL_PLAN_ID_PROFESSIONAL` |
| Team | $89/month | `PAYPAL_PLAN_ID_TEAM` |
| Agency | $149/month | `PAYPAL_PLAN_ID_AGENCY` |

PayPal subscription plan IDs are created in the PayPal Developer Dashboard and stored as environment variables. Never hardcode them.

### Checkout Flow

```
1. User visits /pricing, selects a tier
2. PayPal JS SDK renders the PayPal button (client-side only, no server call yet)
3. User clicks the PayPal button → PayPal popup opens
4. createSubscription callback fires:
   → POST /api/seo/subscription/confirm is NOT called here
   → Return the PayPal plan ID for the selected tier
   → PayPal creates the subscription in their system
5. onApprove callback fires with { subscriptionID }:
   → POST /api/seo/subscription/confirm { subscriptionId, tier }
   → Backend verifies subscription state via PayPal API before storing
   → Status stored as 'pending_webhook'
6. PayPal fires BILLING.SUBSCRIPTION.ACTIVATED webhook within seconds:
   → POST /api/seo/subscription/webhook
   → Backend updates status to 'active'
   → Features become available immediately
```

### Webhook Handler Requirements

The webhook endpoint must be unauthenticated (PayPal calls it, not the user). Validate PayPal webhook signatures using PayPal's verification API to prevent spoofing.

```csharp
// SubscriptionController.cs — Webhook method (ISubscriptionService only — provider inside service)

[HttpPost("webhook"), AllowAnonymous]
public async Task<IActionResult> Webhook(CancellationToken ct)
{
    var rawBody = await new StreamReader(Request.Body).ReadToEndAsync(ct);
    var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
    var result = await subscriptionService.ProcessWebhookAsync(rawBody, headers, ct);
    return result.IsSuccess ? Ok() : result.ErrorCode == "unauthorized" ? Unauthorized() : StatusCode(500);
}
```

### Feature Gate Tiers

| Feature | Starter ($29) | Professional ($59) | Team ($89) | Agency ($149) |
|---|---|---|---|---|
| Content editor | 20 docs/mo | 60 docs/mo | 150 docs/mo | Unlimited |
| Real-time scoring | Yes | Yes | Yes | Yes |
| Content briefs | 20/mo | 60/mo | 150/mo | Unlimited |
| AI writing (outline) | Yes | Yes | Yes | Yes |
| AI writing (full draft) | 5/mo | 20/mo | 50/mo | Unlimited |
| Keyword research | 50 lookups/mo | 200/mo | 500/mo | Unlimited |
| Keyword clustering | Yes | Yes | Yes | Yes |
| Site-wide audit | No | 1 site | 3 sites | Unlimited |
| Single-page audit | 5/mo | 20/mo | Unlimited | Unlimited |
| GSC + rank tracking | No | Yes | Yes | Yes |
| PDF reports | No | Yes | Yes | Yes |
| Re-crawl alerts | No | Yes | Yes | Yes |
| Full-article AI (Surfer AI / ContentShake) | 3/mo | 15/mo | 40/mo | Unlimited |
| WordPress publish | Yes | Yes | Yes | Yes |
| Topical map refresh | No | 2/mo | 4/mo | Unlimited |
| Published content audit | No | Yes | Yes | Yes |
| Plagiarism checks | 10/mo | 50/mo | 150/mo | Unlimited |
| Deep SERP (50) | 5/mo | 30/mo | 100/mo | Unlimited |
| GEO visibility queries | No | 5 | 20 | Unlimited |
| GA4 dashboard | No | Yes | Yes | Yes |
| Bulk article jobs | No | 1 job (10 kw) | 3 jobs | Unlimited |
| Public API + API keys | No | No | No | Yes |
| Brand voices | 1 | 3 | 10 | Unlimited |
| Humanizer | 10/mo | 50/mo | Unlimited | Unlimited |
| Auto-optimize | 20/mo | 100/mo | Unlimited | Unlimited |
| AI detection checks | 20/mo | 100/mo | Unlimited | Unlimited |
| Internal link suggestions | Unlimited | Unlimited | Unlimited | Unlimited |
| Cannibalization reports | No | 5/mo | 20/mo | Unlimited |

### Unit economics (COGS guardrails)

Estimated variable cost per **full document lifecycle** (SERP + 10 crawls + brief + draft + 5 re-scores + 1 plagiarism):

| Cost driver | Est. per doc | Mitigation |
|---|---|---|
| DataForSEO SERP (10) | $0.003 | 24h cache key `(keyword, location)` |
| Playwright crawls (10 pages) | $0.00 (self-hosted) | Singleton pool; cap 10 competitors |
| Claude (brief + draft + terms) | $0.15–$0.40 | Token caps in `IAIProvider`; brand voice cached |
| Copyscape | $0.05 | Starter: 10/mo cap |
| GPTZero | $0.02 | Metered |
| Deep SERP (50) | $0.01 | Separate cache; 5–30/mo tier caps |

**Target gross margin:** ≥70% on Professional ($59) at 20 full-article equivalents/mo. `UsageLimits.cs` must enforce hard caps so a single Starter user cannot exceed ~$8 COGS/mo. Log per-request token + API spend in structured logs for monthly review.

---

## 10. GSC Integration

### OAuth 2.0 Consent Flow

Required scope: `https://www.googleapis.com/auth/webmasters.readonly`

The GscService constructs the authorization URL, stores a CSRF token (base64-encoded `userId:projectId:csrfNonce` in the state parameter), and handles the callback. On successful callback, the code is exchanged for tokens via a POST to `https://oauth2.googleapis.com/token`.

### Token Storage

The refresh token must never be stored in plaintext. AES-256-GCM encryption:

```csharp
// GeekApplication/Services/Seo/GscService.cs — EncryptRefreshToken

private (byte[] CipherText, byte[] IV, byte[] Tag) EncryptRefreshToken(string refreshToken)
{
    var key = Convert.FromBase64String(
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? throw new InvalidOperationException("ENCRYPTION_KEY not set"));

    using var aes = new AesGcm(key, 16);  // 128-bit tag
    var plaintext = Encoding.UTF8.GetBytes(refreshToken);
    var iv = new byte[12];       // 96-bit nonce for GCM
    var ciphertext = new byte[plaintext.Length];
    var tag = new byte[16];

    RandomNumberGenerator.Fill(iv);
    aes.Encrypt(iv, plaintext, ciphertext, tag);

    return (ciphertext, iv, tag);
}

private string DecryptRefreshToken(byte[] ciphertext, byte[] iv, byte[] tag)
{
    var key = Convert.FromBase64String(
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? throw new InvalidOperationException("ENCRYPTION_KEY not set"));

    using var aes = new AesGcm(key, 16);
    var plaintext = new byte[ciphertext.Length];
    aes.Decrypt(iv, ciphertext, tag, plaintext);

    return Encoding.UTF8.GetString(plaintext);
}
```

### Daily GSC Sync (IHostedService)

```csharp
// GeekAPI/BackgroundServices/GscSyncService.cs

public sealed class GscSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<GscSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = GetNextRunTime();
            await Task.Delay(nextRun, stoppingToken);

            await SyncAllProjectsAsync(stoppingToken);
        }
    }

    private async Task SyncAllProjectsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var rankService = scope.ServiceProvider.GetRequiredService<IRankTrackingService>();

        // IRankTrackingService.SyncAllConnectedProjectsAsync — Application layer only
        // (internally uses IGscRepository + IRankRepository; worker never touches repositories)
        await rankService.SyncAllConnectedProjectsAsync(ct);
    }

    private static TimeSpan GetNextRunTime()
    {
        var now = DateTimeOffset.UtcNow;
        var nextRun = now.Date.AddDays(1).AddHours(2); // 2 AM UTC daily
        return nextRun - now;
    }
}
```

### GSC API Call Pattern

```csharp
// GeekApplication/Services/Seo/GscService.cs — FetchSearchAnalyticsAsync

public async Task<Result<IReadOnlyList<GscDataPoint>>> FetchSearchAnalyticsAsync(
    Guid projectId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
{
    var connectionResult = await _gscRepository.GetConnectionAsync(projectId, ct);
    if (!connectionResult.IsSuccess) return Result<IReadOnlyList<GscDataPoint>>.NotFound("GSC not connected");

    var connection = connectionResult.Value!;
    var refreshToken = DecryptRefreshToken(
        connection.EncryptedRefreshToken,
        connection.EncryptionIv,
        connection.EncryptionTag);

    // Exchange refresh token for access token
    var accessToken = await GetAccessTokenAsync(refreshToken, ct);

    // Call Search Analytics API
    var requestBody = new
    {
        startDate = startDate.ToString("yyyy-MM-dd"),
        endDate = endDate.ToString("yyyy-MM-dd"),
        dimensions = new[] { "query", "page", "date" },
        rowLimit = 25000,
        startRow = 0
    };

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var response = await http.PostAsJsonAsync(
        $"https://www.googleapis.com/webmasters/v3/sites/{Uri.EscapeDataString(connection.SiteUrl)}/searchAnalytics/query",
        requestBody, ct);

    response.EnsureSuccessStatusCode();
    // Deserialize and map to IReadOnlyList<GscDataPoint>
    // Each row: keys[0]=query, keys[1]=page, keys[2]=date, clicks, impressions, ctr, position
}
```

---

## 11. Flat Implementation Steps

Ordered for forward momentum. **Layer rule on every step:** GeekAPI → Application Service → Repository → DB/Storage. No shortcuts.

### A. Database & contracts

1. Create PostgreSQL role `geekseo_app` on Supabase `mpnruwauxsqbrxvlksnf` with grants on schema `geek_seo` only (see Section 2). Set `GEEK_SEO_DATABASE_URL` in Railway — never expose to Vercel.

2. Add provider interfaces in `GeekApplication/Interfaces/Seo/` per Section 4 (core + clone providers with full C# contracts).

3. Add all service interfaces per Section 6 including `IBackgroundJobService`, `EnsureAccessAsync`, `ProcessWebhookAsync`, extended `IContentScoringService` / `IAIWritingService`.

4. Add repository interfaces for all Section 5 tables including `IBackgroundJobRepository`.

5. Add `GeekApplication/Models/Seo/` — `ContentScoreHubResult`, `ScoreUpdateMessage` (with `SerpFeatures`, `EeatAdvisories`), `SerpOverviewResult`, `AutoOptimizeResult`.

6. Add `GeekApplication/Constants/Seo/FeatureGates.cs`, `UsageLimits.cs` (complete matrix in Section 6).

7. Create `SeoDbContext` — **31** entity mappings, `HasDefaultSchema("geek_seo")`, global query filters, **no RLS**.

8. `dotnet ef migrations add InitialSeoSchema --context SeoDbContext`. Deploy via `MigrateAsync()`.

### B. GeekRepository (data + external providers)

9. Implement all SEO repositories including `BackgroundJobRepository`, clone table repos.

10. Implement `SeoStorageRepository` (bucket `seo-reports`).

11. Implement providers: `ClaudeProvider`, `DataForSEOSerpProvider` (maps `SerpFeatures` from DataForSEO), `DataForSEOKeywordProvider`, `PlaywrightCrawlerProvider` (**singleton browser pool**), `TipTapProvider`, `PlaywrightPdfProvider`, `PayPalPaymentProvider`, `CopyscapePlagiarismProvider`, `WordPressRestProvider`, `GoogleAnalytics4Provider`, `GeoVisibilityProvider` (**Google AI Overviews only v1**), `GPTZeroProvider`.

12. Register repositories/providers; `SiteAuditWorker` upserts `seo_site_page_inventory` on audit complete.

### C. GeekApplication (services)

13. `ContentDocumentService` — `EnsureAccessAsync`, CRUD, `UpdateStatusAsync`, publish snapshots.

14. `ContentScoringService` — scoring spec, hub orchestration, `AutoOptimizeAsync`, `GetSerpFeatureGuidanceAsync`, E-E-A-T advisories; calls `IContentDocumentService.EnsureAccessAsync` (not repository directly).

15. `AIWritingService` — `EnqueueFullArticleAsync` (writes `seo_background_jobs`), `HumanizeAsync`, `DetectAsync`. `BackgroundJobWorker` processes job queue.

16. Implement remaining clone services; `SubscriptionService.ProcessWebhookAsync` owns PayPal verification.

17. Register all services in `GeekApplication/ServiceCollectionExtensions.cs`.

### D. GeekAPI (presentation)

18. Architecture test: GeekAPI must not reference GeekRepository.

19. `SeoContentScoringHub` — `IContentScoringService` only.

20. `SeoFeatureGateMiddleware` + `SeoUsageGateMiddleware` per Section 6 matrix.

21. Create **23** controllers (Section 6 + `JobsController`) — `ISubscriptionService` only on webhooks.

22. Background workers: `GscSyncService`, `SiteAuditWorker`, `BackgroundJobWorker`, `GeoScanService` — resolve `I*Service` from scope.

23. Register `geekseo` OAuth client, SignalR hub, CORS for Vercel origin.

24. Railway env vars per Section 3 + `GPTZERO_API_KEY`, `COPYSCAPE_API_KEY`.

### E. Next.js frontend

25. Create `frontend/` — no Prisma, Supabase client, NextAuth, or direct AI SDKs.

26. GeekBackend OAuth 2.1 PKCE (`useAuth`, Section 3).

27–36. All Section 7 routes including Guided Mode, calendar, clone editor toolbar.

### F. Clone integrations (Surfer + ContentShake)

37. `GuidedWizardPage` — 6 steps; poll `GET /api/seo/jobs/{id}`.

38. WordPress connect + publish; dogfood on geekatyourspot.com.

39. `ContentCalendarPage` kanban + status PATCH.

40. Auto-optimize, AI detection badge, SERP feature guidance panel (per Section 6 editor workflow).

41–44. Google Docs add-on, Chrome extension (Agency API key), ChatGPT OpenAPI, Agency API key UI.

### G. Test & deploy

45. Playwright E2E: OAuth → editor WebSocket score → GSC → calendar → auto-optimize → detect.

46. Deploy GeekBackend; verify `MigrateAsync()` and `/health`.

47. Deploy Vercel frontend.

48. Dogfood on geekatyourspot.com (5 local keyword docs, GSC, 2–4 weeks).

49. Landing page with real GSC proof screenshots.
