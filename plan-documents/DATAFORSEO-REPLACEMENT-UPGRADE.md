# GeekDataForSEO — Provider Upgrade & Expansion Plan

## Context

GeekSeo currently uses two DataForSEO endpoints out of dozens available:
- SERP organic: `POST /v3/serp/google/organic/live/advanced`
- Keyword suggestions: `POST /v3/keywords_data/google_ads/keywords_for_keywords/live`

Competitors SE Ranking, SerpAPI, BrightData, and Ahrefs expose rank tracking, backlink analysis, domain intelligence, multi-engine SERP, and AI search visibility — all of which DataForSEO's full API supports. This plan builds `GeekDataForSEO`: a reorganized provider layer that unlocks the full DataForSEO API surface, adds SerpAPI as a production fallback, and implements SE Ranking-style AI engine visibility (ChatGPT / Gemini / Perplexity).

**Non-negotiable constraint:** `ISerpProvider` and `IKeywordProvider` interface contracts are frozen. Six services depend on `SerpResult` shape. All new capabilities get new interfaces.

---

## Architecture Decision

`GeekDataForSEO` is not a monolithic god-class. It is:
- A reorganized folder structure
- A proper injectable `DataForSeoHttpClientService` (replaces static `DataForSeoClient.cs`)
- New capability-specific providers sharing that shared HTTP client

**New provider folders:**
```
GeekSeoBackend/Providers/Seo/DataForSeo/     ← existing providers move here
GeekSeoBackend/Providers/Seo/SerpApi/        ← new fallback provider
GeekSeoBackend/Providers/Seo/AiVisibility/   ← ChatGPT / Gemini / Perplexity probes
```

**SERP provider routing via env var:**
```
SERP_PROVIDER=dataforseo          # dataforseo (default) | serpapi
RANK_SNAPSHOT_PROVIDER=dataforseo # dataforseo (default) | serpapi
```

**New env vars (additive):**
```
SERPAPI_API_KEY=...
OPENAI_API_KEY=...
GOOGLE_GEMINI_API_KEY=...
PERPLEXITY_API_KEY=...
```

---

## New Interfaces (GeekSeo.Application/Interfaces/Seo/)

| Interface | Sprint |
|-----------|--------|
| `IDataForSeoHttpClient` | 1 (internal to backend only) |
| `IRankSnapshotProvider` | 2 |
| `ITrackedKeywordRepository` + `IRankSnapshotRepository` | 2 |
| `IKeywordLabsProvider` + `IDomainAnalyticsProvider` | 3 |
| `IBacklinkProvider` | 4 |
| `IAiVisibilityProbe` | 5 |
| `IMultiEngineSerpProvider` | 6 |
| `IOnPageAuditProvider` | 7 |

---

## New EF Core Entities (GeekSeo.Persistence)

| Entity | Sprint | Purpose |
|--------|--------|---------|
| `SeoTrackedKeyword` | 2 | keyword + location + device + engine per project |
| `SeoRankSnapshot` | 2 | daily position + page URL + SERP features JSON |
| `SeoKeywordLabsCache` | 3 | domain ranked-keywords, 24h TTL |
| `SeoDomainAnalytics` | 3 | traffic estimate + organic keyword count |
| `SeoBacklinkSummary` | 4 | domain rank, counts, 48h TTL |
| `SeoBacklinkRow` | 4 | top 100 links per domain (cache) |
| `SeoReferringDomain` | 4 | top 50 referring domains per domain (cache) |
| `SeoSerpEngineCache` | 6 | multi-engine SERP results with per-engine TTL |
| `SeoOnPageTask` | 7 | DataForSEO crawl task ID + polling status |

*Sprint 5 (AI visibility): add `Engine` column to existing `SeoGeoMentionSnapshot` via migration. No new table.*

---

## Sprint 1 — Provider Architecture Consolidation (Weeks 1–2)

**Goal:** Zero regressions. Proper injectable HTTP client. Folder reorganization. Fallback wiring.

### Backend

