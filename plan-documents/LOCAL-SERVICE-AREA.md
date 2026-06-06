# Local SEO — business address + service radius

**Status:** Planned — not started (implementation skipped for now)  
**Decision (2026-06-06):** Prefer **global local via address + adjustable radius** (default **20 miles**). Use **Google Maps Platform** (Geocoding / Places) server-side for address → coordinates and places-within-radius — **not** Google My Business Profile OAuth. See [`LOCAL-GBP-INTEGRATION.md`](./LOCAL-GBP-INTEGRATION.md) (My Business on hold).  
**Builds on:** Step 11 `LocalGapGenerator` (shipped) — today uses schema `areaServed` + on-site location URLs only  
**Dogfood site:** `https://www.geekatyourspot.com`

---

## Product direction

Local SEO should work for every project **without connecting Google**. The user sets (or confirms) a **business address** and a **service radius**. Geek SEO treats everything within that radius as the service area for gaps, topics, and recommendations.

| Setting | Default | Notes |
|---------|---------|--------|
| Business address | From project URL / schema when available | Street, city, state, postal code, country |
| Service radius | **20 miles** | User-adjustable per project (e.g. 5–100 mi) |
| Apply globally | On for local-service audience types | Can default radius at account or org level later |

**Plain language:** “We’re based here; we serve within X miles — do we have pages and content for places in that circle?”

---

## Why not Google My Business first

Google **My Business / Business Profile** (OAuth, listing picker, NAP compare) is **deferred**. See [`LOCAL-GBP-INTEGRATION.md`](./LOCAL-GBP-INTEGRATION.md).

Instead, use **Google Maps Platform** on the backend:

| API | Use | Auth |
|-----|-----|------|
| **Geocoding API** | Address → lat/lng | Server API key (`GOOGLE_MAPS_API_KEY`) — no user login |
| **Places API** (optional) | Validate address, nearby cities | Same key; quota-aware |

No end-user OAuth. No “Connect Google Business” flow. Maps is infrastructure for radius math, not listing management.

Address + radius:

- No extra accounts or consent screens
- Works for sites without a GBP listing
- Same model for niche analyzer, topical map, and Content Guard
- My Business can be an **optional enrichment layer later**, not the foundation

---

## What Step 11 does today (shipped)

`LocalGapGenerator` compares:

1. **Declared areas** — from JSON-LD `areaServed` (and related schema)
2. **Location pages** — URLs like `/locations/`, `/areas/`, city slugs from sitemap/crawl

Output: gaps (“Broward County listed on site but no landing page”) + `suggest_local_page` actions.

**Limitation:** Depends on schema listing counties/cities explicitly. Many sites only have an address, not a full `areaServed` list.

---

## Planned behavior (when implemented)

### 1. Project settings — local service area

| Field | Storage (sketch) | UI |
|-------|------------------|-----|
| `business_address` | JSON on `seo_projects` or `seo_project_local_settings` | Address form with geocode validation |
| `service_radius_miles` | Integer, default **20** | Slider or number input (5–100) |
| `local_enabled` | Boolean, default true for `local_service` audience | Toggle “Include local SEO for this project” |

Optional: inherit org-wide default radius (20 mi) with per-project override.

### 2. Geocode + radius → area list

1. Geocode address once (cache lat/lng on project).
2. Resolve **cities, towns, and counties** within radius:
   - **Preferred:** Google Maps Geocoding + distance filter on cached place dataset, or Places Nearby Search bounded by radius
   - **Fallback:** OpenStreetMap Nominatim + boundary dataset (no Google bill)
   Document provider choice before build; cap results (e.g. top 50 by population or distance).
3. Produce `ServiceAreaDefinition`:
   - `center`: lat/lng
   - `radiusMiles`: 20 (or user value)
   - `places[]`: `{ name, type, distanceMiles }` capped for UI (e.g. top 50 by population or distance)

### 3. Niche analyzer (Step 11 evolution)

Merge or replace pure `areaServed` logic:

| Source | Priority |
|--------|----------|
| Radius-derived places | Primary when address + radius set |
| Schema `areaServed` | Supplement / validate (warn if schema areas fall outside radius) |
| On-site location pages | Unchanged — match slugs to place names |

