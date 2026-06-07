# Local service area — implementation plan (draft)

**Status:** Draft — Phase 1 in progress  
**Product spec:** [`LOCAL-SERVICE-AREA.md`](./LOCAL-SERVICE-AREA.md)  
**Deferred:** Google My Business OAuth — [`LOCAL-GBP-INTEGRATION.md`](./LOCAL-GBP-INTEGRATION.md)  
**Default service radius:** **20 miles** (user-adjustable per project)

---

## Goal

Every local-service project gets a **business address** and **service radius**. Geek SEO uses that to decide which cities/counties matter for location-page gaps, topical map modifiers, and Copilot — without asking the user to connect Google My Business.

**Maps usage:** Google Maps Platform **Geocoding API** (server API key) to turn address → lat/lng. No user OAuth. Places/nearby lookup comes in Phase 2.

---

## What exists today

| Piece | Status |
|-------|--------|
| Step 11 `LocalGapGenerator` | Shipped — schema `areaServed` vs `/locations` URLs |
| `SeoProject.DefaultLocation` | Shipped — keyword/SERP **market** string (e.g. "United States"), not street address |
| Project settings UI | Google connect only — no address/radius |
| Persistence | `seo_projects` via GeekRepository internal API |

---

## Data model (Phase 1)

Add columns on `geek_seo.seo_projects`:

| Column | Type | Default | Purpose |
|--------|------|---------|---------|
| `business_address` | `text` nullable | null | Full street address (single field v1) |
| `service_radius_miles` | `int` not null | **20** | Service area radius |
| `local_seo_enabled` | `bool` not null | `true` | Master toggle for local features |

**Later (Phase 2+):** `geocode_lat`, `geocode_lng`, `geocoded_at`, `places_snapshot` JSONB — do not add until geocode service exists.

**Relationship to `DefaultLocation`:** Keep both. `DefaultLocation` = DataForSEO/SERP market. New fields = physical service area for local content gaps.

---

## Phases

### Phase 1 — Settings persist + UI (this sprint)

| # | Task | Layer | Done when |
|---|------|-------|-----------|
| 1.1 | EF migration + `SeoProject` entity fields | GeekSeo.Persistence | Migration in repo |
| 1.2 | `CreateProjectRequest` / `UpdateProjectRequest` + JSON on API | GeekSeo.Application, GeekSeoBackend | PUT accepts radius 5–100 |
| 1.3 | **GeekRepository** internal projects controller maps new columns | GeekBackend (separate deploy) | ✅ `ProjectRepository` maps address/radius/enabled |
| 1.4 | `updateProject()` + extend `SeoProject` type | frontend | API client complete |
| 1.5 | `LocalServiceAreaSettings` on project page | frontend | User can set address + radius |
| 1.6 | Validation: radius clamp 5–100, empty address OK | Application | Invalid values rejected |

**Out of Phase 1:** geocoding, Step 11 radius merge, topical map city modifiers.

### Phase 2 — Geocode + places within radius

| # | Task | Layer |
|---|------|-------|
| 2.1 | `IGeocodeService` + Google Maps Geocoding (`GOOGLE_MAPS_API_KEY`) | GeekSeoBackend |
| 2.2 | Cache lat/lng on project after save | GeekRepository |
| 2.3 | `ServiceAreaResolver` — cities/counties within radius (static dataset or Places API) | GeekSeoBackend |
| 2.4 | Unit tests with fixed coordinates (Broward/Palm Beach fixture) | GeekSeoBackend.Tests |

### Phase 3 — Step 11 uses radius places

| # | Task | Layer |
|---|------|-------|
| 3.1 | Pass project local settings into `NicheAnalyzerService` Step 11 | GeekSeoBackend |
| 3.2 | Merge radius places with schema `areaServed` (radius primary when address set) | `LocalGapGenerator` |
| 3.3 | Extend `LocalGeographyAnalysis` + step log outputs | Application + frontend panel |
| 3.4 | `FusionActionRecommender` — gap actions name radius places | GeekSeoBackend |

### Phase 4 — Downstream consumers

| # | Task |
|---|------|
| 4.1 | Topical map — `{pillar} + {city}` for radius places |
| 4.2 | Dashboard Copilot — “Add pages for cities within 20 mi” |
| 4.3 | Content Guard — optional place badge on decaying location URLs |
| 4.4 | Re-analyze dogfood: geekatyourspot.com with address + 20 mi |

---

## API changes (Phase 1)

**Existing routes — extended body/response:**

```
PUT /api/seo/projects/{id}
{
  "businessAddress": "123 Main St, Fort Lauderdale, FL 33301",
  "serviceRadiusMiles": 20,
  "localSeoEnabled": true
}
```

```
GET /api/seo/projects/{id}
→ includes businessAddress, serviceRadiusMiles, localSeoEnabled
```

Internal mirror: `PUT api/seo/internal/projects/{id}` (GeekRepository).

---

## UI (Phase 1)

**Location:** Project page (`/app/projects/[id]`), below Google connect.

**Copy:**
- Title: **Local service area**
- Help: “We use your business address and how far you travel to find location-page gaps. Default is 20 miles.”
- Fields: address (textarea), radius (number 5–100), enabled toggle
- No mention of OAuth or My Business

---

## Environment (Phase 2+)

| Variable | Service | Phase |
|----------|---------|-------|
| `GOOGLE_MAPS_API_KEY` | GeekSeoBackend | 2 |
| `DEFAULT_SERVICE_RADIUS_MILES` | optional platform default | 1 (fallback 20 in code) |

---

## GeekBackend dependency (blocking production Phase 1)

GeekSeoBackend calls GeekRepository over HTTP. After merging this repo:

1. Apply EF migration on Railway Postgres (`geek_seo` schema).
2. Update GeekRepository `SeoProject` entity + internal projects PATCH/PUT to read/write three columns.
3. Redeploy GeekRepository, then GeekSeoBackend.
4. Smoke: PUT project radius → GET returns 20 → UI reload shows saved value.

Until step 2–3 ship, frontend save may 400 or silently drop fields — **do not mark Phase 1 done in production without GeekBackend.**

---

## Testing

| Phase | Tests |
|-------|--------|
| 1 | xUnit: UpdateProjectRequest validation (radius bounds) |
| 1 | Vitest: `LocalServiceAreaSettings` form submit payload (optional) |
| 2 | xUnit: `ServiceAreaResolver` with mock geocode |
| 3 | Extend `LocalGapGenerator_FlagsAreaServedWithoutLocationPages` with radius places |
| 4 | Manual re-analyze geekatyourspot.com |

---

## Build order (execute)

1. ✅ This plan document  
2. ✅ Persistence entity + migration (Geek-SEO repo)  
3. ✅ Application request/DTO fields  
4. ✅ Frontend settings panel + `updateProject`  
5. ⏳ GeekBackend GeekRepository (separate PR/deploy) — **required before production save works**  
6. Phase 2 geocode service  
7. Phase 3 Step 11 merge  

---

## Success criteria (v1 local)

- [ ] User sets address + 20 mi on project settings; survives reload after GeekRepository deploy  
- [ ] Re-analyze lists gaps for cities within radius, not only schema counties  
- [ ] Topical map suggests local article titles using radius places  
- [ ] No Google My Business OAuth anywhere in the flow  

---

*Draft — 2026-06-06. Jeff: global local via address + adjustable radius; Maps yes, My Business deferred.*