1. **Replace** static `DataForSeoClient.cs` with `DataForSeoHttpClientService : IDataForSeoHttpClient`
   - Injectable scoped service (not static)
   - Wraps `IHttpClientFactory` for named `"DataForSEO"` client
   - Returns `Result<JsonDocument>` instead of raw `HttpResponseMessage`
   - Polly retry: exponential backoff on HTTP 429, max 3 retries, min 1s delay. No retry on other 4xx.

2. **Move** `DataForSEOSerpProvider.cs` → `Providers/Seo/DataForSeo/DataForSeoSerpProvider.cs`  
   **Move** `DataForSEOKeywordProvider.cs` → `Providers/Seo/DataForSeo/DataForSeoKeywordProvider.cs`  
   Update both to inject `IDataForSeoHttpClient`.

3. **Add** `FallbackSerpProvider : ISerpProvider`
   - Wraps primary + fallback `ISerpProvider`
   - Tries primary, on failure tries fallback
   - Preserves `ProviderName` from whichever provider actually responded

4. **Update** `SeoBackendExtensions.cs`
   - Read `SERP_PROVIDER` env var
   - Register `FallbackSerpProvider` as `ISerpProvider` (DataForSEO primary, SerpAPI stub fallback)

5. **Harden** `SerpAnalysisService`: use `DeepSerpResult.Provider` field instead of hardcoded strings

### Tests
- Update `DataForSEOSerpProviderTests.cs` to mock `IDataForSeoHttpClient`
- Add `FallbackSerpProviderTests.cs`: verify fallback called when primary returns `Failure`
- All 8 existing xUnit tests must pass

### Critical files
- `GeekSeoBackend/Providers/Seo/DataForSeoClient.cs` (replace)
- `GeekSeoBackend/Extensions/SeoBackendExtensions.cs` (update registrations)
- `GeekSeoBackend.Tests/DataForSEOSerpProviderTests.cs` (update)

---

## Sprint 2 — Rank Tracking (Weeks 3–4)

**Goal:** SE Ranking-style daily position tracking. Add keywords per project, chart position over 30 days.

**DataForSEO endpoint:** `POST /v3/serp/google/organic/live/regular` (cheaper than `advanced`)  
**SerpAPI alternative** (if `RANK_SNAPSHOT_PROVIDER=serpapi`): `GET /search?engine=google&q={q}&location={location}&device={device}`

### New Application models
```csharp
TrackedKeywordRequest { Keyword, Location, Device, Engine, Domain }
RankSnapshotResult    { Keyword, Position (int?), PageUrl, SerpFeaturesJson, SnapshotDate }
RankHistoryPoint      { Date, Position (int?), PageUrl }
```

### New service
`RankTrackingService` in `GeekSeo.Application/Services/Seo/`
- `AddTrackedKeywordAsync`, `DeleteTrackedKeywordAsync`, `GetRankHistoryAsync`

### New provider
`DataForSeoRankSnapshotProvider : IRankSnapshotProvider`

### Worker update
`SeoMaintenanceWorker` — daily job iterates `SeoTrackedKeyword` rows where `Enabled = true`, calls `IRankSnapshotProvider.FetchRankAsync`, persists to `seo_rank_snapshots`

### New HTTP repositories
`HttpTrackedKeywordRepository`, `HttpRankSnapshotRepository` → internal route `api/seo/internal/rank-tracking/*`

### New endpoints (`RankTrackingController`)
- `GET /api/seo/rank-tracking/{projectId}` — list tracked keywords
- `POST /api/seo/rank-tracking/{projectId}` — add keyword
- `DELETE /api/seo/rank-tracking/{keywordId}` — remove
- `GET /api/seo/rank-tracking/{projectId}/history?keyword=&days=30` — history points

**Feature gate:** `SubscriptionTier.Starter`

### Frontend
New `/app/rank-tracker` — keyword table, add form, sparkline per row, 30-day detail chart (Recharts)

### Tests
`RankTrackingServiceTests.cs`, `DataForSeoRankSnapshotProviderTests.cs`

---

## Sprint 3 — Keyword Labs / Domain Intelligence (Weeks 5–6)

**Goal:** Ahrefs Site Explorer equivalent. Ranked keywords by domain, competitor overlap, keyword gap analysis.

