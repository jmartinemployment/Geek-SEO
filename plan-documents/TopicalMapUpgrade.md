# GeekSeo Topical Map — Feature Upgrade Plan

## Context

GeekSeo's current topical map is GSC-dependent (requires a connected Google Search Console account) and uses three clustering methods: GSC page match, SERP signature, and token overlap fallback. It produces flat topic clusters with coverage status and priority scores, visualized via @xyflow/react.

Competitors (TopicalMap.ai, Surfer SEO, SearchAtlas, Agility Writer, GravityWrite, FatJoe, AskOptimo) reveal five capability gaps:

1. **No seed-keyword mode** — cannot generate a map without GSC data
2. **Flat hierarchy** — no Pillar → Cluster → Article three-tier structure
3. **No entity gap detection** — doesn't identify missing semantic concepts
4. **No internal linking blueprint** — no content sequence or linking recommendations
5. **No export** — no CSV / PDF / Excel output

This plan closes all five gaps across four sprints.

---

## Competitor Benchmark Summary

| Capability | TopicalMap.ai | Surfer SEO | SearchAtlas | Agility Writer | Current GeekSeo |
|------------|:---:|:---:|:---:|:---:|:---:|
| Seed keyword input (no GSC) | ✓ | ✓ | ✓ | ✓ | ✗ |
| 3-tier hierarchy | ✓ | ✗ | ✓ | ✓ | ✗ |
| Entity gap detection | implicit | implicit | implicit | **explicit** | ✗ |
| Internal linking blueprint | ✗ | ✗ | ✗ | **explicit** | ✗ |
| Color-coded coverage | ✓ | ✓ (hex) | ✓ | ✗ | partial |
| CSV/PDF export | ✓ | ✓ | ✓ | ✗ | ✗ |
| 800–1200 keyword scale | ✓ | variable | ✓ | variable | ~200 |
| Auto-refresh (14 days) | ✗ | ✓ | ✗ | ✗ | **✓** |
| Search metrics per topic | ✓ | ✓ | ✓ | minimal | **✓** |
| GSC integration | ✗ | ✓ | ✗ | ✗ | **✓** |

**GeekSeo already leads on:** auto-refresh, real GSC data integration, and live search metrics. Gaps are input flexibility, hierarchy depth, entity analysis, internal links, and export.

---

## Architecture: Two Input Modes

The upgrade adds a parallel `SeedKeywordMode` alongside the existing `GscMode`:

```
TopicalMapService.GenerateAsync(request)
    │
    ├─ if request.Mode == GscMode:
    │   existing pipeline (GSC rows → SERP clusters → enrich) [unchanged]
    │
    └─ if request.Mode == SeedKeywordMode:
        new pipeline (seed → IKeywordProvider keyword ideas → SERP clusters → enrich) — see [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md)
```

Both modes produce the same `TopicalMapResult` shape. New fields are additive (nullable on `TopicalMapTopic`).

---

## New Data Model Fields

All additive to `TopicalMapModels.cs`. No breaking changes.

```csharp
// Upgrade to TopicalMapTopic
TopicalMapTopic {
    // EXISTING (unchanged)
    Name, Queries, Coverage, MatchedDocumentId, MatchedDocumentTitle, MatchedPageUrl,
    TotalImpressions, MainKeyword, PillarName, SearchVolume, KeywordDifficulty, Intent,
    AveragePosition, PriorityScore, ClusterMethod, CompetitorDomains,

    // NEW — Sprint 1: hierarchy
    Tier: TopicalTier,           // Pillar | Cluster | Article
    PillarId: string?,           // links cluster/article back to parent pillar
    ParentClusterId: string?,    // links article to parent cluster

    // NEW — Sprint 2: entity gaps
    EntityGaps: string[],        // semantic concepts missing from existing content
    EntityCoverage: float,       // 0.0–1.0 entity coverage score

    // NEW — Sprint 3: internal linking
    LinkFrom: string[],          // topic IDs that should link TO this topic
    LinkTo: string[],            // topic IDs this topic should link to
    ContentSequence: int?,       // recommended publish order (1 = first)

    // NEW — Sprint 4: export metadata
    SuggestedWordCount: int?,
    SuggestedTitle: string?,
    SuggestedSlug: string?,
    ContentType: string?,        // "pillar" | "cluster" | "faq" | "listicle" | "comparison"
}

enum TopicalTier { Pillar, Cluster, Article }

// New top-level fields on TopicalMapResult
TopicalMapResult {
    // EXISTING (unchanged)
    ...

    // NEW
    Mode: string,                // "gsc" | "seed"
    SeedKeyword: string?,        // populated in seed mode
    PillarCount: int,
    ClusterCount: int,
    ArticleCount: int,
    EntityGapCount: int,
    InternalLinkingBlueprint: InternalLinkingBlueprint?,
}

InternalLinkingBlueprint {
    Sequences: ContentSequenceItem[],  // ordered publish plan
    LinkGraph: LinkGraphEdge[],        // source → target link recommendations
}

ContentSequenceItem { Order, TopicId, TopicName, Tier, Reason }
LinkGraphEdge { SourceTopicId, TargetTopicId, AnchorText, Priority }
```

