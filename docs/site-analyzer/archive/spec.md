# ARCHIVED — superseded by [archive/SiteAnalyzer-PLAN.md](SiteAnalyzer-PLAN.md). Operator workflow: [OPERATOR-WORKFLOW.md](../OPERATOR-WORKFLOW.md).

This file is retained for history only. Do not use for implementation.

---

# SiteAnalyzer — Technical Specification (Phase 0) [archived copy]

> Greenfield .NET 10 project. Existing platform codebase is **out of scope**.

Sections 0A–0E define extraction, SERP, filtering, crawl bounds, and comparison models. Section **0F** locks frontend stack policy, SignalR as the sole real-time channel, and step-gated orchestration for v1.

---

## 0A — JS rendering decision (required before Phase 2)

Evaluate on a **fixed calibration URL set** (document 10–15 URLs spanning React/Next, Angular, Wix, Squarespace, static HTML):

| Signal | Raw HTTP (`HttpClient`) | Headless (Playwright) |
|--------|-------------------------|------------------------|
| Headings H1–H6 present in DOM | count per level | count per level |
| JSON-LD / meta / canonical | present/absent | present/absent |
| Fetch latency & failure rate | ms, errors | ms, errors |

**Decision rule (recommended default unless calibration disproves it)**:

- Use **raw HTTP first** for fetch + parse (AngleSharp or HtmlAgilityPack).
- Escalate to **Playwright** only when the calibration matrix shows ≥2 of: missing H2–H6, missing JSON-LD that headless finds, or empty main content block.
- Persist `fetch_mode` per page (`Http` | `Headless`) for auditability.

Record outcome in ADR `001-fetch-strategy.md`.

---

## 0B — SERP provider strategy

**Multi-provider adapter interface**:

```csharp
public interface ISerpProvider {
    string ProviderKey { get; }
    Task<SerpResult> FetchOrganicResultsAsync(SerpQuery query, CancellationToken ct);
}
```

Concrete adapters v1 (implement ≥2):

- SerpAPI
- DataForSEO
- ValueSERP

Provider selected per project/run via config (`serp_provider_key`). Normalize to internal model: `position`, `url`, `title`, `snippet`, `domain`.

Record adapter contract in ADR `002-serp-providers.md`.

---

## 0C — Relevance filter spec (four buckets — stress-test before Phase 0 closes)

Define deterministic rules (no ML v1). Filter precedence is strict top-to-bottom; first match wins.

**Bucket 1 — Auto-exclude** (`filter_status = Excluded`, unless `include_reference_domains = true` on run):

