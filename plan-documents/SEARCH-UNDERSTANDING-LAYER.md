# Search Understanding Layer — Product & Architecture Draft

**Status:** Implemented in Niche Analyzer (`sul-2.0`) with a canonical 14-step pipeline. Current design direction: no fallback reconstruction or substitution; each step must persist its own artifact and downstream consumers may read only canonical stored outputs.  
**Owner intent:** Capture north-star direction before implementation; component-scoped rollout.  
**Related:** [`docs/reference/site-niche-analyzer-v1-spec.md`](../docs/reference/site-niche-analyzer-v1-spec.md) (first consumer), [`NICHE-ANALYZER-ARTIFACT-PARADIGM.md`](NICHE-ANALYZER-ARTIFACT-PARADIGM.md) (read-from-DB default, execute-on-approval, suggestion tiers), [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) (SERP/keyword APIs), [`ARCHITECTURE.md`](../ARCHITECTURE.md)

---

## North star

GeekSEO cannot be a literal clone of Google, Bing, DuckDuckGo, or any single engine. We do not have their indexes, link graphs, query logs, or ranking models.

**What we can build:** a **close approximate** of how search systems likely understand a site — inferred from a **composite of public and freely available data sources** that reflect characteristics **shared across major search engines**:

1. **Discover** URLs (robots, sitemap, internal links)
2. **Fetch** page content (HTML, rendered DOM where needed)
3. **Extract** topics, entities, and structure (headings, lists, schema, URL patterns)
4. **Associate** topics with pages and each other (internal links, repeated phrases, dedicated URLs)
5. **Validate** against external demand proxies (SERP/keyword APIs — market-available, not engine-internal)
6. **Score** coverage and gaps for recommendations and (later) autonomous fixes

**One-sentence definition:**

> **GeekSEO Site Understanding = weighted fusion of public crawl signals that approximate shared search-engine indexing behavior; owner-connected data (GSC, GA4) augments but does not define the model.**

Suggestions, topical maps, content guard, and autonomous actions must all consume **the same fused model** — not parallel heuristics that drift apart.

---

## What we are not building

| Engine capability | GeekSEO stance |
|-------------------|----------------|
| Full-web index | No — targeted crawl of project domain (+ optional competitors via SERP) |
| PageRank / link-based authority | Partial — internal link graph only unless external backlink provider added later |
| User behavior (clicks, pogo-sticking) | No — except aggregated owner metrics when GSC/GA4 connected |
| Exact ranking formula | No — we infer **topics** and **gaps**, not positions |
| Real-time index refresh | No — scheduled analysis + on-demand re-runs |

---

## Signal taxonomy

### Tier 1 — Public / freely observable (foundation)

These are available to any crawler without owner login. **The composite model must work using Tier 1 alone.**

| Signal | Source | Extractor (existing / planned) | Shared engine behavior |
|--------|--------|--------------------------------|------------------------|
| Structured business data | JSON-LD, microdata | `SchemaOrgExtractor` ✅ | Entity + service hints |
| Visible page content | HTML / Playwright DOM | `PageContentExtractor` 🔲 | Primary topical signals |
| Headings & sections | H1–H3, service blocks | `HomepageHeadingsExtractor` ✅ (partial) | Document outline |
| Site navigation | Nav menus | `NavMenuExtractor` ✅ | Information architecture |
| URL inventory | XML sitemap | `SitemapExtractor` ✅ | Declared important URLs |
| URL patterns | Path segments | `UrlPatternExtractor` 🔲 | Topic ↔ URL association |
| Internal links | Anchor text + href | `InternalLinkExtractor` 🔲 | Topical clustering |
| Crawl / index hints | robots.txt, canonical, noindex | Audit / crawler 🔲 | Inclusion rules |
| Entity disambiguation | `sameAs`, Wikidata/Wikipedia URLs in schema | Extend `SchemaOrgExtractor` 🔲 | Knowledge graph alignment |

### Tier 2 — Market-available APIs (validation layer)

Not free, but **publicly purchasable** proxies for “does search demand exist for this topic?” Shared across engines at the **SERP shape** level, not identical rankings.

| Signal | Provider interface | Status |
|--------|-------------------|--------|
| Search volume, KD | `IKeywordProvider` | Standalone Step 8 (`keywords`) — optional enrichment under review |
| SERP results, competitors | `ISerpProvider` | Planned in niche analyzer |
| Related queries | Keyword discovery providers | [`KEYWORD-DISCOVERY-STRATEGY.md`](KEYWORD-DISCOVERY-STRATEGY.md) |

