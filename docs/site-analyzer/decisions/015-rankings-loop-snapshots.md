# ADR 015: Rankings loop via SERP re-import snapshots

**Status:** Accepted  
**Date:** 2026-06-26  
**Phase:** 2 — Rankings loop

## Context

Frase phase ships a research pack and Content Writer draft. Operators need to know whether the published pillar page **moved in Google** for the target keyword without building Semrush-style rank tracking or GSC integration.

Re-importing saved SERP HTML for the same keyword already **reuses** the same `analysis_runs` row (`SerpAutoImportService.FindOrCreateRunAsync`) but **replaces** `serp_items`, destroying prior positions.

## Decision

1. Append-only **`serp_rank_snapshots`** per successful SERP import on a run.
2. Compute **best organic position** for the Project URL registrable domain at each import.
3. Expose **latest delta** (previous minus current position) on import result and `GET …/research-focus`.
4. Do **not** add GSC, competitor rank history, or Domain Overview expansion in this phase.

## Consequences

- Re-import is the operator action; no scheduled rank checks.
- First import is baseline only; delta appears on second import.
- Competitor seed ranks remain on `competitor_pages.seed_rank_absolute` from crawl time — not updated on SERP re-import (acceptable for MVP).

## Alternatives considered

| Option | Rejected because |
|--------|------------------|
| JSON history column on `analysis_runs` | Harder to query; mixes run metadata with time series |
| Never replace `serp_items` | Breaks filter/crawl pipeline expecting current SERP |
| GSC API first | Scope creep; manual SERP HTML already trusted in Frase |

## References

- [plans/rankings-loop.md](../plans/rankings-loop.md)
- [PRODUCT-PHASES.md](../PRODUCT-PHASES.md)
