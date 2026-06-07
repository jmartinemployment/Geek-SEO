# GeekRepository handoff — Niche scalable persistence

Geek-SEO ships orchestration, EF schema, and HTTP client contracts. **GeekRepository** (`:5050`) and **GeekAPI** proxy must implement these internal routes for production split writes and candidate inventory.

## New PATCH routes (`api/seo/internal/niche-profiles/{id}/…`)

| Route | Body (camelCase JSON) | Notes |
|-------|----------------------|-------|
| `profile-summary` | See `NicheProfileSummaryPatch` below | Metadata only — no fusion jsonb |
| `fusion-snapshot` | `{ "fusionSnapshot": "<json string>" }` | Optional archive |
| `phase-status` | See `NichePhaseStatusPatch` below | Partial updates OK |
| `scores` | *(existing)* | Unchanged |

### `NicheProfileSummaryPatch`

```json
{
  "primaryNiche": "Geek at Your Spot",
  "nicheDescription": "...",
  "nicheTags": ["local it support"],
  "audienceType": "local_service",
  "totalPillarsIdentified": 62,
  "analyzedAt": "2026-06-07T20:00:00Z",
  "nextAnalysisDue": "2026-07-07T20:00:00Z",
  "scanFingerprint": "a1b2c3d4e5f67890",
  "scanChangeScore": 0.9823,
  "persistStage": "scores",
  "structureStatus": "complete",
  "enrichmentStatus": null
}
```

### `NichePhaseStatusPatch`

```json
{
  "structureStatus": "complete",
  "enrichmentStatus": "complete",
  "persistStage": "done",
  "status": "complete"
}
```

Omit null fields — apply only non-null columns (COALESCE patch semantics).

**Timeout:** Keep each PATCH under 5s. Do **not** require monolithic `analysis-results` for production scale.

## New POST/GET routes

### `POST …/{id}/topic-candidates/bulk`

- Header: `Idempotency-Key: {profileId}:candidates:{batchIndex}`
- Body: array of `NicheTopicCandidateBulkUpsert` (max 200)

```json
[
  {
    "nicheProfileId": "uuid",
    "slug": "roof-repair",
    "name": "Roof Repair",
    "confidence": 0.92,
    "isSelected": true,
    "exclusionReason": null,
    "dedicatedPageUrl": "https://example.com/roof-repair",
    "internalLinkCount": 4,
    "contentDepthScore": 0.71,
    "displayOrder": 0,
    "evidenceJson": null
  }
]
```

**UPSERT SQL (Dapper example):**

```sql
INSERT INTO geek_seo.niche_topic_candidates (
  niche_profile_id, slug, name, confidence, is_selected, exclusion_reason,
  dedicated_page_url, internal_link_count, content_depth_score, display_order, evidence_json
) VALUES (...)
ON CONFLICT (niche_profile_id, slug) DO UPDATE SET
  name = EXCLUDED.name,
  confidence = EXCLUDED.confidence,
  is_selected = EXCLUDED.is_selected,
  exclusion_reason = EXCLUDED.exclusion_reason,
  dedicated_page_url = EXCLUDED.dedicated_page_url,
  internal_link_count = EXCLUDED.internal_link_count,
  content_depth_score = EXCLUDED.content_depth_score,
  display_order = EXCLUDED.display_order,
  evidence_json = COALESCE(EXCLUDED.evidence_json, geek_seo.niche_topic_candidates.evidence_json);
```

### `GET …/{id}/topic-candidates?page=1&pageSize=50&selectedOnly=true|false`

Response:

```json
{
  "items": [ { "id": "uuid", "slug": "...", "name": "...", "isSelected": true, ... } ],
  "total": 842,
  "page": 1,
  "pageSize": 50
}
```

Parse `evidenceJson` jsonb → `evidence` array in API response.

## Fallback behavior (GeekSeoBackend)

Until routes exist:

- Fall back to monolithic `PATCH analysis-results` when `profile-summary` returns 404
- Skip candidate UPSERT on 404 (logged warning)
- Topic-candidates API falls back to parsing `fusion_snapshot` jsonb
- Coverage/gaps API falls back to relational pillar rows when Dapper analytics fails

## Migration

Apply EF migration `AddNicheScalablePersistence` from `GeekSeo.Persistence` at GeekRepository startup (schema `geek_seo`).

## Completion contract

- `structure_status = complete` → gaps/coverage-matrix UI enabled
- `enrichment_status = complete` (or `skipped`) + structure complete → `status = complete`

## GeekAPI proxy

Add pass-through routes under `api/seo/internal/niche-profiles/*` matching GeekRepository paths (same as existing niche internal proxy pattern).
