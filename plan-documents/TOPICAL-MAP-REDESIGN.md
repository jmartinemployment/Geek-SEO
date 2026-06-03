# Topical Map Redesign — Full Pipeline Plan

## Philosophy

An SEO topical map is the ultimate blueprint for establishing topical authority.
Instead of chasing disjointed keywords, we map the entire universe of a subject
to prove to search engine crawlers that a site is the definitive resource.

**The goal of this pipeline:** Given any website URL, produce a complete,
hierarchically validated topical map — regardless of GSC data volume.

**Root problem with current implementation:** The existing tool clusters GSC
queries mechanically. Sites with thin GSC history (new sites, low-traffic local
businesses) get 2 topics. A correct topical map must be derived from the site's
actual niche architecture, not from historical query data alone.

---

## Pipeline Overview

```
Site URL
  │
  ▼
[Phase 1] SiteNicheAnalyzer
  Crawl → extract niche + validated pillars
  │
  ▼
[Phase 2] PillarKeywordExpander
  Expand each pillar → full keyword universe (free + paid sources)
  │
  ▼
[Phase 3] TopicClusteringService  ← already exists, extend
  Cluster keywords under pillars → assign tiers
  │
  ▼
[Phase 4] CoverageMapper  ← already exists
  Overlay GSC data + existing content → gap/partial/covered
  │
  ▼
[Phase 5] EntityGapAnalyzer  ← already exists
  Semantic entity coverage per topic
  │
  ▼
[Phase 6] InternalLinkingBlueprintBuilder  ← already exists
  Pillar → cluster → article link graph
  │
  ▼
TopicalMapResult (enriched, pillar-structured)
```

Phases 3–6 already built. This plan covers Phases 1–2 in depth.

---

## Phase 1 — SiteNicheAnalyzer

### Purpose
Identify the Root Entity (core niche) and derive 3–5 validated Core Pillars
from the site's own structure. Zero AI cost. Deterministic.

### The Root Entity Concept
The Root Entity is the single overarching macro-topic the site wants to rank for.
It is the trunk of the content tree.

**The Sweet Spot Rule:** Broad enough to support 50–100 distinct content ideas,
narrow enough to be covered exhaustively. "Technology" is too broad.
"Ergonomic keyboards for programmers" is too narrow.
"Computer repair and IT support Boynton Beach" is correct.

### Data Sources (in priority order)

| Source | What it reveals | Cost |
|--------|----------------|------|
| Schema.org JSON-LD | `@type: LocalBusiness`, `hasOfferCatalog`, `serviceType` — explicit service list | Free |
| Sitemap URL paths | `/services/computer-repair` → pillar names from URL slugs | Free |
| Nav menu `<nav>` links | Owner-defined information architecture | Free (Playwright) |
| H1 + H2 on homepage | Primary and secondary topic signals | Free (Playwright) |
| `<title>` + meta description | Niche summary in ~160 chars | Free (Playwright) |
| Footer links | Often reveals full service/location taxonomy | Free (Playwright) |

### Pillar Extraction Algorithm

```
1. Fetch sitemap.xml → parse all URLs
2. Filter: remove /about, /contact, /privacy, /terms, /cart, /wp-*, /tag/*, /page/*
3. Group by first meaningful path segment
   /services/computer-repair   → segment: "services", child: "computer-repair"
   /locations/boynton-beach    → segment: "locations", child: "boynton-beach"
4. Slug → Title Case (hyphen removal, capitalize)
5. Merge nav menu items as override (owner intent > URL inference)
6. Schema.org services override both (most explicit)
```

### Pillar Validation Framework

Before accepting any pillar, it must pass all three gates:

**Gate 1 — Semantic Scope**
Can this pillar support ≥ 10 distinct sub-topics (child nodes)?
If fewer than 10 keywords can be harvested for it in Phase 2 → demote to sub-topic.