### New EF Core entity (GeekSeo.Persistence)
```
SeoTopicalMapSeedCache: Id, ProjectId, SeedKeyword, Location, ResultJson, GeneratedAt, ExpiresAt
```
Migration: `AddTopicalMapSeedCache`

---

## Sprint 1 — Seed Keyword Mode + 3-Tier Hierarchy (Weeks 1–2)

**Goal:** Match TopicalMap.ai and SearchAtlas. Generate a full map from a seed keyword alone (no GSC required). Organize output into Pillar → Cluster → Article tiers.

### Input change

New `GenerateTopicalMapRequest` field:
```csharp
TopicalMapRequest {
    ProjectId: Guid,
    Mode: "gsc" | "seed",       // NEW
    SeedKeyword: string?,        // required if Mode == "seed"
    Location: string,
    MaxTopics: int = 100,        // NEW — caps output size
}
```

### Seed mode pipeline (new, in `TopicalMapService`)

```
1. IKeywordProvider.GetKeywordSuggestionsAsync(seedKeyword, location, limit: 200)
   → KeywordResult[] (volume, difficulty, CPC, monthly trends)

2. IKeywordLabsProvider.GetKeywordIdeasAsync(seedKeyword, location, limit: 300)
   → broader semantic expansion (`IKeywordDiscoveryProvider` per [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) — stub if not yet built)

3. TopicClusteringService.ClusterKeywordList(keywords)
   → existing algorithm, produces flat clusters

4. [NEW] TopicalHierarchyBuilder.AssignTiers(clusters)
   → classifies each cluster as Pillar | Cluster | Article
   → rules:
       - Pillar: SearchVolume > 1000, broad (1–2 word keyword)
       - Cluster: SearchVolume 100–1000, medium-tail (2–3 words)
       - Article: SearchVolume < 100 or long-tail (4+ words)
   → assigns PillarId to clusters/articles

5. For top 10 clusters by search volume:
   ISerpProvider.GetSerpResultsAsync(clusterKeyword)  // impl: SerpApi or GeekSerp per SEO-PROVIDER-STRATEGY
   → extract competitor domains for each pillar

6. Enrich with existing KeywordResearchService metrics

7. Cache to SeoTopicalMapSeedCache (14-day TTL, same as GSC mode)
```

### GSC mode change

Add `TopicalHierarchyBuilder.AssignTiers()` step after existing clustering — upgrades existing GSC output to 3-tier without changing clustering logic.

### New service
`TopicalHierarchyBuilder` in `GeekSeo.Application/Services/Seo/`  
Single responsibility: takes `TopicalMapTopic[]`, assigns `Tier`, `PillarId`, `ParentClusterId`.

### New endpoints (extend `TopicalMapController`)
- `POST /api/seo/topical-map/{projectId}/generate` — add `mode` + `seedKeyword` to request body (backward compatible: `mode` defaults to `"gsc"`)
- `GET /api/seo/topical-map/{projectId}/seed-cache` — list past seed-mode maps for project

### Frontend (`/app/strategy/topical-map`)
- Add input panel: toggle "From GSC Data" | "From Seed Keyword"
- Seed mode: text field for seed keyword + location selector
- Visualization upgrade: group nodes by tier with visual hierarchy (Pillar = large hub node, Cluster = medium, Article = small spoke)
- Color-coded node rings: Pillar = blue, Cluster = purple, Article = grey
- Existing coverage colors (green/yellow/orange/red) stay on node fill

### Tests
- `TopicalHierarchyBuilderTests.cs` — tier assignment rules, pillar linking
- `TopicalMapServiceTests.cs` — add seed mode path (mock `IKeywordProvider`)

### Verification
`POST /api/seo/topical-map/{projectId}/generate` with `{ mode: "seed", seedKeyword: "content marketing" }` returns `TopicalMapResult` with `PillarCount > 0`, `ClusterCount > 0`, `ArticleCount > 0`.

---

