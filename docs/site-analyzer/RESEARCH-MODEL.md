# Site Analyzer research model

How operator research works and how to document it accurately. Use with [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md), [HANDOFF.md](HANDOFF.md), and [INTEGRATIONS.md](INTEGRATIONS.md).

## Evidence before status

When updating **implementation status** in this file or linked plans:

1. Cite the **code path** (service + method).
2. Cite **verification** (test name, `research-focus` gate, or export JSON on a real run).

Do not mark **Wired** / **Done** / **empty** from intent alone. If production disagrees with docs, fix docs or code — whichever is wrong.

## North star (SRP)

| Layer | Owns |
|-------|------|
| **Site Analyzer (`sa2`)** | SERP, target-site pages, competitor pages, site profile, run focus (`gap_topics`, pillar metadata) |
| **Geek-SEO Content Writer (`geek_seo`)** | Documents, internal outlines, drafts, scores, Sources/Citations, JSON-LD — reads `sa2` when needed |

## Operator workflow (intended)

For each **Project URL** (e.g. `https://www.geekatyourspot.com/`):

1. **Create site profile** — fetch homepage once; business identity, site themes, writing recommendations (visual).
2. **Per keyword** — save **one** Google SERP HTML → `serp_items` + `analysis_runs.keyword`.
3. **Target-site crawl** (required) — crawl Project URL on that run → `pages` + `page_headings` (H2–H6).
4. **Competitor crawl** (required) — SERP seeds → `competitor_pages` + `competitor_page_headings`.
5. **Comparison** (required) — target vs competitors → `findings`.
6. **Run focus assembly** — populate `gap_topics`, `writing_instructions`, pillar metadata on `analysis_runs`.
7. **Handoff** — open Content Writer with `analysisRunId` only when `research-focus` reports ready. See [HANDOFF.md](HANDOFF.md).

Nothing in the SA2 crawl list is optional for **research-ready** in `research_mode = sa2`.

For **manual five-lane** runs (`research_mode = manual`), only HTML lane imports are required — see [MANUAL-FIVE-LANE-RESEARCH.md](MANUAL-FIVE-LANE-RESEARCH.md).

## What is a “pillar”?

**Pillar = the keyword for that run** (`analysis_runs.keyword`), i.e. the Google search you saved as HTML.

It is **not**:

- `niche_tags` on `site_profiles`
- The “Existing pillars detected…” line in `writing_recommendations` (that is homepage `/use-cases/` link labels)

**Content pillars** for a site = the list of distinct `analysis_runs.keyword` values for that Project URL.

## Gap topics — definition

`analysis_runs.gap_topics` answers: *what should this keyword article cover?*

Built from (**all required**):

| Source | Data |
|--------|------|
| Pillar anchor | `analysis_runs.keyword` |
| Your structure | Target-site **H2–H6** (`page_headings`, `is_target_site`) |
| Competitor structure | **H2–H6** (`competitor_page_headings`) |
| Comparison | [`ComparisonService`](../src/SiteAnalyzer2.Services/Pipeline/ComparisonService.cs) → `findings` (FAQ gaps, heading depth, schema, etc.) |
| SERP (optional add-on) | PAA / related searches not already covered |

**Not gap topics:** copying `business_type`, `geo_anchor_nodes`, or other `site_profiles` fields as separate bullets. At most one short business framing line may prefix the brief.

Writer turns `gapTopics` into draft reinforcement and site focus — not a separate SA2 outline.

### Implementation status

| Piece | Status | Evidence |
|-------|--------|----------|
| SERP persist | Works | `KeywordWorkflowService` |
| SERP relevance filter after import | Wired | `OperatorRunFocusService.AfterSerpImport` |
| Competitor crawl + headings | Works | `CompetitorCrawlService` |
| Target crawl on operator path | Wired | `OperatorRunFocusService` / `PageFetchService.RunTargetSiteFetchAsync` |
| Comparison using `competitor_pages` | Wired | `ComparisonService.RunOperatorComparisonAsync` |
| `gap_topics` population | Wired | `SiteProfileAssemblerService.AssembleOperatorRunFocusAsync` |
| `niche_tags` keyword injection | Fixed | Homepage assembly no longer injects run keywords |
| Research-ready gates | Wired | `OperatorResearchService.GetResearchFocusAsync` |

