# Niche Analyzer — Artifact Paradigm & Human-in-the-Loop Workflow

**Status:** Target design (June 2026). Manual pipeline UI still exposes per-step **Run** (execute-on-click); this doc is the north star for troubleshooting, API shape, and AI-suggestion timing.

**Related:** [`SEARCH-UNDERSTANDING-LAYER.md`](SEARCH-UNDERSTANDING-LAYER.md), [`SITE-NICHE-ANALYZER.md`](SITE-NICHE-ANALYZER.md), [`NICHE-SCALABLE-PERSISTENCE.md`](NICHE-SCALABLE-PERSISTENCE.md), [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) (Step 1 testing status)

---

## Locked decisions (do not regress)

Agreed June 2026 — treat as product/architecture constraints until explicitly revised:

1. **Default = read DB.** UI and downstream steps show persisted artifacts. Troubleshooting starts in stored state, not “did Run hit the network?”
2. **Execute = gated mutation.** Re-fetch, recompute, or vendor call only on **first populate** or **user-approved** patch/refresh — not casual navigation.
3. **Step 1 suggestions = Tier A only** until Step 7+ artifacts exist (schema hygiene, extraction quality, JSON-LD on-site fixes). No pillar strategy, keywords, gaps, or authority advice at Step 1.
4. **Approve → patch or execute.** Many corrections patch `niche_profile_schema_signals` without homepage re-fetch. Live re-extract is the exception when site JSON-LD changed or parser was fixed.
5. **One authoritative store per step.** Relational tables for extraction; step run row for status/summary only; vendor cache is global/internal — never user-facing as a third paradigm.
6. **Reset must wipe profile step artifacts** but **never** vendor cache (`seo_serp_results`, `seo_keyword_vendor_snapshots`).
7. **Invalidation is explicit.** Upstream artifact change marks downstream `pending`/`stale`; no silent blend of old fusion + new schema.

---

## One rule (operator mental model)

> **Default: read from the database. Execute only to populate empty artifacts or after an approved change.**

What you see in the UI is the last persisted artifact for each step. Troubleshooting does not require guessing whether a button hit the live site, vendor API, or cache — inspect stored state first.

---

## Target paradigm vs current implementation

| Concern | Target (this doc) | Current code (June 2026) |
|---------|-------------------|---------------------------|
| View step results | `GET` relational artifacts + step status | Same, but UI often tied to Run/poll flow |
| Re-run / Run button | **Execute** only after approval or first fill | **Run** always re-executes that step’s logic |
| Step 1 schema | Read `niche_profile_schema_signals`; fetch live only on approved refresh | Re-run Step 1 always HTTP/Playwright fetch → `ReplaceSchemaSignalsAsync` |
| Downstream steps | Read upstream artifacts from niche DB | Mostly true via `NicheStepRelationalLoader` |
| Vendor steps 8–9 | Read profile enrichment; vendor call only on approved refresh | Re-run calls provider; `DatabaseBacked*` may cache globally |
| Reset | Clear step artifacts + statuses; **never** wipe vendor cache | Reset disabled in UI; incomplete when enabled |
| AI suggestions | Propose → user approves → **patch** or **execute** | Not wired |

Implementation should converge on the target without removing vendor TTL cache — cache stays an **internal** optimization, not a third user-visible paradigm.

---

## Step contract (each of 14 steps)

Every step has exactly one **authoritative store** per profile.

| Step | Authoritative store | Default UI | Execute when |
|------|---------------------|------------|--------------|
| 1 schema | `niche_profile_schema_signals` | Read signals | First populate; approved re-extract; parser version bump |
| 2 site_urls | `niche_profile_discovered_urls` (sitemap rows) | Read URL inventory | Same pattern |
| 3 nav | `niche_profile_navigation_links` | Read nav pillars | Same |
| 4 headings | `niche_profile_headings` | Read outline | Same |
| 5 page_content | `niche_profile_page_content` | Read phrases/sections | Same |
| 6 site_structure | structure + crawl URL rows | Read crawl summary | Same (expensive — avoid casual execute) |
| 7 merging | fusion snapshot + topic candidates | Read fusion | Recompute when steps 1–6 artifacts change |
| 8 keywords | step log artifact + pillar metrics (when persisted) | Read enrichment | Approved refresh; first populate |
| 9 serp_validation | step log + competitors | Read SERP footprint | Approved refresh; first populate |
| 10–14 | profile summary, pillars, scores | Read synthesis | Upstream artifact change |

**Step run row** (`niche_profile_step_runs`): one row per slug — status, summary, timestamps. Metadata only; not the authoritative payload for steps 1–6 (relational tables are).

**Legacy JSON** (`niche_profiles.analysis_step_log`): human-readable log + embedded artifacts for UI; should mirror relational state, not replace it.

---

## Approved change workflow (target)

Three mutation types — only after explicit user approval:

1. **Patch** — write corrected rows to the step’s authoritative store (no live fetch, no vendor). Example: merge duplicate schema service names, fix a mislabeled `knowsAbout` value the user confirms.
2. **Execute** — run step logic (fetch/compute) and replace the store. Example: approved “re-extract schema from live site” after homepage JSON-LD changed.
3. **Invalidate downstream** — mark dependent step statuses stale when an upstream artifact version changes; do not silently mix old fusion with new schema.

Every approval should record: `step`, `action` (patch | execute), `reason`, `approved_at`, optional `artifact_version`.

---

## Invalidation

When step *N* artifact changes:

- Set downstream step statuses to `pending` (or `stale`) in dependency order per `NicheStepCatalog`.
- Downstream **reads** must not blend old and new upstream data.
- **Pending** means “not valid for this analysis pass” — not “empty.” Until execute or patch completes, UI should still show last stored artifact with a **stale** badge if old data remains visible.

