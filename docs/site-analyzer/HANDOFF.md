# Handoff — Site Analyzer → Content Writer

## Rule

**One id in the URL: the analysis run.**

```text
https://<geek-seo>/content-writing?analysisRunId=<analysis_runs.Id>
```

That UUID is `analysis_runs.Id` in `sa2`. The same value appears as `RunId` on child tables (`serp_items`, `competitor_pages`, `pages`, `findings`, etc.).

Geek-SEO **reads research from `sa2` when Writer needs it** (`SITE_ANALYZER2_DATABASE_URL`). It does **not** copy SERP or site research onto `seo_content_documents` as JSON.

---

## Division of responsibility

| System | Schema | Owns |
|--------|--------|------|
| **Site Analyzer** | `sa2` | SERP import, target crawl, competitor crawl, comparison, `gap_topics`, site profile assembly |
| **Content Writer** | `geek_seo` | Article document, draft HTML, score, outline, Insights UI, Sources/Citations apply, JSON-LD in editor |

Site Analyzer is the system of record for research. Content Writer is the system of record for the article.

---

## What happens on handoff

1. Operator completes research in Site Analyzer and clicks **Open Content Writer**.
2. Browser opens Geek-SEO with `analysisRunId` only (see URL above).
3. Geek-SEO **creates** (or opens) a content document:
   - Sets `analysisRunId` = that run.
   - Sets `projectId` from `analysis_runs.ProjectId` (must match the linked Geek-SEO project).
   - Sets `targetKeyword` from `analysis_runs.Keyword` (optional override in UI later).
4. When the user drafts, scores, or views Insights, Geek-SEO **queries `sa2`** using that run id (and the project id on the run).

No `projectId`, `keyword`, or `site_profile` in the handoff URL. Those are resolved from the run row or related `sa2` tables.

---

## How Geek-SEO finds related rows

From `analysisRunId`:

| Need | Source |
|------|--------|
| Keyword, status, gap topics, pillar fields | `sa2.analysis_runs` WHERE `Id` = run id |
| Geek-SEO project | `analysis_runs.ProjectId` (= `geek_seo.seo_projects.Id` when linked) |
| Organic SERP, PAA, related searches | `sa2.serp_items` WHERE `RunId` = run id |
| Competitor pages and headings | `sa2.competitor_pages` (+ headings) WHERE `RunId` = run id |
| Target page headings | `sa2.pages` / `page_headings` for that run |
| Site niche, geo, business summary | `sa2.site_profiles` WHERE `GeekSeoProjectId` = `analysis_runs.ProjectId` |

When linkage is correct, `site_profiles.GeekSeoProjectId` and `analysis_runs.ProjectId` are the **same** Geek-SEO project UUID.

---

## Research-ready (operator gate)

Do not open Content Writer until Site Analyzer shows **Research ready** on the keyword panel.

`GET /analysis-runs/{id}/research-focus` · `OperatorResearchService.GetResearchFocusAsync`

`researchReady` is true only when **all** of:

| Gate | Requirement |
|------|-------------|
| SERP import | ≥1 organic `serp_items` |
| Target-site crawl | target `page_headings` |
| Competitor crawl | ≥1 `competitor_pages` with headings |
| Comparison | ≥1 `findings` |
| Gap topics | non-empty `gap_topics` on the run |

Content Writer create validation should match this bar, not a weaker subset.

---

## What Geek-SEO stores on the document

| Field | Purpose |
|-------|---------|
| `projectId` | Geek-SEO project (from `analysis_runs.ProjectId`) |
| `analysisRunId` | Link to Site Analyzer research |
| `targetKeyword` / `serpKeyword` | Article keyword vs SERP query (usually from the run) |
| `contentHtml`, scores, etc. | Writer-owned |

**Do not use for handoff** (legacy — remove from create path):

- `keywordBundleJson` / `siteFocusJson` — copied research snapshots
- `siteProfileId` on the document — site row is found via `ProjectId` / `GeekSeoProjectId`

---

## Legacy URL parameters (invalid)

These must **not** appear on new handoff links:

| Param | Why dropped |
|-------|-------------|
| `site_profile` | Site context comes from `site_profiles` via `analysis_runs.ProjectId` |
| `keyword` | On `analysis_runs.Keyword` |
| `projectId` | On `analysis_runs.ProjectId` |
| `urlResearchId` | Old pipeline |

---

## Legacy HTTP exports (do not use for handoff)

These endpoints duplicated `sa2` into JSON for copy-on-create. **Handoff does not call them.**

| Endpoint | Status |
|----------|--------|
| `GET /analysis-runs/{id}/content-writer-export` | Debug/operator only; not Writer create |
| `GET /site-profiles/{id}/content-writer-bundle` | Debug/operator only; not Writer create |

Writer reads tables directly (or via a thin `sa2` reader), not export blobs.

---

## Environment

| Variable | Service |
|----------|---------|
| `SITE_ANALYZER2_DATABASE_URL` | Geek-SEO backend + Site Analyzer API — read/write `sa2` |
| `NEXT_PUBLIC_GEEK_SEO_APP_URL` | Site Analyzer Web — handoff link target |
| `NEXT_PUBLIC_API_URL` | Site Analyzer Web → Api |
| `GEEK_SEO_PROJECT_ID` | Site Analyzer Api — bootstrap link for `site_profiles` / `projects` (operator) |

---

## Operator checklist

- [ ] Project URL normalized: `https://www.{domain}/` (lowercase, trailing slash)
- [ ] Site profile exists and `GeekSeoProjectId` matches the Geek-SEO project
- [ ] SERP HTML saved for the keyword
- [ ] Competitor crawl completed
- [ ] Research focus shows **Research ready** (all gates green)
- [ ] Open Content Writer (link contains **only** `analysisRunId`)

---

## What Content Writer does not get from Site Analyzer

- Article outlines (generated in Writer from research)
- Content scores, plagiarism, Sources apply jobs, editor JSON-LD (Writer-owned)

See also:

- [INTEGRATIONS.md](INTEGRATIONS.md) — field-level export notes (operator/debug) and gate tables
- [Content Writing handoff](../content-writing/HANDOFF.md) — pillar editor, cluster spokes, linking workflow, and known UI traps after you land from Site Analyzer
