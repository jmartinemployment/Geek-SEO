# Content Writer V3

**A self-improving content generation system that learns from performance data.**

## TL;DR

V3 breaks from V2's "generate and forget" approach by implementing a **closed feedback loop**:

```
Research → Strategy → Writing → Review → Publish → Measure → Learn → Better Insights
                                                        ↑______________|
```

Content gets better over time because the system learns which insights drive engagement and conversions.

---

## What Problem Does This Solve?

### V2 Problems
- ❌ SERP copying (shallow, derivative, lame)
- ❌ Siloed generation (doesn't know existing content)
- ❌ Forced 5-section template (mediocre content all the same)
- ❌ Generate and forget (no learning loop)
- ❌ One-way publication (no performance feedback)

### V3 Solutions
- ✅ Independent reasoning (deep, original, valuable)
- ✅ Site-aware generation (understands full strategy)
- ✅ Insight-driven outlines (variable length, best ideas first)
- ✅ Performance-driven improvement (insights improve over time)
- ✅ Closed feedback loop (measure → learn → improve)

---

## Architecture Overview

### 6 Implemented Phases

| Phase | Focus | Status |
|-------|-------|--------|
| **0** | Foundation & Setup | ✅ Complete |
| **1** | Research & Intelligence | ✅ Complete |
| **1B** | Insight Extraction | ✅ Complete (independent reasoning) |
| **2** | Strategy Brief & Approval | ✅ Complete (human gate) |
| **3** | Intelligent Drafting | ✅ Complete (site-aware) |
| **4** | Editorial Review | ✅ Complete (quality gate) |
| **5** | Publication | ✅ Complete (multi-platform) |
| **6** | Performance Feedback | ✅ Complete (learning loop) |

### 8 Future Phases (See PHASE_7_ROADMAP.md)

| Phase | Focus | Priority |
|-------|-------|----------|
| **7** | Competitive Intelligence | Tier 1 |
| **8** | Audience Segmentation | Tier 2 |
| **9** | Iterative Refinement | Tier 1 |
| **10** | Content Calendar | Tier 2 |
| **11** | Continuous Feedback | Tier 1 |
| **12** | Multi-Channel Amplification | Tier 2 |
| **13** | A/B Testing | Tier 3 |
| **14** | Knowledge Retrieval | Tier 3 |

---

## Core Concepts

### Insight Ranking (Phase 1B)

Insights are ranked by **Importance × 0.6 + Difficulty × 0.4**, putting the hardest/most important ideas first:

```
Insight 1: "Emergency response window is 2-4 hours"
  → Importance: 9/10, Difficulty: 7/10 → Rank Score: 8.2
  → Section 0 (first, most important)

Insight 2: "Preventive maintenance ROI is 10:1"
  → Importance: 8/10, Difficulty: 6/10 → Rank Score: 7.6
  → Section 1

Insight 3: "DIY drain cleaner myths"
  → Importance: 4/10, Difficulty: 4/10 → Rank Score: 4.0
  → SKIPPED (too lame/obvious)
```

### Site Intelligence (Phase 3)

Before drafting, writer sees full context:
- Existing content and topical clusters
- Authority signals and cornerstone pages
- Product/service offerings
- Audience segments
- Content gaps and competitive positioning

This prevents redundancy and enables strategic cross-linking.

### Quality Gates (Phase 4)

Editor reviews with three scores:
- **Accuracy (1-10)**: How well fact-checked?
- **Strength (1-10)**: How compelling/differentiated?
- **Alignment (1-10)**: How well positioned?

Blocker-level comments must be resolved before approval.

### Performance Loop (Phase 6)

Published content is tracked:
```
Content Performance Metrics (weekly)
  ↓
Which insights drove success?
  ↓
Insight Feedback aggregated
  ↓
System learns:
  - "Cost-of-inaction framing" = Proven winner (9/10 avg)
  - "DIY dangers myth" = Retire (2/10 avg, 0% success)
  ↓
Next content generation sees recommendations
```

---

## Key Architectural Decisions

### Clean Architecture
- **Domain**: Pure entities, no dependencies
- **Application**: Business logic and services
- **Infrastructure**: Database, external APIs, job queue
- **API**: HTTP controllers and DTOs

### Durable Job Queue
Jobs follow this lifecycle:
```
Queued → Running → Completed
                  ↓ (if error)
                  Failed → Retry
```

Concurrent-safe via Dapper's `FOR UPDATE SKIP LOCKED`.

### Optimistic Concurrency
Every entity has a `Version` token to prevent lost updates in high-concurrency scenarios.

### Evidence-Based Claims
All content claims traceable to evidence:
- VerifiedClientFact (highest confidence)
- VerifiedExternalSource
- ObservedMarketLanguage
- Unsupported (flagged for review)

---

## Project Structure

```
backend/
  src/
    ContentWriterV3.Domain/          # Pure entities
      Entities/
        ResearchInsight.cs           # Phase 1B
        StrategyBrief.cs             # Phase 2
        ContentReview.cs             # Phase 4
        Publication.cs               # Phase 5
        ContentPerformance.cs        # Phase 6
        SiteAudit.cs                 # Phase 3
    ContentWriterV3.Application/     # Business logic
      Services/
        InsightExtractor.cs          # Phase 1B
        ContentPlanService.cs        # Phase 2
        ContentIntelligenceValidator.cs  # Phase 3
        ReviewService.cs             # Phase 4
        PublicationService.cs        # Phase 5
        PerformanceService.cs        # Phase 6
    ContentWriterV3.Infrastructure/  # Database, jobs
      Data/
        ContentWriterV3DbContext.cs
      Jobs/
        Handlers/
          InitiateResearchHandler.cs
          ExtractInsightsHandler.cs
          DraftContentHandler.cs
    ContentWriterV3.Api/             # HTTP layer
      Controllers/
        V3CampaignsController.cs
        V3ResearchController.cs
        V3StrategyBriefsController.cs
        V3ReviewController.cs
        V3PublicationController.cs
        V3PerformanceController.cs
        V3DraftingController.cs

frontend/
  app/
    dashboard/                       # Overview
    research/                        # Phase 1
    insights/                        # Phase 1B
    strategy-briefs/                 # Phase 2
    reviews/                         # Phase 4
    publications/                    # Phase 5
    analytics/                       # Phase 6
    components/
      Navigation.tsx
```

---

## Getting Started

### Prerequisites
- .NET 10
- PostgreSQL
- Node.js 18+

### Backend Setup
```bash
cd backend
dotnet build
dotnet ef database update
dotnet run
```

API runs on http://localhost:5000

### Frontend Setup
```bash
cd ../frontend
npm install
npm run dev
```

Frontend runs on http://localhost:3000

---

## API Endpoints Summary

### Research (Phase 1)
- `POST /api/content-writer/v3/research/runs`
- `GET /api/content-writer/v3/research/runs/{id}`

### Insights (Phase 1B)
- `GET /api/content-writer/v3/insights` (ranked, filtered)

### Strategy (Phase 2)
- `POST /api/content-writer/v3/strategy-briefs`
- `PATCH /api/content-writer/v3/strategy-briefs/{id}`
- `POST /api/content-writer/v3/strategy-briefs/{id}/approve`

### Drafting (Phase 3)
- `POST /api/content-writer/v3/drafts/initiate`
- `GET /api/content-writer/v3/drafts/{assetId}/latest`

### Review (Phase 4)
- `POST /api/content-writer/v3/reviews/{assetVersionId}/initiate`
- `PATCH /api/content-writer/v3/reviews/{reviewId}/score`
- `POST /api/content-writer/v3/reviews/{reviewId}/approve`

### Publication (Phase 5)
- `POST /api/content-writer/v3/publications/queue`
- `POST /api/content-writer/v3/publications/{id}/publish-now`
- `POST /api/content-writer/v3/publications/{id}/schedule`

### Performance (Phase 6)
- `GET /api/content-writer/v3/performance/{perfId}`
- `PATCH /api/content-writer/v3/performance/{perfId}/metrics`
- `GET /api/content-writer/v3/performance/insights/{insightId}/recommendation`

---

## Documentation

- **ARCHITECTURE.md** — Complete system design and rationale
- **PHASE_7_ROADMAP.md** — Future enhancements (Phases 7-14)
- **PHASE_*_PLAN.md** — Detailed phase specifications

---

## Key Metrics to Track

### Content Quality
- Review scores (accuracy, strength, alignment)
- Number of Blocker comments found
- Reviewer satisfaction

### Performance
- Views per published piece
- Engagement rate (scrolled >50%)
- Conversion rate (CTA clicks)
- Average time on page
- Bounce rate

### Insight Effectiveness
- Average performance score across uses
- Success rate (# pieces with good performance / # pieces)
- Correlation to business outcomes

### System Health
- Job success rate
- Average time from brief to publication
- Review cycle time
- Publication success rate

---

## Contributing

1. Read ARCHITECTURE.md to understand the system
2. Check PHASE_7_ROADMAP.md for upcoming work
3. Create a branch from the phase you're working on
4. Follow Clean Architecture conventions
5. Write tests for new job handlers
6. Submit PR with description of changes

---

## License

TBD

---

## Feedback & Future Direction

This system is designed to improve continuously. The feedback loop means:
- **Every published piece** teaches the system what works
- **Every failed insight** shows what doesn't work
- **Every performance metric** informs the next generation
- **The system gets smarter over time**

See PHASE_7_ROADMAP.md for how we're building on this foundation in the next 8 phases.
