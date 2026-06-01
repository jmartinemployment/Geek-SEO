# Geek Data Plane — Provider Strategy & Migration Plan

**Canonical file:** `GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md` (renamed from `GEEK-DATA-PLANE.md`, June 2026)  
**Status:** Canonical (supersedes deleted `DATAFORSEO-REPLACEMENT-UPGRADE.md` and `DATAFORSEO-REPLACEMENT-UPGRADE-OLD.md`)  
**Last updated:** June 1, 2026  
**Audience:** Engineering — GeekSEO product + data plane

---

## Purpose

GeekSEO must **own its data plane** (`GeekSerpProvider`, Geek crawl workers, Postgres caches)—not rent more surface area from DataForSEO.

This document is the **single source of truth** for:

- Which **interfaces** product code may depend on
- Which **vendor implementations** are allowed in each phase
- How **Site Audit**, **Topical Map**, and **Rank Tracking** share the same providers
- What **not** to build (superseded vendor-expansion sprints)

**North star:** Eliminate recurring **DataForSEO** spend. **SerpApi** (or equivalent) is an acceptable **interim bridge** behind the same interfaces until Geek-owned fetch + parse is production-ready.

---

## Supersedes

| Retired document | Why retired |
|------------------|-------------|
| `DATAFORSEO-REPLACEMENT-UPGRADE-OLD.md` | **Deleted** — ownership/SerpApi-bridge strategy merged here |
| `DATAFORSEO-REPLACEMENT-UPGRADE.md` | **Deleted** — misaligned vendor-expansion plan (Labs, Backlinks, OnPage); do not recreate |

