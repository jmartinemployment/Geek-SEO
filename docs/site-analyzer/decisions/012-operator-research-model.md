# ADR 012: Operator research model (pillars, gaps, comparison)

## Status
Accepted — **partially implemented** (see gaps below)

## Context

Operators use Site Analyzer Web with:

- One **Project URL** per site profile
- One **SERP HTML** per **keyword** (pillar)
- **Competitor crawl** after every SERP import

Research must land in `sa2` for Content Writer. Several fields were designed (`gap_topics`, `matched_pillar_*`) but never wired on the operator path. Documentation and UI labels conflated pillars, niche tags, and writing recommendations.

## Decisions

### Pillar = keyword

`analysis_runs.keyword` (from SERP title) is the **content pillar** for that run. Not `site_profiles.niche_tags`.

### Required steps per keyword run

1. SERP import → `serp_items`
2. Target-site crawl of Project URL → `pages` / `page_headings`
3. Competitor crawl → `competitor_pages`
4. Comparison → `findings`
5. Run focus assembly → `gap_topics`, `writing_instructions`, pillar metadata

Competitor crawl is **not optional** for research-ready.

### Gap topics

`gap_topics` is built from:

- Comparison **findings** (primary)
- Target **H2–H6** and competitor **H2–H6**
- Pillar keyword as anchor
- Optional SERP PAA/related
- **Not** a dump of `site_profiles` business columns

### SERP import

Persist all line types; run relevance filter to **label** organics (`filter_status`), never delete rows at import.

### Site profile fields

| Field | Role |
|-------|------|
| `writing_recommendations` | Operator **visual** advice on Site Analyzer — keep |
| `niche_tags` | Site themes (headings/schema) — **must not** include run keywords |
| `primary_niche` | Business identity (JSON-LD) — relabel in UI |

### Handoff

URL carries IDs only. Freeze in Geek-SEO is a legacy snapshot; `sa2` is source of truth.

### OperatorRunFocusService

New orchestration (planned) fills `analysis_runs` after workflow steps. Replaces unwired `AssembleForRunAsync` for operator path.

## Implementation gaps (as of ADR acceptance)

| Item | Status |
|------|--------|
| SERP import | Done |
| Competitor crawl | Done |
| Target crawl on operator path | **Done** |
| Comparison on `competitor_pages` | **Done** |
| `gap_topics` population | **Done** |
| `niche_tags` keyword injection | **Fixed** |
| `generated_schema_json` on `site_profiles` | **Done** (sync after extract when profile exists) |

## Consequences

- Extend `ComparisonService` (or sibling) to read `competitor_pages`.
- Wire target-only crawl after SERP import.
- Invoke run focus assembly after comparison.
- Update [RESEARCH-MODEL.md](../RESEARCH-MODEL.md) when each gap closes.

## References

- [RESEARCH-MODEL.md](../RESEARCH-MODEL.md)
- [010-competitor-crawl-planned.md](010-competitor-crawl-planned.md)
- [003-relevance-filter.md](003-relevance-filter.md)
- Plan: [plans/fix-site-analyzer-research.md](../plans/fix-site-analyzer-research.md)