### DataForSEO endpoints
- `POST /v3/dataforseo_labs/google/ranked_keywords/live`
- `POST /v3/dataforseo_labs/google/competitors_domain/live`
- `POST /v3/dataforseo_labs/google/domain_intersection/live`
- `POST /v3/keywords_data/google_ads/keywords_for_site/live`
- `POST /v3/dataforseo_labs/google/keyword_ideas/live`

### New models
```csharp
DomainKeywordsResult  { Domain, Location, Keywords (List<DomainKeywordRow>), FetchedAt }
DomainKeywordRow      { Keyword, Position, TrafficPercent, SearchVolume, KeywordDifficulty, Url }
DomainCompetitorsResult { Domain, Competitors (List<CompetitorDomainRow>) }
CompetitorDomainRow   { Domain, IntersectingKeywords, CommonKeywords }
DomainIntersectionResult { Domain1, Domain2, UniqueToD1, UniqueToD2, Shared (List<SharedKeywordRow>) }
DomainTrafficEstimate { Domain, OrganicTraffic, OrganicKeywords, DataMonth }
```

### New service
`DomainIntelligenceService` — project-scoped auth, cache-then-fetch, 24h TTL enforced in `SeoKeywordLabsCache`

### New provider
`DataForSeoKeywordLabsProvider : IKeywordLabsProvider`

### New endpoints (`DomainIntelligenceController`)
- `GET /api/seo/domain/{domain}/keywords?location=&limit=100`
- `GET /api/seo/domain/{domain}/competitors?location=`
- `GET /api/seo/domain/keyword-gap?d1=&d2=&location=`
- `GET /api/seo/domain/{domain}/traffic?location=`

**Feature gate:** `SubscriptionTier.Professional`

### Frontend
New `/app/competitors` — domain input, ranked keywords table, traffic estimate, competitor list. Keyword gap side-by-side view.

### Integration
After SERP crawl completes, auto-enrich top 3 competitor domains with `GetRankedKeywordsAsync`, store in `SeoDomainAnalytics` cache.

### Tests
`DomainIntelligenceServiceTests.cs`, `DataForSeoKeywordLabsProviderTests.cs`

---

## Sprint 4 — Backlink Analysis (Weeks 7–8)

**Goal:** Ahrefs Backlinks equivalent — summary metrics, link list, referring domains, anchor text.

### DataForSEO endpoints
- `POST /v3/backlinks/summary/live`
- `POST /v3/backlinks/backlinks/live`
- `POST /v3/backlinks/referring_domains/live`
- `POST /v3/backlinks/anchors/live`
- `POST /v3/backlinks/domain_intersection/live`

### New models
`BacklinkSummary`, `BacklinkRow`, `ReferringDomainRow`, `AnchorRow`, `BacklinkIntersection`

### New service
`BacklinkAnalysisService` — 48h cache-then-fetch. Stores top 100 backlinks + top 50 referring domains per domain.

### New provider
`DataForSeoBacklinkProvider : IBacklinkProvider`

### New endpoints (`BacklinksController`)
- `GET /api/seo/backlinks/{domain}/summary`
- `GET /api/seo/backlinks/{domain}/links?limit=50&offset=0`
- `GET /api/seo/backlinks/{domain}/referring-domains?limit=50`
- `GET /api/seo/backlinks/{domain}/anchors`
- `GET /api/seo/backlinks/intersection?d1=&d2=`

**Feature gate:** `SubscriptionTier.Professional`

### Frontend
New `/app/backlinks` — domain input (defaults to project URL), summary metric cards, tabs: All Backlinks / Referring Domains / Anchor Text cloud.

### Integration
Add `DomainRank` to `SerpOrganicResult` — pulled from `BacklinkSummary` cache for competitor domains, surfaced in brief view.

### Tests
`BacklinkAnalysisServiceTests.cs`, `DataForSeoBacklinkProviderTests.cs`

---

## Sprint 5 — AI Search Visibility (Weeks 9–10)

**Goal:** SE Ranking-style AI visibility tab. Track brand/domain presence in ChatGPT, Gemini, and Perplexity alongside existing Google AIO. Probed daily by `SeoMaintenanceWorker`.

