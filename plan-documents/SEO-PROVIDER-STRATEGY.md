# SEO Provider Strategy & Migration

**Canonical file:** [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) (this document)  
**Status:** Canonical  
**Last updated:** June 1, 2026  
**Audience:** Engineering — GeekSEO backends and providers

> **Doc hygiene:** Retired filenames [`DATAFORSEO-REPLACEMENT-UPGRADE.md`](DATAFORSEO-REPLACEMENT-UPGRADE.md) (vendor-expansion Labs/OnPage) and [`GEEK-DATA-PLANE.md`](GEEK-DATA-PLANE.md) are **redirect stubs** only. Plan here: **`SEO-PROVIDER-STRATEGY.md`**.

---

## Purpose

GeekSEO must **own its data plane** (`GeekSerpProvider`, Geek crawl workers, Postgres caches)—not rent more surface area from DataForSEO.

This document is the **single source of truth** for:

- Which **interfaces** product code may depend on
- Which **vendor implementations** are allowed in each phase
- How **Site Audit**, **Topical Map**, and **Rank Tracking** share the same providers
- What **not** to build (superseded vendor-expansion sprints)

**North star:** Eliminate recurring **DataForSEO** spend. **SerpApi** is an acceptable **interim bridge** behind the same interfaces until Geek-owned fetch + parse is production-ready.

---

## Supersedes

| Retired document | Why retired |
|------------------|-------------|
| `DATAFORSEO-REPLACEMENT-UPGRADE-OLD.md` | **Deleted** — merged into this plan |
| `DATAFORSEO-REPLACEMENT-UPGRADE.md` (June 2026 vendor-expansion draft) | **Deleted** — Labs / Backlinks / OnPage sprints; do not recreate |
| `GEEK-DATA-PLANE.md`, `GEEK-DATAFORSEO-REPLACEMENT-FOR-DATAFORSEO-PLAN.md` | **Renamed** — use `SEO-PROVIDER-STRATEGY.md` |

