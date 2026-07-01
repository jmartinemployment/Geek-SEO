# ADR 016 — Manual five-lane Google research

**Status:** Accepted (2026-07-01)  
**Context:** [CRITIQUE-AI-CUSTOMER-JOURNEY-DRAFT.md](../../../plan-documents/CRITIQUE-AI-CUSTOMER-JOURNEY-DRAFT.md)

## Decision

1. **Five manual SERP lanes** per topic (`keyword`, `edu`, `gov`, `local`, `wiki`) persist in **`sa2.serp_items`** with a `research_lane` column on the same `analysisRunId`.
2. **Research stays in `sa2`** — accessed via GeekSeoBackend → GeekAPI → GeekRepository. No new research tables in `geek_seo`.
3. **Two write gates:** `manual` runs use `ValidateManualResearchExport` (keyword + supplemental lanes non-empty); `sa2` runs keep strict `ResearchBackedWriteGate` (competitor crawl + gap topics).
4. **Imports fail loud** — 0 organics or 0 usable supplemental URLs → HTTP 422, no DB row. Enricher skips live SerpAPI only when manual bucket is non-empty.
5. **Citation source from domain** (`.gov`, `.edu`, wikipedia), not import folder.
6. **One run = one `topic_slug`** — mismatched re-import rejected.
7. **Local lane** requires a dedicated parser; not the organic SERP parser.
8. **Existing DB data is disposable** — drop duplicate `geek_seo` research tables; no row migration for pilot.

## Consequences

- Customer-journey pilot does not require a prior full SA2 competitor crawl.
- Full SA2 operator workflow ([OPERATOR-WORKFLOW.md](../OPERATOR-WORKFLOW.md)) remains valid for `research_mode = sa2` runs.
- GeekBackend owns sa2 migrations and import handlers; Geek-SEO owns merger, gates, and UI.

## Alternatives rejected

| Alternative | Why rejected |
|-------------|--------------|
| `seo_manual_research_lanes` in `geek_seo` | Mixes research with GetOrderStack shared schema |
| Folder lane → citation source tag | Mislabels stray URLs (e.g. Forbes in `gov/`) |
| Persist empty lane + suppress enricher | Silent degradation — draft against nothing |
| Reuse old SA2 run for pilot gate data | No production-quality data to preserve |
