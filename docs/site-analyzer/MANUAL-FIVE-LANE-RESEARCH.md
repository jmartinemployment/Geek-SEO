# Manual five-lane Google research

**Status:** Planned (pilot: `customer-journey`)  
**Last updated:** 2026-07-01

How operators import saved Google Search HTML into five research lanes and how Content Writer consumes them. Complements [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md) (full SA2 crawl path) and [HANDOFF.md](HANDOFF.md).

---

## Why five lanes

Critique-driven research for topics like *AI customer journey* needs more than one SERP screenshot:

| Lane | Search intent (operator) | Writer use |
|------|--------------------------|------------|
| `keyword` | Primary query (e.g. `ai customer journey`) | Primary SERP — organics, PAA, related |
| `gov` | `site:.gov {topic}` | Authoritative `.gov` citations |
| `edu` | `site:.edu {topic}` | Research / academic citations |
| `wiki` | `{topic} wikipedia` | Encyclopedia framing |
| `local` | `{topic} {city}` (e.g. Delray Beach) | Local angle hint |

Lanes are **supplemental** except `keyword`, which is the primary SERP for the run.

---

## Operator folder layout

On disk (not read at draft time — must be imported):

```text
research/
  {topic_slug}/           e.g. customer-journey
    keyword/*.html
    edu/*.html
    gov/*.html
    local/*.html
    wiki/*.html
```

CLI ignores `*_files/` sidecar folders from browser Save-As.

---

## Persistence (`sa2` only)

All research rows live in **`sa2`**, not `geek_seo`. Access path:

```text
GeekSeoBackend → GeekAPI → GeekRepository → PostgreSQL sa2
```

| Column / concept | Meaning |
|------------------|---------|
| `analysis_runs.id` | `analysisRunId` — one pack per keyword/topic run |
| `serp_items.research_lane` | `NULL` / `keyword` = primary; `edu` \| `gov` \| `local` \| `wiki` = supplemental |
| `serp_items.topic_slug` | Audit metadata (e.g. `customer-journey`); enforced per run |
| `analysis_runs.research_mode` | `manual` \| `sa2` — selects write gate (see below) |

**Data policy:** Existing DB rows are not production-quality. Pilot may drop and recreate. No migration from `geek_seo` research tables.

**Drop from `geek_seo` (no copy):** `seo_url_research*`, `seo_site_research*`, `seo_site_analyzer_step_run`, `seo_serp_results`, `seo_competitor_pages`, vendor cache tables. Writer documents keep **`analysis_run_id` pointer only**.

---

## Import API

```http
POST /api/seo/internal/analysis-runs/{runId}/serp/import-html?lane={lane}&topic={topic_slug}
```

| `lane` | Required parse result | On failure |
|--------|----------------------|------------|
| `keyword` (default) | ≥1 organic | **422** — do not persist empty lane |
| `edu`, `gov`, `wiki` | ≥1 URL passing domain filter after parse | **422** |
| `local` | ≥1 local-pack signal (dedicated parser) | **422** until parser + fixture exist |

**Fail loud:** An imported-but-empty lane must never suppress live SerpAPI enrichment. Enricher skips a live bucket only when the manual bucket is **non-empty**.

**Run ↔ topic invariant:** First import for a run sets `topic_slug`. Later imports with a different `topic_slug` for the same `runId` are rejected.

---

## Citation source tagging

Citation `source` is derived from the **resolved URL domain**, not the import folder:

| Host pattern | Tag |
|--------------|-----|
| `*.gov` | Government |
| `*.edu` | Research |
| `*wikipedia*` | Wiki |
| Other | Unknown — drop or log; [`AuthoritativeCitationRules`](../../GeekSeo.Application/Services/Seo/AuthoritativeCitationRules.cs) backstop |

A non-`.gov` URL saved into `gov/` must not receive a Government tag.

---

## Write gates (two modes)

### Manual research mode (`research_mode = manual`) — pilot path

`ValidateManualResearchExport`:

- Keyword lane: ≥1 organic
- Supplemental lanes per topic policy (gov, wiki required for customer-journey pilot; edu/local when HTML available)
- **No** competitor crawl, target headings, or gap topics required

### SA2 crawl mode (`research_mode = sa2`) — full operator path

[`ResearchBackedWriteGate`](../../GeekSeo.Application/Services/Seo/ResearchBackedWriteGate.cs) unchanged:

- Organic SERP, target `sourceHeadings`, competitor headings, `gapTopics`

See [INTEGRATIONS.md](INTEGRATIONS.md) for gate alignment with `research-focus`.

---

## Merge order (Content Writer)

1. Load `content-writer-export` for `analysisRunId`
2. `ManualResearchLaneMerger` — supplemental lanes → `CitationCandidates` + `LocalAngleHint` (domain-tagged)
3. `OperatorResearchEnricher` — skip live SerpAPI bucket only if manual bucket non-empty
4. Draft generation

Keyword lane supplies primary `export.Serp`; supplemental lanes do not replace it.

---

## Pilot workflow (`customer-journey`)

1. Create fresh `analysisRunId` with `research_mode = manual` and target keyword.
2. Import `research/customer-journey/keyword/` — must get ≥1 organic.
3. Import `gov/`, `wiki/` (on disk). Import `edu/`, `local/` when operator saves HTML.
4. Open Content Writer with `?analysisRunId=…` — manual gate must pass.
5. Regenerate draft — citations from gov/wiki lanes; local angle when local parser succeeds.

---

## Parsers

| Lane | Parser |
|------|--------|
| `keyword`, `edu`, `gov`, `wiki` | `GoogleSerpHtmlParser` (organic + PAA + related) |
| `local` | **Separate** local-pack parser — organic parser will not extract map pack markup |

Add fixture tests per lane when HTML is available. Single keyword fixture is insufficient for production confidence.

---

## Related docs

- [HANDOFF.md](HANDOFF.md) — URL and division of responsibility
- [INTEGRATIONS.md](INTEGRATIONS.md) — Geek-SEO gate tables
- [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md) — full SA2 crawl track (parallel path)
- [decisions/016-manual-five-lane-research.md](decisions/016-manual-five-lane-research.md) — ADR
- [`plan-documents/CRITIQUE-AI-CUSTOMER-JOURNEY-DRAFT.md`](../../plan-documents/CRITIQUE-AI-CUSTOMER-JOURNEY-DRAFT.md) — product motivation