**Gate 2 — Search Intent Alignment**
Are SERPs for this pillar distinct from adjacent pillars?
Two pillars whose Google results overlap significantly → merge.
Implementation: compare top Autocomplete suggestions; if Jaccard similarity > 0.6 → merge.

**Gate 3 — Commercial Relevance**
Does this pillar map to a service, product, or conversion goal?
Pure informational pillars accepted only if they serve as top-of-funnel entry.

### Pillar Architecture Lenses (applied algorithmically)

**A. Functional/Component Decomposition (primary)**
Nav menu items and service URL segments → structural pillars.
Example: `computer-repair`, `virus-removal`, `network-setup`, `data-recovery`

**B. User Journey / Funnel**
Detect awareness vs. decision vs. transactional intent from URL patterns:
- `/blog/` → informational (top of funnel)
- `/services/` → commercial (bottom of funnel)
- `/compare/` or `/vs/` → evaluative (mid-funnel)

**C. Location Pillars (Local SEO)**
Detect `/locations/`, `/areas/`, or city names in URLs/nav.
Each major service area = a location pillar for local businesses.

### C# Interface

```csharp
public interface ISiteNicheAnalyzer
{
    Task<NicheProfile> AnalyzeAsync(string siteUrl, CancellationToken ct = default);
}

public record NicheProfile
{
    public required string RootEntity { get; init; }       // "Computer repair IT support Boynton Beach"
    public required string NicheSummary { get; init; }     // from title + meta
    public required IReadOnlyList<ContentPillar> Pillars { get; init; }
    public required string DiscoveryMethod { get; init; }  // "schema" | "sitemap" | "nav" | "fallback"
}

public record ContentPillar
{
    public required string Name { get; init; }             // "Computer Repair"
    public required string Slug { get; init; }             // "computer-repair"
    public string? PageUrl { get; init; }                  // "/services/computer-repair"
    public required string Intent { get; init; }           // "commercial" | "informational" | "local"
    public int Depth { get; init; }                        // 1 = top-level pillar
}
```

### Implementation Files

```
GeekSeo.Application/
  Interfaces/Seo/ISiteNicheAnalyzer.cs
  Models/Seo/NicheProfile.cs

GeekSeoBackend/
  Services/Seo/SiteNicheAnalyzer.cs
    ├── ExtractFromSchema()     ← JSON-LD parse
    ├── ExtractFromSitemap()    ← XML parse
    ├── ExtractFromNav()        ← Playwright HTML parse
    └── ValidatePillars()       ← 3-gate validation
```

---

## Phase 2 — PillarKeywordExpander

### Purpose
Expand each validated pillar into a complete keyword universe.
Goal: 50–200 keywords per pillar, covering all intents and subtopics.

### People Also Ask (PAA) Integration

Google PAA and https://alsoasked.com surface questions people ask around a topic.
These are critical for FAQ clusters and informational subtopics.

**Free approach:** Scrape Google's autocomplete question variants:
- `{pillar} how to`
- `{pillar} what is`
- `{pillar} why`
- `{pillar} when`
- `{pillar} vs`
- `{pillar} cost`
- `{pillar} near me`

**AlsoAsked.com:** Has an API (paid at scale, limited free tier).
Used for deeper question mapping — "People Also Ask" tree expansion.
Deferred until budget available; structure built to plug in.

### Keyword Expansion Sources

#### Tier 1 — Free, No Account Required

**Google Autocomplete**
Endpoint: `https://suggestqueries.google.com/complete/search?output=firefox&q={query}&hl=en`
Returns: JSON array of up to 10 suggestions.

Expansion strategy per pillar:
```
{pillar} a → {pillar} z          (26 calls × alphabetical)
{pillar} how                      (1 call)
{pillar} best                     (1 call)
{pillar} near me                  (1 call)
{pillar} cost / price / cheap     (3 calls)
{pillar} vs                       (1 call)
{pillar} for [home/business/mac]  (3 calls)
{pillar} without                  (1 call)
{pillar} [city name]              (N calls for local)
─────────────────────────────────────────────
~37 calls per pillar → ~200–370 raw suggestions
5 pillars = ~185 calls total → ~1,000–1,850 keywords
```

