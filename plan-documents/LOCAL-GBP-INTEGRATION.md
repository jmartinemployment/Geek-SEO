# Local SEO — Google Business Profile & Maps (future)

**Status:** Planned — not started  
**Builds on:** Step 11 `LocalGapGenerator` (shipped) — on-site `areaServed` vs `/locations` URLs only  
**Dogfood site:** `https://www.geekatyourspot.com` (Next.js; counties in schema, no location landing pages yet)

---

## Why this is separate from Step 11

| | **Step 11 (shipped)** | **GBP/Maps (this plan)** |
|---|------------------------|---------------------------|
| Data source | Your site: JSON-LD, sitemap, crawl | Google Business Profile API |
| Question answered | “Do I have a page for each area I claim in schema?” | “Does Google know my business the same way my site/schema do?” |
| OAuth | None | Google OAuth (new scopes) |
| Maps | No | Reads listing metadata tied to Maps; optional grid rank tracking later |

Step 11 stays **always-on** during niche scan (no Google account required). GBP is an **optional connect** per project, same mental model as GSC/GA4.

---

## Product goals (v1 GBP)

1. **Connect** one or more GBP locations to an SEO project.
2. **Compare NAP + service areas** — GBP primary category, address, phone, website, `serviceArea` vs schema `areaServed` and on-site location pages.
3. **Surface gaps** in niche analyzer fusion and Content Guard context:
   - Schema county listed but not in GBP service area
   - GBP service area with no `/locations/…` page (extends Step 11)
   - Website URL on GBP ≠ project URL
   - Missing or stale primary category vs niche pillars
4. **Read-only metrics** (no posting yet): rating, review count, last post date, profile completeness score.
5. **Recommended actions** — extend `FusionActionRecommender` with types like `sync_gbp_service_area`, `create_location_page`, `fix_nap_mismatch`.

**Explicitly out of v1 GBP:** post scheduling, review replies, multi-directory sync (Yelp, Apple, Bing), map-grid rank tracking, paid Places API enrichment at scale.

---

## Phased rollout

### Phase A — OAuth + location picker (foundation)

| Step | Work | Verify |
|------|------|--------|
| A1 | Google Cloud: enable **Business Profile Performance API** (and Account Management API for listing list). Separate OAuth client or extend existing `GOOGLE_*` app with new redirect + scopes. | Dev project connects; token refresh works |
| A2 | Persist connection per project: `seo_google_gbp_connections` (or extend `seo_google_integrations` with `integration_type = 'gbp'`). Store encrypted refresh token, selected `location_id`, linked `account_id`. | Disconnect + reconnect; multi-instance safe state (Redis state store like GSC if needed) |
| A3 | API: `GET connect-url`, `GET callback`, `GET status`, `DELETE disconnect`, `GET locations` (list accounts/locations for picker). Mirror `GoogleIntegrationsController` pattern. | OAuth round-trip from `/app/projects/{id}` |
| A4 | UI: “Connect Google Business Profile” on project settings + niche analyzer empty state when local business detected. | geekatyourspot.com owner can pick listing |

**OAuth scopes (confirm against current Google docs before implement):**

- `https://www.googleapis.com/auth/business.manage` — manage/read locations (verify minimum read-only scope if Google offers split)
- Re-use existing user OAuth flow; **never** store tokens in frontend.

### Phase B — Snapshot + NAP compare (core value)

| Step | Work | Verify |
|------|------|--------|
| B1 | `GbpLocationSnapshotService` — fetch location details + service areas + categories; cache 24h in Postgres JSONB on project or dedicated table. | Snapshot after connect; manual refresh button |
| B2 | `GbpSchemaComparer` — inputs: snapshot + latest `FusedSiteUnderstanding.localGeography` + schema step outputs. Outputs: `GbpAlignmentReport` (matches, mismatches, warnings). | Broward/Palm Beach/Miami-Dade in schema vs GBP service areas |
| B3 | Extend `LocalGeographyAnalysis` model (non-breaking): optional `gbpAlignment?: GbpAlignmentReport`. Step 11 still runs without GBP; when connected, orchestrator merges GBP compare after Step 11. | Re-analyze with GBP connected adds alignment block |
| B4 | UI: **Google listing** panel under Local geography — side-by-side schema vs GBP vs site pages. Plain language, no raw API field names. | User understands “what to fix” without SEO jargon |

### Phase C — Metrics + actions + maintenance

| Step | Work | Verify |
|------|------|--------|
| C1 | Pull Performance API metrics: search views, maps views, website clicks (weekly series). | Sparkline on local panel |
| C2 | `FusionActionRecommender` — GBP-aware actions with priority above generic `suggest_local_page` when NAP mismatch is critical. | Action list shows “Fix phone mismatch on Google listing” |
| C3 | `SeoMaintenanceWorker` — weekly GBP snapshot refresh for connected projects (same pattern as GEO probe). | Railway worker logs success |
| C4 | Content Guard — optional badge: “This URL is your GBP website landing page” when decay detected on GBP-linked URL. | Decay row shows listing link |

### Phase D — Optional later (post-v1 GBP)

- GBP post draft + schedule (AI-assisted, approval queue like Content Guard)
- Review monitoring + suggested replies
- Map-grid local rank (DataForSEO or dedicated local rank provider — see `SEO-PROVIDER-STRATEGY.md`)
- Apple Business Connect / Bing Places (multi-directory NAP)
- `sameAs` validation: schema `sameAs` includes correct Maps place URL (`SameAsClassifier` already recognizes Maps URLs)

