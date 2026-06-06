# SiteNicheAnalyzer — Implementation Plan

> **North star & fusion architecture:** [`SEARCH-UNDERSTANDING-LAYER.md`](SEARCH-UNDERSTANDING-LAYER.md) — public-signal composite (schema, page, sitemap, nav, links, optional GSC/SERP). This doc is the **first consumer** implementation; merge rules here will migrate into `TopicFusionEngine`.

## Purpose

Given a site URL, automatically identify the core niche, extract validated content
pillars, audit existing coverage, score topical authority, and surface content gaps.
Every downstream module — TopicalMapService, PillarKeywordExpander, ClusterBuilder,
ContentCalendar, RankTracking — depends on this module's structured output.

Zero AI cost. Deterministic pillar extraction. SERP validation optional (requires
DataForSEO). Runs as background job. Stores versioned snapshots for authority
progression tracking over time.

---

## What the Module Does End-to-End

1. Crawl sitemap + homepage via Playwright → extract niche signals
2. Pull GSC data (if connected) → surface owned ranking queries
3. Identify candidate pillars deterministically (schema.org → sitemap → nav → H2)
4. Validate pillars via 3-gate algorithm (scope, intent alignment, relevance)
5. Enrich pillar keywords with volume + difficulty → IKeywordProvider (optional)
6. Validate pillars against SERP → ISerpProvider (optional, costs API units)
7. Extract competitor domains from SERP results (if step 6 ran)
8. Score topical coverage per pillar (0–100) against existing site content
9. Flag quick wins (low KD + clear gap + volume > 100)
10. Persist versioned NicheProfile snapshot to Postgres
11. Return structured NicheProfile consumed by TopicalMapService

Steps 6–7 degrade gracefully to skipped if no SERP provider available.
Steps 5 skips gracefully if no keyword provider available.
Analysis still produces a complete, useful result from steps 1–4 + 8–10 alone.

---

## Output Contract

```csharp
// GeekSeo.Application/Interfaces/Seo/ISiteNicheAnalyzer.cs
public interface ISiteNicheAnalyzer
{
    Task<Guid> EnqueueAnalysisAsync(Guid projectId, string siteUrl,
        string? seedTopic = null, CancellationToken ct = default);

    Task<NicheAnalysisStatus> GetStatusAsync(Guid profileId, CancellationToken ct = default);

    Task<NicheProfileResult?> GetProfileAsync(Guid profileId, CancellationToken ct = default);
}

public record NicheAnalysisStatus(Guid ProfileId, string Status, string? ErrorMessage);
```

---

## Domain Entities (EF Core — Write Path)