## Sprint 2 — Entity Gap Detection (Weeks 3–4)

**Goal:** Match Agility Writer's explicit entity gap analysis. Identify semantic concepts missing from existing content vs. what top-ranking competitors cover.

### What "entity gaps" means
A topic that appears in top-10 SERP results for the seed/pillar keyword but has no matching document or GSC ranking in the project. These are semantic blind spots — concepts Google expects a topical authority to cover.

### Implementation

New service: `EntityGapAnalyzer` in `GeekSeo.Application/Services/Seo/`

```
Input: TopicalMapTopic[] (with CompetitorDomains populated), ProjectId

Algorithm:
1. For each Pillar topic, collect competitor SERP URLs (already in CompetitorDomains)
2. Extract entity terms from competitor page titles + h2s via SerpOrganicResult.Title + Snippet
3. Tokenize → n-gram extraction (1–3 word phrases, stopword-filtered)
4. Compare entity set against project's existing TopicalMapTopic.Queries[] and document titles
5. Phrases present in ≥3 competitor results but absent from project content → EntityGap

Output: per-topic EntityGaps: string[] + EntityCoverage: float
```

**Uses existing data** — SERP results already fetched during topical map generation. No new API calls for most cases. For seed mode with fresh topics, uses cached SERP results from `SeoSerpDeepCache`.

**Claude enrichment (optional, gated on `AI_ENTITY_ANALYSIS=true`):**  
For the top 5 pillar topics, call Claude API with competitor titles + project content summary → Claude identifies conceptual gaps beyond keyword matching (e.g., "you cover 'link building' but not 'digital PR' which is how competitors frame the same concept").

### Model updates
Add `EntityGaps: string[]` and `EntityCoverage: float` to `TopicalMapTopic`.

### New endpoint addition
`GET /api/seo/topical-map/{projectId}/entity-gaps` — returns topics sorted by lowest `EntityCoverage` score. Quick wins view.

### Frontend
- New "Entity Gaps" tab on topical map workspace
- Table: Topic | Coverage Score | Missing Entities | Competitor Count
- Clicking an entity gap → side panel shows which competitor pages mention it
- Gap score badge on each map node (red dot if EntityCoverage < 0.5)

### Tests
`EntityGapAnalyzerTests.cs` — entity extraction, gap detection, coverage calculation

### Verification
After generating a map, `GET /api/seo/topical-map/{projectId}/entity-gaps` returns topics with `entityGaps` populated and `entityCoverage` between 0.0–1.0.

---

## Sprint 3 — Internal Linking Blueprint (Weeks 5–6)

**Goal:** Match Agility Writer's internal linking strategy. Generate a content sequence (what to publish first) and a link graph (what should link to what).

### What best-in-class produces
- **Content sequence:** Publish pillars first → clusters second → articles last. Within each tier, publish highest-volume / lowest-difficulty first.
- **Link graph:** Each article links up to its cluster. Each cluster links up to its pillar. Cross-cluster links where entity overlap exists. Pillar links out to all child clusters.

### Implementation

New service: `InternalLinkingBlueprintBuilder` in `GeekSeo.Application/Services/Seo/`

```
Input: TopicalMapResult (with tiers assigned, entity gaps populated)

Algorithm:

1. ContentSequence:
   - Tier order: Pillar first, then Cluster, then Article
   - Within tier: sort by (SearchVolume DESC, KeywordDifficulty ASC)
   - Assign ContentSequence = 1..N
   - Generate Reason string: "Pillar — publish first to establish topical authority"
                              "High-volume cluster (2,400/mo) — foundational sub-topic"
                              "Long-tail article — publish after parent cluster is indexed"

2. LinkGraph:
   - For each Article → add edge to parent Cluster (anchor = Article.MainKeyword)
   - For each Cluster → add edge to parent Pillar (anchor = Cluster.MainKeyword)
   - Cross-links: for each pair of clusters sharing ≥2 entity terms in EntityGaps
                  → add bidirectional edge (anchor = shared entity term)
   - Pillar → add edges to all child Clusters (anchor = Cluster.Name)

3. Assign LinkFrom[] and LinkTo[] back onto each TopicalMapTopic

Output: InternalLinkingBlueprint { Sequences[], LinkGraph[] }
```

**Anchor text generation (Claude API):**  
For the top 20 link edges by priority, call Claude API to suggest natural anchor text variations. Single Claude call with structured tool output → list of `{ sourceTopicId, targetTopicId, anchorVariants: string[] }`.