**Gaps:** “Within your 20-mile service area, these 12 cities have no dedicated page.”

**Actions:** Extend `FusionActionRecommender` — `suggest_local_page` titles use place names from radius list.

### 4. Downstream features (same radius)

| Feature | Use |
|---------|-----|
| Topical map | Local intent modifiers: `{pillar} + {city}` for places in radius |
| Keyword / SERP (future) | Optional “local pack” context for queries tagged local |
| Content Guard | Badge when decaying URL maps to a radius place |
| Dashboard copilot | “Add location pages for 3 cities within your service area” |

---

## Architecture (sketch)

```
Project settings (address, radiusMiles default 20)
        │
        ▼
GeocodeService → lat/lng (cached)
        │
        ▼
ServiceAreaResolver → places within radius
        │
        ├─► LocalGapGenerator (Step 11) — gaps vs location URLs
        ├─► FusedSiteUnderstanding.localGeography — extended model
        └─► Topical map / copilot / Content Guard (read same definition)
```

**Boundaries (unchanged):**

- GeekSeoBackend — geocode call, resolver, gap logic, orchestrator
- GeekRepository — persist address, radius, cached geocode, optional place list snapshot
- GeekSeo.Application — DTOs only
- Frontend — project settings + Local geography panel (plain language)

No user OAuth. **`GOOGLE_MAPS_API_KEY`** (or org secret) in GeekSeoBackend env only; restrict key by IP/referrer per Google Cloud best practice.

---

## Data model (sketch)

```sql
-- Option: columns on seo_projects
ALTER TABLE geek_seo.seo_projects ADD COLUMN IF NOT EXISTS
  local_settings JSONB DEFAULT NULL;
-- {
--   "enabled": true,
--   "address": { "line1", "city", "region", "postalCode", "country" },
--   "radiusMiles": 20,
--   "geocode": { "lat", "lng", "provider", "geocodedAt" },
--   "placesSnapshot": [ { "name", "type", "distanceMiles" } ],
--   "placesSnapshotAt": "ISO8601"
-- }
```

Global default: env or platform config `DEFAULT_SERVICE_RADIUS_MILES=20` until org-level settings exist.

---

## UI copy rules

- Say **“service area”** and **“within X miles of your address”** — not `areaServed`, geocode, or OAuth.
- Local geography panel: show address (masked if needed), radius, count of places, gaps list.
- Settings: one screen under project — address + radius slider, not a separate “integrations” flow.

---

## Phased rollout (when picked up)

| Phase | Scope | Verify |
|-------|--------|--------|
| **1** | Project settings UI + API persist address + radius (default 20) | Save/load; validation |
| **2** | Geocode + places-within-radius (backend) | geekatyourspot.com → sensible FL cities within 20 mi |
| **3** | Step 11 uses radius places + existing location URL match | Re-analyze shows gaps from radius, not only schema |
| **4** | Topical map + copilot + Content Guard read same `ServiceAreaDefinition` | One source of truth |

**Explicitly deferred:** Google My Business Profile OAuth, listing NAP compare, review metrics — see on-hold doc. Maps Platform geocoding is **in scope** for Phase 2 when local radius ships.

---

## Testing strategy (when implemented)

| Layer | Tests |
|-------|--------|
| Unit | Radius place resolver with fixed lat/lng + mock place dataset |
| Unit | `LocalGapGenerator` — radius places vs location URLs; schema outside radius → warning |
| Integration | Geocode fixture (no live API in CI) |
| Manual | geekatyourspot.com: set address, 20 mi, re-analyze; confirm local gaps without schema counties |

---

## Links

- Shipped Step 11: `GeekSeoBackend/Services/NicheExtraction/LocalGapGenerator.cs`
- On-hold GBP plan: [`LOCAL-GBP-INTEGRATION.md`](./LOCAL-GBP-INTEGRATION.md)
- Niche plan index: [`SITE-NICHE-ANALYZER-CHANGES.md`](./SITE-NICHE-ANALYZER-CHANGES.md)
- Backlog: [`TODO.md`](./TODO.md)

---

*Last updated: 2026-06-06 — Jeff: address + 20 mi default; skip GBP/OAuth for now.*