### Tier 3 — Owner-connected (augmentation only)

Available only when the user connects their property. **Must not be required** for a complete analysis.

| Signal | Source | Role in composite |
|--------|--------|-------------------|
| Queries with impressions/clicks | Google Search Console | Confirm / boost topics Google already associates with the site |
| Landing page performance | GA4 | Coverage quality hint |
| Rank snapshots | Rank tracking module | Progress over time, not initial discovery |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                  14 persisted canonical steps                    │
│  schema → site_urls → nav → headings → page_content →          │
│  site_structure → merging → keywords → serp_validation →       │
│  profile → local → coverage → scoring → complete               │
└────────────────────────────┬────────────────────────────────────┘
                             │ persisted artifacts only
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Stored site-understanding state                │
│  step log · step statuses · crawled URLs · candidates · fusion  │
│  profile summary · pillars · subtopics · competitors · scores   │
└────────────────────────────┬────────────────────────────────────┘
                             │ no reconstruction / no substitution
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Consumers                                │
│  NicheAnalyzer · TopicalMap · ContentGuard · Autonomous actions  │
└─────────────────────────────────────────────────────────────────┘
```

### Layer boundaries (Geek-SEO repo)

| Layer | Location | Rules |
|-------|----------|-------|
| Contracts | `GeekSeo.Application` | DTOs, interfaces — no HTTP, no EF |
| Extractors | `GeekSeoBackend/Services/SiteUnderstanding/` (new) or `NicheExtraction/` (migrate gradually) | HttpClient / Playwright only |
| Fusion | `TopicFusionEngine` (new pure service) | Deterministic, unit-testable |
| Persistence | Snapshots via GeekRepository | Store fused output + full candidate pool for debugging (optional JSON column) |
| UI | `frontend/src/components/niche-analyzer/` | Provenance, included/excluded topics |

---

## Core contracts (draft)

### `TopicCandidate`

One normalized topic phrase from one or more extractors **before** pillar selection.

```csharp
public sealed record TopicCandidate
{
    public required string Name { get; init; }           // "AI Strategy Consulting"
    public required string Slug { get; init; }
    public required IReadOnlyList<TopicEvidence> Evidence { get; init; }
    public decimal Confidence { get; init; }               // 0–1 fusion score
    public string? DedicatedPageUrl { get; init; }        // best matching URL if any
    public int InternalLinkCount { get; init; }
}

