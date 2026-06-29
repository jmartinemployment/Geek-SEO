# Frase Phase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development or executing-plans to implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver complete keyword research in `sa2` so Content Writer can outline, draft, score, cite, and render JSON-LD for one pillar keyword — without Semrush UI drift or rankings promises.

**Architecture:** Keep existing operator chain (SERP import → filter → target crawl → competitor seed crawl → comparison → assembly). **Site Analyzer persists research in `sa2`; Content Writer reads by `analysisRunId` when needed.** Writer owns outlines, FAQs structure, content score, Sources/Citations, meta, and JSON-LD. Do not build a parallel outline product in SA2.

**Tech Stack:** .NET 10 (`SiteAnalyzer2.*`), PostgreSQL `sa2`, Next.js 15 Web, SSE competitor crawl (ADR 011).

**Product boundary:** [PRODUCT-PHASES.md](../PRODUCT-PHASES.md) · ADR [014-product-phases-frase-first.md](../decisions/014-product-phases-frase-first.md)

---

## Documentation rule: evidence before status

Do **not** mark a field, gate, or workflow step **Wired**, **Done**, or **empty** in docs unless you can cite:

1. **Code path** — service/method that produces or reads the value (file + symbol).
2. **Verification** — at least one of: unit test name, `GET …/research-focus` gate, or `GET …/content-writer-export` field present on a real run.

If docs and production disagree, **fix docs to match evidence** (or fix code in a separate PR). Never flip status on intent alone.

---

## Current state (already shipped)

Evidence: code paths in parentheses.

| Piece | Status | Evidence |
|-------|--------|----------|
| SERP HTML import | Done | `KeywordWorkflowService` · `POST /imports/keyword-page` |
| Relevance filter + Rejected | Done | `OperatorRunFocusService.AfterSerpImport` |
| Competitor seed crawl | Done | `CompetitorCrawlService` — one page/domain; `SeedRankAbsolute` |
| Crawl-eligible = Included + PendingReview | Done | `SerpCrawlEligibility` |
| Target crawl + extract | Done | `OperatorRunFocusService` / `PageFetchService.RunTargetSiteFetchAsync` |
| Operator comparison | Done | `ComparisonService.RunOperatorComparisonAsync` |
| `gap_topics` + `writing_instructions` | Done | `SiteProfileAssemblerService.AssembleOperatorRunFocusAsync` |
| Filter counts on import summary | Done | `KeywordWorkflowService` + Web panel |
| Content Writer export v1 (debug) | Done | `ContentWriterKeywordBundleBuilder` — mirrors `sa2`; not handoff transport |
| Research-focus gates | Done | `OperatorResearchService.GetResearchFocusAsync` |
| Domain Overview panel | Shipped but **out of Frase scope** | Demote in UI |

---

## Research-ready gates

