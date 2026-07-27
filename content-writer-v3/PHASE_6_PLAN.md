# Content Writer V3 — Phase 6: Performance Feedback Loop

## Problem Phase 6 Solves

**V2 Issue:** "Generate and forget" — content published, never measured, insights never improved. No learning loop. Same weak insights used repeatedly.

**V3 Fix:** Closed feedback loop:
1. Track content performance (views, engagement, conversions)
2. Link performance back to insights that drove it
3. Measure which insights actually work
4. Update insights based on real-world data
5. Retire consistently underperforming insights
6. Recommendations for next iteration

---

## Phase 6 Architecture

### Entities

**ContentPerformance**
- PublicationId → which piece of content
- PublishedUrl → where it lives
- Metrics (synced from Google Analytics or custom tracking):
  - Views
  - EngagedViews (scrolled 50%+)
  - Conversions (CTA clicks)
  - AvgTimeOnPage (seconds)
  - BounceRate (%)
  - RankPosition (if from GSC)
- LastSyncedAt: When we last pulled metrics
- EstimateQualityScore(): 1-10 based on engagement + conversions + low bounce

**InsightPerformanceLink**
- ContentPerformanceId → which piece of content this insight was used in
- ResearchInsightId → which insight
- ContributionScore: 1-10, how much did this insight drive the performance?
  - Determined by: editorial assessment + correlation analysis
- IsKeyDifferentiator: Did this insight stand out vs competitors?
- FeedbackNotes: "This insight was the hook that got people scrolling"

**InsightFeedback**
- ResearchInsightId
- AveragePerformanceScore: 1-10 across all content using it
- TimesUsed: How many pieces used this insight?
- TimesSuccessful: How many had good performance (>6/10)?
- WhatWorkedWellJson: ["The cost-of-inaction framing", "Specific ROI numbers"]
- WhyItsStruggling: ["Too obvious to readers", "Competitors say same thing"]
- ShouldBeRetired: Bool (used 3+ times, success rate <30%)

### Workflow

```
Content Published
    ↓
    [Daily/Weekly: Sync Metrics from GA/Custom Tracking]
    ↓
    [ContentPerformance updated with latest views/engagement/conversions]
    ↓
    [InsightPerformanceLink: Editor/LLM estimates which insights drove success]
    ↓
    [InsightFeedback aggregates across all uses]
    ↓
Decision:
  - Insight consistently high-performing (>7/10 avg) → "Proven winner"
  - Insight struggling (<5/10 avg, used 3+ times) → "Needs revision"
  - Insight too obvious/weak → Mark for retirement
    ↓
    [Next time this insight comes up in research, system flags it]
    [Writer can skip it or revise it based on feedback]
```

### Metrics Integration

**Google Analytics Sync:**
- Daily cron job pulls metrics for published URLs
- Updates ContentPerformance.Views, EngagedViews, AvgTimeOnPage, BounceRate
- Falls back to manual upload if GA not connected

**Google Search Console (optional):**
- Pulls rank position for tracked keywords
- Updates ContentPerformance.RankPosition

**Custom CTA Tracking:**
- Track clicks on CTAs embedded in content
- Updates Conversions metric

### Insight Performance Analysis

When reviewing performance:

1. **Contribution Scoring:**
   - Editor manually reviews: which insights were the hooks?
   - OR LLM analyzes: "Which section correlates with engagement increase?"
   - Scores each insight 1-10 for how much it drove success

2. **Aggregation:**
   - InsightFeedback aggregates across all uses
   - AveragePerformanceScore = mean of all contribution scores
   - TimesSuccessful = count where contribution score > 6

3. **Retirement Logic:**
   - If TimesUsed >= 3 and TimesSuccessful/TimesUsed < 30%, mark for retirement
   - System flags on next research: "This insight has underperformed historically"
   - Writer can skip or attempt revision

### Quality Loop

Example: "Cost of Inaction" insight

1. Used in 3 pieces
2. Performance scores: 9, 8, 4 → avg 7/10 (Proven winner)
3. Feedback: "The financial impact framing drives engagement"
4. System learns: This insight is battle-tested
5. Next research: System recommends it as proven angle

Counterexample: "DIY Dangers" insight

1. Used in 2 pieces
2. Performance scores: 3, 2 → avg 2.5/10 (Struggling)
3. Feedback: "Readers see this everywhere, not differentiating"
4. System marks: ShouldBeRetired = true
5. Next research: System flags "Historically underperformed, consider skipping or major revision"

### API Endpoints

**POST /api/content-writer/v3/performance/sync**
- Trigger metrics sync for a publication
- Pulls from GA, updates ContentPerformance

**GET /api/content-writer/v3/performance/{publicationId}**
- Full performance data + insight contributions

**POST /api/content-writer/v3/insights/{insightId}/feedback**
- Editor evaluates: which insights drove this content's success?
- Params: contributionScores: { insightId: score }, feedbackNotes
- Updates InsightPerformanceLink, InsightFeedback

**GET /api/content-writer/v3/insights/{insightId}/performance**
- Historical performance of this insight across all uses
- Shows: avg score, success rate, feedback, retirement status

**GET /api/content-writer/v3/insights/recommended**
- List insights ranked by performance score
- Separate lists: "Proven Winners" vs. "Needs Revision" vs. "Retire"

### Dashboard Insights (Frontend)

**Performance by Content**
- Table: Title | Views | Engagement | Conversions | Time on Page | Quality Score
- Filter by: date range, status (high/low/avg performers)

**Performance by Insight**
- Table: Insight Title | Used (times) | Avg Score | Success Rate | Status
- "Proven Winners" → green
- "Needs Revision" → yellow
- "Retire" → red

**Feedback Log**
- Comments on what worked/didn't work in each piece
- Help writers learn patterns

### Exit Criteria

- ✓ Content performance metrics synced from analytics
- ✓ Insight contributions scored and recorded
- ✓ Insight feedback aggregated with performance trends
- ✓ Retirement logic flagging underperformers
- ✓ Recommendations for next iteration
- ✓ Closes the loop: insights improve based on real-world performance

---

## Complete V3 Loop (All Phases)

```
Phases 0-2: Foundation & Strategy
  ↓
Phase 1: Site Intelligence Audit
  ↓
Phase 1B: Research Insights (independent reasoning, ranked)
  ↓
Phase 2: Strategy Brief (human approval)
  ↓
Phase 3: Intelligent Drafting (site-aware)
  ↓
Phase 4: Editorial Review (quality gate)
  ↓
Phase 5: Publication (multi-platform)
  ↓
Phase 6: Performance Feedback (measure & improve)
  ↓
[Loop back to Phase 1 with learnings → Insights improve over time]
```

The system is now **self-improving**: as content performs well or poorly, insights get better or are retired, making future content stronger.
