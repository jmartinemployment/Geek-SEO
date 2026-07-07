# Geek-SEO

Monorepo for **Geek SEO** (seo.geekatyourspot.com): niche analysis, rank tracking, topical maps, and AI-assisted **Content Writer**.

## Architecture

| Layer | Path | Role |
|-------|------|------|
| **Frontend** | `frontend/` | Next.js app (Vercel) |
| **Product API** | `GeekSeoBackend/` | REST + SignalR on port **5051**; hosts Content Writer routes |
| **Shared domain** | `GeekSeo.Application/` | Services, DTOs, usage metering |
| **Persistence models** | `GeekSeo.Persistence/` | EF Core entities + migrations (`geek_seo` schema) |
| **Content Writer** | `content-writer/backend/` | Generation pipeline; mounted inside GeekSeoBackend |
| **Data gateway** | External **GeekAPI** | Postgres access via `GEEK_API_URL` (GeekRepository) |

GeekSeoBackend does **not** connect to Postgres directly. Repositories under `GeekSeoBackend/HttpClients/Repo/` call GeekAPI internal routes. Content Writer uses its own database (`content_writer` schema) via `CONTENT_WRITER_DATABASE_URL`.

## Local development

### Backend (GeekSeoBackend + Content Writer)

```bash
cd GeekSeoBackend
cp .env.example .env   # set GEEK_API_URL, provider keys, CONTENT_WRITER_DATABASE_URL
dotnet run --urls http://localhost:5051
```

### Frontend

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

Open http://localhost:3000 — Content Writer at `/content-writer`.

See `content-writer/README.md` for LLM providers, SQLite vs Postgres, and import tooling.

## Railway deployment

One **GeekSeoBackend** service per environment (`Dockerfile.geekseo` at repo root).

Required env vars (copy from retired standalone services if migrating):

- `GEEK_API_URL` — GeekAPI gateway
- `CONTENT_WRITER_DATABASE_URL` — Supabase Postgres (`content_writer` schema)
- `LlmProviders__*` — OpenAI / Anthropic / LM Studio keys
- Google OAuth, DataForSEO, etc. (see `GeekSeoBackend/.env.example`)

Vercel frontend: `NEXT_PUBLIC_SEO_API_URL` → `https://seo-api.geekatyourspot.com` (Content Writer uses the same base URL).

Do **not** deploy a separate ContentWriter Railway service (`content-writer/railway.toml` is deprecated).

## Database migrations

Apply Geek SEO schema migrations against the GeekAPI/GeekRepository Postgres instance:

```bash
dotnet ef database update --project GeekSeo.Persistence --startup-project GeekSeo.Persistence
```

Recent removals (July 2026):

- **Site Analyzer 2** — `RemoveSiteAnalyzer2Tables` drops `seo_site_research*`, `seo_site_analyzer_step_run`
- **URL research pipeline** — `RemoveUrlResearchTables` drops `seo_url_research*` and `UrlResearchId` on content documents

Run both migrations before deploying backend builds that no longer reference those tables.

## Retired features

The following were removed from this repo; delete any matching Railway projects/services in the dashboard:

| Feature | Notes |
|---------|--------|
| **SiteAnalyzer2** | Standalone site crawl + SA2 handoff; replaced by Niche Analyzer + Content Writer |
| **URL research API** | `/api/seo/url-research` (SA2 page research); no frontend consumers |
| **Standalone ContentWriter service** | API now lives inside GeekSeoBackend |

Legacy columns on `seo_content_documents` (`SiteProfileId`, `KeywordBundleJson`, `AnalysisRunId`, `SiteFocusJson`) are kept for existing rows but are no longer written by Site Analyzer.