- Seeded `reference_exclude_domains` (Wikipedia, Britannica, dictionary sites, pure news wire)
- URL patterns: `/wiki/`, informational `.gov` pages (configurable)
- Any URL whose registrable domain is in `project_owned_domains` (client's own secondary properties — never classified as a competitor)

**Bucket 2 — Known-platform include** (`filter_status = Included`, `include_reason = KnownPlatform`):

- Seeded `known_platform_domains` / URL patterns: Reddit, Quora, YouTube, Stack Exchange, major industry forums
- Included by platform identity alone — **no commercial schema required** (Reddit/Quora threads rank without Product/Service JSON-LD)
- Neither reference noise nor commercial-intent gated; these are first-class SERP-stacking signals

**Bucket 3 — Commercial / competitive include** (`filter_status = Included`, other `include_reason` values):

- Domain on optional `competitor_seed_domains` for the project
- Post-fetch: page contains commercial schema types (`Product`, `Service`, `LocalBusiness`, `Offer`, `FAQPage`) or pricing/comparison language in title/snippet
- **Multi-property stacking (tightened)**: an additional URL on the same registrable domain as an *already-included* URL (from Bucket 2 or 3) may cascade-include **only if**:
  - Registrable domain is **not** in `project_owned_domains` or the target site's domain
  - Subdomain is **not** on the noise list (`support.`, `help.`, `docs.`, `status.`, `community.`) — those go to Bucket 4 instead
  - The triggering included URL was included via `KnownPlatform`, `CompetitorSeed`, or `CommercialIntent` — not via a prior cascade alone (prevents chain reactions through irrelevant subdomains)

**Bucket 4 — Pending review** (`filter_status = PendingReview`):

- Ambiguous cases: noise-pattern subdomains on otherwise-competitive domains, domains in same family as target but not in `project_owned_domains`, SERP URLs with no schema and no platform match
- Excluded from fetch/graph/compare until manually approved

**Phase 0 filter stress-test (required before Phase 0 gate closes)**:

- Ship fixture SERP JSON files in `tests/fixtures/serp/` covering at least:
  - Wikipedia result → `Excluded` / reference reason
  - Reddit thread → `Included` / `KnownPlatform` (no schema)
  - Quora answer → `Included` / `KnownPlatform`
  - Two URLs same competitor domain (www + landing) → cascade include
  - `support.competitor.com` in SERP → `PendingReview`, not auto-included
  - Client secondary domain in SERP (in `project_owned_domains`) → `Excluded`, not competitor
- Unit-test matrix against fixtures must pass; document results in `docs/decisions/003-relevance-filter.md`
- Lock **Phase 2 verification keyword** in spec (e.g., a high-volume commercial query where Wikipedia reliably appears in top 20) as the default integration keyword; allow override via env, but default must be documented

---

## 0D — Target-site crawl bounds (locked numbers)

Defaults apply to every run unless overridden on `projects`:

| Parameter | Default | Per-project override column |
|-----------|---------|----------------------------|
| `max_crawl_depth` | **4** hops from homepage | `projects.max_crawl_depth` |
| `max_crawl_pages` | **150** pages | `projects.max_crawl_pages` |
| Stage timeout | **10 minutes** for target BFS fetch stage | env `CRAWL_STAGE_TIMEOUT` |

BFS stops when **either** limit is hit. SERP-sourced URLs (included candidates) are fetched in addition to the BFS cap — they do not count toward `max_crawl_pages`. Document rationale in ADR `004-crawl-bounds.md`.

---

## 0E — Comparison finding taxonomy + check model

Lock v1 finding types (each maps to a `finding_type` enum + JSON payload):

- `StructuredDataGap` — schema type present in ≥N top URLs, absent on target
- `HeadingStructureGap` — H2–H6 depth/count vs SERP median
- `ContentBlockGap` — FAQ block, comparison table, pricing table detected in competitors, not target
- `InternalOrphanPage` — crawlable page with zero inbound internal links
- `InternalDepthIssue` — page depth from homepage > threshold
- `InternalAuthoritySkew` — bounded PageRank concentration vs SERP set baseline
- `OutboundLinkSignal` — competitor set links to shared domains target does not (uses **cross-domain edges only**)

**Comparison execution model**: `ComparisonService` writes one row per finding type to `comparison_checks` (`run_id`, `finding_type`, `outcome`: `Finding` | `NoFinding`, `payload_json`). The `findings` table holds only rows where `outcome = Finding`. A well-optimized target site with zero gaps is a **pass**, not a gate failure.

**SerpSet PageRank edge rule** (document in ADR `005-bounded-pagerank.md`):

- `TargetInternal` scope: all edges in `internal_links` (target site only)
- `SerpSet` scope: **only** `cross_run_links` where `is_internal_to_domain = false` (inter-domain outbound among analyzed URLs). Intra-domain self-links on competitor sites are stored for audit but **excluded** from SerpSet PageRank to prevent a content-heavy site from dominating via internal self-link loops

---

## 0F — Real-time channel + step-gated orchestration (required before Phase 2)

This section locks three previously-undeclared constraints: frontend stack version policy, SignalR as the only real-time channel, and step-gated (human-click) orchestration for v1.

### Frontend stack version lock

Phase 4 UI: **Next.js (App Router) + React**, version pinned at Phase 4 start, recorded in `docs/decisions/006-frontend-stack.md`. "Latest" is not an acceptable spec value once Phase 4 begins — pin the exact version and revisit only via a new ADR, not silent upgrades mid-build.

### Real-time channel: SignalR only

- **No raw WebSockets.** All server-to-client push goes through a SignalR hub: **`RunProgressHub`**.
- Hub responsibilities: broadcast stage transitions, gate pass/fail results, and validation messages to subscribed clients for a given `run_id`. The hub does **not** drive orchestration — it observes and reports it.
- Client (Phase 4 UI) subscribes to a `run_id` group on entering the run detail page and receives push events as each stage completes; no polling loop for primary state, though a `GET /runs/{id}` fallback endpoint remains available for reload/reconnect.
- Record adapter choice and hub method contract in `docs/decisions/007-signalr-channel.md`.

### Step-gated orchestration (v1 execution model)

This replaces the prior model where `AnalysisRunOrchestrator` ran steps 2 through 8 continuously in one server-side pass. v1 requires explicit human click-through between every stage, consistent with the project's existing binary/no-fallback philosophy (refer back, don't degrade) — applied at per-stage granularity instead of only at whole-run granularity.