**Do not implement** from the retired vendor-expansion plan: Sprints **3–4** (DataForSEO Labs / Backlinks), Sprint **7** (DataForSEO OnPage audit). See [Rejected work](#rejected-work-from-superseded-vendor-expansion-plan).

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
        ├── SerpApi*Provider      ← interim bridge (SERP, rank poll)
        ├── DataForSeo*Provider   ← shrink to zero; last holdout usually keywords
        └── Geek*Provider         ← end state (fetch + parse + corpus)
```

**Registration** via [`SeoProviderRegistration.AddSeoDataProviders()`](../GeekSeoBackend/Extensions/SeoProviderRegistration.cs), called from [`SeoBackendExtensions.cs`](../GeekSeoBackend/Extensions/SeoBackendExtensions.cs). Implementation is selected by **env vars** (Phase 0 shipped June 2026).

Swapping vendors must not require feature rewrites if `SerpResult` / keyword DTO contracts stay stable.

### Frozen contracts

| Contract | Rule |
|----------|------|
| [`SerpResult`](../GeekSeo.Application/Models/SerpResult.cs) | **Frozen** for v1 consumers (deep SERP, scoring, briefs, topical map, GEO organic). New fields only as optional. Extended shapes get new types. |
| `ProviderName` on providers / cache rows | Must reflect **whichever implementation answered** (`serpapi`, `dataforseo`, `geek`, `internal`), including inside `FallbackSerpProvider`. |
| GSC rank vs API rank | **Never commingle.** GSC → `/app/rankings`. API-polled snapshots → rank tracker tables. |

### Keyword interface split

| Interface | Responsibility | Primary consumers |
|-----------|----------------|-------------------|
| `IKeywordProvider` | Volume, difficulty, CPC, related keywords for **planner** and seed-mode **ideas** | `KeywordResearchService`, `TopicalMapService` (seed pipeline) |
| `IKeywordDiscoveryProvider` | **Broader semantic expansion** from a seed (project-aware discovery, not raw DFS Labs) | `TopicalMapService.GenerateSeedModeAsync` |

**Implementation detail:** [`KEYWORD-DISCOVERY-STRATEGY.md`](KEYWORD-DISCOVERY-STRATEGY.md) is the canonical plan for `IKeywordDiscoveryProvider` (stub: `InternalKeywordDiscoveryProvider` today). Phase B must keep the split above — do not duplicate discovery logic inside `GeekKeywordProvider` without updating that doc.

---

## Today (as-built)

**Default env** (production posture when unset): all `*_PROVIDER` → `dataforseo`. No SerpApi key required.

| Interface | Default implementation | Also available (env) |
|-----------|------------------------|----------------------|
| `ISerpProvider` | `DataForSEOSerpProvider` | `SerpApiSerpProvider`; optional `FallbackSerpProvider` (`SERP_PROVIDER_FALLBACK=dataforseo`) |
| `IKeywordProvider` | `DataForSEOKeywordProvider` | Phase B: `gsc_ads`, `geek` (not implemented) |
| `IRankSnapshotProvider` | `DataForSeoRankSnapshotProvider` | `SerpApiRankSnapshotProvider` |
| `IKeywordDiscoveryProvider` | `InternalKeywordDiscoveryProvider` | **Stub** — replace per [KEYWORD-DISCOVERY-STRATEGY.md](KEYWORD-DISCOVERY-STRATEGY.md) |
| `ICrawlerProvider` | `PlaywrightCrawlerProvider` (or no-op) | Geek (Playwright) |

**Env (credentials):**

| Vendor | Variables |
|--------|-----------|
| DataForSEO | `DATAFORSEO_LOGIN`, `DATAFORSEO_PASSWORD` |
| SerpApi (only if `serpapi` provider) | `SERPAPI_API_KEY` |

**Env (selection):** `SERP_PROVIDER`, `KEYWORD_PROVIDER`, `RANK_SNAPSHOT_PROVIDER`, optional `SERP_PROVIDER_FALLBACK`

**Ops:** `GET /health/providers` — resolved names + credential flags (no secrets)

### Who consumes what

| Feature | Service | Provider need |
|---------|---------|---------------|
| Deep SERP | `SerpAnalysisService` | `ISerpProvider` — organic, PAA, features, cache (`Provider` from `serp.ProviderName`) |
| Content scoring | `ContentScoringService` | `ISerpProvider` — top URLs for benchmarks |
| Content briefs | `ContentBriefService` | `ISerpProvider` |
| Competitor insights | `CompetitorInsightsService` | `ISerpProvider` → crawl targets |
| Topical map (GSC + seed) | `TopicalMapService` | `IKeywordProvider` + `IKeywordDiscoveryProvider` + `ISerpProvider` |
| GEO probe (Google) | `GeoVisibilityService` | `ISerpProvider` (AIO flag from organic payload today) |
| Keyword planner | `KeywordResearchService` | `IKeywordProvider` |
| Site audit | `SiteAuditService` | `ICrawlerProvider` (Playwright) |
| Rank tracker (API) | `RankTrackingService` | `IRankSnapshotProvider` |
| Rankings (owned site) | `GoogleDataService` / GSC | **Not** `IRankSnapshotProvider` |

### Rank tracker note (Sprint 2, June 2026)

Rank tracking **shipped** on `DataForSeoRankSnapshotProvider` by design (Sprint 2). That is **planned bridge debt**, not a feature to rip out. **Phase A** swaps the provider behind `IRankSnapshotProvider`; product/API/schema for tracked keywords stay.

---

## Target configuration model

```bash
# SERP (deep SERP, briefs, scoring, topical clusters, default GEO organic)
SERP_PROVIDER=serpapi              # serpapi | geek | dataforseo (dataforseo = sunset only)
SERP_PROVIDER_FALLBACK=            # optional: dataforseo while migrating

# Keyword research (planner metrics + seed ideas)
KEYWORD_PROVIDER=dataforseo        # dataforseo → gsc_ads → geek
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

- **SerpApi OK** temporarily for SERP + rank snapshots.
- **DataForSEO trends to zero** — do not add new DFS endpoints (Labs, Backlinks, OnPage).
- **Implement behind interfaces** so SerpApi → Geek swap is DI-only.
- **GEO multi-LLM** (ChatGPT, Gemini, Perplexity) uses **direct LLM APIs** in backlog — not SerpApi scope creep in Phase A unless a single engine is explicitly scoped.

---

## Cost guardrails & metering

Bridge phase can **increase** total API spend if rank polls are frequent. Track unit economics before cutting DFS.

| Workload | Risk | Guardrail |
|----------|------|-----------|
| Deep SERP (on-demand) | SerpApi per-search pricing | Cache via existing SERP cache; tie to `IUsageMeteringService` |
| Rank tracker (`SeoMaintenanceWorker` hour 5 UTC) | **Highest risk** — keywords × projects × daily | Cap keywords per project/tier; document poll cadence; alert on SerpApi dashboard |
| Keyword planner | DFS or Ads quota | Rate-limit per user/project |
| GeekSerp (Phase C) | Infra + proxy cost | Dogfood with budget cap before defaulting `SERP_PROVIDER=geek` |

**Actions (Phase A.1):**

- Align route/meter names with [`IUsageMeteringService`](../GeekSeoBackend/Services/UsageMeteringService.cs) for SERP + rank snapshot calls.
- Add **monthly spend ceiling** env (e.g. `SERPAPI_MONTHLY_BUDGET_USD`) for ops alerts — implementation optional but document target in Railway.
- Reconcile against vendor dashboards monthly; [`api-comparison.md`](api-comparison.md) is **historical** — re-verify list prices before Phase A sign-off (note date in PR).

**Rough decision rule:** SerpApi bridge is acceptable when **(DFS eliminated or near-zero) AND (all-in cost ≤ prior DFS run-rate at current usage)**. If rank polling dominates cost, reduce cadence before adding GeekSerp infra.

---

## Provider interface matrix

| Capability | Interface | Interim implementation | Target implementation | Explicitly rejected |
|------------|-----------|------------------------|----------------------|---------------------|
| SERP fetch + parse | `ISerpProvider` | `SerpApiSerpProvider` | `GeekSerpProvider` (Playwright fetch + parser) | More DFS SERP endpoints |
| Keyword suggestions (metrics) | `IKeywordProvider` | `DataForSEOKeywordProvider` → `GscAdsKeywordProvider` | `GeekKeywordProvider` (GSC + Google Ads + SERP seeds) | DFS Keyword Labs as default |
| Broader seed expansion | `IKeywordDiscoveryProvider` | `InternalKeywordDiscoveryProvider` (stub) → project-aware impl | Geek corpus + GSC — see [KEYWORD-DISCOVERY-STRATEGY.md](KEYWORD-DISCOVERY-STRATEGY.md) | `IKeywordLabsProvider` tied to DFS only |
| Rank position poll | `IRankSnapshotProvider` | `SerpApiRankSnapshotProvider` | `GeekSerpRankSnapshotProvider` | `DataForSeoRankSnapshotProvider` long-term |
| Site crawl / audit | `ICrawlerProvider` | `PlaywrightCrawlerProvider` | Geek crawl fleet (scaled Playwright + queue) | DataForSEO OnPage API |
| Backlinks / domain intel | `IBacklinkProvider`, `IDomainAnalyticsProvider` | **Defer** or licensed index later | Geek link graph **or** skip module | DFS Backlinks / Labs sprints |
| Multi-LLM GEO | `IAiVisibilityProbe` *(per engine)* | Direct OpenAI / Gemini / Perplexity APIs | Same | Bundling inside DFS wrapper |

### Composite / fallback pattern (allowed)

- Injectable HTTP client (replace static `DataForSeoClient.cs` when retiring DFS).
- `FallbackSerpProvider` (or generic chain): try primary, then fallback; preserve actual `ProviderName`.
- Polly on vendor HTTP: retry **429** only (max 3, min 1s backoff).

### Provider folder layout (Phase 0)

```
GeekSeoBackend/Providers/Seo/
  SerpApi/
  DataForSeo/
  Geek/
  Composite/          # FallbackSerpProvider, etc.
```

Move existing `DataForSEOSerpProvider.cs` et al. into `DataForSeo/` when touched — no big-bang rename required.

---

## Overlap resolutions

### Site Audit (do not fork)

| Track | Approach | Verdict |
|-------|----------|---------|
| **Current + target** | `ICrawlerProvider` → Playwright → extend worker, PSI, depth | **Canonical** |
| REDESIGN Phase 6 | Same Playwright crawl + Lighthouse polish | Aligns with above |
| Retired vendor-expansion Sprint 7 | DataForSEO OnPage + “Quick vs Deep” toggle | **Rejected** — second stack, ongoing DFS cost, duplicates Playwright |

**One audit story:** deepen Playwright / Geek crawl. No `IOnPageAuditProvider` bound to DataForSEO.

### Topical Map (consumer, not a separate vendor plan)

| Item | Notes |
|------|--------|
| **Shipped** | `TopicalMapService.GenerateSeedModeAsync` — `IKeywordProvider` + `IKeywordDiscoveryProvider` + hierarchy, entities, linking blueprint |
| [`TopicalMapUpgrade.md`](TopicalMapUpgrade.md) | UX gaps — data deps point **here** + [KEYWORD-DISCOVERY-STRATEGY.md](KEYWORD-DISCOVERY-STRATEGY.md) |
| SERP clustering | Uses `ISerpProvider` only — benefits automatically when SerpApi/Geek replaces DFS |
| TODO #12b V2.2 | Phrase as “keyword discovery vs GSC”, not “DataForSEO diff” |

### Rank tracking

| Item | Notes |
|------|--------|
| Product | `/app/rank-tracker`, `RankTrackingService`, `SeoTrackedKeyword` / snapshots (`GeekSeoBackend/CLAUDE.md` session 12) |
| Provider swap | **Phase A** — replace `DataForSeoRankSnapshotProvider` with SerpApi; **not** a redo of Sprint 2 feature work |
| GSC | `/app/rankings` stays separate — do not merge with API snapshot rows |
| Cross-repo | Persistence via GeekAPI `api/seo/internal/rank-tracking*` → GeekRepository. **Provider swap is GeekSeoBackend-only** unless adding `ProviderName` column on snapshots (recommended in Phase A.1) |

### AI visibility (GEO)

| Item | Notes |
|------|--------|
| Google organic + AIO | `GeoVisibilityService` + `ISerpProvider` today |
| Multi-LLM probes | Product backlog ([`TODO.md`](TODO.md) parity #20) — **direct LLM APIs**, not DFS |
| Phase A scope | Do **not** expand SerpApi integration for every GEO engine unless explicitly tasked; keep SerpApi to SERP + rank snapshot bridge |

---

## Rejected work (from superseded vendor-expansion plan)

| Sprint | Proposal | Why rejected |
|--------|----------|--------------|
| 3 | DataForSEO Keyword Labs / domain intelligence | Locks in DFS spend; target = Geek corpus + crawl-derived intel |
| 4 | DataForSEO Backlinks API | Ahrefs parity via reseller; defer or own link graph later |
| 7 | DataForSEO OnPage audit | Conflicts with Playwright audit; not ownership |
| 2 (as written) | Rank tracking **primary** on DFS long-term | Use SerpApi → Geek; DFS rank provider is bridge only |
| 6 (as written) | SerpApi as **fallback** | SerpApi should be **primary** during bridge phase |

---

## Migration phases (planning — not “v1 complete”)

Execute in order. Each phase ends with **measurable DFS call reduction** and passes [verification](#verification-by-phase).

### Phase 0 — Env-driven DI (prerequisite)

**Status: shipped (June 2026)** in `GeekSeoBackend/Extensions/SeoProviderRegistration.cs`.

| Task | Detail |
|------|--------|
| Provider factory | `AddSeoDataProviders()` — `SERP_PROVIDER`, `KEYWORD_PROVIDER`, `RANK_SNAPSHOT_PROVIDER` (default `dataforseo`) |
| HttpClient names | `DataForSEO`, `SerpApi` named clients registered |
| Health | `GET /health/providers` — resolved names + credential flags (no secrets) |
| Unimplemented env | `geek` / `gsc_ads` → **fail at startup** with message pointing to next phase |
| Folder layout | SerpApi under `Providers/Seo/SerpApi/`; DFS providers still at `Providers/Seo/` (move deferred) |

**Verification:**

- [x] `SeoProviderRegistrationTests` — default env → DataForSEO implementations
- [x] `SeoProviderRegistrationTests` — `serpapi` + `SERPAPI_API_KEY` → SerpApi implementations
- [x] Local DI: `SERP_PROVIDER=serpapi` + `SERPAPI_API_KEY` → `SerpApiSerpProvider` (unit tests)

### Phase A — Bridge: SerpApi primary

**Status: code shipped (June 2026)** in `GeekSeoBackend/Providers/Seo/SerpApi/`. **Production:** stays on `dataforseo` until env flip + SerpApi key.

| Task | Detail | Status |
|------|--------|--------|
| Implement | `SerpApiSerpProvider`, `SerpApiRankSnapshotProvider` under `Providers/Seo/SerpApi/` | [x] |
| Wire env | `SERP_PROVIDER=serpapi`, `RANK_SNAPSHOT_PROVIDER=serpapi` | [x] code; [ ] Railway flip (optional) |
| Fallback | `FallbackSerpProvider` when `SERP_PROVIDER_FALLBACK=dataforseo` | [x] |
| DFS client | Refactor static `DataForSeoClient.cs` to injectable | [ ] deferred |
| GEO scope | SerpApi for SERP + rank only in this phase | [x] |

**Verification:**

- [x] `SerpApiSerpProviderTests`, `SerpApiRankSnapshotProviderTests` (JSON parse fixtures)
- [x] `DataForSEOSerpProviderTests` (existing DFS baseline)
- [ ] Deep SERP API/UI: response or cache shows `provider: serpapi` after env flip
- [ ] Rank tracker: snapshot job completes on SerpApi; DFS rank endpoint call count → ~0 after flip
- [ ] **Optional schema:** persist `ProviderName` on `SeoRankTracking` rows (GeekRepository migration)

### Phase A.1 — Metering & ops (same release train as A)

**Status: code shipped (June 2026)** — rank + background SERP metering; ops alert env documented only.

| Task | Detail | Status |
|------|--------|--------|
| Metering | `rank_snapshot` on each successful rank provider call (`MeteredRankSnapshotProvider`) | [x] |
| Metering | `serp_fetch` for topical map + GEO background SERP (`SerpFetchMetering`); user deep SERP stays `deep_serp` via middleware (no double count) | [x] |
| Rank caps | Max enabled keywords per project (`tracked_rank_keyword` tier limits) on add | [x] |
| Rank caps | Pre-flight monthly `rank_snapshot` budget before worker batch per project | [x] |
| Alerts | `SERPAPI_MONTHLY_BUDGET_USD` documented in `.env.example` (alerting TBD) | [x] doc |
| Alerts | SerpApi dashboard + monthly budget check | [ ] ops process |

### Phase B — Keyword path off DFS

| Task | Detail |
|------|--------|
| `GeekKeywordProvider` v0 | GSC queries (per project) + **Google Ads Keyword Planner** (reuse Google OAuth stack; new scopes/quota doc in PR) + optional SERP seeds via `ISerpProvider` |
| `IKeywordProvider` swap | `KEYWORD_PROVIDER=gsc_ads` then `geek` without feature code changes |
| Discovery | Implement real `IKeywordDiscoveryProvider` per [KEYWORD-DISCOVERY-STRATEGY.md](KEYWORD-DISCOVERY-STRATEGY.md) — **do not** fold into DFS Labs |
| Degrade UX | If Ads quota unavailable, document planner fields that may be null vs DFS |

**Verification:**

- [ ] `KeywordResearchService` + topical seed mode work with `KEYWORD_PROVIDER` ≠ `dataforseo`
- [ ] DFS `keywords_for_keywords` call count → ~0
- [ ] `InternalKeywordDiscoveryProvider` replaced or gated behind feature flag for seed mode

### Phase C — GeekSerp v1 (MVP scope)

**Not a single sprint** — bounded MVP only.

| In scope (v1) | Out of scope (v2+) |
|---------------|-------------------|
| US + English, desktop organic | Full locale/device matrix |
| Parse: organic results, PAA, basic SERP features needed by `SerpResult` | Full DFS advanced parity |
| Playwright fetch + **documented** proxy policy | Full Bright Data–class fleet |
| Contract tests vs `DataForSEOSerpProviderTests` baseline | Anti-bot/CAPTCHA automation at scale |

| Task | Detail |
|------|--------|
| Implement | `GeekSerpProvider` + `GeekSerpRankSnapshotProvider` (may share fetch layer) |
| Dogfood | Deep SERP + content scoring only first |
| Legal/ops | Document ToS risk for owned fetch; proxy vendor decision |

**Verification:**

- [ ] Shared contract test suite passes for `provider: geek`
- [ ] Staging: `SERP_PROVIDER=geek` for internal projects before production default
- [ ] SerpApi SERP call volume drops for dogfooded routes

### Phase D — DataForSEO zero

| Task | Detail |
|------|--------|
| DI | Remove DFS registrations |
| Env | Remove `DATAFORSEO_*` from production |
| Code | Delete or archive `DataForSeo/*` providers + unused `DataForSeoClient` |

**Verification:**

- [ ] No production traffic to `api.dataforseo.com` (monitor logs 7 days)
- [ ] `dotnet test` green; no DFS types referenced from `GeekSeo.Application`

Site audit and topical map **ride Phases A–C** via existing interfaces — no parallel DFS feature tracks.

---

## Verification by phase

| Phase | Metric | How to check |
|-------|--------|--------------|
| 0 | Env switch changes DI type | Breakpoint or `/health/providers` |
| A | DFS SERP/rank calls | DataForSEO dashboard or HTTP client logs → ~0 |
| A | `ProviderName` accurate | SERP cache / API `Provider` field = `serpapi` |
| B | DFS keyword calls | DFS dashboard → ~0 for `keywords_for_keywords` |
| C | Parity | `GeekSeoBackend.Tests/DataForSEOSerpProviderTests.cs` → shared fixture |
| D | Zero DFS | Log grep `dataforseo.com` in production |

---

## Cross-repo boundaries

| Change | Repo |
|--------|------|
| New SerpApi/Geek providers, env DI, rank poll logic | **Geek-SEO** (`GeekSeoBackend`) |
| `SeoTrackedKeyword` / rank history CRUD | **GeekRepository** (already shipped for Sprint 2) |
| Optional `ProviderName` on rank snapshots | **GeekRepository** migration + internal API DTO |
| Google Ads Keyword Planner credentials | **Geek-SEO** (extend existing Google integration) |

---

## Relationship to other plan docs

| Document | Role |
|----------|------|
| [`KEYWORD-DISCOVERY-STRATEGY.md`](KEYWORD-DISCOVERY-STRATEGY.md) | **`IKeywordDiscoveryProvider` implementation** — Phase B companion |
| [`TODO.md`](TODO.md) | Product backlog (scoring v2, integrations, REDESIGN, ops) |
| [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) | What is live in production |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Services, ports, API gateway |
| [`TopicalMapUpgrade.md`](TopicalMapUpgrade.md) | Topical map UX gaps — data deps point here |
| [`REDESIGN-PLAN.md`](REDESIGN-PLAN.md) | Semrush-style UI — reference only for shell/audit UX |
| [`api-comparison.md`](api-comparison.md) | Historical vendor research — **re-verify pricing/endpoints before Phase A** |

Rank/agency UI backlog lives in [`TODO.md`](TODO.md) (integrations, post-v1 upgrade notes).

---

## Review checklist (use before any provider work)

- [ ] Does this change call a **new DataForSEO endpoint**? If yes, **stop** — find interface + Geek/SerpApi path.
- [ ] Does product code import a vendor SDK type? If yes, **wrong layer**.
- [ ] Is `SerpResult` shape preserved for all SERP consumers?
- [ ] Are GSC rankings and API rank snapshots still separate?
- [ ] Does Site Audit stay on `ICrawlerProvider` only?
- [ ] Is `ProviderName` on caches accurate after fallback?
- [ ] Does topical map use `IKeywordProvider` / `IKeywordDiscoveryProvider` / `ISerpProvider` only — no DFS types?
- [ ] Is Phase 0 (env DI) required for this change? If changing provider impl, yes.

---

## Decision summary

| Question | Answer |
|----------|--------|
| Canonical plan file? | **`SEO-PROVIDER-STRATEGY.md`** |
| What is “GeekDataForSeo”? | **Misnomer** in retired vendor-expansion plan. Target = owned providers (Geek SERP, crawl, Postgres cache). |
| Old `DATAFORSEO-REPLACEMENT-UPGRADE.md` filename? | **Redirect stub** — vendor-expansion plan deleted; do not revive Labs/OnPage sprints. |
| SerpApi? | **Yes**, interim **primary** for SERP + rank during bridge (after Phase 0). |
| DataForSEO? | **Trend to zero**; no Labs/Backlinks/OnPage expansion. |
| Rank tracker on DFS today? | **Expected** — Sprint 2; swap provider in Phase A. |
| Site audit vs DFS OnPage? | **Playwright / Geek crawl only.** |
| Topical map vs DFS Labs? | **`IKeywordProvider` + `IKeywordDiscoveryProvider`** — see KEYWORD-DISCOVERY-STRATEGY. |
| Env vars work today? | **Yes** — Phase 0 + A shipped; default `dataforseo`; SerpApi when `serpapi` + `SERPAPI_API_KEY`. |