Operator source of truth: Site Analyzer `research-focus`. Geek-SEO document create should enforce the same checks. See [INTEGRATIONS.md](../INTEGRATIONS.md#research-ready-gates).

### Site Analyzer — `research-focus` (operator source of truth)

`GET /analysis-runs/{id}/research-focus` · [`OperatorResearchService`](../src/SiteAnalyzer2.Services/Integrations/OperatorResearchService.cs)

| Gate id | Label | `researchReady` requires |
|---------|--------|--------------------------|
| `serp` | SERP import | ≥1 organic `serp_items` |
| `target_crawl` | Target-site crawl | target `page_headings` exist |
| `competitor_crawl` | Competitor crawl | ≥1 `competitor_pages` |
| `comparison` | Comparison findings | ≥1 `findings` row |
| `gaps` | Gap topics assembled | `gap_topics` non-empty |

**Operator rule:** Wait until `researchReady: true` before opening Content Writer (`?analysisRunId=…` only).

### Geek-SEO document create

`ResearchBackedWriteGate` · `ContentWriterHandoffService` — should validate the same richness as `research-focus` before create. Implementation lives in Geek-SEO; operators still follow SA2 gates first.

### Legacy Geek-SEO in-repo wizard (not SA2 operator path)

`Geek-SEO/plan-documents/SITE-ANALYZER-CONTENT-WRITING.md` describes a stricter `SiteAnalyzerPackValidator` on `seo_url_research`. That path is separate from `site-analyzer.geekatyourspot.com` → `analysisRunId` handoff. See [INTEGRATIONS.md](../INTEGRATIONS.md).

---

## Frase definition of done

For a keyword run, an operator can:

1. Import SERP → see filter breakdown (included / rejected / crawl-eligible).
2. Run competitor crawl → ~1 page per domain.
3. See **Research ready** when all SA2 gates pass (`research-focus`).
4. Review **gap themes** + **writing brief** in the research panel (not a duplicate outline editor in SA2).
5. Open Content Writer (`analysisRunId` only); Writer reads `sa2` — SERP, competitors with headings, target headings, `gap_topics`, `writing_instructions`, benchmarks.
6. Writer Insights shows SERP overview, PAA, writing brief; Writer derives outline/section hints via `ContentWriterSerpExportMapper` → `ArticlePromptBuilder`.

**Verify with evidence:** steps 5–6 via `research-focus` + Writer document with populated Insights — not doc claims alone.

## Content quality bar (SEO + AEO)

Frase does not ship Surfer-style scores. The bar for the pillar page is:

> **Does this page fully answer the pillar better than the seed pages?**

Supporting criteria:

| Criterion | Meaning |
|-----------|---------|
| **Answer the pillar** | Covers intent, PAA, and competitor themes — not keyword stuffing |
| **AEO-ready structure** | Clear definitions, FAQ blocks, scannable headings AI can quote |
| **Citations & named sources** | Claims backed by identifiable sources; Writer applies via Sources/Citations |
| **Prudent AI** | Human edits facts, tone, differentiators |

SA2 encodes this in `writing_instructions` + rich export facts. Writer enforces via prompts, scoring, and Insights.

**Rankings loop** (later) measures position delta after publish.

---

## Division: SA2 pack vs Writer authoring

| Site Analyzer (`sa2`) | Content Writer (`geek_seo`) |
|------------------------|-----------------------------|
| SERP, crawls, comparison, `gap_topics`, export | Internal outline step |
| `writing_instructions` factual brief | Draft HTML |
| `sourceHeadings`, `competitors[].headings` | Section hints, competitor patterns in prompts |
| Benchmarks | Target word/H2 heuristics |
| — | Content score, Sources, Citations, meta, JSON-LD (×2), Insights rail |

---

## File map (Frase phase — no ContentOutline)

| File | Responsibility |
|------|----------------|
| `src/SiteAnalyzer2.Services/Integrations/OperatorResearchService.cs` | Research-ready gates (existing) |
| `src/SiteAnalyzer2.Services/Integrations/ContentWriterKeywordBundleBuilder.cs` | Pack export v1 (existing) |
| `src/SiteAnalyzer2.Services/ProfileAssembly/SiteProfileAssemblerHelpers.cs` | Enrich `writing_instructions` with quality bar (optional) |
| `src/SiteAnalyzer2.Web/src/app/page.tsx` | Pack stats in research panel; demote Domain Overview |
| `docs/INTEGRATIONS.md` | Export contract + gate table |
| `docs/OPERATOR-WORKFLOW.md` | Operator steps aligned to gates |

**Explicitly not in scope:** `ContentOutline` models, `ContentOutlineJson`, bundle v2 for outlines.

---

## Task 1: Pack contract documentation

**Files:** `docs/INTEGRATIONS.md`, `docs/HANDOFF.md`

- [x] **Step 1:** Field-by-field export table with Geek-SEO consumer (`ContentWriterSerpExportMapper`).
- [x] **Step 2:** Gate resolution section (SA2 vs Writer vs legacy wizard).
- [x] **Step 3:** Remove stale “often empty today” where operator path populates fields after gates pass.
- [x] **Step 4:** Commit — `docs: content writer export contract and gate resolution`

---

## Task 2: Writing brief quality (optional code)

**Files:** `SiteProfileAssemblerHelpers` or assembly path

- [x] **Step 1:** Append content quality bar to `writing_instructions` when assembling run focus.
- [x] **Step 2:** Unit test — instructions reference pillar + citations expectation.
- [ ] **Step 3:** Verify via export on a run with `researchReady: true`.
- [x] **Step 4:** Commit — `feat(frase): enrich writing_instructions with quality bar`

---

## Task 3: Operator UI — research pack panel

**Files:** `src/SiteAnalyzer2.Web/src/app/page.tsx`

- [x] **Step 1:** Show pack stats when gates pass: PAA count, competitor page count, heading count, gap topic list (`ResearchPackStatsDto` on research-focus).
- [x] **Step 2:** Label panel **Research pack** — not “Content outline.”
- [x] **Step 3:** Demote `DomainOverviewPanel` — collapsible “Domain positions (optional).”
- [x] **Step 4:** Disable **Open Content Writer** when `!researchReady`.
- [ ] **Step 5:** Manual verify on production after deploy.
- [x] **Step 6:** Commit — `feat(frase): research pack panel; demote domain overview`

---

## Task 4: Pack enrichment (optional v1.1 — coordinate Geek-SEO)

Only if Writer consumes new fields:

- [x] `citationCandidates` from organic URLs + `authorityPageUrls`
- [x] Competitor FAQ schema signal (`hasFaqSchema` on `competitors[]`)
- [x] Document in `INTEGRATIONS.md`; bundle version unchanged (additive fields)

---

## Task 5: Documentation sync

**Files:** `docs/OPERATOR-WORKFLOW.md`, `docs/RESEARCH-MODEL.md`, `docs/PLAN.md`, `docs/PRODUCT-PHASES.md`, `docs/decisions/014-product-phases-frase-first.md`, `docs/plans/fix-site-analyzer-research.md`, `docs/decisions/README.md`

- [x] Apply **evidence before status** rule everywhere.
- [x] Mark `fix-site-analyzer-research` todos done where code exists.
- [x] Point active work here; remove outline references.
- [x] Commit — `docs: writer-centric frase phase and gate resolution`

---

## Task 6: ADR 010 competitor crawl note

**Files:** `docs/decisions/010-competitor-crawl-planned.md`

- [x] Amendment — operator path is **seed-only** (no BFS).

---

## Testing checklist (Frase phase)

```bash
dotnet test tests/SiteAnalyzer2.Tests/SiteAnalyzer2.Tests.csproj --filter "OperatorResearch|ContentWriter"
cd src/SiteAnalyzer2.Web && npm run build
```

**Manual E2E (evidence)**

1. SERP import → filter counts visible
2. Competitor crawl → N pages ≈ N domains
3. `GET …/research-focus` → all gates true, `researchReady: true`
4. `GET …/content-writer-export` → `competitors[].headings`, `gapTopics`, `sourceHeadings` non-empty
5. Content Writer handoff → Insights rail populated; outline/draft/score run in Writer

---

## Out of scope (Frase phase)

| Item | Phase |
|------|-------|
| `ContentOutline` in Site Analyzer | Never — Writer owns |
| Bundle v2 for outlines | Never |
| SERP position delta | Rankings loop |
| Surfer-style content score in SA2 | Surfer (Writer today) |
| Domain Overview expansion | Deferred |
| Geek-SEO gate tightening | Done — `ResearchBackedWriteGate` matches SA2 gates ([Geek-SEO `2b8c439`](https://github.com/jmartinemployment/Geek-SEO)) |

---

## Success metrics (Frase — not rankings)

| Metric | Evidence |
|--------|----------|
| Pack complete | Export JSON fields populated on `researchReady` run |
| Operator handoff | Gates green before Writer link |
| Writer usable | Insights + draft without competitor tab hoarding |
| No drift | No Semrush-dashboard or SA2 outline tasks in Frase PRs |

Rankings loop metrics defined in a **separate plan** after Frase ships.
