# Keyword Discovery Strategy — IKeywordDiscoveryProvider Implementation Plan

**Parent plan:** [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) (Phase B — keyword path off DFS)  
**Status:** Stub implemented, planning phase  
**Last Updated:** 2026-06-01  
**Owner:** Jeff Martin

---

## Current State

**InternalKeywordDiscoveryProvider (Stub)**
- Simple in-memory expansion of seed keyword with modifiers ("best", "top", "how to", etc.)
- No external API calls, no repository access, no projectId context
- Builds clean, integrated into DI
- Used by `TopicalMapService.GenerateSeedModeAsync()` for seed mode pipeline

**Why it's a stub:**
- Doesn't access real data (GSC queries, keyword relationships, content analysis)
- Just generates syntactic variations, not semantic discovery
- No project context → can't leverage project-specific data

---

## Available Data Sources (What We Have)

### 1. Google Search Console Queries
**Where:** `SeoGscQuery` entity (GeekRepository `geek_seo.seo_gsc_queries`)  
**What:** Real search queries that brought traffic to project  
**Access:** IKeywordRepository → GetByProjectAsync(projectId)  
**Pros:**
- Most relevant to the project
- Real user intent signals
- Already classified by impressions/clicks/position

**Cons:**
- Requires projectId context
- Limited to queries that drove traffic (discovery bias)
- May have privacy/sampling limits from GSC API

### 2. SeoKeyword Table (Historical Keyword Research)
**Where:** `SeoKeyword` entity (GeekRepository `geek_seo.seo_keywords`)  
**What:** Keywords from previous DataForSEO imports, SERP analysis, manual research  
**Access:** IKeywordRepository → GetByProjectAsync(projectId)  
**Pros:**
- Already scored (volume, difficulty, CPC)
- Rich metadata (Intent, Location, CachedAt)
- Multiple sources (SERP, research, clustering)

**Cons:**
- Historical → may be stale
- Limited to keywords already researched
- Requires projectId context

### 3. Topic Cluster Relationships
**Where:** `TopicalMapTopic` entities from previous map generations  
**What:** Semantic relationships (Pillar → Cluster → Article structure)  
**Access:** Hierarchical navigation from topical map  
**Pros:**
- Captures semantic intent ("best SEO tools" clusters with "free SEO tools")
- Hierarchical structure shows relationship strength

**Cons:**
- Requires parsing existing topical maps
- Circular dependency (building map while discovering keywords for map)

### 4. Published Content Analysis
**Where:** Article content from WordPress, internal blog  
**What:** Keywords naturally occurring in published content  
**Access:** WordPressApiProvider (crawl endpoint), PlaywrightCrawlerProvider  
**Pros:**
- Shows what content exists
- Can extract entities, topics, keyword density
- Real-world keyword validation

**Cons:**
- Requires crawl overhead
- Slow (async HTTP + parsing)
- High latency for discovery phase

### 5. Google Search Results (Free SERP Data)
**Where:** Google SERPs for seed keyword  
**What:** Related searches, "People also ask", featured snippets  
**Access:** PlaywrightCrawlerProvider (already configured)  
**Pros:**
- Free, no rate limits on initial crawl
- Real market signals
- Includes trending variants

**Cons:**
- Requires Playwright crawl (slower)
- HTML parsing fragility (Google layout changes)
- Not project-specific (generic market data)
- Risk of being blocked/rate-limited if abused

---

## Architectural Problem: ProjectId Context

### Current Interface
```csharp
public interface IKeywordDiscoveryProvider
{
    Task<Result<IReadOnlyList<KeywordResult>>> GetRelatedKeywordsAsync(
        string seedKeyword, string location, int count, CancellationToken ct = default);
}
```

**Issue:** No projectId → can't access project-specific data (GSC, SeoKeyword table)

### Solution Options

#### Option A: Modify Interface (BREAKING CHANGE)
```csharp
Task<Result<IReadOnlyList<KeywordResult>>> GetRelatedKeywordsAsync(
    Guid projectId, string seedKeyword, string location, int count, CancellationToken ct = default);
```
**Pros:** Clean, explicit, all providers know project context  
**Cons:** Breaking change to interface contract; external providers (DataForSEO, Claude) don't need projectId

#### Option B: Context Injection (ICurrentUserContext pattern)
```csharp
public class InternalKeywordDiscoveryProvider(
    IKeywordRepository keywordRepo, 
    ICurrentUserContext userContext) : IKeywordDiscoveryProvider
{
    public Task<Result<IReadOnlyList<KeywordResult>>> GetRelatedKeywordsAsync(...)
    {
        var projectId = userContext.CurrentProjectId; // somehow
        ...
    }
}
```
**Pros:** Doesn't break interface  
**Cons:** Fragile (depends on execution context); userContext might not have projectId