```csharp
// GeekSeo.Persistence/Entities/NicheAnalysis/NicheProfile.cs
public class NicheProfile
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string PrimaryNiche { get; set; } = string.Empty;       // "Computer repair and IT support, Boynton Beach"
    public string NicheDescription { get; set; } = string.Empty;   // from meta description
    public string[] NicheTags { get; set; } = [];                  // ["local service","Florida","IT support"]
    public string AudienceType { get; set; } = string.Empty;       // local_service | ecommerce | saas | blog | agency | other
    public string CompetitionLevel { get; set; } = string.Empty;   // low | medium | high | very_high
    public string DiscoveryMethod { get; set; } = string.Empty;    // schema | sitemap | nav | headings | fallback
    public decimal TopicalAuthorityScore { get; set; }             // 0–100 composite
    public int TotalPillarsIdentified { get; set; }
    public int PillarsCovered { get; set; }
    public int PillarsPartial { get; set; }
    public int PillarsGap { get; set; }
    public DateTimeOffset? AnalyzedAt { get; set; }
    public DateTimeOffset? NextAnalysisDue { get; set; }           // auto re-analysis via SeoMaintenanceWorker
    public string AnalysisVersion { get; set; } = "1.0";           // versioned snapshots
    public string Status { get; set; } = "queued";                 // queued | processing | complete | failed
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public SeoProject Project { get; set; } = null!;
    public ICollection<NichePillar> Pillars { get; set; } = [];
    public ICollection<NicheCompetitor> Competitors { get; set; } = [];
    public ICollection<NicheEntity> Entities { get; set; } = [];
}

// GeekSeo.Persistence/Entities/NicheAnalysis/NichePillar.cs
public class NichePillar
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string PillarTopic { get; set; } = string.Empty;        // "Computer Repair"
    public string PillarSlug { get; set; } = string.Empty;         // "computer-repair"
    public string PrimaryKeyword { get; set; } = string.Empty;     // "computer repair boynton beach"
    public string? PageUrl { get; set; }                           // "/services/computer-repair"
    public string SearchIntent { get; set; } = "commercial";       // informational | transactional | commercial | navigational
    public int SearchVolume { get; set; }
    public decimal KeywordDifficulty { get; set; }
    public string CoverageStatus { get; set; } = "gap";            // covered | partial | gap
    public decimal CoverageScore { get; set; }                     // 0–100
    public int ExistingPageCount { get; set; }
    public int RequiredSubtopicCount { get; set; }
    public int CoveredSubtopicCount { get; set; }
    public int Priority { get; set; }
    public string StrategicPriority { get; set; } = "expansion";   // must_have | high_value | expansion
    public string? ContentAngle { get; set; }                      // one-liner content hook
    public decimal EstimatedTrafficPotential { get; set; }
    public string Source { get; set; } = string.Empty;             // schema | sitemap | nav | heading
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public NicheProfile NicheProfile { get; set; } = null!;
    public ICollection<NicheSubtopic> Subtopics { get; set; } = [];
    public ICollection<NichePillarPage> ExistingPages { get; set; } = [];
}

// GeekSeo.Persistence/Entities/NicheAnalysis/NicheSubtopic.cs
public class NicheSubtopic
{
    public Guid Id { get; set; }
    public Guid PillarId { get; set; }
    public string SubtopicTitle { get; set; } = string.Empty;
    public string TargetKeyword { get; set; } = string.Empty;
    public string SearchIntent { get; set; } = "informational";
    public int SearchVolume { get; set; }
    public decimal KeywordDifficulty { get; set; }
    public string CoverageStatus { get; set; } = "gap";            // covered | partial | gap
    public string? ExistingUrl { get; set; }                       // null if gap
    public string RecommendedFormat { get; set; } = "how_to";      // how_to | listicle | comparison | definition | faq | case_study | local_page | tool_review
    public int RecommendedWordCount { get; set; }
    public string FixEffort { get; set; } = "create";              // create | expand | optimize
    public bool IsQuickWin { get; set; }                           // KD < 35 + gap + volume > 100
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public NichePillar Pillar { get; set; } = null!;
}

// GeekSeo.Persistence/Entities/NicheAnalysis/NicheCompetitor.cs
public class NicheCompetitor
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public int SerpPresence { get; set; }                          // # of target SERPs this domain appears in
    public decimal EstimatedAuthorityScore { get; set; }
    public int PillarsRanking { get; set; }
    public string StrengthAssessment { get; set; } = "moderate";   // weak | moderate | strong | dominant

    public NicheProfile NicheProfile { get; set; } = null!;
}

// GeekSeo.Persistence/Entities/NicheAnalysis/NicheEntity.cs
public class NicheEntity
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string EntityName { get; set; } = string.Empty;         // "Google Search Console"
    public string EntityType { get; set; } = string.Empty;         // person | organization | concept | tool | location | standard | event
    public int MentionFrequency { get; set; }
    public bool PresentOnDomain { get; set; }
    public Guid[] AssociatedPillarIds { get; set; } = [];

    public NicheProfile NicheProfile { get; set; } = null!;
}

// GeekSeo.Persistence/Entities/NicheAnalysis/NichePillarPage.cs
public class NichePillarPage
{
    public Guid Id { get; set; }
    public Guid PillarId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? PageTitle { get; set; }
    public int WordCount { get; set; }
    public string CoverageQuality { get; set; } = "thin";          // thin | adequate | strong
    public decimal RelevanceScore { get; set; }                    // 0–100
    public string[] TopicsFound { get; set; } = [];
    public string[] GapsFound { get; set; } = [];

    public NichePillar Pillar { get; set; } = null!;
}
```

---

## Database Schema (Postgres / Supabase — geek_seo schema)

