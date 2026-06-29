# Rankings loop — Phase 2 implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development or executing-plans to implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After a pillar page is published, let the operator **re-import SERP HTML** for the same keyword and see **position delta for the client domain** — proof the page moved (or did not).

**Architecture:** Re-use existing keyword import (`SerpAutoImportService` finds-or-creates run by keyword + Project URL). Before SERP rows are replaced, history is preserved in `serp_rank_snapshots`. Each successful import appends one snapshot with best organic position for the target domain. Delta = previous snapshot position minus current (positive = improved).

**Tech Stack:** .NET 10, PostgreSQL `sa2`, Next.js Web. No GSC, no Semrush rank tracker, no portfolio Domain Overview expansion.

**Product boundary:** [PRODUCT-PHASES.md](../PRODUCT-PHASES.md) · ADR [015-rankings-loop-snapshots.md](../decisions/015-rankings-loop-snapshots.md)

---

## Documentation rule: evidence before status

Same as Frase phase — cite code path + verification before marking **Done**.

---

## MVP scope

| In scope | Out of scope (later) |
|----------|----------------------|
| Snapshot table per re-import | GSC / Search Console import |
| Target domain best organic position | Competitor position deltas |
| Delta on 2nd+ import | Email / Slack alerts |
| `research-focus` + import result fields | Writer-side rank widgets |
| Operator UI panel on pillar run | Historical charts across many keywords |

---

## Data model

**Table:** `sa2.serp_rank_snapshots`

| Column | Purpose |
|--------|---------|
| `run_id` | Same pillar run across re-imports |
| `project_id` | FK convenience |
| `import_sequence` | 1-based count for this run |
| `serp_captured_at` | From parsed SERP HTML |
| `recorded_at` | When snapshot persisted |
| `target_organic_position` | Best organic rank for Project URL domain; null = not in SERP |
| `target_organic_url` | URL of best-ranked owned organic row |
| `organic_result_count` | Organic-only count at capture |

**Position rule:** Among organic, non-ad `serp_items` for the run whose registrable domain matches Project URL, take lowest `rank_absolute` (fallback `rank_group`). Logic shared with `OwnedDomainIndexService` via `SerpTargetRankResolver`.

---

## Implementation tasks

### Task 1: Persistence + resolver

**Files:**
- Create: `src/SiteAnalyzer2.Domain/Entities/SerpRankSnapshot.cs`
- Create: `src/SiteAnalyzer2.Services/Rankings/SerpTargetRankResolver.cs`
- Create: `src/SiteAnalyzer2.Services/Rankings/SerpRankHistoryService.cs`
- Modify: `AppDbContext.cs`, new EF migration

- [x] Entity + migration
- [x] Resolver unit tests (owned domain match, not ranking, multi-URL picks best)
- [x] History service: `RecordAfterImportAsync`, `GetSummaryAsync`

### Task 2: Hook import pipeline

**Files:**
- Modify: `SerpAutoImportService.cs` — after `ImportHtmlAsync`, call `RecordAfterImportAsync`
- Modify: `SerpHtmlImportResultDto`, `KeywordPageImportResultDto` — optional `RankingsDelta` fields

- [x] Snapshot recorded on every successful import (including first — baseline)
- [x] Import API returns delta when `import_sequence >= 2`

### Task 3: Operator API + UI

**Files:**
- Modify: `OperatorResearchService.cs` — attach `RankingsSummary` to `RunResearchFocusDto`
- Modify: `page.tsx` — Rankings section in research focus panel; toast on re-import delta

- [x] `GET /analysis-runs/{id}/research-focus` includes rankings history + latest delta
- [x] UI shows baseline vs recapture when 2+ snapshots exist

### Task 4: Docs

- [x] This plan (tasks checked as shipped)
- [x] Update `PRODUCT-PHASES.md`, `PLAN.md`, `OPERATOR-WORKFLOW.md`, `RESEARCH-MODEL.md` (RESEARCH-MODEL: defer field glossary until next doc pass)
- [x] ADR 015

---

## Definition of done (MVP)

1. Operator imports SERP for keyword K → baseline snapshot stored (no delta UI).
2. Operator re-uploads SERP HTML for same keyword + Project URL → new snapshot; UI shows previous position, current position, and delta.
3. Target not in SERP → position shown as “Not ranking”; delta handles null transitions.
4. Unit tests pass for resolver + history delta math.
5. Docs cite `SerpRankHistoryService` and `GET …/research-focus` rankings fields.

---

## Verification

```bash
dotnet test tests/SiteAnalyzer2.Tests/SiteAnalyzer2.Tests.csproj --filter "FullyQualifiedName~SerpRank"
```

Manual: import canonical fixture twice (second import after editing rank in HTML or using two fixtures) → research-focus shows 2 snapshots and non-null delta.