#### Option C: Factory Pattern
```csharp
public interface IKeywordDiscoveryProviderFactory
{
    IKeywordDiscoveryProvider Create(Guid projectId);
}
```
**Pros:** Clean DI, explicit projectId wiring  
**Cons:** Extra abstraction layer; complicates TopicalMapService call site

#### Option D: Stay Generic (Current Stub)
Keep interface project-agnostic, only use free/public data sources (Google SERP scrape, generic AI).  
**Pros:** No breaking changes  
**Cons:** Can't leverage project-specific signals (GSC queries, keyword history)

---

## Viable Approaches (Ranked by Impact vs Effort)

### 1. **GSC Query Analysis** (HIGH IMPACT, HIGH EFFORT)
**Algorithm:**
- Load all GSC queries for project
- Tokenize seed keyword + each GSC query
- Jaccard similarity ≥ 0.25 → candidate
- Score by: jaccard * impressions * (1 - position/100)
- Return top N

**Requirements:**
- Option A (modify interface) OR Option C (factory pattern)
- IKeywordRepository.GetByProjectAsync(projectId)
- SeoGscQuery entity loaded (must exist in schema)

**Timeline:** 2–3 hours (depends on entity existence)  
**Best for:** Hyper-relevant discovery (what *this* project actually ranks for)

### 2. **Keyword Table + GSC Fusion** (HIGH IMPACT, MEDIUM EFFORT)
**Algorithm:**
- Load SeoKeyword + GSC queries for project
- Build union of keyword pool
- Find relationships via Jaccard + volume/position scoring
- Return enriched results (include volume, difficulty, traffic data)

**Requirements:** Option A or C; both repositories  
**Timeline:** 3–4 hours  
**Best for:** Most comprehensive (combines historical research + traffic signals)

### 3. **SERP Scrape (People Also Ask)** (MEDIUM IMPACT, MEDIUM EFFORT)
**Algorithm:**
- PlaywrightCrawlerProvider crawls Google SERP for seed keyword
- Extract "People also ask" section + related searches
- Tokenize, deduplicate, score by position
- Return top N

**Requirements:**
- Playwright already configured ✓
- HTML parsing for Google SERP structure (fragile, maintainability risk)
- No projectId needed (generic approach)

**Timeline:** 2–3 hours  
**Best for:** Free, market-aware discovery; works without project context

### 4. **AI-Based Expansion** (MEDIUM IMPACT, HIGH COST)
**Algorithm:**
- Prompt Claude/OpenAI with seed keyword + optional project context
- Request N semantically related keyword variations
- Score by relevance

**Requirements:**
- ANTHROPIC_API_KEY configured
- Interface stays generic
- Cost: ~$0.01–0.05 per discovery call

**Timeline:** 1 hour (simple API call)  
**Best for:** Fast, high-quality semantic expansion; ongoing API cost

### 5. **Hybrid: SERP + GSC Fallback** (HIGH IMPACT, LOW EFFORT)
**Algorithm:**
- Try SERP scrape first (free, generic)
- If fails or returns <N results, load GSC queries (project-specific)
- Combine + deduplicate

**Requirements:**
- Partial Option A (add projectId parameter) OR accept that fallback won't work if interface stays generic
- Implement graceful fallback

**Timeline:** 2–3 hours  
**Best for:** Best of both worlds; works even if one source unavailable

---

## Recommendation

**Phase 1 (Now):** Keep stub, document architecture  
**Phase 2 (Sprint 3 continuation):** Implement SERP scrape (Option 3)
- No breaking changes
- Free, maintainable (Playwright already in use)
- Works without projectId refactor
- Gives real keyword discovery signals

**Phase 3 (Later):** Add GSC fusion (Option 2)
- Requires Option C (factory pattern) or deliberate interface change
- Higher ROI once we validate SERP scrape works
- Better for existing projects with rich GSC/keyword history

---

## Decision Log

- ✅ **2026-06-01:** Stub approved; full planning deferred
- ⏳ **2026-06-XX:** Choose between Option 2 (SERP scrape) vs Option 1 (GSC analysis)
- ⏳ **2026-06-XX:** Implement chosen approach; update TopicalMapService integration

---

## Open Questions

1. Does `SeoGscQuery` entity exist in schema? (blocks GSC-based approaches)
2. How stable is Google SERP HTML structure? (affects SERP scrape maintainability)
3. Should we add projectId to interface now or refactor later? (architectural tradeoff)
4. What's acceptable cost/latency for keyword discovery? (affects AI vs SERP vs GSC choice)
