# Content Writer V3: Complete Architecture

## Overview

Content Writer V3 is a **self-improving content generation system** that breaks from Version 2's "generate and forget" approach. It implements a closed feedback loop: Research → Strategy → Writing → Review → Publication → Performance Measurement → Learning → Better Insights.

---

## Six-Phase Architecture

### Phase 0: Foundation
**Setup & infrastructure**
- Client workspaces and campaigns
- Brand voice profiles
- Domain entities and database schema
- API infrastructure

### Phase 1: Research & Intelligence
**Evidence collection**
- Research runs aggregate sources
- Evidence classification (VerifiedClientFact > VerifiedExternalSource > ObservedMarketLanguage)
- Pain point identification
- Reconciliation of evidence to pain points

**Status**: Foundation ready. Evidence model in place.

### Phase 1B: Insight Extraction ✅ IMPLEMENTED
**Independent reasoning** (not SERP copying)
- LLM analyzes evidence independently
- Generates 3-4 genuinely important insights (not forced 5-section template)
- Ranks by Importance × 0.6 + Difficulty × 0.4
- **Ruthlessly selective**: Skips lame/obvious insights entirely
- Output: ResearchInsight with title, description, why-it-matters, what-people-get-wrong, difficulty (1-10), importance (1-10)

**Why this matters**: Breaks V2 flaw of copying SERP structure and skimming surfaces. Each insight must be independently justified.

### Phase 2: Strategy Brief & Approval ✅ IMPLEMENTED
**Human editorial gate**
- Strategy brief captures angle, audience, buying stage, CTA
- Must be explicitly approved before drafting
- Approval workflow with audit trail
- Links evidence supporting the brief

**Prevents**: Content that sounds good but doesn't align with business positioning.

### Phase 3: Intelligent Drafting ✅ IMPLEMENTED
**Site-context aware generation** (breaks siloed writing)
- SiteAudit provides full context:
  - Existing content inventory and topical clusters
  - Authority signals and cornerstone pages
  - Product/service offerings
  - Audience segments
  - Content gaps
  - Competitor positioning
- Writer has full picture: "How does this fit our strategy?"
- Automatically detects redundancy vs. existing content
- Validates offering alignment and audience segment specificity
- Output: Draft with validation warnings and recommendations

**Content positioning types**:
- Cornerstone: Foundational, comprehensive, links to 5+ supporting pieces
- Differentiator: Competitive positioning, unique angle
- Supporting: Builds on cornerstone, narrower scope
- FAQ: Quick answer, quick decision

**Why this matters**: V2 generated in isolation. V3 writer understands what already exists and strategically expands, not duplicates.

### Phase 4: Editorial Review & Approval ✅ IMPLEMENTED
**Quality gate with human expertise**
- Editor reviews draft with context:
  - Validation warnings from Phase 3
  - Strategy brief and positioning
  - Related existing content
  - Implied intent of each insight
- Threaded comments with severity levels:
  - Blocker: Can't publish as-is
  - Major: Should fix
  - Minor: Nice to fix
  - Suggestion: Feedback but not required
- Comment types: Factual | Tone | Clarity | Structure | CTA | Evidence | Offering | Competitive
- Scores accuracy (1-10), strength (1-10), alignment (1-10)
- Editor summary captures assessment
- **Blocks publication if Blocker comments remain unresolved**

**Output**: Approved ContentReview with scores and approval timestamp.

### Phase 5: Publication ✅ IMPLEMENTED
**Multi-platform publishing with audit trail**
- Publication entity tracks:
  - Target platform (WordPress, custom CMS, Supabase, etc.)
  - Status: Queued → Scheduled → Publishing → Published
  - PublishedUrl (for cross-linking and tracking)
  - Retry count for failed publishes
- Supports:
  - Immediate publishing
  - Scheduled publishing (publish at future date/time)
  - Retry with exponential backoff
  - Unpublishing (remove from live)
- PublicationEvent audit trail logs every action
- Platform-agnostic: IPublishAdapter abstraction
  - WordPressAdapter: REST API + plugin
  - CustomCMSAdapter: Internal API
  - SupabaseAdapter: Storage + frontend webhook

**Why this matters**: V2 had fragmented publishing (manual, brittle automation). V3 is structured with retries, scheduling, and full audit.

### Phase 6: Performance Feedback Loop ✅ IMPLEMENTED
**Closes the loop: Learn from real-world outcomes**

**ContentPerformance** (synced from analytics):
- Views, engaged views (scrolled 50%+), conversions (CTA clicks)
- AvgTimeOnPage, bounce rate, rank position (optional GSC)
- Quality score calculated: (engagement% × 0.4 + conversion% × 0.4 + low-bounce × 0.2)