### Model updates
Add `LinkFrom`, `LinkTo`, `ContentSequence` to `TopicalMapTopic`.  
Add `InternalLinkingBlueprint` to `TopicalMapResult`.

### New endpoint
`GET /api/seo/topical-map/{projectId}/linking-blueprint` — returns `InternalLinkingBlueprint` from cached map.

### Frontend
- New "Internal Links" tab on topical map workspace
- Two sub-views:
  1. **Publish Order** — numbered list with tier badges and reason text. "Step 1: Publish [Content Marketing Guide] — Pillar, 12,000 searches/mo"
  2. **Link Graph** — @xyflow/react directed graph (already in project), edges labeled with anchor text. Filter by tier. Export as CSV.
- On node click → sidebar shows "links from" and "links to" lists

### Tests
`InternalLinkingBlueprintBuilderTests.cs` — sequence ordering, link graph edges, cross-cluster detection

### Verification
`GET /api/seo/topical-map/{projectId}/linking-blueprint` returns `sequences` ordered by tier then volume, and `linkGraph` edges where every Article links to its parent Cluster.

---

## Sprint 4 — Export + Content Metadata (Weeks 7–8)

**Goal:** Match TopicalMap.ai and SearchAtlas export quality. CSV, PDF, and JSON export. Add `SuggestedTitle`, `SuggestedSlug`, `SuggestedWordCount`, `ContentType` per topic.

### Content metadata generation

**In `TopicalHierarchyBuilder` (Sprint 1 service, extended):**

```
For each topic after tier assignment:

SuggestedWordCount:
  - Pillar: 3000–5000 words (based on competitor avg from SerpBenchmarkCalculator — already exists)
  - Cluster: 1500–2500 words
  - Article: 800–1500 words
  - If SerpBenchmarkCalculator data available: use actual competitor avg

ContentType:
  - Pillar + informational intent → "pillar-guide"
  - Cluster + commercial intent → "comparison"
  - Cluster + informational + question keyword → "faq"
  - Article + list-type query (best/top/X ways) → "listicle"
  - Article + vs/versus in keyword → "comparison"
  - Default → "article"

SuggestedTitle + SuggestedSlug:
  - Claude API batch call: send top 30 topics as JSON,
    return { topicId, title, slug } for each
  - Single call with structured output (not 30 separate calls)
  - Cache on TopicalMapTopic — not regenerated on re-fetch
```

### Export service

New `TopicalMapExportService` in `GeekSeo.Application/Services/Seo/`:

```csharp
Task<byte[]> ExportCsvAsync(TopicalMapResult map)
Task<byte[]> ExportExcelAsync(TopicalMapResult map)   // OpenXml
Task<string> ExportJsonAsync(TopicalMapResult map)
Task<byte[]> ExportPdfAsync(TopicalMapResult map)     // PdfPig or QuestPDF
```

**CSV columns (matches SearchAtlas + TopicalMap.ai standard):**
```
Tier | Pillar | Cluster | Article Title | Keyword | Search Volume | Difficulty | Intent |
Coverage | Suggested Word Count | Content Type | Suggested Slug | Sequence Order |
Link From (comma-separated) | Link To (comma-separated) | Entity Gaps (comma-separated)
```

### New endpoints (extend `TopicalMapController`)
- `GET /api/seo/topical-map/{projectId}/export?format=csv|excel|json|pdf`
  - Returns file download (Content-Disposition: attachment)
  - Metered route: `topical_map_export`
  - Feature gate: CSV/JSON → `SubscriptionTier.Starter` | Excel/PDF → `SubscriptionTier.Professional`

### Frontend
- Export button in topical map workspace toolbar (dropdown: CSV | Excel | JSON | PDF)
- Each topic row in list view shows `SuggestedWordCount`, `ContentType` badge, `SuggestedTitle`
- "Content Plan" view: flat table with all export columns — sortable, filterable. Same data as export but interactive.
- "Copy to Clipboard" one-click (for FatJoe/AskOptimo-style quick sharing)

### New `seo-api.ts` function
```typescript
exportTopicalMap(projectId: string, format: 'csv' | 'excel' | 'json' | 'pdf'): Promise<Blob>
```

### Tests
`TopicalMapExportServiceTests.cs` — CSV column count, Excel sheet structure, JSON schema, PDF non-empty  
`TopicalHierarchyBuilderTests.cs` — content type classification, word count ranges

### Verification
`GET /api/seo/topical-map/{projectId}/export?format=csv` returns a downloadable CSV with correct column headers and one row per topic. PDF includes project name, generation date, topic count.

---

## Sprint Dependency Order