---

## Architecture (Geek SEO boundaries)

```
Browser → GeekSeoBackend
            ├─ GbpOAuthService (mirror GoogleOAuthService)
            ├─ GbpDataService → Google Business Profile APIs
            └─ HttpGbpConnectionRepository → GeekAPI → GeekRepository → geek_seo

NicheAnalyzerService Step 11
            ├─ LocalGapGenerator (always — on-site)
            └─ GbpSchemaComparer (when connection active)
```

- **GeekSeoBackend:** OAuth, provider calls, comparison logic, orchestrator hook.
- **GeekRepository:** CRUD for connections + snapshot JSONB; no direct Google calls.
- **GeekSeo.Application:** DTOs only (`GbpLocationSnapshot`, `GbpAlignmentReport`, `GbpConnectionStatus`).
- **Frontend:** connect flow, Local geography panel extension, project settings.

No WordPress write path required for v1 (copy-paste checklist for NAP fixes is fine).

---

## Data model (sketch)

```sql
-- Option A: dedicated table
CREATE TABLE geek_seo.seo_gbp_connections (
  id UUID PRIMARY KEY,
  project_id UUID NOT NULL REFERENCES geek_seo.seo_projects(id),
  user_id UUID NOT NULL,
  google_account_id TEXT NOT NULL,
  location_id TEXT NOT NULL,
  location_title TEXT,
  snapshot JSONB,           -- last GbpLocationSnapshot
  snapshot_fetched_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL,
  updated_at TIMESTAMPTZ NOT NULL,
  UNIQUE (project_id)
);
```

Store **minimal** snapshot fields (name, address, phone, website, categories, service areas, lat/lng if provided, rating, review count). Do not store full review text in v1.

---

## API surface (sketch)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/seo/integrations/gbp/connect-url?projectId=` | Start OAuth |
| GET | `/api/seo/integrations/gbp/callback` | OAuth callback (anonymous) |
| GET | `/api/seo/integrations/gbp/status?projectId=` | Connected? location name? |
| DELETE | `/api/seo/integrations/gbp?projectId=` | Disconnect |
| GET | `/api/seo/integrations/gbp/locations?projectId=` | Picker list (after OAuth) |
| POST | `/api/seo/integrations/gbp/select-location` | Save chosen `location_id` |
| POST | `/api/seo/integrations/gbp/refresh` | Force snapshot refresh |
| GET | `/api/seo/niche-analyzer/{profileId}/gbp-alignment` | Optional dedicated read (or embed in `analysis-details.fusionSnapshot`) |

---

## UI surfaces

1. **Project settings** — Connect / change listing / disconnect (parallel to GSC connect).
2. **Niche analyzer → Local geography** — three columns when GBP connected:
   - What schema says (`areaServed`)
   - What Google listing says (service areas, address)
   - What site has (location pages from Step 11)
3. **Dashboard copilot** — “Connect Google Business Profile to check listing vs your site.”
4. **Recommended actions** — GBP-specific fixes ranked by impact.

Copy rule: explain outcomes (“Your Google listing doesn’t mention Palm Beach County”) not API terms (“serviceArea mismatch”).

---

## Google Cloud / compliance checklist (before Phase A)

- [ ] Confirm API product names and quotas (Business Profile Performance vs legacy My Business API deprecation).
- [ ] OAuth consent screen: add scopes; verification if Google requires app review for `business.manage`.
- [ ] Separate test GBP listing for dev (or Google’s test accounts doc).
- [ ] Token storage: encrypt refresh tokens at rest in GeekRepository; same pattern as GSC tokens.
- [ ] Rate limits + exponential backoff in `GbpDataService`.
- [ ] Privacy: document in settings what GBP data is stored and retention (snapshots, not reviews body in v1).

---

## Testing strategy

| Layer | Tests |
|-------|--------|
| Unit | `GbpSchemaComparer` — schema areas vs mock snapshot; NAP normalize phone/URL |
| Unit | Extend `LocalGapGenerator` tests — unchanged when GBP null |
| Integration | Recorded HTTP fixtures for GBP API responses (no live Google in CI) |
| Manual | geekatyourspot.com: connect real listing; re-analyze; confirm 3 county gaps + GBP alignment |
| Playwright (later) | Connect flow mock; panel renders three-column compare |

---

## Build order summary

1. **Phase A** — OAuth + picker (unblocks everything)
2. **Phase B** — Snapshot + compare + niche UI (delivers user-visible value)
3. **Phase C** — Metrics, worker refresh, Content Guard tie-in
4. **Phase D** — Posts, reviews, grid rank (separate product decision)

Estimated effort: **Phase A+B ≈ 1 sprint**, **Phase C ≈ half sprint**, **Phase D multi-sprint**.

---

## Links

- Shipped local (on-site): `GeekSeoBackend/Services/NicheExtraction/LocalGapGenerator.cs`
- GSC OAuth reference: `GeekSeoBackend/Controllers/Seo/GoogleIntegrationsController.cs`
- Niche plan index: [`SITE-NICHE-ANALYZER-CHANGES.md`](./SITE-NICHE-ANALYZER-CHANGES.md)
- Competitor benchmark: SE Ranking Local Marketing Tool (`docs/research/competitors/seranking/…/local-marketing-tool_html-14/`)

---

*Last updated: 2026-06-06 — planned for post-v1 niche analyzer; Jeff confirmed interest.*
