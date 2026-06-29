# Geek-SEO / Content Writer integration

## Division of responsibility

| System | Schema | Owns | Does not own |
|--------|--------|------|----------------|
| Site Analyzer | `sa2` | SERP, crawls, comparison, `gap_topics`, site profile | Draft HTML, outlines, content score, Sources/Citations apply, JSON-LD in editor |
| Geek-SEO Content Writer | `geek_seo` | Outline step, draft, score, Insights, Sources, Citations, meta, JSON-LD (×2) | SERP facts, competitor crawl, live SERP refresh on research-backed docs |

Content Writer **must not** be the system of record for SERP or competitor crawl data.

## How research reaches Content Writer

**Canonical handoff:** [HANDOFF.md](HANDOFF.md)

1. Operator opens Content Writer with **one** query param: `analysisRunId` (= `analysis_runs.Id` = `RunId` on child tables).
2. Geek-SEO creates a document with that `analysisRunId` and `projectId` from `analysis_runs.ProjectId`.
3. When Writer drafts, scores, or shows Insights, Geek-SEO **reads `sa2`** via `SITE_ANALYZER2_DATABASE_URL`.

No research JSON is copied onto `seo_content_documents` at create. No `site_profile`, `keyword`, or `projectId` in the handoff URL.

Site Analyzer does not read `geek_seo`.

### Legacy (do not use for handoff)

| Pattern | Status |
|---------|--------|
| URL with `site_profile`, `keyword`, `projectId` | Invalid — use `analysisRunId` only |
| `KeywordBundleJson` / `SiteFocusJson` on documents | Legacy snapshot — remove from create path |
| `ContentWriterHandoffService.FreezeAsync` | Legacy — validate run + read `sa2` instead |
| `GET …/content-writer-export` / `content-writer-bundle` at create | Operator/debug only — not Writer handoff |

## Research-ready gates

### Site Analyzer operator UI (source of truth)

`GET /analysis-runs/{id}/research-focus` · `OperatorResearchService.GetResearchFocusAsync`

`researchReady` is true only when **all** of: organic SERP, target `page_headings`, `competitor_pages`, `findings`, non-empty `gap_topics`.

**Operator rule:** Do not open Content Writer until `researchReady: true`.

### Geek-SEO document create

`ResearchBackedWriteGate.ValidateAnalysisRunExport` should match the same bar as `research-focus` (run not failed, organic SERP, `sourceHeadings`, competitor headings, `gapTopics`).

Documents created under older code may have thin or missing validation — operators still follow Site Analyzer gates.

### Legacy Geek-SEO in-repo wizard

`Geek-SEO/plan-documents/SITE-ANALYZER-CONTENT-WRITING.md` — `SiteAnalyzerPackValidator` on `seo_url_research`. **Not** the `site-analyzer.geekatyourspot.com` operator path.

## Resolving rows from `analysisRunId`

| Writer needs | `sa2` source |
|--------------|--------------|
| Keyword, status, gap topics, pillar fields | `analysis_runs` WHERE `Id` = run id |
| Geek-SEO project | `analysis_runs.ProjectId` |
| Organic SERP, PAA, related searches | `serp_items` WHERE `RunId` = run id |
| Competitor pages and headings | `competitor_pages` (+ headings) WHERE `RunId` = run id |
| Target page headings | `pages` / `page_headings` for that run |
| Site niche, geo, business summary | `site_profiles` WHERE `GeekSeoProjectId` = `analysis_runs.ProjectId` |

When linkage is correct, `site_profiles.GeekSeoProjectId` and `analysis_runs.ProjectId` are the same Geek-SEO project UUID.

## Research fields — `sa2` → Writer

Geek-SEO maps these when loading research (e.g. `ContentWriterSerpExportMapper`, `SiteWritingFocusFromBundlesMapper`). Sources are **tables**, not export JSON.

| Concept | `sa2` source | Geek-SEO consumer |
|---------|--------------|-------------------|
| Keyword | `analysis_runs.keyword` | `DerivedKeyword` / `SerpKeyword` |
| SERP (organic, PAA, related, AI overview) | `serp_items` | Organic list, Insights, citations |
| Competitors | `competitor_pages` + headings | Section hints, competitor context |
| Target headings | `page_headings` (target site) | Brief vs competitor structure |
| Gap topics | `analysis_runs.gap_topics` | Site focus, draft gaps |
| Writing brief | `analysis_runs.writing_instructions` | Insights, draft prompt |
| Pillar metadata | `analysis_runs` matched pillar fields | Intent framing |
| Benchmarks | computed from competitor headings | Word/H2 heuristics |
| Site business / geo | `site_profiles` | `SiteWritingFocus` |
| Citation seeds | organic URLs + `site_profiles.authority_page_urls` | Sources pipeline |

**Verification:** After `research-focus` is green, spot-check the run in SQL or `GET …/content-writer-export` (debug) — `gap_topics`, target headings, and competitor headings must be populated.

## What Content Writer does with research

| Writer surface | Data source |
|----------------|-------------|
| Insights rail | Live read from `sa2` by `analysisRunId` |
| Outline step | `ArticlePromptBuilder` — section hints from mapped research (Writer-owned outline) |
| Draft | Run brief, gap topics, recommended terms |
| Score sidebar | Terms, meta, citations |
| Sources / Citations | Organic URLs from `serp_items` — no live SERP on research-backed docs |
| JSON-LD panel | Article + FAQPage from render pipeline |

## Linking projects

`site_profiles.geek_seo_project_id` is set at SERP import / link time. `analysis_runs.ProjectId` uses the same UUID when the operator path is linked (`EnsureGeekSeoProjectRowAsync`).

## Documentation rule

When updating this file, cite **code path + verification** before claiming a field is populated or a gate is enforced. See [plans/frase-phase.md](plans/frase-phase.md#documentation-rule-evidence-before-status).

## Related docs

- [HANDOFF.md](HANDOFF.md)
- [RESEARCH-MODEL.md](RESEARCH-MODEL.md)
- [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md)
- [plans/frase-phase.md](plans/frase-phase.md)
