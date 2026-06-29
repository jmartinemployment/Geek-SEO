# Plan: Keyword Overview (Semrush analog)

**Status:** Planning — implement in slices; **do not break** Phase 1 operator path or Content Writer handoff.

**Context:** Within a Geek-SEO project, **Keyword Overview** is a **keyword-scoped** feature (one query per report). It must **not** own or display **site data** (business identity, themes, homepage JSON-LD, writing recommendations). Those stay on the **project / site profile** track.

See also: [Semrush Keyword Overview KB](https://www.semrush.com/kb/257-keyword-overview), [OPERATOR-WORKFLOW.md](../OPERATOR-WORKFLOW.md), [RESEARCH-MODEL.md](../RESEARCH-MODEL.md).

---

## Semrush Keyword Overview — what it is

Top-level report for **one keyword** (or bulk up to 100). Answers: *Is this keyword worth targeting? What does the SERP look like? What related queries exist?*

### Core report sections (Semrush)

| Section | What it shows |
|---------|----------------|
| **Keyword metrics** | Volume, global volume, intent, CPC, competitive density, trend, keyword difficulty (KD%) |
| **Personalized domain metrics** *(optional overlay)* | Personal Keyword Difficulty (PKD%), topical authority, potential SERP position, domain competitive power — only when a domain is in context |
| **Keyword ideas** | Variations, questions, keyword clusters (portal to Keyword Magic / Strategy Builder) |
| **Organic SERP** | Top ranking URLs/domains, SERP features, “View SERP” |
| **Paid SERP** | PLA copies, ad copies, ad history |
| **Bulk analysis** | Up to 100 keywords — intent, volume, KD, SERP features side-by-side |

**Important:** The default report is **about the keyword**. Site/domain personalization is an **add-on layer**, not mixed into every keyword widget as “site profile” fields.

---

## Our mapping (Geek-SEO project)

| Semrush | Site Analyzer | Scope |
|---------|---------------|--------|
| Keyword Overview | **Keyword Overview** (`analysis_runs` + `serp_items` + crawl outputs) | **Per keyword** |
| SEO Writing Assistant | Content Writer handoff | Consumes keyword overview + gaps |
| Site Audit | Phase 2a | Per project URL |
| Domain Overview | Competitor overview (Phase 2c) | Per project, ≤5 domains |
| Project / site context | **Site profile** (`site_profiles`) | Per project URL — **not** inside Keyword Overview |

### Naming

| Old / internal | Operator label | Notes |
|----------------|----------------|--------|
| Keyword import, pillar research | **Keyword Overview** | UI + docs |
| `analysis_runs` | Same table — one row = one keyword overview | No rename migration in v1 |
| Pillar | Keep in code/API (`analysis_runs.keyword`) | Means “the keyword for this run” |

---

## Boundary: Keyword Overview vs site data

### Belongs in Keyword Overview (keyword-scoped)

| Data | Source today |
|------|----------------|
| Keyword query | `analysis_runs.keyword` |
| Intent heuristic | `matched_pillar_intent` |
| SERP result count | `serp_se_results_count` |
| Organic / paid / AI / PAA / related rows | `serp_items` |
| SERP capture time | `serp_captured_at` |
| Competitor crawl status + pages | `competitor_pages`, crawl fields on run |
| Target page headings (for **this keyword’s** article) | `pages` / `page_headings` (`is_target_site`) |
| Comparison findings + `gap_topics` | `findings`, `analysis_runs` |
| Content Writer readiness gates | `research-focus` |

### Does **not** belong in Keyword Overview (project / site-scoped)

| Data | Where it lives |
|------|----------------|
| Business identity, summary, type | `site_profiles` |
| Site themes (`niche_tags`) | `site_profiles` |
| Geo anchors, service area | `site_profiles` |
| Writing recommendations (homepage ops) | `site_profiles` |
| Recommended homepage JSON-LD | `site_profiles` / assembly |
| Content pillar **list** for project | `GET /sites/content-pillars` — project nav, not inside a keyword report |
| Site Audit health | `site_audit_runs` (future) |

**Handoff:** Content Writer opens with `analysisRunId` only — site profile and keyword resolve from `sa2`. See [HANDOFF.md](../HANDOFF.md).

---

## What we already have (preserve)

Phase 1 operator path remains the backbone:

```
SERP HTML import → analysis_runs + serp_items
  → target crawl → competitor crawl → comparison → gap_topics
  → research-focus → Content Writer (?analysisRunId=)
```

| Asset | Keep as-is |
|-------|------------|
| `POST /imports/keyword-page` | Yes — rename in UI only |
| `POST /runs/{id}/competitor-crawl` | Yes |
| `GET /analysis-runs/{id}/content-writer-export` | Yes (debug / operator verify) |
| `GET /analysis-runs/{id}/research-focus` | Yes |
| `OperatorRunFocusService` | Yes |
| Handoff URL | `analysisRunId` only |

**Rule:** New Keyword Overview surfaces are **read models** over existing tables until a slice explicitly needs new columns.

---

## Gap vs Semrush (honest v1)

| Semrush metric | v1 (SERP HTML + crawl) | Later slice |
|----------------|------------------------|-------------|
| Search volume | ❌ not in saved HTML | External API or operator entry |
| Global volume | ❌ | External API |
| Keyword difficulty (KD%) | ❌ | Heuristic or API |
| CPC / competitive density | ⚠️ partial — paid rows in SERP | Parse/enrich |
| Trend (12 mo) | ❌ | External API |
| Intent | ✅ heuristic (`matched_pillar_intent`) | Improve classifier |
| SERP features | ✅ AI overview, PAA, related, ads | Expand parser |
| Organic SERP table | ✅ `serp_items` | Add columns (domain authority N/A v1) |
| Keyword variations | ✅ related searches | — |
| Questions | ✅ PAA / PASF | — |
| Keyword clusters | ❌ | Phase 2+ or Magic-tool analog |
| Bulk analysis (100 kw) | ❌ | Separate slice |
| PKD / topical authority / potential position | ❌ | Requires domain + rank data — **project overlay**, not site profile dump |
| Competitor page depth (our extension) | ✅ after competitor crawl | — |

v1 Keyword Overview = **SERP-faithful report + crawl-enriched SERP analysis**, not full Semrush database metrics.

---

## Phased delivery (no breaking changes)

Each slice: one pass/fail check. Prior slice must pass before the next.

### Slice KO-0 — Docs + IA boundary (this plan)

**Pass:** Glossary updated; team agrees Keyword Overview ≠ site profile UI.

### Slice KO-1 — Keyword Overview read API

**Deliverable:** `GET /analysis-runs/{runId}/keyword-overview`

DTO contains **only** keyword-scoped fields (assembled from `analysis_runs`, `serp_items`, competitor summary counts, research-focus gates). **No** `site_profiles` join.

**Pass:** Response for a run with SERP import includes keyword, intent, serp table, idea lists (related + questions), crawl status; response never includes `niche_tags`, `businessSummary`, etc.

### Slice KO-2 — UI separation

**Deliverable:** Web IA split:

- **Project setup** area: Project URL, Create Site Profile, Site Profile panel (site data only).
- **Keyword Overview** area: SERP upload, import summary, competitor crawl, research focus, Content Writer — **no** Site Profile panel embedded.

Optional: tabs or collapsible project header; site panel not inside keyword report body.

**Pass:** Operator can complete SERP import + crawl without expanding site business fields; site profile still reachable from project section.

### Slice KO-3 — Keyword Overview report layout (Semrush-shaped)

**Deliverable:** Single-page report sections mirroring Semrush skimmability:

1. Metrics strip (keyword, intent, result count, capture date; placeholders for volume/KD when absent)
2. Keyword ideas (variations + questions from `serp_items`)
3. SERP analysis table (organic + features)
4. Post-crawl: competitor benchmarks (from export builder logic, keyword-scoped)

**Pass:** Visual sections map 1:1 to plan wireframe; existing flows still work.

### Slice KO-4 — Operator labels

Rename user-facing copy: “Keyword import” → “Keyword Overview”; “Keyword import saved” → “Keyword Overview saved”; pillar list label “Saved keywords” in keyword nav.

**Pass:** No user-facing “keyword import” on main operator page; APIs unchanged.

### Slice KO-5 — Optional domain overlay (Semrush PKD analog)

**Deliverable:** When `site_profiles.site_url` is linked, show **optional** “Your domain on this SERP” block: are you in organic results? seed rank if present. No business profile fields.

**Pass:** Overlay uses SERP + target URL only; site themes still not shown in keyword report.

### Deferred

- Bulk keyword overview (100 keywords)
- Volume / KD / trend from third-party API
- Keyword clusters / Magic Tool analog
- Breaking `analysisRunId` handoff URL

---

## API surface (additive)

| Endpoint | Slice | Notes |
|----------|-------|--------|
| `GET /analysis-runs/{id}/keyword-overview` | KO-1 | New read model |
| `GET /analysis-runs/{id}/content-writer-export` | — | Unchanged |
| `GET /analysis-runs/{id}/research-focus` | — | Unchanged |
| `GET /sites?siteUrl=` | — | Site data only |
| `GET /sites/content-pillars` | — | Project keyword list (nav) |

---

## UI wireframe (target)

```text
┌─ Project (site data only) ─────────────────────────────┐
│ Project URL · [Create Site Profile] · Site profile ▼   │
└──────────────────────────────────────────────────────┘

┌─ Keyword Overview ───────────────────────────────────┐
│ Keyword: {query}          Captured: {date}           │
│ Intent · Results · [volume/KD placeholders]          │
├──────────────────────────────────────────────────────┤
│ Ideas: Variations | Questions                          │
├──────────────────────────────────────────────────────┤
│ SERP: organic table + AI / PAA / related / paid      │
├──────────────────────────────────────────────────────┤
│ [Upload SERP HTML] · [Competitor crawl] · gates        │
├──────────────────────────────────────────────────────┤
│ Research focus · gap_topics · [Open Content Writer]    │
└──────────────────────────────────────────────────────┘
```

---

## Anti-patterns (do not do)

- Joining `site_profiles` into `keyword-overview` DTO “for convenience”
- Moving site themes into `gap_topics` or keyword export
- Renaming `analysis_runs` table or breaking `analysisRunId` handoff
- Removing competitor crawl or comparison from keyword path
- Treating Keyword Overview as Site Audit or Competitor overview (Domain Overview)

---

## Related Semrush features (out of scope here)

| Tool | Our phase |
|------|-----------|
| Keyword Magic Tool | Future — seed expansion |
| Keyword Gap | Phase 2b (project portfolio) |
| Keyword Strategy Builder | Future — keyword lists per project |
| Domain Overview | Phase 2c Competitor overview |
