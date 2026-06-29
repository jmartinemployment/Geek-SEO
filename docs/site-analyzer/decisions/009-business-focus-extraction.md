# ADR 009: Target-Site Business-Focus Extraction

## Status
Accepted — extends target-site extraction only (Addendum 0H)

## Context
Downstream components may need a stable answer to “what does this business do/sell?” for the **target site** only. SiteAnalyzer crawls and extracts the target site once per run; 0H adds BFS priority tuning, confirms text capture, and adds a **single unconditional AI call per target site per run** with caching (or `human` manual profile).

**This is not Google My Business.** Output is derived from the **crawled target website** (headings, meta, JSON-LD, content blocks).

**Content Writer (Geek-SEO) today:** Does **not** use `GET /business-profile` for site-about when writing. Site context comes from `sa2.site_profiles` (via `analysis_runs.ProjectId`); keyword research from `sa2` by `analysisRunId`. See [INTEGRATIONS.md](../INTEGRATIONS.md) and [HANDOFF.md](../HANDOFF.md).

## Step 1 audit findings (2026-06-21 — codebase inspection)

| Artifact | Status |
|----------|--------|
| `page_content_blocks.content` | **Already captures cleaned block text** (main/article/table/list/faq), truncated to 4000 chars per block — not structure-only |
| `page_json_ld.raw_json` | **Full raw JSON stored** |
| `page_json_ld.parsed_type` | **`@type` parsed** via `PageExtractionService.TryParseSchemaType` |
| `page_headings.text` | **Full heading text stored** (H1–H6, document order) |

**Conclusion**: Step 3 schema extension (`text_content` column) is **not required** — existing `content` column satisfies the intent. Optional future rename to `text_content` is cosmetic only.

## BFS priority patterns (Step 2)

Maintain as seeded/config table `crawl_priority_url_patterns` (not hardcoded inline). Default patterns (case-insensitive path segment match):

- `/about`, `/about-us`, `/services`, `/what-we-do`, `/contact`, `/team`, `/who-we-are`
- URLs linked from primary site navigation (`nav`, `header`, `[role='navigation']`) on homepage

**Queue behavior**: priority URLs dequeue before same-or-greater-depth non-priority URLs. Does **not** change `max_crawl_depth`, `max_crawl_pages`, or timeout (0D unchanged).

## Business-focus classification (Step 4)

### Placement in pipeline
Runs **at the end of the Extract stage** (same `POST .../stages/Extract/advance` click) — after `PageExtractionService` persists headings/meta/JSON-LD/blocks for all pages. **Not** a separate gated stage or advance button in v1.

### Unconditional AI call
- **Always** runs once per target site per run when Extract stage executes
- **Not** gated on JSON-LD presence/absence
- Existing JSON-LD is **input**, not a bypass
- **No AI call** for SERP/competitor pages

### Input
Target-site pages only: headings, meta tags, existing `page_json_ld.raw_json`, `page_content_blocks.content` from priority pages (Step 2 ordering improves coverage within crawl cap).

### Output (persisted to `target_site_business_profiles`)
| Field | Purpose |
|-------|---------|
| `business_type` | Category / type string |
| `primary_services_json` | JSON array of services/products |
| `service_area` | Optional geography |
| `description` | Plain-language business summary |
| `generated_schema_json` | schema.org JSON-LD block for client homepage |
| `has_existing_schema` | Target site had JSON-LD on crawl |
| `existing_schema_matches` | AI assessment vs existing markup (nullable tri-state) |
| `generated_at` | UTC timestamp |
| `reused_from_run_id` | Set when cache hit (no new AI call) |

### Caching (v1)
Before calling AI, look up the most recent `target_site_business_profiles` row for the same `project_id` + normalized `target_site_url`. If found, **reuse** (copy forward with `reused_from_run_id`) — **no fresh AI call** on default re-run.

Manual invalidation: env `BUSINESS_PROFILE_FORCE_REFRESH=true` or future explicit API flag (v1: env only).