```
Sprint 1 (seed mode + hierarchy) ── prerequisite for Sprints 2, 3, 4
    ├─► Sprint 2 (entity gaps)     ── requires tier assignment from S1
    ├─► Sprint 3 (linking)         ── requires tier assignment + entity data from S1+S2
    └─► Sprint 4 (export)          ── requires all metadata from S1+S2+S3
                                      (can start export UI in parallel with S2/S3)
```

---

## Files to Modify

| File | Change |
|------|--------|
| `GeekSeo.Application/Models/Seo/TopicalMapModels.cs` | Add new fields (Tier, EntityGaps, LinkFrom, etc.) |
| `GeekSeo.Application/Services/TopicClusteringService.cs` | No change — used as-is |
| `GeekSeoBackend/Services/TopicalMapService.cs` | Add seed mode pipeline branch |
| `GeekSeoBackend/Controllers/Seo/TopicalMapController.cs` | Add export + entity-gaps + blueprint endpoints |
| `GeekSeoBackend/Program.cs` / `SeoBackendExtensions.cs` | Register new services |
| `GeekSeo.Persistence/` | Add `SeoTopicalMapSeedCache` entity + migration |
| `frontend/src/app/app/strategy/topical-map/page.tsx` | Add seed input, tier visualization, new tabs |
| `frontend/src/lib/seo-api.ts` | Add export function, seed mode params |

### New files

| File | Purpose |
|------|---------|
| `GeekSeo.Application/Services/Seo/TopicalHierarchyBuilder.cs` | Tier assignment + content metadata |
| `GeekSeo.Application/Services/Seo/EntityGapAnalyzer.cs` | Entity gap detection |
| `GeekSeo.Application/Services/Seo/InternalLinkingBlueprintBuilder.cs` | Link graph + sequence |
| `GeekSeo.Application/Services/Seo/TopicalMapExportService.cs` | CSV/Excel/JSON/PDF export |
| `GeekSeoBackend.Tests/TopicalHierarchyBuilderTests.cs` | Unit tests |
| `GeekSeoBackend.Tests/EntityGapAnalyzerTests.cs` | Unit tests |
| `GeekSeoBackend.Tests/InternalLinkingBlueprintBuilderTests.cs` | Unit tests |
| `GeekSeoBackend.Tests/TopicalMapExportServiceTests.cs` | Unit tests |
| `frontend/src/components/strategy/topical-map-export-menu.tsx` | Export dropdown UI |
| `frontend/src/components/strategy/entity-gaps-tab.tsx` | Entity gaps tab |
| `frontend/src/components/strategy/linking-blueprint-tab.tsx` | Link graph tab |
| `frontend/src/components/strategy/content-plan-table.tsx` | Flat content plan table |

---

## Implementer Notes

1. **`SerpResult` shape is frozen** — `TopicalMapTopic` new fields are all nullable. Existing topic list consumers get `null` for new fields and must handle gracefully.

2. **Seed mode does not replace GSC mode** — users with GSC connected get richer data (real impressions, real CTR). Seed mode is for new projects or competitive research without GSC.

3. **Entity gap extraction uses existing SERP cache** — `SeoSerpDeepCache` already stores organic result titles and snippets (7-day TTL). `EntityGapAnalyzer` reads from cache first, no extra vendor SERP calls for cached keywords.

4. **Claude API calls must be batched** — `SuggestedTitle`/`SuggestedSlug` generation uses a single Claude call for up to 30 topics at once (structured JSON output). Never one call per topic.

5. **Internal link graph is advisory** — these are recommendations, not auto-inserted links. The frontend shows them; WordPress integration (if connected) can optionally scan draft posts and flag missing links.

6. **Export PDF dependency** — use QuestPDF (MIT license, no external process, pure .NET) or PdfPig. Do not use headless Chrome/Playwright for PDF generation in this service.

7. **`ContentSequence` is deterministic** — same inputs always produce same sequence order. No randomization. Users can override sequence manually in the frontend (stored in local state, not persisted).

8. **Backward compatibility** — existing `TopicalMapResult` consumers (dashboard, brief generation, content guard) must not break. All new fields nullable with sensible defaults.

---

## Success Metrics (Post-Launch)

| Metric | Target |
|--------|--------|
| Seed mode adoption | ≥40% of topical map generations use seed mode within 30 days |
| Export usage | ≥25% of Professional users export at least once per month |
| Entity gaps surfaced | Average ≥10 entity gaps per pillar topic |
| Linking blueprint generation time | < 5 seconds added to existing map generation time |
| Map generation scale | Seed mode produces ≥150 unique topics per run |