**Execution model**:

- Each pipeline stage (SERP, Filter, Fetch, Extract, Graph, PageRank, Comparison) runs to completion, writes its `run_gates` row (`passed`: true | false), and then **halts**. It does not invoke the next stage automatically.
- On halt, the orchestrator persists `analysis_runs.status = AwaitingConfirmation` and broadcasts the stage result (pass/fail + validation message) via `RunProgressHub`.
- The next stage only executes on an explicit API call triggered by a UI button click: `POST /runs/{id}/stages/{stage}/advance`.
- If a stage's gate fails (`passed = false`), the run halts in `Failed` state and does not offer an "advance" action — no partial/degraded continuation, consistent with the existing forward-only, no-fallback rule. The only recourse is to inspect the validation message and start a new run (or, in a later version, an explicit retry — out of scope for v1).

**Validation message requirement**:

Every stage, on completion (pass or fail), must produce a human-readable `validation_message` string summarizing the gate outcome in concrete terms — not just `passed: true/false`. Examples:

- SERP: `"14 results returned, 5 included after filtering, 2 excluded as reference domains, 7 pending review."`
- Extraction failure: `"0 of 6 fetched pages returned JSON-LD; gate requires >= 1. Stage failed."`
- Comparison: `"7 of 7 checks evaluated. 3 findings (1 high, 2 medium severity), 4 no-finding."`

This message is persisted on the `run_gates` row (see schema change below) and is what both the UI and `RunProgressHub` broadcast surface — it is the primary explanation a user sees before deciding whether to click "advance" or to abandon the run.

**`analysis_runs.status` enum revision**:

| Status | Meaning |
|--------|---------|
| `Running` | Stage actively executing |
| `AwaitingConfirmation` | Stage passed, halted, waiting for click-through |
| `Failed` | Stage gate failed; no advance available |
| `Completed` | Final stage (Comparison) passed and confirmed |

### Schema change required in Phase 1

- `run_gates` gains a `validation_message TEXT NOT NULL` column.
- `analysis_runs` gains a `current_stage TEXT` column (tracks which stage is awaiting confirmation or running).

### New/changed API surface (Phase 3)

| Endpoint | Action |
|----------|--------|
| `POST /runs/{id}/stages/{stage}/advance` | Triggers next stage after prior gate passed and was confirmed |
| `GET /runs/{id}` | Now includes `current_stage`, `status`, and latest `validation_message` |

### Explicitly still out of scope (0F reaffirmation)

- Auto-advance / continuous pipeline execution in v1 (may be reconsidered as a v2 "auto-run" toggle once the manual flow is proven)
- Partial/degraded stage retries (a failed stage ends the run; retry is a new run)
- WebSockets outside of SignalR's transport abstraction
