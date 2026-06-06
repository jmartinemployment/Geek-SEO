# Site Niche Analyzer — change plan (v1.5)

**Scope:** Changes to the **Site Niche Analyzer orchestrator** only — how a scan runs, step order, SignalR/progress, and what gets persisted on the profile from the pipeline.

**Reference (v1 shipped spec, do not duplicate here):** [`SITE-NICHE-ANALYZER.md`](./SITE-NICHE-ANALYZER.md)

**Dogfood:** [`docs/reference/geekatyourspot-niche-baseline.md`](../docs/reference/geekatyourspot-niche-baseline.md)

**Ship this plan as Phase 1 only** (three build steps). Do not start step abstraction or step-log wiring in the same session.

---

## In scope (this plan — Phase 1)

| Area | Files |
|------|--------|
| Orchestrator | `GeekSeoBackend/Services/NicheAnalyzerService.cs` |
| Background job | `GeekSeoBackend/Services/NicheAnalysisBackgroundJob.cs` (only if step slugs touch it) |
| Profile save from pipeline | Remove `discoveryMethod`; align step slugs with UI/SignalR |
| Progress | `TotalSteps = 10`, canonical slugs below, no unused step 9 |

**Not in this plan** — each gets its **own** plan when you build it:

- `INicheScanStep` refactor → **separate plan** (Phase 2; do not start here)
- Step log persistence / `INicheStepLogWriter` → **NicheStepLog plan**
- `PillarMerger` / `PillarValidator`
- `SchemaOrgExtractor`, `SitemapExtractor`, `NavMenuExtractor`, `HomepageHeadingsExtractor`
- `NicheAuthorityScorer` (formula changes)
- Site URL crawl (`SiteUrlDiscoveryService`)
- Coverage matcher, local gap generator, analysis-details API
- `NicheAnalyzerController` (unless a one-line enqueue change falls out of orchestrator work)
- Frontend components (update listener labels only if step slugs change in Phase 1)
- SERP, keywords, GBP, entity gap, sitemap publish

---

## Problem (v1 orchestrator)

| Issue | Why it's wrong |
|-------|----------------|
| `BuildHeadingsFromSchema` when headings are empty | The headings step must reflect what the extractor actually found; schema substitution corrupts the per-source audit trail and hides a failed/empty homepage parse. |
| `DetermineDiscovery` + `discoveryMethod` on profile | v1 used one label to mean “which path won”; v1.5 runs every path and unifies at merge — a single discovery method misstates how the scan worked. |
| Step slugs `validating`, `saving` | UI and SignalR should match the canonical 10-step model (`profile`, `local`, `coverage`, `scoring`, `complete`) so progress is honest per phase. |
| Unused step 9; no `local` / `coverage` in pipeline | Step numbers and slugs should match the target model; gaps can no-op until sibling components exist — but slots must exist in order and progress. |
| Progress copy implying fallback | e.g. “using sitemap and headings” when schema is empty — reads as substitution; copy should describe what **ran**, not what replaced what. |

---

## Target — 10 scan steps (orchestrator owns order + progress only)

| # | Slug | Orchestrator responsibility (Phase 1) |
|---|------|-------------------------------------|
| 1 | `schema` | Call `SchemaOrgExtractor`; push progress |
| 2 | `site_urls` | Call existing sitemap path (crawl service is a later plan) — **not** profile `discoveryMethod` |
| 3 | `nav` | Call `NavMenuExtractor` if browser available |
| 4 | `headings` | Call `HomepageHeadingsExtractor`; **never** overwrite with schema |
| 5 | `merging` | Call `PillarMerger.Merge` with all four lists (merger behavior = PillarMerger plan) |
| 6 | `profile` | Root entity, niche string, audience type (today’s post-merge block) |
| 7 | `local` | Push progress; **no business logic** until LocalGapGenerator plan |
| 8 | `coverage` | Push progress; **no business logic** until NicheCoverageMatcher plan |
| 9 | `scoring` | Call `NicheAuthorityScorer`; persist counts/score |
| 10 | `complete` | Final status + `next_analysis_due` |

Phase 1 does **not** implement steps 2/7/8 beyond slug + progress — no new services, no stubs with TODO behavior.

---

## Phase 1 — build order (one Claude Code prompt)

| Step | Work | Verify |
|------|------|--------|
| 1 | Replace step slugs + `PushProgress` / `UpdateStatusAsync` to canonical 10; fix `TotalSteps`; update frontend listener labels if slugs changed | SignalR shows 1–10 with new names |
| 2 | Delete `BuildHeadingsFromSchema`, `DetermineDiscovery`; stop exposing `discoveryMethod` on profile API/UI; **still send empty `discoveryMethod` on save** until GeekRepository drops the column | Grep clean; analysis completes in prod |
| 3 | Reorder pipeline: after merge → `profile` → `local` (progress only) → `coverage` (progress only) → `scoring` → persist → `complete` | Step numbers monotonic; no `validating`/`saving` slugs |

**Do not in Phase 1:** introduce `INicheScanStep`, new step classes, step-log interfaces, migrations, or placeholder services for local/coverage/crawl.

---

## Phase 2 — separate plan (not this document)

When ready, write **`SITE-NICHE-ANALYZER-SCAN-STEPS.md`** (or equivalent) for:

- `INicheScanStep` + DI registration
- One thin class per step wrapping existing calls
- No new business logic inside step classes

That refactor has its own blast radius — keep it out of the cleanup PR.

---

## Delete from orchestrator (grep checklist)

- `BuildHeadingsFromSchema`
- `DetermineDiscovery`
- `discoveryMethod` on **profile API/UI** (and `DetermineDiscovery` logic)
- User-facing progress text that frames sitemap/headings as **fallback** when schema is empty

---

## Out of scope — do not add to Phase 1 PR

- `INicheScanStep` / step class extraction
- `INicheStepLogWriter` / `analysis_step_log`
- Merger algorithm, gates, synonym map → **PillarMerger plan**
- Crawl BFS, 150-page index → **SiteUrlDiscovery plan**
- URL ↔ pillar matching → **NicheCoverageMatcher plan**
- Counties/cities matrix → **LocalGapGenerator plan**
- UI step breakdown panel → **frontend plan**

---

## Cross-repo dependency (Phase 1)

`PATCH repo/seo/niche-profiles/{id}/analysis-results` in **GeekRepository** still requires `discoveryMethod` in the JSON body (`SaveNicheAnalysisResultsRequest`). Phase 1 removes it from the **read** API and UI only; the orchestrator sends `""` on save until GeekRepository makes the field optional or the column is dropped.

**Before deploy:** run one niche analysis against staging/production and confirm status reaches `complete` (not failed at step 9 save).

---

*Last updated: 2026-06-06*