```sql
CREATE TABLE geek_seo.niche_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    domain TEXT NOT NULL,
    primary_niche TEXT,
    niche_description TEXT,
    niche_tags TEXT[],
    audience_type TEXT CHECK (audience_type IN ('local_service','ecommerce','saas','blog','agency','other')),
    competition_level TEXT CHECK (competition_level IN ('low','medium','high','very_high')),
    discovery_method TEXT NOT NULL DEFAULT 'fallback',
    topical_authority_score DECIMAL(5,2) DEFAULT 0,
    total_pillars_identified INT DEFAULT 0,
    pillars_covered INT DEFAULT 0,
    pillars_partial INT DEFAULT 0,
    pillars_gap INT DEFAULT 0,
    analyzed_at TIMESTAMPTZ,
    next_analysis_due TIMESTAMPTZ,
    analysis_version TEXT DEFAULT '1.0',
    status TEXT NOT NULL DEFAULT 'queued'
        CHECK (status IN ('queued','processing','complete','failed')),
    error_message TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE geek_seo.niche_pillars (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    niche_profile_id UUID NOT NULL REFERENCES geek_seo.niche_profiles(id) ON DELETE CASCADE,
    pillar_topic TEXT NOT NULL,
    pillar_slug TEXT NOT NULL,
    primary_keyword TEXT NOT NULL,
    page_url TEXT,
    search_intent TEXT CHECK (search_intent IN ('informational','transactional','commercial','navigational')),
    search_volume INT DEFAULT 0,
    keyword_difficulty DECIMAL(5,2) DEFAULT 0,
    coverage_status TEXT NOT NULL DEFAULT 'gap'
        CHECK (coverage_status IN ('covered','partial','gap')),
    coverage_score DECIMAL(5,2) DEFAULT 0,
    existing_page_count INT DEFAULT 0,
    required_subtopic_count INT DEFAULT 0,
    covered_subtopic_count INT DEFAULT 0,
    priority INT,
    strategic_priority TEXT CHECK (strategic_priority IN ('must_have','high_value','expansion')),
    content_angle TEXT,
    estimated_traffic_potential DECIMAL(10,2) DEFAULT 0,
    source TEXT NOT NULL DEFAULT 'sitemap',
    display_order INT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE geek_seo.niche_subtopics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pillar_id UUID NOT NULL REFERENCES geek_seo.niche_pillars(id) ON DELETE CASCADE,
    subtopic_title TEXT NOT NULL,
    target_keyword TEXT NOT NULL,
    search_intent TEXT,
    search_volume INT DEFAULT 0,
    keyword_difficulty DECIMAL(5,2) DEFAULT 0,
    coverage_status TEXT NOT NULL DEFAULT 'gap'
        CHECK (coverage_status IN ('covered','partial','gap')),
    existing_url TEXT,
    recommended_format TEXT CHECK (recommended_format IN
        ('how_to','listicle','comparison','definition','faq','case_study','local_page','tool_review')),
    recommended_word_count INT,
    fix_effort TEXT CHECK (fix_effort IN ('create','expand','optimize')),
    is_quick_win BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE geek_seo.niche_competitors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    niche_profile_id UUID NOT NULL REFERENCES geek_seo.niche_profiles(id) ON DELETE CASCADE,
    domain TEXT NOT NULL,
    serp_presence INT DEFAULT 0,
    estimated_authority_score DECIMAL(5,2) DEFAULT 0,
    pillars_ranking INT DEFAULT 0,
    strength_assessment TEXT CHECK (strength_assessment IN ('weak','moderate','strong','dominant')),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE geek_seo.niche_entities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    niche_profile_id UUID NOT NULL REFERENCES geek_seo.niche_profiles(id) ON DELETE CASCADE,
    entity_name TEXT NOT NULL,
    entity_type TEXT CHECK (entity_type IN
        ('person','organization','concept','tool','location','standard','event')),
    mention_frequency INT DEFAULT 0,
    present_on_domain BOOLEAN DEFAULT FALSE,
    associated_pillar_ids UUID[],
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE geek_seo.niche_pillar_pages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pillar_id UUID NOT NULL REFERENCES geek_seo.niche_pillars(id) ON DELETE CASCADE,
    url TEXT NOT NULL,
    page_title TEXT,
    word_count INT DEFAULT 0,
    coverage_quality TEXT CHECK (coverage_quality IN ('thin','adequate','strong')),
    relevance_score DECIMAL(5,2) DEFAULT 0,
    topics_found TEXT[],
    gaps_found TEXT[],
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_niche_profiles_project_id  ON geek_seo.niche_profiles(project_id);
CREATE INDEX idx_niche_profiles_status      ON geek_seo.niche_profiles(status);
CREATE INDEX idx_niche_profiles_domain      ON geek_seo.niche_profiles(domain);
CREATE INDEX idx_niche_pillars_profile_id   ON geek_seo.niche_pillars(niche_profile_id);
CREATE INDEX idx_niche_pillars_coverage     ON geek_seo.niche_pillars(coverage_status);
CREATE INDEX idx_niche_subtopics_pillar_id  ON geek_seo.niche_subtopics(pillar_id);
CREATE INDEX idx_niche_subtopics_quick_wins ON geek_seo.niche_subtopics(is_quick_win)
    WHERE is_quick_win = TRUE;
CREATE INDEX idx_niche_competitors_profile  ON geek_seo.niche_competitors(niche_profile_id);
CREATE INDEX idx_niche_entities_profile     ON geek_seo.niche_entities(niche_profile_id);
CREATE INDEX idx_niche_pillar_pages_pillar  ON geek_seo.niche_pillar_pages(pillar_id);
```

---

## Pillar Extraction — Deterministic, No AI

Runs sources in priority order. All sources attempted; results merged.

