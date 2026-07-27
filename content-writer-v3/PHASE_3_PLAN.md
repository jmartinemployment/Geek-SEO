# Content Writer V3 — Phase 3: Intelligent Drafting

## Critical Architecture Change from V2

**V2 Flaw (Siloed Generation):**
- Research keyword in isolation
- Generate content without site context
- Avoid redundancy only by accident, not design
- No understanding of full business offering
- Content positioned against SERP, not against client's own content

**V3 Fix (Intelligent Site-Aware Generation):**
- Phase 1 Pre-Work: Site Intelligence Audit (new)
- Phase 1 Research: Discover insights in context of existing assets
- Phase 3 Drafting: Writer has full business context
- Cross-link strategically, not by accident
- Position as "how this fits our strategy," not "here's what the SERP says"

---

## Phase 1 Pre-Work (New): Site Intelligence Audit

**Before researching any keyword, ingest:**
- `SiteAudit` entity: Snapshot of client's existing content landscape
  - URL structure and content inventory (what pages exist, topical clusters)
  - Authority signals (internal link map, which topics are cornerstone)
  - Product/service offerings and positioning
  - Audience segments they actively target
  - Current content gaps (what's missing from their strategy)
  - Competitor positioning (not SERP, but their brand positioning)

**Job: `AuditSite`**
- Input: Client website URL + existing content metadata
- Output: `SiteAudit` entity with structured analysis
- Scope: Crawl top 100 URLs, extract topical structure, identify gaps

**Result:** Research runs now have full context.

---

## Phase 1 Revised: Research in Context

**ResearchInsight changes:**
- Add `PositioningVsExisting` field: How does this insight differ from/build-on what they already own?
- Add `ExistingContentToReference` list: Which existing pages should mention this?
- Add `ContentPositioning` enum: Cornerstone (foundational) | Differentiator | Supporting | FAQ

**Phase 1 Job Output Now Includes:**
- Insights ranked by importance/difficulty ✓ (existing)
- Positioning relative to existing content (new)
- Cross-link recommendations (new)
- Audience segment alignment (new)

---

## Phase 2 Revised: Strategy Brief

**StrategyBrief now includes:**
- Content positioning type (Cornerstone/Differentiator/Supporting)
- Link to SiteAudit context
- Existing content to reference/link-to
- Intended internal link targets
- Audience segment specificity

---

## Phase 3: Intelligent Drafting

**Core Principle:** Writer is not filling a template. Writer is strategically expanding/connecting the client's content ecosystem.

**Input to Draft Job:**
- StrategyBrief (angle, audience, buying stage, positioning)
- Ranked insights ordered by importance
- SiteAudit context (what exists, what gaps, what positioning)
- Existing content to reference (URLs, anchor text, context)
- ContentPlan with insight-linked sections

**ContentDocument Generation Changes:**
- Sections reference existing content: "We covered X in detail here [link]; this insight builds on that by..."
- Writer understands: "This is a Cornerstone piece, so it needs to be comprehensive and link to 5+ supporting pieces"
- Writer avoids: Saying what's already been said (detected via SiteAudit)
- Writer includes: Strategic CTAs that funnel from this topic to related offerings

**New Validation Layer:**
- `ContentIntelligenceValidator` checks:
  - Is this claiming something their existing content already says? (flag redundancy)
  - Does it reference the key existing pieces? (flag missing context)
  - Does the positioning match the designated type? (Cornerstone should be comprehensive, not a quick tip)
  - Are the CTAs aligned with the audience segment? (avoid misaligned conversion pressure)

**Claim Validation:**
- Claims must be supported by evidence ✓ (existing)
- Claims must not contradict client's existing positioning (new)
- Claims must align with their product offering (new, not "generic best practice")

---

## V2 Anti-Patterns to Avoid (Document for Future Phases)

1. ✅ **SERP copying** — FIXED in Phase 1B (Insight Extraction)
2. ✅ **Siloed generation** — FIXING in Phase 3 (Site Intelligence)
3. **Flat evidence model** — Evidence weight should vary by type (client fact > verified external > observed market)
4. **No originality filter** — Check insights against competitor claims before writing
5. **Loose evidence traceability** — Each claim must link to specific evidence, not just general support
6. **No evidence staleness** — Track when evidence was collected, deprecate old data
7. **Buying-stage agnostic** — Content arc should change by stage (not just CTA)
8. **Flat audience model** — Treat audience segments distinctly, not generically
9. **No competitive differentiation** — Explicitly position against competitors, not just SERP
10. **No outcome loop** — Phase 5+ should measure performance and update insights

---

## Phase 3 Exit Criteria

- Draft intelligently references existing content (not isolated)
- No redundancy with existing pages
- Positioning type (Cornerstone/Differentiator/Supporting) reflected in depth/scope
- All claims traceable to evidence
- Claims don't contradict client positioning
- CTAs aligned to audience segment and buying stage
- Validator catches positioning drift
- Writer sees full business context, not just brief
