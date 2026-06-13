# Geek SEO — Repo Root

## Solution Layout

```
Geek-SEO/
  GeekSeo.Application/     Contracts only — interfaces, models, DTOs, application services
  GeekSeo.Persistence/     EF Core entities + migrations (geek_seo schema); no business logic
  GeekSeoBackend/          ASP.NET Core API — providers, services, controllers, SignalR hub
  GeekSeoBackend.Tests/    xUnit tests; InternalsVisibleTo on main project
  frontend/                Next.js 15 App Router SaaS UI
  plan-documents/          Design specs for in-flight features
  docs/                    Architecture references
  BOUNDARIES.md            Hard rules on what each layer may touch
  PROJECT_STATUS.md        Current roadmap and sprint status
```

## Runtime Architecture

```
Browser (Next.js on Vercel)
    │ JWT (Bearer)
    ▼
GeekSeoBackend   :5051   ← this repo; providers + scoring + SignalR
    │ HTTP  GEEK_API_URL
    ▼
GeekAPI          :5000   ← GeekBackend repo; validates JWTs; SEO internal proxy
    │ HTTP  REPO_URL
    ▼
GeekRepository   :5050   ← GeekBackend repo; all DB writes; EF Core + Dapper
    │
    ▼
PostgreSQL (Railway)  schema: geek_seo
```

**Identity:** JWT issued by GeekOAuth. User IDs are `asp_net_users.id` UUIDs. NOT Supabase auth.users.

**Persistence rule:** GeekSeoBackend has NO direct DB access. All persistence via `api/seo/internal/*` on GeekAPI → GeekRepository. HTTP repos live in `HttpClients/Repo/`.

## Build & Test

```bash
# from repo root
dotnet build GeekSEO.slnx
dotnet test GeekSEO.slnx

# run GeekSeoBackend
dotnet run --project GeekSeoBackend   # http://localhost:5051/health

# EF migration (from repo root)
export DATABASE_URL='postgresql://...'
dotnet ef migrations add MigrationName \
  --project GeekSeo.Persistence/GeekSeo.Persistence.csproj \
  --context SeoDbContext
```

## Key Boundaries

- **GeekSeoBackend** → may reference `GeekSeo.Application` only; never GeekBackend directly
- **GeekSeo.Application** → no EF, no HttpClient, no infrastructure packages; pure contracts
- **GeekSeo.Persistence** → owned by this repo; GeekRepository references it at runtime
- **Frontend** → calls GeekSeoBackend only; never GeekAPI directly
- **GeekOAuth** — DO NOT TOUCH. Platform identity; separate repo, separate deploy, separate team scope.

## Sub-directory CLAUDE.md Files

| Location | Contents |
|----------|---------|
| `GeekSeoBackend/CLAUDE.md` | Run, endpoints, env, providers, auth, session notes |
| `GeekSeoBackend/Services/CLAUDE.md` | Service inventory, SignalR, background worker patterns |
| `GeekSeoBackend/HttpClients/Repo/CLAUDE.md` | HTTP repo pattern + full inventory |
| `GeekSeo.Application/CLAUDE.md` | Interface + model + application service inventory |
| `GeekSeo.Persistence/CLAUDE.md` | Entities, migration commands, consumers |
| `frontend/CLAUDE.md` | Routes, key files, local dev, tests, session notes |
| `frontend/src/components/niche-analyzer/CLAUDE.md` | Niche analyzer component inventory |

### Session Notes

**2026-06-13:** **Niche analyzer refactor docs aligned** — Documentation now reflects the backend-owned canonical 14-step pipeline, no fallback reconstruction/substitution, explicit persisted step artifacts, and the rule that Step 8 `keywords` is a standalone executable section under usefulness review. Updated the primary source-of-truth docs in `plan-documents/SEARCH-UNDERSTANDING-LAYER.md`, `plan-documents/SITE-NICHE-ANALYZER.md`, `GeekSeoBackend/CLAUDE.md`, `GeekSeoBackend/Services/CLAUDE.md`, `GeekSeoBackend/HttpClients/Repo/CLAUDE.md`, `GeekSeo.Application/CLAUDE.md`, `frontend/CLAUDE.md`, `frontend/src/components/niche-analyzer/CLAUDE.md`, and `PROJECT_STATUS.md`.

**2026-06-08:** **Event-driven workers + cache fixes + step 8 perf** — Replaced 5s/6s polling loops in `FullArticleJobWorker`, `BulkArticleJobWorker`, `NicheAnalysisJobWorker` with three typed `Channel<byte>` singletons (cap 500); workers do startup drain then `ReadAllAsync` — zero polling. Fixed 401s on `jobs/pending`: added `&userId=` to `HttpBackgroundJobRepository.GetPendingAsync`. Fixed silent cache failure: `HttpSerpCacheRepository` + `HttpKeywordVendorSnapshotRepository` were sending no `userId` → every upsert 401'd → vendor API hit on every analysis. `SeoMaintenanceWorker` niche re-analysis now queues profiles via channel instead of running `NicheAnalysisBackgroundJob` inline. `SeoUsageGateMiddleware` caches subscription tier 5 min/user. Step 8 (`PillarDemandEnricher`): keyword+SERP phases now parallel, `MaxConcurrency` 4→8, O(N²) sort fixed. `VendorPersistenceSettings` simplified — `RETENTION_DAYS` removed, `SEO_VENDOR_SERP_CACHE_DAYS` + `SEO_VENDOR_KEYWORD_CACHE_DAYS` are sole config (both 90 in Railway); missing env now throws at startup. `SERP_PROVIDER=serpapi` confirmed active. **Next:** production re-analyze geekatyourspot.com; verify cache hits on re-analysis (no DataForSEO spend).

**2026-06-07:** **sul-2.0 + hang fix** — `TopicFusionEngine` → `PillarSelector`; schema/GSC topics unconditionally selected. Deleted TopicCorroboration, FusionSnapshotEnricher; renamed FusedSiteUnderstanding → SiteTopicProfile. Frontend Fusion* components renamed. SignalR race fix. Job timeout 8→15 min, stall warning 2→5 min (more pillars = slower step 8). Worker `totalSteps: 12` → 14. GeekBackend pin: `197c14d`. **Vendor DB persistence** — `DatabaseBackedSerpProvider` + `DatabaseBackedKeywordProvider`; tables `seo_serp_results` + `seo_keyword_vendor_snapshots`. Env: `SEO_VENDOR_SERP_CACHE_DAYS=30`, `SEO_VENDOR_KEYWORD_CACHE_DAYS=60`. **Next:** production re-analyze geekatyourspot.com; verify sul-2.0 schema topics all selected.

**2026-06-06:** **SUL validated (unit)** — 45 tests pass (`NicheExtractionTests` + `LocalServiceAreaDefaultsTests`); fusion `sul-1.3`; Phases A–E shipped in 14-step `NicheAnalyzerService`. **Next:** production re-analyze geekatyourspot.com vs [`docs/reference/geekatyourspot-niche-baseline.md`](docs/reference/geekatyourspot-niche-baseline.md); GeekRepository deploy for local service area Phase 1. PayPal deferred.

**2026-06-03:** Drafted [`plan-documents/SEARCH-UNDERSTANDING-LAYER.md`](plan-documents/SEARCH-UNDERSTANDING-LAYER.md). Phase A partial: schema step log, pillar cap, provenance UI.

*Last updated: 2026-06-13*