### Source 1 — Schema.org JSON-LD (highest trust)

```
Find all <script type="application/ld+json"> on homepage
Parse each block (skip if malformed)
Target @type: LocalBusiness, Service, ProfessionalService,
             Organization, any business subtype

Extract service names from:
  hasOfferCatalog.itemListElement[].itemOffered.name
  hasOfferCatalog.itemListElement[].name
  makesOffer[].itemOffered.name
  serviceType (array or string)

Extract niche context from:
  description → NicheDescription
  name → brand (strip LLC/Inc/Co)
  areaServed → location modifiers

Each service name → NichePillar { Source="schema", Intent="commercial" }
```

### Source 2 — Sitemap.xml

```
Discovery order:
  GET /sitemap.xml
  GET /sitemap_index.xml
  Parse robots.txt → "Sitemap:" directive
  Fallback: try /sitemap-{pages,services,posts}.xml

Sitemap index: fetch first 3 child sitemaps only (pages, services, locations priority)
Cap total URLs at 5,000

Noise path filter — strip these, never become pillars:
  /about, /about-us, /contact, /privacy, /terms, /legal
  /cart, /checkout, /account, /login, /register
  /wp-admin, /wp-content, /wp-includes, /wp-login, /feed
  /tag/*, /author/*, /page/*, /search, /404

Group remaining URLs by first path segment:
  /services/computer-repair → segment "services", child "computer-repair"
  /locations/boynton-beach  → segment "locations", child "boynton-beach"

Assign intent:
  /services/*, /solutions/*, /products/* → "commercial"
  /locations/*, /areas/*                 → "local"
  /blog/*, /resources/*, /guides/*       → "informational"
  default                                → "commercial"

Each path group with ≥ 1 child → NichePillar candidate
Nav items used as child slugs when pillar is a dropdown parent
```

### Source 3 — Nav Menu (Playwright)

```
Selectors tried in order (stop on first returning ≥ 2 links):
  nav a
  header nav a
  [role='navigation'] a
  .nav a, .navbar a, .menu a, #menu a

Filter:
  Same-domain or relative href only
  Non-empty text content
  Not in noise path list

Mobile nav fallback:
  If < 2 links on 1440px viewport → try 375px viewport
  If hamburger button found → click, wait 500ms, re-query

Dropdown refinement:
  If nav item has children → use CHILDREN as pillars (not parent)
  "Services" is not a pillar — "Computer Repair" is
```

### Source 4 — H1 + H2 Headings (fallback only)

```
Used only when sources 1–3 yield < 3 pillar candidates

Filter H2 noise:
  "Why Choose Us", "Our Process", "Testimonials", "About Us",
  "Contact Us", "Get a Quote", "FAQ", "Meet the Team"

Remaining H2s → pillar candidates, Intent="commercial"
```

### Source 5 — Title + Meta Description (niche summary only)

```
Not used for pillar extraction.
title tag → split on | or - → longest segment → RootEntity candidate
meta description → NicheDescription
Combined with top pillar names → PrimaryNiche string
```

---

## Pillar Validation — 3 Gates

Applied to ALL candidates after merging all sources.

**Gate 1 — Semantic Scope**
Pillar must support ≥ 5 sub-topics.
Pass: ≥ 3 child URLs in sitemap, OR ≥ 2 nav dropdown children, OR generic service term
Fail action: demote to sub-topic, merge under nearest passing pillar

**Gate 2 — Search Intent Alignment**
Two pillars must be semantically distinct.
Measure: Jaccard similarity on slug tokens + static synonym map
Threshold: similarity > 0.6 → merge, keep higher-source-priority name

Static synonym pairs:
```
repair ↔ fix ↔ service ↔ maintenance
removal ↔ remove ↔ elimination ↔ clean
setup ↔ install ↔ installation ↔ configure
support ↔ help ↔ assistance
recovery ↔ restore ↔ backup
```

**Gate 3 — Commercial/Topical Relevance**
Pass patterns: service verbs, product categories, location names, industry terms
Fail patterns: about, contact, privacy, testimonials, sitemap, 404, news (unless content strategy)
Fail action: remove entirely

---

## Merge & Sort

```
Priority: schema > sitemap > nav > heading

1. Start with schema pillars
2. Sitemap pillars: match slug → enrich existing / add new
3. Nav pillars: match slug → enrich with childSlugs / add new
4. Heading pillars: add only if total count < 3
5. Run all 3 gates
6. Sort: schema first, then by ChildPageCount descending
7. Cap at 7 pillars
8. Minimum 3 required → if fewer, add location pillars from areaServed
```

---

## Coverage Scoring Engine (NicheAuthorityScorer)

Pure function class. No external dependencies. Fully unit testable.

