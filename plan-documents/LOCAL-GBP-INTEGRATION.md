# Google My Business Profile (deferred)

**Status:** **Deferred** — do not implement listing OAuth until address + radius local is shipped.  
**Active plan:** [`LOCAL-SERVICE-AREA.md`](./LOCAL-SERVICE-AREA.md) — business address + service radius (default **20 miles**), **Google Maps Platform** (Geocoding/Places) server-side for geocode and places-within-radius.

---

## Decision (2026-06-06)

Jeff chose **address + adjustable radius** (default 20 mi) instead of leading with **Google My Business** (formerly Google My Business Profile).

**Use Maps, not My Business (for now):**

| | Google Maps Platform | Google My Business Profile |
|---|----------------------|----------------------------|
| Purpose | Geocode address, places within radius | Listing management, reviews, NAP |
| Auth | Server API key | User OAuth + `business.manage` |
| When | Phase 2 of LOCAL-SERVICE-AREA | Deferred optional enrichment |

My Business connect may return later to answer “does my Google listing match my site?” — not the foundation for local SEO in Geek SEO.

---

## What remains shipped (unchanged)

Step 11 `LocalGapGenerator` — on-site only:

- Schema `areaServed` vs location landing pages (`/locations/`, `/areas/`, etc.)
- No Google account required

Code: `GeekSeoBackend/Services/NicheExtraction/LocalGapGenerator.cs`

---

## Archived intent (reference only)

The original v1 GBP plan covered: OAuth + location picker, listing snapshot, NAP/service-area compare vs schema, performance metrics, Content Guard tie-in, and later posts/reviews/map-grid.

That design assumed GBP as the primary local signal. The new direction uses **address + radius** as primary; GBP would only add “does Google match my site?” when/if resumed.

For full archived phase breakdown, see git history of this file before 2026-06-06.

---

*Last updated: 2026-06-06 — on hold per product direction.*