Rate limiting: add 150ms delay between calls. No auth, no cost.

#### Tier 2 — Free with Account

**Bing Autosuggest API** (Azure Cognitive Services)
Free tier: 1,000 calls/month
Endpoint: `https://api.bing.microsoft.com/v7.0/suggestions?q={query}`
Requires: Azure free account (no billing required for free tier)
Env var: `BING_AUTOSUGGEST_KEY`

**Google Search Console** (already integrated)
Overlay actual query data on top of discovered keywords.
Existing queries get search volume/position data; discovered keywords get flagged as gaps.

#### Tier 3 — Paid (future, plug-in ready)

**DataForSEO `domain_organic`**
Pass project URL → get all keywords site ranks for in DataForSEO database.
Directly solves thin-GSC problem. ~$0.001 per request.
Env var: `DATAFORSEO_LOGIN`, `DATAFORSEO_PASSWORD` (already in env)

**DataForSEO `keywords_for_site`** (Google Ads API wrapper)
Similar to domain_organic but uses Google's data.

**SEMrush `domain_organic`**
Pro plan ($119/mo) unlocks. Same purpose.
Env var: `SEMRUSH_API_KEY`

**AlsoAsked.com API**
PAA tree expansion — questions + child questions per keyword.
Env var: `ALSO_ASKED_API_KEY`

### Deduplication & Scoring

After harvesting from all sources:
1. Normalize: lowercase, strip punctuation, deduplicate
2. Score each keyword:
   - Has search volume data? +weight
   - Question format (how/what/why)? → FAQ intent tag
   - Contains city/location? → local intent tag
   - Contains buy/price/cost/hire? → commercial intent tag
3. Group under parent pillar by keyword overlap
4. Drop keywords with < 0.15 Jaccard similarity to any pillar

### C# Interface

```csharp
public interface IPillarKeywordExpander
{
    Task<IReadOnlyList<PillarKeywordSet>> ExpandAsync(
        NicheProfile niche,
        string location,
        CancellationToken ct = default);
}

public record PillarKeywordSet
{
    public required ContentPillar Pillar { get; init; }
    public required IReadOnlyList<DiscoveredKeyword> Keywords { get; init; }
}

public record DiscoveredKeyword
{
    public required string Keyword { get; init; }
    public required string Source { get; init; }    // "autocomplete" | "bing" | "dataforseo" | "gsc"
    public required string Intent { get; init; }    // "informational" | "commercial" | "local" | "faq"
    public int? SearchVolume { get; init; }         // null until enriched
    public decimal? KeywordDifficulty { get; init; }
}
```

### Implementation Files

```
GeekSeo.Application/
  Interfaces/Seo/IPillarKeywordExpander.cs
  Models/Seo/PillarKeywordSet.cs
  Models/Seo/DiscoveredKeyword.cs

GeekSeoBackend/
  Providers/Seo/
    GoogleAutocompleteProvider.cs   ← Tier 1, free
    BingAutosuggestProvider.cs      ← Tier 2, Azure free
    AlsoAskedProvider.cs            ← Tier 3, paid stub
  Services/Seo/
    PillarKeywordExpander.cs        ← orchestrates providers
    KeywordDeduplicator.cs          ← normalize + dedupe
    KeywordIntentClassifier.cs      ← tag intent from keyword text
```

---

## Phase 3 — TopicClusteringService (Extension)

**Already exists.** Requires these additions:
- Accept `IReadOnlyList<PillarKeywordSet>` as input (currently only takes GSC rows)
- Preserve pillar assignment through clustering (currently loses pillar context)
- Enforce pillar boundary — no keyword crosses pillar lines unless overlap > threshold