**InsightPerformanceLink**:
- Each insight scored 1-10 for "how much did this drive success?"
- Determined by editor assessment or LLM correlation analysis
- IsKeyDifferentiator: Did this stand out vs. competitors?

**InsightFeedback** (aggregated across all uses):
- AveragePerformanceScore: Mean of all contribution scores
- TimesUsed and TimesSuccessful tracked
- WhatWorkedWellJson: Patterns ("Urgency framing drives engagement")
- WhyItsStruggling: Common issues ("Too obvious", "Competitors say same thing")
- **ShouldBeRetired**: If used 3+ times with <30% success rate, mark for retirement

**Feedback Loop**:
```
Content Published
        ↓
Metrics Synced (Daily/Weekly)
        ↓
Insight Contributions Scored
        ↓
Insight Feedback Aggregated
        ↓
System Learns:
   - "Cost-of-Inaction" = Proven Winner (9/10 avg)
   - "DIY Dangers" = Retire (2/10 avg, 0% success)
        ↓
Next Content Generation:
   - System recommends proven insights
   - Flags retirement candidates
   - Writer learns what actually works
```

**Why this matters**: V2 generated and forgot. V3 improves: insights get better over time based on real performance data.

---

## Key Architectural Principles

### 1. Clean Architecture
- **Domain**: Pure entities, no dependencies
- **Application**: Business logic, services, workflows
- **Infrastructure**: Database, external APIs, jobs
- **API**: Controllers, DTOs, HTTP layer

### 2. Durable Job Queue
- **IJobHandler<TPayload>** pattern for type-safe jobs
- Status: Queued → Running → Completed/Failed
- Dapper-based concurrent-safe claiming (FOR UPDATE SKIP LOCKED)
- Exponential backoff for retries
- Audit trail via Job entity

**Jobs implemented**:
- InitiateResearch
- ExtractInsights (Phase 1B)
- DraftContent (Phase 3)
- PublishContent (Phase 5, future)

### 3. Optimistic Concurrency
- Version token on every entity
- Prevents lost updates in high-concurrency scenarios
- Configured in EF Core

### 4. Evidence-Based Architecture
- All claims traceable to evidence
- Evidence classified by support level
- Claim validation checks consistency
- No generic "best practice" unsupported claims

### 5. Site Intelligence Foundation
- Every content generation starts with SiteAudit context
- Understands existing content, gaps, positioning, offerings
- Prevents blind spots and redundancy
- Enables strategic cross-linking

### 6. Feedback-Driven Improvement
- Performance metrics feed back to insight quality
- Underperformers flagged for retirement
- Success patterns documented for reuse
- System continuously improves

---

## Database Schema (PostgreSQL)

### Core Tables
- Clients, Workspaces
- ClientProfiles, ClientProfileVersions
- ContentCampaigns, ContentAssets, ContentAssetVersions
- Jobs

### Phase 1: Research
- ResearchRuns, ResearchSources, ResearchEvidence
- PainPoints, PainPointEvidenceLinks
- ReconciliationProposals

### Phase 1B: Insights
- ResearchInsights (with RankScore, IncludeInOutline, OrderIndex)
- InsightEvidenceLinks

### Phase 2: Strategy
- StrategyBriefs, StrategyBriefEvidenceLinks
- ApprovalEvents

### Phase 3: Site Intelligence
- SiteAudits, ContentNodes, TopicalClusters, ContentGaps

### Phase 4: Review
- ContentReviews, ReviewComments

### Phase 5: Publication
- Publications, PublicationEvents

### Phase 6: Performance
- ContentPerformances, InsightPerformanceLinks, InsightFeedbacks

---

## API Routes

### Campaigns
- POST /api/content-writer/v3/campaigns
- GET /api/content-writer/v3/campaigns/{id}
- PATCH /api/content-writer/v3/campaigns/{id}

### Research
- POST /api/content-writer/v3/research/runs
- GET /api/content-writer/v3/research/runs/{id}
- POST /api/content-writer/v3/research/reconciliation/approve

### Insights (Phase 1B)
- GET /api/content-writer/v3/insights (filtered, ranked)

### Strategy Briefs
- POST /api/content-writer/v3/strategy-briefs
- GET /api/content-writer/v3/strategy-briefs/{id}
- PATCH /api/content-writer/v3/strategy-briefs/{id}
- POST /api/content-writer/v3/strategy-briefs/{id}/approve
- POST /api/content-writer/v3/strategy-briefs/{id}/return-to-research

### Drafting (Phase 3)
- POST /api/content-writer/v3/drafts/initiate
- GET /api/content-writer/v3/drafts/{assetId}/latest

