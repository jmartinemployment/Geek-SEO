# GeekRepository handoff — Niche scalable persistence

Geek-SEO ships orchestration, EF schema, and HTTP client contracts. **GeekRepository** (`:5050`) and **GeekAPI** proxy must implement these internal routes for production split writes and candidate inventory.

## New PATCH routes (`api/seo/internal/niche-profiles/{id}/…`)

| Route | Body | Notes |
|-------|------|-------|
| `profile-summary` | `NicheProfileSummaryPatch` | Metadata only — no fusion jsonb |
| `fusion-snapshot` | `{ fusionSnapshot: string }` | Optional archive; disable on hot path when `NICHE_FUSION_ARCHIVE_ENABLED` unset |
| `phase-status` | `NichePhaseStatusPatch` | `structureStatus`, `enrichmentStatus`, `persistStage`, optional `status` |
| `scores` | *(existing)* | Unchanged |

**Timeout:** Increase command timeout for bulk routes; **do not** use 100s monolithic `analysis-results` for full profile + fusion.

## New POST/GET routes

| Route | Purpose |
|-------|---------|
| `POST …/{id}/topic-candidates/bulk` | UPSERT batch; honor `Idempotency-Key` header |
| `GET …/{id}/topic-candidates?page&pageSize&selectedOnly` | Paginated inventory |

## Fallback behavior (GeekSeoBackend)

Until routes exist, clients:
- Fall back to monolithic `PATCH analysis-results` when `profile-summary` returns 404
- Skip candidate UPSERT on 404 (logged warning)
- Topic-candidates API falls back to parsing `fusion_snapshot` jsonb

## Migration

Apply EF migration `AddNicheScalablePersistence` from `GeekSeo.Persistence` at GeekRepository startup (schema `geek_seo`).

## Completion contract

- `structure_status = complete` → gaps/coverage-matrix UI enabled
- `enrichment_status = complete` (or `skipped`) + structure complete → `status = complete`
