# Site Niche Analyzer — change plan (v1.5)

**Scope:** Orchestrator + what gets persisted and shown for each scan.

**Reference (v1 shipped spec):** [`SITE-NICHE-ANALYZER.md`](./SITE-NICHE-ANALYZER.md)  
**Dogfood:** [`docs/reference/geekatyourspot-niche-baseline.md`](../docs/reference/geekatyourspot-niche-baseline.md)

---

## Phase status

| Phase | Status | One-line goal |
|-------|--------|----------------|
| **1** | Shipped (`a649e16`, `fac20ea`) | Canonical 10 step slugs; no `discoveryMethod` on read API/UI |
| **1.5** | Implemented (local) | Persist + show what each step found (no re-analyze to audit) |
| **2** | Later | `INicheScanStep` refactor only — no new behavior |
| **Other** | Separate plans | PillarMerger, crawl, coverage matcher, local/GBP, keywords |

---

## Phase 1.5 — step log (implemented locally)

**Shipped in workspace:** migration `20260606120000_AddNicheProfileAnalysisStepLog`, GeekRepository SQL `0008`, `GET …/analysis-details`, `AnalysisStepBreakdown.tsx` on results + live poll during analyze.

**Deploy:** Run SQL `0008` on production if EF auto-migrate does not apply; redeploy GeekRepository + GeekSeoBackend + frontend.

**Problem:** Step messages go out on SignalR only. After complete (or refresh), the UI shows generic labels or pillars — not what each step found. Users must **Re-analyze** to see discovery detail again.

**Rule:** One write path, one read path. **Do not** build a throwaway SignalR-only step list in the frontend.

### Build order (3 steps)

| Step | Work | Verify |
|------|------|--------|
| 1 | Migration: `analysis_step_log JSONB` on `niche_profiles` (GeekSeo.Persistence + GeekRepository). Orchestrator appends one entry per step when that step finishes. | Row exists after run; failed runs retain partial log |
| 2 | `GET /api/seo/niche-analyzer/{profileId}/analysis-details` — returns `{ stepLogVersion, steps[] }` | 200 for complete/failed; owner-only |
| 3 | UI: collapsible **“How this scan worked”** on niche analyzer results (reads analysis-details). Live progress may still use SignalR; **after** complete, panel is the source of truth | geekatyourspot.com run shows all 10 without re-analyze |

### Step log entry shape

```json
{
  "stepNumber": 1,
  "slug": "schema",
  "title": "Schema.org",
  "status": "complete",
  "summary": "Found 7 topics from schema.org.",
  "outputs": { }
}
```

- **`summary`** — same text as today’s `PushProgress` message (single source; generate once, persist + SignalR).
- **`outputs`** — structured payload per step (table below). Cap list lengths in v1 (e.g. 20 items) to keep JSONB small.

### What each step persists (`outputs`)

| # | Slug | `outputs` (minimum) |
|---|------|---------------------|
| 1 | `schema` | `serviceNames[]`, `areaServed[]`, `description`, `extractMethod` |
| 2 | `site_urls` | `totalUrls`, `sampleUrls[]`, `pillarCount` |
| 3 | `nav` | `extractMethod`, `pillarCount`, `sampleLabels[]` |
| 4 | `headings` | `title`, `headingCount`, `sampleHeadings[]` |
| 5 | `merging` | `candidateCount`, `mergedCount`, `samplePillarNames[]` |
| 6 | `profile` | `primaryNiche`, `audienceType`, `nicheTags[]` |
| 7 | `local` | `enabled: false`, `message` (until LocalGapGenerator plan) |
| 8 | `coverage` | `enabled: false`, `message` (until NicheCoverageMatcher plan) |
| 9 | `scoring` | `authorityScore`, `covered`, `partial`, `gap`, `pillarCount` |
| 10 | `complete` | `analyzedAt`, `nextAnalysisDue` |

Dogfood expectations: [`geekatyourspot-niche-baseline.md`](../docs/reference/geekatyourspot-niche-baseline.md) § Expected output.

### Files (Phase 1.5 only)

| Layer | Files |
|-------|--------|
| Persistence | `GeekSeo.Persistence` entity + migration; GeekRepository repo + internal PATCH if needed |
| Orchestrator | `NicheAnalyzerService.cs` — write log entry in `PushProgress` (or helper called from there) |
| API | `NicheAnalyzerController.cs` — `analysis-details` |
| Frontend | New `AnalysisStepBreakdown.tsx`; wire on `niche-analyzer/page.tsx` when `profile.status === 'complete' \|\| 'failed'` |

**Not in Phase 1.5:** `INicheScanStep`, `INicheStepLogWriter` abstraction, crawl, merger changes, local/coverage logic.

**Before deploy:** Complete one analyze on production; open results without Re-analyze; confirm 10 rows match baseline.

---

## Phase 1 — shipped

Canonical 10 steps, slugs aligned with UI/SignalR, `discoveryMethod` off read API/UI, empty string on save for GeekRepository.

**Cross-repo:** `PATCH …/analysis-results` still requires `discoveryMethod` in JSON until GeekRepository drops it.

---

## Phase 2 — scan step refactor (later, separate PR)

`INicheScanStep` + thin wrappers around existing extractors. **No** new behavior; step log writes stay in orchestrator or move with steps only after 1.5 is stable.

Do **not** write `SITE-NICHE-ANALYZER-SCAN-STEPS.md` until Phase 1.5 ships.

---

## Target — 10 scan steps (orchestrator order)

| # | Slug | Runs today |
|---|------|------------|
| 1 | `schema` | `SchemaOrgExtractor` |
| 2 | `site_urls` | `SitemapExtractor` (crawl = later plan) |
| 3 | `nav` | `NavMenuExtractor` if browser |
| 4 | `headings` | `HomepageHeadingsExtractor` — no schema substitution |
| 5 | `merging` | `PillarMerger.Merge` (all four lists) |
| 6 | `profile` | Root entity, audience, tags |
| 7 | `local` | Progress only until LocalGapGenerator |
| 8 | `coverage` | Progress only until NicheCoverageMatcher |
| 9 | `scoring` | `NicheAuthorityScorer` + persist pillars/scores |
| 10 | `complete` | Final status + `next_analysis_due` |

---

## Out of scope (own plans when needed)

PillarMerger algorithm, SiteUrlDiscovery crawl, NicheCoverageMatcher, LocalGapGenerator, GBP OAuth, keywords/SERP, step-class extraction before Phase 2.

---

*Last updated: 2026-06-06*