### New interface
```csharp
IAiVisibilityProbe {
    string Engine { get; }  // "chatgpt" | "gemini" | "perplexity"
    bool IsConfigured { get; }
    Task<Result<AiMentionResult>> ProbeAsync(string query, string domain, CancellationToken ct);
}
AiMentionResult { Engine, Mentioned, CitationSnippet, CitationUrl, ProbeDate }
```

### New providers (`Providers/Seo/AiVisibility/`)

**`ChatGptVisibilityProbe`**
- `POST https://api.openai.com/v1/responses` with `tools: [{ type: "web_search_preview" }]`
- Parse response text + citations for domain mentions
- Timeout: 10s | Env: `OPENAI_API_KEY`

**`GeminiVisibilityProbe`**
- `POST https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent`
- Include `tools: [{ googleSearch: {} }]` to enable grounding
- Parse `groundingMetadata.searchEntryPoint` + response text for mentions
- Timeout: 8s | Env: `GOOGLE_GEMINI_API_KEY`

**`PerplexityVisibilityProbe`**
- `POST https://api.perplexity.ai/chat/completions` model `llama-3.1-sonar-large-128k-online`
- Parse `choices[0].message.content` + `citations[]` array for domain matches
- Timeout: 12s | Env: `PERPLEXITY_API_KEY`

### Persistence
Add `Engine` column to `SeoGeoMentionSnapshot`. Migration: `AddEngineToGeoMentionSnapshot`.  
Default existing rows to `Engine = "google_aio"`. No new table needed.

### `GeoVisibilityService` updates
- `GetPlatformStatus()` checks each env var, returns `configured: true` per engine
- `ProbeTrackedQueryAsync` calls all configured probes via `Task.WhenAll` (max 3 concurrent)
- Individual `try/catch` per probe — one failing provider must not block others
- Already called daily by `SeoMaintenanceWorker` — no worker changes needed

### `SeoBackendExtensions.cs`
- Register all three as `IEnumerable<IAiVisibilityProbe>`
- Add named HTTP clients: `"OpenAI"`, `"Gemini"`, `"Perplexity"`

### No new endpoints
Existing `GET /api/seo/geo/platforms` and `GET /api/seo/geo/queries/{id}/trends` already return per-platform data.

### Frontend updates
- `/app/geo`: platform status cards for ChatGPT / Gemini / Perplexity (green/grey)
- Engine filter tabs on trends chart: All | Google AIO | ChatGPT | Gemini | Perplexity

### Tests
`ChatGptVisibilityProbeTests.cs`, `GeminiVisibilityProbeTests.cs`, `PerplexityVisibilityProbeTests.cs`  
Each: mock HTTP, parse response, assert domain mention detected / not detected

---

## Sprint 6 — SerpAPI Fallback + Multi-Engine SERP (Weeks 11–12)

**Goal:** Production-ready SerpAPI fallback. Bing and YouTube SERP engines added.

### SerpAPI endpoints
- `GET https://serpapi.com/search?engine=google&q={q}&location={location}&num=50`
- `GET https://serpapi.com/search?engine=bing&q={q}`
- `GET https://serpapi.com/search?engine=youtube&search_query={q}`

### DataForSEO alternative (if `SERP_PROVIDER=dataforseo`)
- `POST /v3/serp/bing/organic/live/regular`
- `POST /v3/serp/youtube/organic/live/regular`

### New provider
`SerpApiSerpProvider : ISerpProvider` in `Providers/Seo/SerpApi/`
- Named HTTP client `"SerpApi"` → `https://serpapi.com`, 30s timeout
- Auth via `api_key` query param from `SERPAPI_API_KEY`
- Maps SerpAPI JSON → `SerpResult` (organic_results, related_questions, ai_overview, video_results)
- `ProviderName` returns `"serpapi"`

### New interface
`IMultiEngineSerpProvider` with `SerpEngine` enum: `Google | Bing | YouTube`

### New service
`MultiEngineSerpService` — per-engine TTL: Google 7d, Bing 3d, YouTube 1d

