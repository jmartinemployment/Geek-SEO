# Data provider strategy — augment / replace DataForSEO

**Audience:** Engineering — Geek platform (product + data plane)  
**North star:** Build **Geek-owned equivalents** of [SE Ranking](https://seranking.com/), [SerpApi](https://serpapi.com/) (+ [integrations](https://serpapi.com/integrations)), [Bright Data](https://brightdata.com/), and [Ahrefs](https://ahrefs.com/) so GeekSEO no longer depends on DataForSEO as the long-term SERP/keyword/backlink backbone.  
**Not in scope here:** Pricing comparisons.

---

## Platform ownership vision (read this first)

| Competitor | What they are | **Your version (target)** | Replaces DataForSEO how |
|------------|---------------|---------------------------|-------------------------|
| **SE Ranking** | SEO + GEO **SaaS** (projects, rank tracker, audit, reports, agency kit) | **GeekSEO product** (`seo.geekatyourspot.com`) — UI, workflows, white-label | Uses your data plane; not an API vendor |
| **SerpApi** | **SERP/search API** (engines, locations, JSON, MCP, multi-language SDKs) | **Geek SERP service** — fetch + parse Google/Bing/… → `SerpResult`; optional public `/api/serp` + MCP for agents | Replaces `DataForSEOSerpProvider` |
| **Bright Data** | **Web data infra** (proxies, unlocker, SERP API at scale, crawl, datasets) | **Geek crawl/SERP infra** — Playwright fleet, proxy pool, queue workers, anti-bot, batch jobs | Scale + reliability behind Geek SERP + audits |
| **Ahrefs** | **SEO data platform** (link index, keywords, rank, audit, Brand Radar) | **Geek SEO data** — rank history DB, keyword corpus (GSC + crawl + Ads), backlink index (phased), AI visibility probes | Replaces `DataForSEOKeywordProvider` + backlinks over time |

**Important:** Competitor sites are **blueprints**. `geek-scrape` deconstructs their **features and UX**. Production data must come from **code you control** (`ISerpProvider` → `GeekSerpProvider`, workers, GeekRepository), not from scraping those homepages at runtime.

**Interim vendors (SerpApi, DataForSEO, Bright Data APIs)** are acceptable **bootstrap** while `GeekSerpProvider` and crawl infra mature — they are not the end state if the goal is full ownership.

---

## Two different kinds of “research”

| Activity | Tool | Output | Powers production? |
|----------|------|--------|-------------------|
| **Competitor UI / workflow research** | `geek-scrape`, browser MCP, feature specs | Screenshots, `page.json`, `content.md`, clone specs | **Defines what to build** (SE Ranking / Ahrefs modules) |
| **Own data plane** | `GeekSerpProvider`, crawl workers, Postgres | `SerpResult`, rank snapshots, keyword rows | **Yes** — replaces DataForSEO when implemented |

---

## What the “DataForSEO component” is today

Registered in `GeekSeoBackend/Extensions/SeoBackendExtensions.cs`:

| Interface | Implementation | DataForSEO API used |
|-----------|----------------|---------------------|
| `ISerpProvider` | `DataForSEOSerpProvider` | `POST /v3/serp/google/organic/live/advanced` |
| `IKeywordProvider` | `DataForSEOKeywordProvider` | `POST /v3/keywords_data/google_ads/keywords_for_keywords/live` |

**Env:** `DATAFORSEO_LOGIN`, `DATAFORSEO_PASSWORD`

### Consumers of `ISerpProvider` (must keep `SerpResult` shape stable)

| Feature | Service | What it needs from SERP |
|---------|---------|------------------------|
| Deep SERP | `SerpAnalysisService` | Organic (up to 50), PAA, related, featured snippet, feature flags, cache |
| Content scoring | `ContentScoringService` | Top URLs → benchmarks (word count, titles, domains) |
| Content briefs | `ContentBriefService` | SERP context for brief generation |
| Competitor insights | `CompetitorInsightsService` | SERP URLs for crawl targets |
| Topical map v2 | `TopicalMapService` | SERP URL overlap for clustering |
| GEO probe | `GeoVisibilityService` | Organic position + `HasAiOverview` (AIO flag from same organic call) |

### Consumers of `IKeywordProvider`

| Feature | Service | What it needs |
|---------|---------|----------------|
| Keyword research / planner | `KeywordResearchService` | Volume, CPC, competition, related keywords from seed |

### Not DataForSEO today

| Capability | Provider |
|------------|----------|
| Site audit crawl | Playwright (`ICrawlerProvider`) |
| Rankings (owned site) | GSC via Google integration |
| AI writing / scoring narrative | Anthropic (`IAIProvider`) |
| Plagiarism | Separate provider |

---

## Competitor products vs GeekSEO provider roles

| Vendor | What they are | GeekSEO use |
|--------|---------------|-------------|
| **SE Ranking** | Full SEO SaaS (rank, audit, keywords, GEO UI, reports) | **Product benchmark** — what modules to build; not an API vendor |
| **Ahrefs** | SEO + Brand Radar + social + analytics | **Product benchmark**; backlinks/domain gap later via Ahrefs API or DataForSEO backlinks — not a drop-in SERP provider |
| **SerpApi** | SERP/search **API** (90+ engines, MCP for dev) | **Direct substitute for `ISerpProvider`** (+ dedicated engines for GEO) |
| **Bright Data** | Proxies + SERP API + Crawl API + datasets | **Optional** second `ISerpProvider` or rank-worker backend; unblock at scale; **no keyword volume DB** |

---

## Augment vs replace — recommended split

### Option A — **Augment** (low risk, recommended first)

Keep DataForSEO for everything it does today; add SerpApi in parallel.

| Layer | Provider |
|-------|----------|
| `ISerpProvider` | **Configurable:** `dataforseo` (default) **or** `serpapi` per env/project |
| `IKeywordProvider` | **Stay DataForSEO** (SerpApi has no volume/KD/CPC DB) |
| Rank snapshots (U1) | **SerpApi** `engine=google` (or DataForSEO if you add rank endpoint) |
| GEO AIO dedicated probe | **SerpApi** `google_ai_overview` / `google_ai_mode` (clearer than inferring AIO from organic payload) |
| Local pack / Maps | **SerpApi** `google_local`, `google_maps` when you build local features |

**Why:** SerpApi matches SE Ranking / SerpApi-style **SERP + GEO engines**; DataForSEO keeps **keyword research** and acts as fallback SERP parser you already tested in production.

### Option B — **Hybrid production** (pragmatic long-term)

| Data type | Primary | Fallback |
|-----------|---------|----------|
| SERP / deep analysis / briefs | SerpApi | DataForSEO |
| Keyword suggestions | DataForSEO | — (or Google Ads API later) |
| Daily rank history | SerpApi | DataForSEO |
| High-volume batch rank (agency tier) | Bright Data SERP API async | SerpApi |

### Option C — **Full replace DataForSEO** (only if you replace keyword data too)

You cannot drop DataForSEO with SerpApi alone. You would need:

1. **SerpApi** — all `ISerpProvider` + rank + GEO engines  
2. **Another source for keywords** — e.g. keep DataForSEO *only* for `IKeywordProvider`, or Google Ads Keyword Planner API, or Ahrefs/Semrush API (different product)  
3. **Backlinks** (U5) — DataForSEO backlinks API, Ahrefs API, or skip  

Bright Data does **not** replace keyword research; it replaces **SERP fetch reliability/volume**.

---

## SerpApi — map to GeekSEO `SerpResult`

Target: one `SerpApiSerpProvider : ISerpProvider` mapping into existing `SerpResult` / `SerpFeatures` (no frontend changes).

| `SerpResult` field | SerpApi source |
|--------------------|----------------|
| `OrganicResults` | `organic_results[]` (position, link, title, snippet) |
| `PeopleAlsoAsk` | `related_questions[]` |
| `RelatedSearches` | `related_searches[]` |
| `FeaturedSnippetText` | `answer_box` / featured snippet blocks |
| `Features.HasLocalPack` | `local_results` present |
| `Features.HasAiOverview` | `ai_overview` present |
| `Features.HasKnowledgePanel` | `knowledge_graph` |
| `Features.*` | Map from SerpApi rich results (images, videos, etc.) |

**GEO worker (upgrade):** call `engine=google_ai_overview` for probes that today only set `HasAiOverview` from organic advanced.

**Dev-only:** SerpApi hosted MCP (`mcp.serpapi.com/{key}/mcp`) for Cursor — same API, not tenant-facing.

**Integrations page relevance:** .NET via `HttpClient` to `https://serpapi.com/search` — aligns with GeekBackend patterns (no frontend SerpApi calls).

---

## Bright Data — when it augments (not primary UI competitor)

Use Bright Data when SerpApi/DataForSEO are insufficient for:

- **Mass parallel rank checks** (agency tier, thousands of keywords/day) — SERP API async mode, 195 countries  
- **Markdown/JSON SERP for LLM pipelines** — `brd_json=1`, markdown output  
- **Crawl at scale** — Crawl API if Playwright audit cap is raised beyond on-box browser  
- **Hard blocks** — Unlocker API before SERP fetch  

Implement as `BrightDataSerpProvider : ISerpProvider` **or** a separate `IRankSnapshotProvider` used only by the rank worker — avoid two providers fighting in `ContentScoringService` unless you add explicit routing.

Bright Data is **infrastructure**, like SerpApi — not a model for “competitor analysis UI.”

---

## What competitor research changes in the **product** (feeds provider requirements)

From SE Ranking / Ahrefs feature sets, these **product capabilities** drive **provider** needs:

| Product capability (from competitors) | Provider requirement |
|------------------------------------|----------------------|
| Rank tracker: daily history, desktop/mobile, top 100, SERP features column | SerpApi or Bright Data rank job → `seo_rank_snapshots` |
| AI visibility: ChatGPT, Perplexity, Gemini, AIO separate | SerpApi engines per platform — **not** generic organic SERP |
| Local rank by city/ZIP | SerpApi `google_local` + location params |
| Keyword research with volume/KD | **Keep DataForSEO** `IKeywordProvider` |
| Backlink monitor | DataForSEO backlinks or Ahrefs API (new provider) |
| Deep SERP 50+ with export | Already `ISerpProvider`; ensure SerpApi `num=50` |

**geek-scrape** on competitor rank-tracker pages → spec for **UI fields** (prompts table, AIO tab, SERP feature icons). Implementation still calls `ISerpProvider`.

---

## Implementation checklist (provider layer)

### Phase 1 — SerpApi SERP (augment)

1. `SerpApiSerpProvider` + parser tests (mirror `DataForSEOSerpProviderTests`)  
2. `SERP_PROVIDER=serpapi|dataforseo` in `SeoBackendExtensions`  
3. Store `provider` on `seo_serp_deep_cache` / usage metering (already planned in upgrade doc)  
4. Generic error messages in `SerpController` / `SerpAnalysisService` (not “DataForSEO” hardcoded)  
5. Integration test with real `SERPAPI_API_KEY`  

### Phase 2 — GEO engines

1. `IGeoSerpProbe` or extend geo worker to call SerpApi AIO engine  
2. Platform status API: show `serpapi` vs `dataforseo` per platform  

### Phase 3 — Rank history (U1)

1. `seo_tracked_keywords` + `seo_rank_snapshots` migrations  
2. Worker: SerpApi `google` + location + device; persist position + `serp_features` JSON  
3. Optional Bright Data async for bulk tier  

### Phase 4 — Keyword provider (only if replacing DataForSEO entirely)

1. Evaluate Google Ads API vs keep DataForSEO for `IKeywordProvider` only  
2. Do **not** remove DataForSEO until keyword path is decided  

---

## Configuration model (target)

```bash
# SERP features (deep SERP, briefs, scoring, topical map, default GEO organic)
SERP_PROVIDER=serpapi          # serpapi | dataforseo

# Keyword research (planner, clusters) — independent
KEYWORD_PROVIDER=dataforseo    # dataforseo only today

# Rank worker (U1)
RANK_SNAPSHOT_PROVIDER=serpapi # serpapi | dataforseo | brightdata

SERPAPI_API_KEY=...
DATAFORSEO_LOGIN=...           # still required if KEYWORD_PROVIDER=dataforseo
DATAFORSEO_PASSWORD=...
BRIGHTDATA_SERP_ZONE=...       # optional
```

---

## Decision summary

| Goal | Action |
|------|--------|
| Use competitor research for **UI/features** | `geek-scrape` + specs → frontend; **no provider change** |
| **Augment** DataForSEO with SerpApi | Add `SerpApiSerpProvider`; switch `SERP_PROVIDER`; keep `IKeywordProvider` on DataForSEO |
| **Replace** DataForSEO for SERP only | Same as above + migrate GEO to dedicated SerpApi engines |
| **Replace** DataForSEO entirely | SerpApi + new keyword provider + backlink provider — **multi-quarter** |
| Match Bright Data / enterprise scale | Bright Data SERP API on rank worker only |

**If the goal is vendor ownership (Jeff’s stated direction):** treat Option A as **temporary bridge** only. Priority order:

1. **`GeekSerpProvider`** — Playwright/Bright-style fetch + parser → `ISerpProvider` (replaces DataForSEO SERP + SerpApi dependency).  
2. **Geek crawl scale** — audit 250k pages, competitor URLs, sitemap jobs (replaces “need Bright Data” for crawl).  
3. **Geek keyword corpus** — GSC (owned queries) + Google Ads API + SERP-derived seeds (replaces DataForSEO keywords).  
4. **Geek link graph** — phased crawl/backlink store or licensed index (replaces DataForSEO/Ahrefs API for U5).  
5. **GeekSEO UI** — ship SE Ranking / Ahrefs **modules** against the above (not against DataForSEO).

**Interim bridge (optional):** SerpApi for `ISerpProvider` while (1) is built; DataForSEO for keywords while (3) is built.

---

## Related docs

- [`UPGRADE-se-ranking-agency-serpapi.md`](UPGRADE-se-ranking-agency-serpapi.md) — U1–U6 backlog, SerpApi engine table  
- [`scripts/scrape/README.md`](../scripts/scrape/README.md) — competitor page research (not runtime data)  
- [`plan-documents/api-comparison.md`](api-comparison.md) — historical API notes (verify against current vendor docs)