public sealed record TopicEvidence
{
    public required string Source { get; init; }           // schema | page | sitemap | nav | heading | link | gsc | serp
    public string? Snippet { get; init; }                  // short quote or JSON path
    public string? Url { get; init; }
    public decimal Weight { get; init; }                   // extractor-specific
}
```

### `FusedSiteUnderstanding`

Output of fusion — consumed by Niche Analyzer and downstream modules.

```csharp
public sealed record FusedSiteUnderstanding
{
    public required IReadOnlyList<TopicCandidate> AllCandidates { get; init; }
    public required IReadOnlyList<TopicCandidate> SelectedPillars { get; init; }
    public required IReadOnlyList<TopicCandidate> ExcludedCandidates { get; init; }
    public required IReadOnlyDictionary<string, string> ExclusionReasons { get; init; } // slug → reason
    public required string FusionVersion { get; init; }     // e.g. "sul-1.0"
    public required IReadOnlyList<string> SignalSourcesPresent { get; init; }
}
```

### Confidence scoring (initial heuristic — tune with tests)

| Evidence pattern | Confidence boost |
|------------------|------------------|
| Schema `knowsAbout` or offer catalog | +0.35 |
| Same phrase on dedicated URL (sitemap + slug match) | +0.25 |
| Nav item + page H2 | +0.20 |
| Page body only (lists, service sections) | +0.15 |
| Heading only | +0.10 |
| GSC query cluster match (Tier 3) | +0.20 |
| SERP validates topic (Tier 2) | +0.15 |

**Merge rule:** Same slug from multiple sources → single `TopicCandidate` with stacked evidence; confidence capped at 1.0.

**Selection rule:** Sort by confidence descending; apply `PillarValidator` gates; select top N (default **7**, configurable per project in v2). **Every excluded candidate gets a logged reason** (below cap, failed gate, duplicate of higher-confidence topic).

---

## Relationship to current Niche Analyzer

Today (`SITE-NICHE-ANALYZER.md` merge section):

- Priority: **schema > sitemap > nav > heading**
- Headings only if total **< 3**
- Hard **`MaxPillars = 7`**
- Step log `candidateCount` = naive sum of raw lists (misleading)

**Known gap (geekatyourspot.com example):** Homepage JSON-LD declares **7** `knowsAbout` topics; page copy lists **5** additional services. Current pipeline outputs **7** — under-modeling vs what any crawler reads.

**Migration path:** Niche Analyzer becomes a strict **consumer of persisted canonical artifacts** instead of repairing missing data in the UI or controller layer. `PillarMerger` evolves into `TopicFusionEngine` or wraps it for backward-compatible `DiscoveredPillar` output until all callers migrate.

## Persistence rule

Every canonical step must persist:
- a step status entry
- a step log entry
- a step-specific artifact if downstream sections depend on more than the human-readable log

If a required artifact is missing, malformed, or stale, the API must return an explicit unavailable/not-ready/error state. The system must not:
- derive statuses from the step log
- rebuild fusion from topic candidates in the frontend
- substitute an older successful profile
- widen a specialized read into a broader fallback snapshot

## `keywords` step rule

Step 8 `keywords` is now treated as a standalone executable section, not as an implicit side effect of generic demand enrichment.

It must answer independently:
- what keyword artifact is persisted per pillar
- which downstream steps are allowed to read it
- whether it materially improves search-engine-style site understanding

This makes `keywords` removable or disable-able if it proves low-value without destabilizing the core Tier 1 site-understanding model.

---

## Validation (2026-06-06, updated)

| Check | Result |
|-------|--------|
| **Tier 1** — `NicheExtractionTests` (fusion, extractors, coverage) | **45 passed** |
| Fusion version | `sul-1.3` |
| Fixture baseline | geekatyourspot JSON-LD → 12 schema topics + page verticals |
| **Tier 2 ops** — `/health/providers` | Env wired: `serpProvider=dataforseo`, `keywordProvider=dataforseo`; credential flags **true** |
| **Tier 2 live** — `SUL_LIVE=1 npm run test:integration:sul-providers` | **Blocked:** DataForSEO API returns **402 Payment Required** (account out of credits). Steps 8–9 skip in Niche Analyzer until fixed. |
| **Tier 2 fix options** | (1) Fund DataForSEO account, or (2) `SERP_PROVIDER=serpapi` on GeekSeoBackend Railway (`SERPAPI_API_KEY` already present). Keywords still need DataForSEO or future `KEYWORD_PROVIDER` swap. |
| Production re-analyze | Run after Tier-2 live probe passes; compare step log to [`geekatyourspot-niche-baseline.md`](../docs/reference/geekatyourspot-niche-baseline.md) |

PayPal / billing work **deferred**.

---

## Phased implementation

Each phase is a **shippable component**. Later phases do not block earlier ones.

**Ship status (June 2026):**

| Phase | Code in repo | Production behavior today |
|-------|----------------|---------------------------|
| **A** Public fusion | ✅ | ✅ Tier 1 runs without vendors |
| **B** Structure signals | ✅ | ✅ |
| **C** Keyword + SERP demand | ✅ | ⏸ **Skipped** — DataForSEO 402; not “unwired,” vendor billing |
| **D** GSC overlay | ✅ | ✅ when Google connected |
| **E** Action recommendations | ✅ | ✅ from fusion snapshot |

`PillarMerger` retained for tests; production path uses `TopicFusionEngine`.

### Phase A — Public composite merge (Niche Analyzer) ✅

**Goal:** Page + schema + sitemap + nav as **peers**, not schema-first with heading fallback.

| # | Deliverable |
|---|-------------|
| A1 | `TopicCandidate` + `TopicEvidence` in `GeekSeo.Application` |
| A2 | `PageContentExtractor` — service-like phrases from homepage + top N sitemap URLs |
| A3 | `TopicCandidatePool` builder in orchestrator |
| A4 | `TopicFusionEngine` — replace priority-only merge; retain `PillarValidator` gates |
| A5 | Step log + UI: all candidates, selected, excluded + reasons |
| A6 | Revisit `MaxPillars`: soft cap with transparency (show all ≥ threshold; highlight top 7) |

**Exit criteria:** geekatyourspot re-analyze shows 12+ candidates, provenance per topic, explicit exclusion if cap applies. Unit tests ✅; live re-analyze pending.

### Phase B — Structure signals ✅

| # | Deliverable |
|---|-------------|
| B1 | `InternalLinkExtractor` — anchor text → topic evidence |
| B2 | `UrlPatternExtractor` — `/services/ai-chatbots` → topic boost |
| B3 | Confidence model unit tests with fixture HTML |

### Phase C — Demand validation (Tier 2) — code ✅, ops blocked

Wire existing plan steps 5–6 from `SITE-NICHE-ANALYZER.md`:

| # | Deliverable |
|---|-------------|
| C1 | `IKeywordProvider` enrichment on fused pillars |
| C2 | `ISerpProvider` validation — demote topics with no SERP footprint |
| C3 | Competitor domains from SERP → `NicheCompetitor` |

### Phase D — Owner augmentation (Tier 3) ✅

| # | Deliverable |
|---|-------------|
| D1 | `GscQueryExtractor` → query clusters as `TopicEvidence` |
| D2 | Boost / confirm pillars matching GSC; flag “page says X, GSC silent” |
| D3 | Banner when GSC not connected (existing plan) — analysis still complete on Tier 1 |

### Phase E — Autonomous actions ✅

**Prerequisite:** Phases A–B stable; provenance trustworthy.

| Action | Trigger | Guardrails |
|--------|---------|------------|
| Schema sync | Page + nav agree; schema missing topic | User approval or Content Guard policy |
| Suggest pillar page | High confidence, no `DedicatedPageUrl` | Draft only first |
| Content brief | Gap in `FusedSiteUnderstanding` | Existing calendar pipeline |

All actions read **the same** `FusedSiteUnderstanding` snapshot ID — no shadow logic.

---

## Design principles

1. **Public-first** — Tier 1 alone must produce a useful model.
2. **Provenance always** — no topic without `Evidence[]`.
3. **No silent drops** — cap and gate failures → `ExclusionReasons`.
4. **Extractors stay dumb** — URL/HTML in, `TopicCandidate[]` out.
5. **Fusion stays central** — one engine, many consumers.
6. **Deterministic by default** — same inputs → same output; AI optional later for labeling, not discovery.
7. **Version everything** — `FusionVersion`, `AnalysisVersion`, snapshot IDs for authority tracking.

---

## UI / API surfacing

### Step log (analysis-details)

Extend existing Phase 1.5 step log:

```json
{
  "slug": "merge-topics",
  "label": "Fuse public signals into topics",
  "output": {
    "signalSourcesPresent": ["schema", "page", "sitemap", "nav"],
    "candidateCount": 12,
    "selectedCount": 7,
    "excludedCount": 5,
    "candidates": [
      {
        "name": "AI Consulting",
        "confidence": 0.55,
        "sources": ["page"],
        "selected": false,
        "exclusionReason": "below_pillar_cap"
      }
    ]
  }
}
```

### Niche Analyzer UI

- **“Where these pillars came from”** callout → evolve to full candidate matrix
- Filter: schema-only | page-only | multi-source
- Link to “Add to schema” / “Create page” when Phase E exists

---

## Testing strategy

| Level | Focus |
|-------|-------|
| Unit | `TopicFusionEngine`, confidence weights, slug dedupe, gate interaction |
| Fixture | geekatyourspot baseline — 12 public topics, 7 schema |
| Integration | Full analyze job → snapshot contains `FusedSiteUnderstanding` |
| Regression | Prior snapshots remain readable when `FusionVersion` bumps |

Reference baseline: [`docs/reference/geekatyourspot-niche-baseline.md`](../docs/reference/geekatyourspot-niche-baseline.md)

---

## Open decisions

| # | Question | Proposed default |
|---|----------|------------------|
| 1 | Persist full candidate pool or selected only? | JSON on `niche_profiles` analysis metadata (debug + UI); pillars table = selected only |
| 2 | Default pillar cap | 7 recommended, 12 max display; project setting in v2 |
| 3 | New folder vs extend `NicheExtraction/` | Start in `NicheExtraction/`; rename to `SiteUnderstanding/` when ≥3 extractors moved |
| 4 | AI labeling of page phrases | Defer — regex/heading/list heuristics first |
| 5 | Cross-engine SERP | Single `ISerpProvider`; engine param optional later |

---

## Success metrics

- **Accuracy:** Multi-source topics (schema + page) rank higher than single-source noise.
- **Transparency:** User can answer “why 7 not 12?” from UI without support.
- **Downstream:** Topical map and gap scores use fused pillars — no second topic list.
- **Automation readiness:** Schema sync suggestions match fused “page-only” gaps ≥90% on test fixtures.

---

## Document history

| Date | Change |
|------|--------|
| 2026-06-03 | Initial draft — north star, signal tiers, contracts, phases A–E, niche analyzer migration |
| 2026-06-13 | Updated for canonical 14-step persisted pipeline, no-fallback rule, and standalone `keywords` step |