```
Pillar Coverage Score (0–100):
  (CoveredSubtopics / RequiredSubtopics) × 60   → content breadth
  AverageRelevanceScore of existing pages × 25   → content quality
  EntityCoverage (entities present / total) × 15 → semantic completeness

Topical Authority Score (0–100):
  WeightedAverage of all PillarCoverageScores
  Weights: must_have = 3×, high_value = 2×, expansion = 1×
  Penalty: -5 per pillar with zero coverage (hard gap)
  Bonus: +3 per pillar with CoverageScore > 80

QuickWin flag:
  KeywordDifficulty < 35 AND CoverageStatus = 'gap' AND SearchVolume > 100

CoverageQuality on NichePillarPage:
  RelevanceScore < 40 → "thin"
  RelevanceScore 40–70 → "adequate"
  RelevanceScore > 70 → "strong"
```

---

## Dapper Read Models

```csharp
// GeekSeo.Application/ReadModels/NicheAnalysis/

public record NicheProfileSummary(
    Guid Id, string Domain, string PrimaryNiche,
    decimal TopicalAuthorityScore, int TotalPillars,
    int PillarsCovered, int PillarsGap,
    string CompetitionLevel, DateTimeOffset? AnalyzedAt, string Status
);

public record PillarCoverageMatrix(
    Guid PillarId, string PillarTopic, string PrimaryKeyword,
    int SearchVolume, decimal KeywordDifficulty, decimal CoverageScore,
    int CoveredSubtopics, int TotalSubtopics, int GapSubtopics,
    string CoverageStatus, string StrategicPriority, bool HasQuickWins
);

public record TopicalGapSummary(
    Guid SubtopicId, string PillarTopic, string SubtopicTitle,
    string TargetKeyword, int SearchVolume, decimal KeywordDifficulty,
    bool IsQuickWin, string RecommendedFormat, string FixEffort
);

public record AuthorityProgressPoint(
    DateTimeOffset SnapshotDate, decimal TopicalAuthorityScore,
    int PillarsCovered, int TotalSubtopicsCovered, int TotalGaps
);

public record CompetitorNicheOverlap(
    string CompetitorDomain, int SharedPillarCount,
    int CompetitorOnlyPillarCount, int OurOnlyPillarCount,
    decimal EstimatedAuthorityScore
);

public record EntityCoverageReport(
    string EntityName, string EntityType,
    int MentionFrequency, bool PresentOnDomain, int AssociatedPillarCount
);
```

### Key Dapper Query — Coverage Matrix

```sql
SELECT
    p.id                                                        AS PillarId,
    p.pillar_topic                                              AS PillarTopic,
    p.primary_keyword                                           AS PrimaryKeyword,
    p.search_volume                                             AS SearchVolume,
    p.keyword_difficulty                                        AS KeywordDifficulty,
    p.coverage_score                                            AS CoverageScore,
    p.covered_subtopic_count                                    AS CoveredSubtopics,
    p.required_subtopic_count                                   AS TotalSubtopics,
    (p.required_subtopic_count - p.covered_subtopic_count)      AS GapSubtopics,
    p.coverage_status                                           AS CoverageStatus,
    p.strategic_priority                                        AS StrategicPriority,
    EXISTS (
        SELECT 1 FROM geek_seo.niche_subtopics s
        WHERE s.pillar_id = p.id AND s.is_quick_win = TRUE
    )                                                           AS HasQuickWins
FROM geek_seo.niche_pillars p
WHERE p.niche_profile_id = @ProfileId
ORDER BY p.display_order ASC;
```

### Key Dapper Query — Authority Progress (time-series)

```sql
SELECT
    analyzed_at             AS SnapshotDate,
    topical_authority_score AS TopicalAuthorityScore,
    pillars_covered         AS PillarsCovered,
    (SELECT COUNT(*) FROM geek_seo.niche_subtopics s
     JOIN geek_seo.niche_pillars p ON s.pillar_id = p.id
     WHERE p.niche_profile_id = np.id
     AND s.coverage_status = 'covered')    AS TotalSubtopicsCovered,
    pillars_gap             AS TotalGaps
FROM geek_seo.niche_profiles np
WHERE project_id = @ProjectId
  AND status = 'complete'
ORDER BY analyzed_at ASC;
```

---

## Repository Interfaces