**Do not implement** from the retired NEW doc: Sprints **3–4** (DataForSEO Labs / Backlinks), Sprint **7** (DataForSEO OnPage audit). See [Rejected work](#rejected-work-from-superseded-new-plan).

---

## Platform vision

| External benchmark | What they sell | **Geek target** | Replaces paid vendors how |
|--------------------|----------------|-----------------|---------------------------|
| SE Ranking | SEO + GEO SaaS | **GeekSEO UI** (`seo.geekatyourspot.com`) | Product layer; not an API bill |
| SerpApi | SERP/search JSON API | **`GeekSerpProvider`** → `SerpResult` | Replaces `DataForSEOSerpProvider` + SerpApi |
| Bright Data | Proxies, unlocker, scale SERP/crawl | **Geek crawl fleet** (Playwright + queue + proxies) | Scale behind SERP + site audit |
| Ahrefs | Link index, keywords, rank history | **Geek SEO data** in Postgres | Rank snapshots, keyword corpus, phased link graph |

**Rule:** Competitor sites define **UX and modules** (`geek-scrape`, feature specs). **Runtime data** comes from code you control (`ISerpProvider` → `GeekSerpProvider`), not from scraping competitor homepages.

---

## Architecture principle: interface-first, swappable implementations

Product services **must not** reference DataForSEO, SerpApi, or Playwright types. They depend only on **application interfaces** in `GeekSeo.Application/Interfaces/`.

```text
Feature services (TopicalMap, SerpAnalysis, ContentScoring, SiteAudit, RankTracking, …)
        │
        ▼
   Interfaces (ISerpProvider, IKeywordProvider, IRankSnapshotProvider, ICrawlerProvider, …)
        │
        ├── SerpApi*Provider      ← interim bridge (SERP, rank poll, GEO engines)
        ├── DataForSeo*Provider   ← shrink to zero; last holdout usually keywords
        └── Geek*Provider         ← end state (fetch + parse + corpus)
```

**Registration** in [`GeekSeoBackend/Extensions/SeoBackendExtensions.cs`](../GeekSeoBackend/Extensions/SeoBackendExtensions.cs) selects implementation via env vars. Swapping vendors must not require feature rewrites if `SerpResult` / keyword DTO contracts stay stable.

### Frozen contracts

| Contract | Rule |
|----------|------|
| [`SerpResult`](../GeekSeo.Application/Models/SerpResult.cs) | **Frozen** for v1 consumers (deep SERP, scoring, briefs, topical map, GEO organic). New fields only as optional. Extended shapes get new types. |
| `ProviderName` on cache rows | Must reflect **whichever implementation answered** (SerpApi vs dataforseo vs geek), not a hardcoded string. |
| GSC rank vs API rank | **Never commingle.** GSC → existing rankings UI. API-polled snapshots → rank tracker tables. |

---

## Today (as-built)

Registered in `SeoBackendExtensions.cs`:

| Interface | Implementation today | Vendor |
|-----------|---------------------|--------|
| `ISerpProvider` | `DataForSEOSerpProvider` | DataForSEO `…/serp/google/organic/live/advanced` |
| `IKeywordProvider` | `DataForSEOKeywordProvider` | DataForSEO `…/keywords_for_keywords/live` |
| `IRankSnapshotProvider` | `DataForSeoRankSnapshotProvider` | DataForSEO `…/organic/live/regular` |
| `ICrawlerProvider` | `PlaywrightCrawlerProvider` (or no-op) | **Geek** (Playwright) |

**Env:** `DATAFORSEO_LOGIN`, `DATAFORSEO_PASSWORD`

### Who consumes what

| Feature | Service | Provider need |
|---------|---------|---------------|
| Deep SERP | `SerpAnalysisService` | `ISerpProvider` — organic, PAA, features, cache |
| Content scoring | `ContentScoringService` | `ISerpProvider` — top URLs for benchmarks |
| Content briefs | `ContentBriefService` | `ISerpProvider` |
| Competitor insights | `CompetitorInsightsService` | `ISerpProvider` → crawl targets |
| Topical map (GSC + seed) | `TopicalMapService` | `ISerpProvider` + `IKeywordProvider` |
| GEO probe (Google) | `GeoVisibilityService` | `ISerpProvider` (AIO flag from organic payload today) |
| Keyword planner | `KeywordResearchService` | `IKeywordProvider` |
| Site audit | `SiteAuditService` | `ICrawlerProvider` (Playwright) |
| Rank tracker (API) | `RankTrackingService` | `IRankSnapshotProvider` |
| Rankings (owned site) | `GoogleDataService` / GSC | **Not** `IRankSnapshotProvider` |

---

## Target configuration model

```bash
# SERP (deep SERP, briefs, scoring, topical clusters, default GEO organic)
SERP_PROVIDER=serpapi              # serpapi | geek | dataforseo (dataforseo = sunset only)
SERP_PROVIDER_FALLBACK=            # optional: dataforseo while migrating

# Keyword research (planner, seed topical map expansion)
KEYWORD_PROVIDER=dataforseo        # interim: dataforseo → gsc_ads → geek
# Future: KEYWORD_PROVIDER=geek

# Daily position polls (rank tracker — separate from GSC rankings page)
RANK_SNAPSHOT_PROVIDER=serpapi     # serpapi → geek (not dataforseo long-term)

# Site audit — no third-party OnPage API
# Uses ICrawlerProvider (Playwright → Geek crawl fleet)

SERPAPI_API_KEY=...
DATAFORSEO_LOGIN=...               # remove from prod when KEYWORD_PROVIDER != dataforseo
DATAFORSEO_PASSWORD=...
```

**Policy (Jeff, June 2026):**

- **SerpApi OK** temporarily for SERP + rank snapshots + dedicated GEO engines.
- **DataForSEO trends to zero** — do not add new DFS endpoints (Labs, Backlinks, OnPage).
- **Implement behind interfaces** so SerpApi → Geek swap is DI-only.

---

## Provider interface matrix

| Capability | Interface | Interim implementation | Target implementation | Explicitly rejected |
|------------|-----------|------------------------|----------------------|---------------------|
| SERP fetch + parse | `ISerpProvider` | `SerpApiSerpProvider` | `GeekSerpProvider` (Playwright fetch + parser) | More DFS SERP endpoints |
| Keyword suggestions | `IKeywordProvider` | Minimal `DataForSEOKeywordProvider` **or** SerpApi slice if viable | `GeekKeywordProvider` (GSC + Google Ads API + SERP seeds) | DFS Keyword Labs as default |
| Broader seed expansion | `IKeywordDiscoveryProvider` *(new, optional)* | Planner + SerpApi-related | Geek corpus | `IKeywordLabsProvider` tied to DFS only |
| Rank position poll | `IRankSnapshotProvider` | `SerpApiRankSnapshotProvider` | `GeekSerpRankSnapshotProvider` | `DataForSeoRankSnapshotProvider` long-term |
| Site crawl / audit | `ICrawlerProvider` | `PlaywrightCrawlerProvider` | Geek crawl fleet (scaled Playwright + queue) | DataForSEO OnPage API |
| Backlinks / domain intel | `IBacklinkProvider`, `IDomainAnalyticsProvider` | **Defer** or licensed index later | Geek link graph **or** skip module | DFS Backlinks / Labs sprints |
| Multi-LLM GEO | `IAiVisibilityProbe` *(per engine)* | Direct OpenAI / Gemini / Perplexity APIs | Same | Bundling inside “GeekDataForSEO” DFS wrapper |

### Composite / fallback pattern (allowed)

From superseded NEW plan Sprint 1 — **ownership-safe** salvage only:

- Injectable HTTP client (replace static `DataForSeoClient.cs` when retiring DFS).
- `FallbackSerpProvider` (or generic chain): try primary, then fallback; preserve actual `ProviderName`.
- Polly on vendor HTTP: retry **429** only (max 3, min 1s backoff).

---

## Overlap resolutions

### Site Audit (do not fork)

| Track | Approach | Verdict |
|-------|----------|---------|
| **Current + target** | `ICrawlerProvider` → Playwright → extend worker, PSI, depth | **Canonical** |
| REDESIGN Phase 6 | Same Playwright crawl + Lighthouse polish | Aligns with above |
| Retired NEW Sprint 7 | DataForSEO OnPage + “Quick vs Deep” toggle | **Rejected** — second stack, ongoing DFS cost, duplicates Playwright |

**One audit story:** deepen Playwright / Geek crawl. No `IOnPageAuditProvider` bound to DataForSEO.

### Topical Map (consumer, not a separate vendor plan)

| Item | Notes |
|------|--------|
| **Shipped** | `TopicalMapService.GenerateSeedModeAsync` — `IKeywordProvider` + hierarchy, entities, linking blueprint |
| [`TopicalMapUpgrade.md`](TopicalMapUpgrade.md) | Must **not** depend on “DataForSEO Sprint 3 Labs”; use `IKeywordDiscoveryProvider` + this doc |
| SERP clustering | Uses `ISerpProvider` only — benefits automatically when SerpApi/Geek replaces DFS |
| TODO #12b V2.2 | Phrase as “keyword discovery vs GSC”, not “DataForSEO diff” |

### Rank tracking

| Item | Notes |
|------|--------|
| Product | `/app/rank-tracker`, `RankTrackingService`, `SeoTrackedKeyword` / snapshots (see `GeekSeoBackend/CLAUDE.md` session 12) |
| **Debt** | `DataForSeoRankSnapshotProvider` registered today — **migrate to SerpApi then Geek** |
| GSC | `/app/rankings` stays separate — do not merge with API snapshot rows |

### AI visibility (GEO)

Multi-LLM probes (ChatGPT, Gemini, Perplexity) are **product backlog** ([`TODO.md`](TODO.md) parity #20), not a reason to expand DataForSEO. Use direct LLM APIs or SerpApi GEO engines behind probes.

---

## Rejected work (from superseded NEW plan)

| Sprint | Proposal | Why rejected |
|--------|----------|--------------|
| 3 | DataForSEO Keyword Labs / domain intelligence | Locks in DFS spend; target = Geek corpus + crawl-derived intel |
| 4 | DataForSEO Backlinks API | Ahrefs parity via reseller; defer or own link graph later |
| 7 | DataForSEO OnPage audit | Conflicts with Playwright audit; not ownership |
| 2 (as written) | Rank tracking **primary** on DFS | Use SerpApi → Geek; DFS rank provider is transitional debt only |
| 6 (as written) | SerpApi as **fallback** | SerpApi should be **primary** during bridge phase |

**Salvage (concept only):** folder layout under `Providers/Seo/{SerpApi,DataForSeo,Geek}/`, provider health endpoint, metering route names, rank-tracker UI spec, separate GSC vs API rank tables.

---

## Migration phases (planning — not “v1 complete”)

Execute in order. Each phase ends with **measurable DFS call reduction**.

### Phase A — Bridge: SerpApi primary

- Add `SerpApiSerpProvider`, `SerpApiRankSnapshotProvider`.
- Wire `SERP_PROVIDER=serpapi`, `RANK_SNAPSHOT_PROVIDER=serpapi`.
- Optional `FallbackSerpProvider`; DFS only as explicit fallback env.
- Refactor static DFS HTTP client when touching DFS code.
- **Verification:** Deep SERP + rank tracker show `provider: serpapi` on cache; DFS SERP calls trend down.

### Phase B — Keyword path off DFS

- Design `GeekKeywordProvider` v0: project GSC queries + Google Ads Keyword Planner API + SERP-derived seeds.
- Reduce `IKeywordProvider` DFS usage (topical seed mode, planner).
- **Verification:** `KEYWORD_PROVIDER` can switch without feature code changes.

### Phase C — GeekSerp v1

- `GeekSerpProvider`: controlled fetch (Playwright + proxy policy) + parser → `SerpResult`.
- Dogfood on deep SERP + content scoring before cutting SerpApi.
- **Verification:** Same `SerpResult` contract tests pass with `provider: geek`.

### Phase D — DataForSEO zero

- Remove DFS registrations from DI.
- Remove `DATAFORSEO_*` from production env.
- Delete or archive `DataForSeo*` provider classes when unused.

Site audit and topical map **ride Phases A–C** via existing interfaces — no parallel DFS feature tracks.

---

## Relationship to other plan docs

| Document | Role |
|----------|------|
| [`TODO.md`](TODO.md) | Product backlog (scoring v2, integrations, REDESIGN, ops) |
| [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) | What is live in production |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Services, ports, API gateway |
| [`TopicalMapUpgrade.md`](TopicalMapUpgrade.md) | Topical map UX gaps — **data deps point here** |
| [`REDESIGN-PLAN.md`](REDESIGN-PLAN.md) | Semrush-style UI — reference only for shell/audit UX |
| [`api-comparison.md`](api-comparison.md) | Historical vendor research — verify against current vendor docs |

Rank/agency UI backlog previously in missing `UPGRADE-se-ranking-agency-serpapi.md` lives in [`TODO.md`](TODO.md) (integrations, post-v1 upgrade notes).

---

## Review checklist (use before any provider work)

- [ ] Does this change call a **new DataForSEO endpoint**? If yes, **stop** — find interface + Geek/SerpApi path.
- [ ] Does product code import a vendor SDK type? If yes, **wrong layer**.
- [ ] Is `SerpResult` shape preserved for all six SERP consumers?
- [ ] Are GSC rankings and API rank snapshots still separate?
- [ ] Does Site Audit stay on `ICrawlerProvider` only?
- [ ] Is `ProviderName` on caches accurate after fallback?
- [ ] Does topical map depend only on `IKeywordProvider` / `ISerpProvider`, not DFS types?

---

## Decision summary

| Question | Answer |
|----------|--------|
| What is “GeekDataForSeo”? | **Misnomer** in retired NEW plan. Use **Geek Data Plane** = owned fetch, parse, cache. |
| Keep both old DFS upgrade docs? | **No** — deleted; this file only. |
| SerpApi? | **Yes**, interim, primary for SERP + rank during bridge. |
| DataForSEO? | **Trend to zero**; no Labs/Backlinks/OnPage expansion. |
| Site audit vs DFS OnPage? | **Playwright / Geek crawl only.** |
| Topical map vs DFS Labs? | **`IKeywordProvider` / `IKeywordDiscoveryProvider`**, not DFS Labs sprint. |