## Field glossary (`site_profiles`)

| Column | Meaning today | UI label (target) | Notes |
|--------|---------------|-------------------|--------|
| `site_url` | Project URL | Project URL | Canonical identity |
| `primary_niche` | JSON-LD business name/type | **Business identity** | Not a market “niche” |
| `niche_tags` | Homepage headings + JSON-LD | **Site themes** | No run keyword injection |
| `writing_recommendations` | Ops advice (JSON-LD paste, reposition copy) | Writing recommendations | **Keep** for Site Analyzer UI |
| `generated_schema_json` | Often NULL | — | Lives on `target_site_business_profiles` after Extract; sync planned |
| `authority_page_urls` | Often empty until assembly | — | From organic `serp_items` after run assembly when wired |

## Field glossary (`analysis_runs`)

| Column | Meaning |
|--------|---------|
| `keyword` | Pillar / Google search query |
| `status` | `Running` → `SerpReady` → `ResearchReady` or `ResearchFailed` / `SerpFailed` |
| `current_stage` | Legacy advance API only; null after operator SERP import |
| `competitor_crawl_status` | `idle` → `running` → `pages_saved` → `complete`; `failed` = zero pages |
| `target_site_url` | Same as Project URL |
| `matched_pillar_topic` | Should equal keyword (or heading match when target crawl exists) |
| `matched_pillar_intent` | SERP heuristic (commercial, informational, local, …) |
| `matched_pillar_angle` | Often first PAA question |
| `gap_topics` | Comparison-backed topics + headings (see above) |
| `writing_instructions` | Factual per-run brief for export — **not** writing_recommendations |

## SERP rows (`serp_items`)

Import **persists all parsed rows** (organic, paid, AI overview, related searches). Rows are not deleted at import.

| Signal | Columns |
|--------|---------|
| Line type | `type`, `ads` |
| Filter / non-competitor for crawl | `filter_status`, `filtered`, `exclude_reason`, `include_reason` |

Relevance filter runs after operator import (`OperatorRunFocusService.AfterSerpImport`). See [ADR 003](decisions/003-relevance-filter.md).

## Content Writer handoff

| Step | What happens |
|------|----------------|
| **URL** | `/content-writing?analysisRunId=<analysis_runs.Id>` only |
| **Create** | Document stores `analysisRunId` + `projectId` from the run row |
| **Write / score / Insights** | Geek-SEO queries `sa2` by `RunId` (and site profile via `ProjectId`) |

No JSON snapshot on the document at create. Full spec: [HANDOFF.md](HANDOFF.md). Gates: [INTEGRATIONS.md](INTEGRATIONS.md#research-ready-gates).

## Wrong assumptions (do not use)

| Assumption | Why wrong |
|------------|-----------|
| Competitor crawl is optional | Required for `research-ready` |
| `niche_tags` = pillars or entered keywords | Pillars = `analysis_runs.keyword` |
| `gap_topics` from site profile fields alone | Must be comparison + headings |
| `AssembleForRunAsync` runs after import | Not API-wired on operator path |
| SA2 should export article outlines | Writer owns outline step |
| Writer create gate should match research-ready | Tighten `ResearchBackedWriteGate` in Geek-SEO to match `research-focus` |

## Resolved on operator path (historical)

These were true gaps; operator wiring shipped in `OperatorRunFocusService` / `AssembleOperatorRunFocusAsync`:

- Comparison on `competitor_pages` (not legacy `pages` only)
- `gap_topics` populated after crawl + comparison
- Target crawl triggered after SERP import

## Implementation plan

Active: [plans/frase-phase.md](plans/frase-phase.md). Historical wiring spec: [plans/fix-site-analyzer-research.md](plans/fix-site-analyzer-research.md) (superseded). ADR: [012-operator-research-model.md](decisions/012-operator-research-model.md).
