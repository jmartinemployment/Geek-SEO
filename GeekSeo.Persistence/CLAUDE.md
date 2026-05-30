# GeekSeo.Persistence

**Product-owned** PostgreSQL schema for Geek SEO (`geek_seo`). Geek SEO owns schema evolution; GeekRepository is the runtime data plane that references this assembly.

## Contains

| Path | Role |
|------|------|
| `Entities/` | EF entity types (`SeoProject`, `SeoContentDocument`, …) |
| `Data/SeoDbContext.cs` | DbContext + fluent configuration |
| `Data/SeoDbContextOptionsExtensions.cs` | Npgsql + migration history table in `geek_seo` schema |
| `Data/SeoDbContextDesignTimeFactory.cs` | Design-time `dotnet ef` |
| `Migrations/` | EF migrations for `geek_seo` |

## Commands (from Geek-SEO repo root)

```bash
export DATABASE_URL='postgresql://...'   # or GEEK_SEO_DATABASE_URL

dotnet ef migrations add MigrationName \
  --project GeekSeo.Persistence/GeekSeo.Persistence.csproj \
  --context SeoDbContext

dotnet ef migrations list \
  --project GeekSeo.Persistence/GeekSeo.Persistence.csproj \
  --context SeoDbContext

dotnet ef database update \
  --project GeekSeo.Persistence/GeekSeo.Persistence.csproj \
  --context SeoDbContext
```

Uses `SeoDbContextDesignTimeFactory` — do **not** pass `--startup-project GeekSeoBackend` (no Design package there).

GeekRepository at runtime uses the same context types; apply migrations from here before or as part of deploy.

## Consumers

| Consumer | Reference |
|----------|-----------|
| `GeekSeo.Application` (M2) | Entity types for interfaces |
| `GeekRepository` (GeekBackend) | `ProjectReference` to this project; sibling path locally |
| `GeekApplication` (GeekBackend) | May reference for build layout; SEO types are not in platform DLL after M6 |

## Plan

[`plan-documents/PLATFORM-DECOUPLING.md`](../plan-documents/PLATFORM-DECOUPLING.md) — **M3 complete**