### Fallback wiring
`FallbackSerpProvider` (Sprint 1) now wraps real `SerpApiSerpProvider` instead of stub.  
`SeoBackendExtensions.cs` reads `SERP_PROVIDER` to choose primary.

### New endpoints (extend `SerpController`)
- `GET /api/seo/serp/engine?keyword=&location=&engine=bing`
- `GET /api/seo/serp/engine?keyword=&location=&engine=youtube`

**Feature gate:** `SubscriptionTier.Professional`

### Frontend
Add engine selector to `/app/serp` Deep SERP form. YouTube results show title, channel, view count.

### Tests
`SerpApiSerpProviderTests.cs`, updated `FallbackSerpProviderTests.cs`

---

## Sprint 7 — DataForSEO OnPage Audit Provider (Weeks 13–14)

**Goal:** DataForSEO OnPage API as fast alternative to Playwright crawl. Instant single-page check.

### DataForSEO endpoints
- `POST /v3/on_page/task_post` — create crawl task
- `GET /v3/on_page/tasks_ready` — poll ready task IDs
- `GET /v3/on_page/pages?id={taskId}` — page-level issues
- `POST /v3/on_page/summary?id={taskId}` — aggregate issue counts
- `POST /v3/on_page/instant_pages` — single-URL live check (no queue)

### New interface
```csharp
IOnPageAuditProvider {
    Task<Result<string>> CreateAuditTaskAsync(string domain, int maxCrawlPages, CancellationToken ct);
    Task<Result<OnPageSummary>> GetTaskSummaryAsync(string taskId, CancellationToken ct);
    Task<Result<IReadOnlyList<OnPagePageResult>>> GetPageResultsAsync(string taskId, int limit, CancellationToken ct);
    Task<Result<OnPageInstantResult>> AuditSinglePageAsync(string url, CancellationToken ct);
}
```

### New models
`OnPageSummary`, `OnPagePageResult`, `OnPageIssue`, `OnPageInstantResult`

### New entity
`SeoOnPageTask` — task ID, polling status (pending / processing / complete), domain, project ID

### Worker update
`SeoMaintenanceWorker` polls pending `SeoOnPageTask` rows hourly.  
When DataForSEO marks complete: fetch summary + first 50 pages, store in `seo_site_audit_pages` (add `Source` column: `playwright` | `dataforseo`).

### New endpoints (extend `SiteAuditController`)
- `POST /api/seo/audit/site/dataforseo/{projectId}` — start task
- `GET /api/seo/audit/site/dataforseo/{projectId}/status` — status + summary
- `POST /api/seo/audit/page/instant?url=` — instant single-page check (metered route)

### Frontend
`/app/audit`: provider toggle — "Quick (DataForSEO)" vs "Deep (Playwright)".  
Instant page check panel in Deep SERP sidebar (CWV, title length, meta issues).

### Tests
`OnPageAuditProviderTests.cs`, extend `SiteAuditServiceTests.cs`

---

## Sprint 8 — Consolidation + Agency Tier (Weeks 15–16)

**Goal:** Provider health surface, usage metering for all new routes, Agency tier API key management.

### Work items

1. **Provider health endpoint** `GET /api/seo/providers/health` (no auth required)
   ```json
   {
     "dataforseo": "configured",
     "serpapi": "missing_key",
     "openai": "configured",
     "gemini": "configured",
     "perplexity": "missing_key",
     "serpProvider": "dataforseo",
     "rankProvider": "dataforseo"
   }
   ```

2. **Usage metering** — add to `MeteredRoutes.cs`:
   - `rank_history_lookup` → `GET /api/seo/rank-tracking/*/history`
   - `domain_keywords_lookup` → `GET /api/seo/domain/*/keywords`
   - `backlink_summary` → `GET /api/seo/backlinks/*/summary`
   - `instant_page_audit` → `POST /api/seo/audit/page/instant`

3. **HTTP client consolidation** in `SeoBackendExtensions.cs`:
   - `"DataForSEO"` → `https://api.dataforseo.com`, 60s timeout
   - `"SerpApi"` → `https://serpapi.com`, 30s timeout
   - `"OpenAI"` → `https://api.openai.com`, 30s timeout
   - `"Gemini"` → `https://generativelanguage.googleapis.com`, 30s timeout
   - `"Perplexity"` → `https://api.perplexity.ai`, 30s timeout