```csharp
// EF Core — INicheProfileRepository (write path)
public interface INicheProfileRepository
{
    Task<NicheProfile> CreateAsync(NicheProfile profile, CancellationToken ct = default);
    Task<NicheProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NicheProfile?> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null, CancellationToken ct = default);
    Task UpdateScoresAsync(Guid id, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default);
    Task BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default);
    Task BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default);
    Task AddCompetitorAsync(NicheCompetitor competitor, CancellationToken ct = default);
    Task BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default);
    Task BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default);
}

// Dapper — INicheAnalyticsDapperRepository (read path)
public interface INicheAnalyticsDapperRepository
{
    Task<NicheProfileSummary?> GetProfileSummaryAsync(Guid profileId);
    Task<IReadOnlyList<PillarCoverageMatrix>> GetCoverageMatrixAsync(Guid profileId);
    Task<IReadOnlyList<TopicalGapSummary>> GetTopicalGapsAsync(Guid profileId, bool quickWinsOnly = false);
    Task<IReadOnlyList<AuthorityProgressPoint>> GetAuthorityProgressAsync(Guid projectId, int months = 12);
    Task<IReadOnlyList<CompetitorNicheOverlap>> GetCompetitorOverlapAsync(Guid profileId);
    Task<IReadOnlyList<EntityCoverageReport>> GetEntityCoverageAsync(Guid profileId);
    Task<IReadOnlyList<NicheProfileSummary>> GetProfileHistoryAsync(Guid projectId);
}
```

---

## Service Orchestration

```
NicheAnalyzerService.RunAnalysisAsync(projectId, domain, seedTopic?)
  │
  ├── 1. Create NicheProfile { status: "processing" }  ← EF Core
  │
  ├── 2. SchemaOrgExtractor.ExtractAsync(domain)        ← HttpClient
  ├── 3. SitemapExtractor.ExtractAsync(domain)          ← HttpClient
  ├── 4. NavMenuExtractor.ExtractAsync(domain)          ← Playwright (homepage only)
  │
  ├── 5. PillarMerger.Merge(schema, sitemap, nav, headings)
  ├── 6. PillarValidator.Validate(merged)               ← 3-gate, no external calls
  │
  ├── 7. [Optional] IKeywordProvider.GetMetricsAsync(pillarKeywords)
  │       → enrich SearchVolume + KeywordDifficulty per pillar
  │       → skip gracefully if unavailable
  │
  ├── 8. [Optional] ISerpProvider.GetSerpResultsAsync(pillarKeywords)
  │       → extract competitor domains
  │       → skip gracefully if unavailable / no budget
  │
  ├── 9. GSC overlay: IGoogleDataService.GetRankingsAsync()
  │       → map owned queries to pillars → set CoverageStatus signals
  │       → skip gracefully if GSC not connected
  │
  ├── 10. ICrawlerProvider.CrawlSiteAsync(domain, depth:2, maxPages:50)
  │        → map crawled pages to pillars → NichePillarPage records
  │        → score RelevanceScore, CoverageQuality per page
  │
  ├── 11. NicheAuthorityScorer.Score(pillars, pages, entities)
  │        → CoverageScore per pillar
  │        → TopicalAuthorityScore composite
  │        → IsQuickWin flag per subtopic
  │
  ├── 12. NicheRootEntityBuilder.Build(pillars, titleMeta)
  │        → PrimaryNiche string (no AI)
  │
  ├── 13. Persist all entities via INicheProfileRepository  ← EF Core
  │
  └── 14. UpdateStatusAsync(id, "complete")
           NextAnalysisDue = UtcNow + 30 days
```

---

## API Endpoints

All under `/api/seo/niche-analyzer` on GeekSeoBackend.

```
POST   /api/seo/niche-analyzer/analyze
       Body: { projectId, domain, seedTopic? }
       Returns: { profileId, status: "queued" }
       Triggers background analysis

GET    /api/seo/niche-analyzer/{profileId}/status
       Returns: { status, errorMessage? }
       Frontend polls every 3s until status = "complete"

GET    /api/seo/niche-analyzer/{profileId}
       Returns: full NicheProfile + pillars + subtopics + competitors + entities

GET    /api/seo/niche-analyzer/{profileId}/coverage-matrix
       Returns: PillarCoverageMatrix[]   ← Dapper

GET    /api/seo/niche-analyzer/{profileId}/gaps?quickWinsOnly=true|false
       Returns: TopicalGapSummary[]      ← Dapper

GET    /api/seo/niche-analyzer/{profileId}/competitors
       Returns: CompetitorNicheOverlap[] ← Dapper

GET    /api/seo/niche-analyzer/{profileId}/entities
       Returns: EntityCoverageReport[]   ← Dapper

GET    /api/seo/niche-analyzer/project/{projectId}/history
       Returns: NicheProfileSummary[]    ← all snapshots

GET    /api/seo/niche-analyzer/project/{projectId}/progress
       Returns: AuthorityProgressPoint[] ← time-series ← Dapper

DELETE /api/seo/niche-analyzer/{profileId}
       Soft delete / archive snapshot
```

---

## Background Job Pattern

