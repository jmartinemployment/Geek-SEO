# ADR 013: Site Audit runs (separate from keyword runs)

**Status:** Accepted (design) — implement in Phase 2a slices after Phase 1 verified.

## Context

Phase 1 uses `analysis_runs` for **pillar research** (one keyword + SERP + competitor crawl + `gap_topics`). Semrush **Site Audit** is a **project-scoped** crawl with health score, category rollups, and an issues table — a different lifecycle and UI.

Overloading `analysis_runs` would conflate keyword research with site-wide audits and break operator mental model.

## Decision

Introduce **`site_audit_runs`** bound to `site_profiles` (Project URL). Site audit:

- Crawls **target site only** (reuse `PageFetchService` BFS, site-scoped)
- Runs **`SiteAuditCheckService`** categories (Crawlability, HTTPS, Markups first)
- Persists **`findings`** with `site_audit_run_id` (nullable; keyword runs keep `run_id` only)
- Exposes **overview + issues** APIs and a **Site Audit** tab in Web UI

Do **not** extend `AnalysisRunOrchestrator` or legacy `/runs/*` for audit. Do **not** run audit checks inside `AssembleFromHomepageAsync` / Create Site Profile.

## Data model (minimal)

```text
site_audit_runs
  id, site_profile_id, status, message
  crawl_started_at, crawl_finished_at
  pages_crawled, pages_healthy, pages_with_issues, pages_broken
  health_score (0–100)
  errors_count, warnings_count, notices_count
  category_rollups_json   -- per SiteAuditCategory %
```

```text
findings (extend)
  site_audit_run_id  nullable FK → site_audit_runs
  audit_category     nullable (Crawlability | Https | Markups | …)
  audit_issue_code   nullable string (stable UI key, e.g. missing_title_tag)
```

Keyword-run findings (`run_id` on `analysis_runs`, no `site_audit_run_id`) remain comparison/gap findings.

## Job pattern

Mirror competitor crawl:

| Layer | Site Audit |
|-------|------------|
| Job | `SiteAuditJobService` |
| Status columns | on `site_audit_runs` |
| Progress (later) | SSE + `site_audit_progress_logs` if needed |
| Checks | `SiteAuditCheckService` → `SiteAuditRollupService` |

## API (Phase 2a)

| Method | Route |
|--------|-------|
| POST | `/sites/{siteProfileId}/audit` |
| GET | `/sites/{siteProfileId}/audit/latest` |
| GET | `/sites/{siteProfileId}/audit/{auditRunId}` |
| GET | `/sites/{siteProfileId}/audit/{auditRunId}/issues` |

## UI

Semrush-style **Overview + Issues** on a **Site Audit** tab (see `docs/plans/site-audit-ui-design.md`). Keyword research tab unchanged.

## Consequences

- New migration + entity in slice **2a-1**
- `SiteAuditCheckService` / `SiteAuditRollupService` scaffolded now (pure logic, unit-tested)
- `SiteAuditJobService` wired in **2a-1** after schema lands