No rewrite. Add overload `ClusterPillarKeywords(IReadOnlyList<PillarKeywordSet>)`.

---

## Phase 4 — CoverageMapper (Extension)

**Already exists.** No changes needed.
GSC data overlays on discovered keywords: query found in GSC → coverage signal.
Existing content documents matched to keywords → covered/partial/gap.

---

## Phase 5 — EntityGapAnalyzer

**Already exists.** No changes needed.

---

## Phase 6 — InternalLinkingBlueprintBuilder

**Already exists.** Gains richer input since pillars are now explicit and validated.
Pillar pages link down to clusters; clusters link back up and laterally.

---

## Updated GenerateAsync Flow

```
OLD:
  GSC queries → cluster → enrich → save

NEW:
  1. SiteNicheAnalyzer.AnalyzeAsync(project.Url)
     → NicheProfile { pillars }

  2. PillarKeywordExpander.ExpandAsync(niche, location)
     → PillarKeywordSet[] { keyword universe per pillar }

  3. If GSC data available (> 20 queries):
       Merge GSC rows into keyword sets (add coverage signals)

  4. TopicClusteringService.ClusterPillarKeywords(pillarKeywordSets)
     → TopicalMapTopic[] with pillar preserved

  5. CoverageMapper → EntityGapAnalyzer → BlueprintBuilder
     (unchanged)

  6. Save + return TopicalMapResult
```

GSC data is now an enrichment layer, not the source. Site with zero GSC history
produces a full topical map. Site with rich GSC history gets better coverage signals.

---

## Topical Map Result Changes

Add to `TopicalMapResult`:
```csharp
public NicheProfile? NicheProfile { get; init; }     // root entity + pillars
public string DiscoveryMethod { get; init; }          // "site-analysis" | "gsc" | "seed" | "hybrid"
public int PillarCount { get; init; }                 // validated pillar count
```

Add to `TopicalMapTopic`:
```csharp
public string? PillarSlug { get; init; }             // machine-readable pillar key
public string? KeywordSource { get; init; }           // "autocomplete" | "bing" | "gsc" | "dataforseo"
```

---

## Build Order

| Order | Component | Effort | Blocks |
|-------|-----------|--------|--------|
| 1 | `SiteNicheAnalyzer` — schema + sitemap + nav | Medium | Phase 2 |
| 2 | `GoogleAutocompleteProvider` | Small | Phase 2 |
| 3 | `KeywordIntentClassifier` | Small | Phase 3 |
| 4 | `PillarKeywordExpander` (orchestrator) | Medium | Phase 3 |
| 5 | `TopicClusteringService` overload | Small | Phase 4+ |
| 6 | Wire into `TopicalMapService.GenerateAsync` | Medium | Done |
| 7 | `BingAutosuggestProvider` | Small | Parallel |
| 8 | `AlsoAskedProvider` stub | Small | Future |
| 9 | SEMrush provider | Medium | Future (paid) |

---

## Environment Variables (new)

| Variable | Required | Purpose |
|----------|----------|---------|
| `BING_AUTOSUGGEST_KEY` | Optional | Tier 2 keyword expansion |
| `SEMRUSH_API_KEY` | Optional | Tier 3 keyword + domain organic |
| `ALSO_ASKED_API_KEY` | Optional | PAA tree expansion |

All optional. Tool degrades gracefully — Google Autocomplete always runs.

---

## Status

- [ ] Phase 1: SiteNicheAnalyzer
- [ ] Phase 2: PillarKeywordExpander + GoogleAutocompleteProvider
- [ ] Phase 2: BingAutosuggestProvider
- [ ] Phase 2: AlsoAskedProvider (stub, activate when key available)
- [ ] Phase 3: TopicClusteringService overload
- [ ] Phase 6: Wire into TopicalMapService.GenerateAsync
- [ ] Frontend: display NicheProfile + pillar tree view

*Last updated: 2026-06-02*