Vendor tables (`seo_serp_results`, `seo_keyword_vendor_snapshots`) are **global** and TTL-scoped. Profile reset or artifact clear must **not** delete vendor cache (avoids bypassing retention and re-spend).

---

## Step 1 re-execute — when it is useful

Live schema re-extraction is **not** the default troubleshooting action. It is appropriate when the user approves:

- Site’s JSON-LD changed and stored signals should refresh
- Extractor/parser was fixed and a full re-read is needed
- First-time analysis populate (no signals yet)

Corrections that do not require re-fetch should **patch** `niche_profile_schema_signals` directly after approval.

---

## AI suggestions — what belongs at Step 1 review

### Not too early (good at Step 1)

These depend only on stored schema signals + raw extraction metadata:

| Category | Examples | Apply via |
|----------|----------|-----------|
| **On-site JSON-LD** | Missing `LocalBusiness` / `Service` types; weak `knowsAbout`; no `areaServed`; `sameAs` gaps | Recommendation for site implementer (no pipeline execute) |
| **Extraction quality** | Duplicate topics, normalization (e.g. “IT Support” vs “it support”), parser missed obvious `@type` fields | Patch stored signals after approval, or approved re-extract |
| **Signal inventory audit** | “We extracted N topics from M JSON-LD blocks”; list sources (`service`, `knowsAbout`, `offer_catalog`) | Read-only; builds trust in stored artifact |

### Too early (defer until later steps)

| Category | Wait for | Why |
|----------|----------|-----|
| Pillar prioritization / “focus on X niche” | Step 7 `merging` | Pillars are a fusion outcome, not raw schema |
| Keyword targets, volume, KD | Step 8 | Vendor + pillar context |
| SERP competitiveness, demotions | Step 9 | SERP validation |
| Content gaps, quick wins | Step 12 `coverage` | Needs pillar list + URL match |
| Authority score narrative | Step 13 | Composite |

### Middle ground — label clearly

Suggestions like “you declare 15 services but nav only exposes 4” need **Step 3** or **Step 7** inputs. At Step 1-only, phrase as **hypothesis** (“schema declares X; confirm after nav/merge steps”) or show as blocked until dependencies complete.

---

## Suggestion tiers (product)

| Tier | When | Step 1 examples |
|------|------|------------------|
| **A — Signal hygiene** | Any time Step 1 artifact exists | JSON-LD fixes, dedupe, areaServed |
| **B — Topic strategy** | After Step 7 | Pillar cap, exclusions, GSC silent pillars |
| **C — Content & demand** | After Steps 8–12 | Gaps, keywords, SERP, quick wins |

**Recommendation:** Ship **Tier A** at Step 1 in the manual pipeline. Treat Tier B/C as gated panels that unlock when canonical artifacts exist — avoids wrong advice and matches the read-from-DB paradigm.

---

## Troubleshooting checklist (single paradigm)

1. Load authoritative store for the step (relational table or fusion snapshot).
2. Check step run row: `status`, `error_message`, `completed_at`.
3. If downstream looks wrong, verify upstream artifact `completed_at` / stale flags — not whether someone clicked Run.
4. Only if artifact is empty or user approved refresh → execute.
5. Vendor spend: check global cache row + TTL before assuming a re-run billed API units.

---

## Migration notes (implementation backlog)

- Replace casual **Run** with **View** (default) + **Apply approved change** (patch | execute + reason).
- First project visit: optional auto-**prepare** (create profile + pending steps) without executing extraction.
- Complete **Reset** when re-enabled: clear profile step artifacts + step runs + fusion; preserve vendor cache.
- Artifact versioning field on profile or per-step run for invalidation audit trail.
- Step 1 suggestion API: read `GetSchemaSignalsAsync` only; no live fetch in suggestion path.

---

## Step 1 — testing status (June 2026)

**Ready for manual Step 1 testing** on production after deploys through `110e7cd` (GeekSeoBackend + Vercel + GeekRepository phase-1/2 routes).

| Capability | Status |
|------------|--------|
| Start analysis (14 pending steps, no auto-run) | ✅ Shipped |
| Run Step 1 (`schema`) — live fetch + `ReplaceSchemaSignalsAsync` | ✅ Shipped |
| Single Run click → poll until complete + summary | ✅ Shipped (`dba8c15`) |
| Background run-step user context (no silent 401) | ✅ Shipped (`6cce52d`) |
| Step run row + summary/error on status API | ✅ Shipped |
| Relational `niche_profile_schema_signals` persist | ✅ Shipped (GeekRepository `a17411a+`) |
| Reset analysis | ❌ Hidden — incomplete artifact wipe |
| View-from-DB without Run (target UX) | ❌ Not yet — Run still executes |
| Tier A AI suggestions at Step 1 | ❌ Not built |
| Approve → patch signals API | ❌ Not built |

### Manual test script (Step 1)

1. Open `/app/strategy/niche-analyzer` on `seo.geekatyourspot.com`.
2. Select or create a project with **default location** set (required).
3. **Start analysis** — expect 14 steps `pending`, profile status `pending`/`processing`.
4. On Step 1 **Schema.org**, click **Run** once — wait for **complete** (not double-click).
5. Expect summary like “Found N schema topic(s)…” and expandable step outputs (services, knowsAbout, areaServed) when analysis-details loads.
6. Step 2 should remain runnable; Step 7 stays blocked until steps 1–6 complete.

### Known gaps while testing

- **Run** still re-fetches live homepage (interim UX until approval-gated execute).
- Step outputs come from **analysis step log** after run; dedicated read-only schema-signals panel not yet in UI.
- Playwright is **not** used for manual `run-step` (`browser=null`); HTTP JSON-LD parse only unless full worker pipeline.

*Last updated: 2026-06-14*