`POST /analyze` creates the NicheProfile record (status: queued) and returns
`profileId` immediately. The actual analysis runs in `NicheAnalysisBackgroundJob`,
queued via the existing `SeoMaintenanceWorker` channel pattern.

### Real-Time Status via SignalR (preferred over polling)

The existing `/hubs/seo-scoring` SignalR hub is extended with an
`AnalysisProgress` event. The background job emits progress at each step:

```csharp
// NicheAnalysisBackgroundJob.cs — push at each extraction step
await _hub.Clients.User(userId).SendAsync("AnalysisProgress", new
{
    ProfileId = profileId,
    Step = "schema",          // schema | sitemap | nav | validating | scoring | complete | failed
    StepNumber = 1,
    TotalSteps = 10,
    Message = "Extracting schema.org data…",
    Status = "processing"
});
```

Steps emitted: `queued → schema → sitemap → nav → merging → validating →
keywords → serp → scoring → complete` (or `failed` with errorMessage).

Frontend subscribes to the hub on mount:
```typescript
// AnalysisStatusListener.tsx
connection.on('AnalysisProgress', (msg) => {
  setProgress(msg);
  if (msg.status === 'complete') loadFullProfile(msg.profileId);
  if (msg.status === 'failed')   setError(msg.errorMessage);
});
```

`GET /{profileId}/status` endpoint **kept** as fallback — used for:
- Initial page load (check if job already finished while navigating)
- Reconnect recovery if SignalR drops

Frontend flow:
1. `POST /analyze` → get `profileId`
2. Subscribe to hub → listen for `AnalysisProgress`
3. On mount: call `GET /{profileId}/status` → if already complete, skip hub wait
4. Hub fires step-by-step → progress bar updates in real time
5. `complete` → fetch full profile; close hub subscription

Monthly auto re-analysis: `SeoMaintenanceWorker` checks `next_analysis_due <= NOW()`
for all complete profiles. Enqueues re-analysis. Powers the authority progression chart.

---

## Implementation Files

```
GeekSeo.Application/
  Interfaces/Seo/
    ISiteNicheAnalyzer.cs
    INicheProfileRepository.cs
    INicheAnalyticsDapperRepository.cs
  Models/Seo/
    NicheAnalysisStatus.cs
    NicheProfileResult.cs
  ReadModels/NicheAnalysis/
    NicheProfileSummary.cs
    PillarCoverageMatrix.cs
    TopicalGapSummary.cs
    AuthorityProgressPoint.cs
    CompetitorNicheOverlap.cs
    EntityCoverageReport.cs

GeekSeo.Persistence/
  Entities/NicheAnalysis/
    NicheProfile.cs
    NichePillar.cs
    NicheSubtopic.cs
    NicheCompetitor.cs
    NicheEntity.cs
    NichePillarPage.cs
  Migrations/
    AddNicheAnalysisTables.cs       ← EF Core migration
  Repositories/
    NicheProfileRepository.cs       ← EF Core writes
    NicheAnalyticsDapperRepository.cs ← Dapper reads

GeekSeoBackend/
  Controllers/Seo/
    NicheAnalyzerController.cs
  Services/Seo/
    NicheAnalyzerService.cs         ← orchestrator
    NicheAuthorityScorer.cs         ← pure scoring, no deps
    NicheRootEntityBuilder.cs       ← PrimaryNiche string assembly
  Services/Seo/NicheExtraction/
    SchemaOrgExtractor.cs
    SitemapExtractor.cs
    NavMenuExtractor.cs             ← Playwright
    PillarValidator.cs              ← 3-gate validation
    PillarMerger.cs                 ← merge + deduplicate
    NoisePaths.cs                   ← static filter list
    PillarSynonymMap.cs             ← static synonym pairs
  Jobs/
    NicheAnalysisBackgroundJob.cs   ← queued via SeoMaintenanceWorker

Frontend/
  src/app/app/niche-analyzer/
    page.tsx                         ← project list + Run Analysis CTA
    [profileId]/page.tsx             ← full analysis view
    [profileId]/gaps/page.tsx        ← gap explorer
    [profileId]/competitors/page.tsx
  src/components/niche-analyzer/
    AnalyzeDomainForm.tsx
    AnalysisStatusListener.tsx       ← SignalR hub subscriber, step progress bar; GET /status fallback on mount/reconnect
    NicheHeader.tsx                  ← PrimaryNiche + authority gauge
    CoverageMatrixTable.tsx          ← shadcn Table, pillar rows
    TopicalGapsPanel.tsx             ← quick wins toggle, gap table
    EntityCoverageGrid.tsx
    CompetitorLandscape.tsx
    AuthorityProgressChart.tsx       ← Recharts line, historical snapshots
```

---