### Reviews (Phase 4)
- POST /api/content-writer/v3/reviews/{assetVersionId}/initiate
- GET /api/content-writer/v3/reviews/{reviewId}
- POST /api/content-writer/v3/reviews/{reviewId}/comment
- PATCH /api/content-writer/v3/reviews/{reviewId}/score
- POST /api/content-writer/v3/reviews/{reviewId}/approve
- POST /api/content-writer/v3/reviews/{reviewId}/request-changes
- POST /api/content-writer/v3/reviews/{reviewId}/reject

### Publications (Phase 5)
- POST /api/content-writer/v3/publications/queue
- GET /api/content-writer/v3/publications/{publicationId}
- POST /api/content-writer/v3/publications/{publicationId}/publish-now
- POST /api/content-writer/v3/publications/{publicationId}/schedule
- POST /api/content-writer/v3/publications/{publicationId}/retry
- POST /api/content-writer/v3/publications/{publicationId}/unpublish

### Performance (Phase 6)
- POST /api/content-writer/v3/performance/record
- GET /api/content-writer/v3/performance/{perfId}
- PATCH /api/content-writer/v3/performance/{perfId}/metrics
- POST /api/content-writer/v3/performance/{perfId}/insight-contribution
- GET /api/content-writer/v3/performance/insights/{insightId}/recommendation
- GET /api/content-writer/v3/performance/insights/recommendations/list

---

## Frontend Structure

### Dashboards
- `/dashboard` — Overview, recent activity, metrics
- `/analytics` — Content and insight performance

### Phase-Specific Pages
- `/research` — Keywords and research runs
- `/insights` — Phase 1B ranked insights
- `/strategy-briefs` — Strategy briefs with approval workflow
- `/reviews` — Phase 4 editorial reviews
- `/publications` — Phase 5 publishing status
- `/pain-points` — Pain point management
- `/reconciliation` — Evidence reconciliation

### Content Management
- `/campaigns` — Campaign management
- `/assets` — Content assets

---

## Critical Differences from V2

| Aspect | V2 | V3 |
|--------|----|----|
| **Research** | SERP copying | Independent reasoning |
| **Content Structure** | Forced 5-section template | Variable-length, insight-driven |
| **Section Order** | SERP ranking | By importance × difficulty |
| **Insight Selection** | All sections included | Ruthlessly selective (skip lame ones) |
| **Writing Context** | Siloed by keyword | Full site intelligence |
| **Redundancy Detection** | Accidental | Deliberate (SiteAudit) |
| **Review Process** | Minimal | Structured with quality gates |
| **Publishing** | Manual or brittle automation | Durable job queue, multi-platform |
| **Performance Tracking** | None (generate & forget) | Comprehensive with feedback loop |
| **Insight Improvement** | Static | Dynamic (learn from outcomes) |

---

## Deployment Considerations

### Database Migrations
- EF Core migrations for schema management
- Version tokens on every entity for optimistic concurrency
- Unique indexes for idempotency keys

### Job Processing
- Background service polling queue every 5 seconds
- Batch size: 10 jobs per poll
- Lease-based claiming (prevents double-processing)
- Graceful error handling with retry logic

### Analytics Integration
- Google Analytics API for metrics sync
- Google Search Console optional (rank tracking)
- Custom tracking via conversion pixels

### Platform Adapters
- WordPress: REST API + plugin for metadata
- Custom CMS: HTTP endpoints
- Supabase: Storage + webhook to frontend

---

## Future Enhancements

See **PHASE_7_ROADMAP.md** for detailed enhancement plan.

### Quick Reference

1. **Phase 7: Competitive Intelligence** — Position vs. actual competitors, not just SERP
2. **Phase 8: Audience Segmentation** — Different angles for different buyer personas
3. **Phase 9: Iterative Refinement** — Edit sections surgically, not all-or-nothing
4. **Phase 10: Content Calendar** — Quarterly batch planning and auto-publishing
5. **Phase 11: Continuous Feedback Loop** — Performance data guides next generation
6. **Phase 12: Multi-Channel Amplification** — Email, LinkedIn, social, partnerships
7. **Phase 13: A/B Testing** — Test variants before full rollout
8. **Phase 14: Knowledge Retrieval** — Searchable institutional memory of insights

### Immediate Quick Wins
- LLM Integration: Replace MockContentGenerator with Claude/GPT-4
- Cross-linking Automation: Auto-link related content based on InsightPerformanceLinks
- Semantic Search: Find similar content not just by keyword but by meaning
- Team Collaboration: Comments and feedback loops for team members
- Webhooks: Real-time alerts for content changes, approvals, publications