**No automatic content-hash invalidation in v1.**

### Cost discipline
One AI call per target site per run maximum (cache miss). Never per keyword, article, or competitor URL. Documented exception to collection-only posture — see plan guardrails.

### Provider configuration

| Variable | Required | Purpose |
|----------|----------|---------|
| `BUSINESS_FOCUS_PROVIDER` | **yes** | `openai`, `anthropic`, or `human` — no default; operator chooses explicitly |
| `BUSINESS_FOCUS_AI_PROVIDER` | — | Legacy alias for `BUSINESS_FOCUS_PROVIDER` |
| `OPENAI_API_KEY` | when `openai` | OpenAI key |
| `ANTHROPIC_API_KEY` | when `anthropic` | Anthropic key |
| `OPENAI_MODEL` | — | Default `gpt-4o-mini` |
| `ANTHROPIC_MODEL` | — | Default `claude-haiku-4-5-20251001` |

**`openai` / `anthropic`**: one AI call after Extract (cache rules unchanged). Requires the matching API key.

**`human`**: no AI call after Extract. Profile is supplied via `PUT /runs/{id}/business-profile` or the Web UI form after Extract passes. Cached profiles from prior runs still copy forward on re-run unless `BUSINESS_PROFILE_FORCE_REFRESH=true`.

**`auto` is not supported** — removed to avoid vendor/pricing assumptions; set the provider you intend to use.

### `GET /config`

Returns `{ "businessFocusProvider": "openai" | "anthropic" | "human" }` for UI routing. **503** with `{ "error": "..." }` when `BUSINESS_FOCUS_PROVIDER` is missing or invalid.

### Future finding (not built in 0H)
`StructuredDataMismatch` when existing JSON-LD materially disagrees with AI synthesis — fits comparison taxonomy later.

## Public API contract

### `GET /runs/{id}/business-profile`

**Available** (200): run has completed Extract stage and profile row exists.

**Not available yet** (404 + JSON body):
```json
{
  "error": "business_profile_not_available",
  "reason": "extract_stage_not_completed" | "profile_not_generated"
}
```

Never return 200 with null/empty fields implying absence vs not-yet-generated.

### `PUT /runs/{id}/business-profile`

Upserts a manually authored business profile (required when `BUSINESS_FOCUS_PROVIDER=human`, also available under other providers). Requires Extract stage passed. Body matches the GET response fields (`generatedSchemaJson` as JSON object). Returns the same shape as GET on success.

**Response shape (200)** — contract-tested in integration suite:
```json
{
  "businessType": "string",
  "primaryServices": ["string"],
  "serviceArea": "string | null",
  "description": "string",
  "generatedSchemaJson": "object",
  "hasExistingSchema": true,
  "existingSchemaMatches": true | false | null,
  "lastGeneratedAt": "ISO-8601",
  "reusedFromRunId": "uuid | null"
}
```

Content Writer and other Geek-SEO consumers use **`SITE_ANALYZER2_DATABASE_URL`** read-only for `sa2` research (see [HANDOFF.md](../HANDOFF.md)). Site Analyzer HTTP APIs remain for operator UI and debug export.

**Shipped today for Content Writer:** `sa2` reads by `analysisRunId` — keyword, `serp_items`, competitors, `gap_topics`, site profile via `ProjectId`. Debug mirror: `GET …/content-writer-export`.

**This endpoint (`GET /runs/{id}/business-profile`):** Target-site summary within SiteAnalyzer; optional input for future consumers; not the primary Content Writer site-about source today.

## Out of scope (0H)
- Geek-SEO Niche Analyzer (site understanding lives there)
- Content Writer consumption of **competitor crawl** (separate future work — [010-competitor-crawl-planned.md](010-competitor-crawl-planned.md))
- Second target-site crawl
- AI on competitor pages
- `StructuredDataMismatch` finding implementation
- Automatic cache invalidation by content hash