4. **`UsageLimits.cs`** — add per-tier limits for all new features

5. **Dashboard widget** — `DashboardOverviewService` includes provider health summary.  
   `/app/dashboard` shows "Data Providers" module with green/grey chips.

6. **Settings integrations tab** — `/app/settings` shows provider health chips, links to configure

7. **Agency tier API key management** (entities already exist in schema):
   - `GET /api/seo/api-keys` — list
   - `POST /api/seo/api-keys` — create
   - `DELETE /api/seo/api-keys/{id}` — revoke

### Tests
`ProvidersHealthTests.cs`, full integration sweep `test:integration:providers`

---

## Sprint Dependency Order

```
Sprint 1 (refactor) ─── prerequisite for all
    ├─► Sprint 2 (rank tracking)        ─ independent
    ├─► Sprint 3 (keyword labs) ─────────► Sprint 4 (backlinks)
    ├─► Sprint 5 (AI visibility)         ─ independent
    ├─► Sprint 6 (SerpAPI fallback) ─────► Sprint 7 (OnPage audit)
    └──────────────── all ───────────────► Sprint 8 (consolidation)
```

Sprints 2, 5, and 6 can run in parallel after Sprint 1. Sprint 4 requires Sprint 3. Sprint 7 requires Sprint 6.

---

## Implementer Notes

1. **Do not rename `DataForSEOSerpProvider`** to `GeekDataForSeoProvider` — unnecessary churn across references. Move to subfolder, keep class name. The `GeekDataForSEO` identity lives in the folder structure and client service name.

2. **`SerpResult` shape is frozen.** Any new fields go in a new `ExtendedSerpResult` or are added as nullable properties with defaults. Do not change required properties without a full audit of all six dependent services.

3. **`FallbackSerpProvider` must preserve `ProviderName`** from whichever provider actually answered. `SerpAnalysisService` stores `provider` on `DeepSerpResult.Provider` — cached items must reflect actual source.

4. **`SeoRankTracking` vs new tables:** The existing `SeoRankTracking` entity is populated from GSC data via `GoogleDataService`. New `SeoTrackedKeyword` + `SeoRankSnapshot` tables are DataForSEO/SerpAPI-polled. Do not co-mingle GSC rank rows with API-polled rank rows.

5. **DataForSEO Labs endpoints cost more** per request than organic SERP. Enforce the 24h cache aggressively. A `ranked_keywords` query at `limit=500` costs ~$0.005. Cache misses must be metered.

6. **AI visibility probes are slow** (2–5s per LLM call). Worker calls them via `Task.WhenAll` with individual `try/catch` per probe — one failing provider must not block others. Per-probe timeouts: ChatGPT 10s, Gemini 8s, Perplexity 12s.

7. **Polly on `DataForSeoHttpClientService`:** Handle HTTP 429 with min 1s delay and max 3 retries. Do not retry other 4xx errors.

---

## Verification Per Sprint

| Sprint | Verify By |
|--------|-----------|
| 1 | All 8 existing xUnit tests pass. `dotnet build` clean. |
| 2 | Add keyword → worker runs → `GET history` returns position points. Frontend chart renders. |
| 3 | `GET /api/seo/domain/ahrefs.com/keywords` returns keyword rows. Cache hit on second call (no DataForSEO request). |
| 4 | `GET /api/seo/backlinks/example.com/summary` returns `BacklinkSummary`. Anchor text tab renders. |
| 5 | `GET /api/seo/geo/platforms` returns `chatgpt.configured = true`. Daily probe stores `Engine = "chatgpt"` row. |
| 6 | `GET /api/seo/serp/engine?engine=bing` returns Bing organic results. Fallback test suite passes. |
| 7 | `POST /api/seo/audit/page/instant?url=https://example.com` returns issues JSON in <5s. |
| 8 | `GET /api/seo/providers/health` returns all five provider statuses. Dashboard widget visible. |