## EF Core vs Dapper Decision Boundary

| Operation | Layer | Why |
|-----------|-------|-----|
| Create NicheProfile | EF Core | Domain object, relationships, cascades |
| Bulk insert pillars + subtopics | EF Core BulkInsert | Transactional integrity |
| Update status / scores | EF Core | Simple tracked entity update |
| Coverage matrix with gap counts | Dapper | Multi-table aggregation |
| Authority progress time-series | Dapper | Date-series, no LINQ equivalent |
| Entity coverage with pillar join | Dapper | Array unnesting + join |
| Competitor overlap comparison | Dapper | Cross-entity aggregation |

---

## Integration with TopicalMapService

```csharp
// TopicalMapService.GenerateAsync — after EnsureProjectAsync
var latestProfile = await _nicheProfileRepository.GetLatestByProjectAsync(projectId, ct);

if (latestProfile is { Status: "complete" })
{
    // Use detected pillars to seed topical map generation
    // Pass to PillarKeywordExpander (Phase 2)
}
else
{
    // No analysis exists or still running → fall back to GSC-only clustering
    logger.LogInformation("No niche profile for {ProjectId}, using GSC-only mode", projectId);
}
```

---

## Open Questions — Resolved

| # | Question | Decision |
|---|----------|----------|
| 1 | New snapshot vs overwrite? | Always new snapshot. Authority tracking is the product. |
| 2 | Warn if GSC not connected? | Yes — banner warning, do not block analysis. |
| 3 | Crawl depth? | Depth 2, max 50 pages. Configurable per project in v2. |
| 4 | Pillar count ceiling? | Fixed 3–7 for MVP. User override in v2. |
| 5 | Auto re-analysis? | Monthly via SeoMaintenanceWorker. `next_analysis_due` field on niche_profiles. |

---

## Build Order

| # | Component | Effort | Notes |
|---|-----------|--------|-------|
| 1 | DB migration: all 6 niche tables | Small | GeekRepository EF migration |
| 2 | EF Core entities + DbContext registration | Small | |
| 3 | Dapper read models + NicheAnalyticsDapperRepository | Small | |
| 4 | NoisePaths + PillarSynonymMap (static data) | Small | |
| 5 | SchemaOrgExtractor | Small | HttpClient, JSON parse |
| 6 | SitemapExtractor | Small | HttpClient, XML parse |
| 7 | NavMenuExtractor | Medium | Playwright, mobile fallback |
| 8 | PillarValidator (3 gates) | Small | Pure logic |
| 9 | PillarMerger | Small | Pure logic |
| 10 | NicheRootEntityBuilder | Small | String assembly |
| 11 | NicheAuthorityScorer | Small | Pure math |
| 12 | NicheAnalyzerService orchestrator | Medium | Wires all extractors |
| 13 | NicheProfileRepository (EF Core) | Small | |
| 14 | NicheAnalysisBackgroundJob | Small | Queue pattern already exists |
| 15 | NicheAnalyzerController (9 endpoints) | Medium | |
| 16 | DI registration | Small | |
| 17 | Unit tests | Medium | Scorer + Validator + Merger |
| 18 | Integration test vs geekatyourspot.com | Small | |
| 19 | Wire into TopicalMapService.GenerateAsync | Small | |
| 20 | Frontend components | Large | 8 components |

---

## Status

- [x] DB migration — `AddNicheAnalysis` EF migration generated (2026-06-02)
- [x] EF Core entities — 6 entities in `GeekSeo.Persistence/Entities/NicheAnalysisEntities.cs`
- [x] Dapper repositories — `INicheAnalyticsDapperRepository` + `HttpNicheAnalyticsDapperRepository`
- [x] Static data (NoisePaths, SynonymMap) — `NoisePaths.cs`, `PillarSynonymMap.cs`
- [x] SchemaOrgExtractor
- [x] SitemapExtractor
- [x] NavMenuExtractor
- [x] PillarValidator
- [x] PillarMerger
- [x] NicheRootEntityBuilder
- [x] NicheAuthorityScorer
- [x] NicheAnalyzerService
- [x] NicheProfileRepository — `INicheProfileRepository` + `HttpNicheProfileRepository`
- [x] NicheAnalysisBackgroundJob
- [x] NicheAnalyzerController — 10 endpoints
- [x] DI registration — `SeoBackendExtensions.cs`
- [ ] Tests — unit tests for Scorer, Validator, Merger
- [ ] Wire into TopicalMapService
- [x] Frontend — page, 5 components, sidebar link
- [x] GeekRepository — `NicheProfileRepository`, `NicheAnalyticsDapperRepository`, `NicheProfilesController`, `NicheAnalyticsController` (2026-06-02)

*Last updated: 2026-06-02*
