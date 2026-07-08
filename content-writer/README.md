# Content Writer

AI-assisted content generation for IT consulting projects. Takes a crawled client site plus manually-scraped
SERP research (keyword results, .edu/.gov/Wikipedia, local pack, competitor crawl, People-Also-Ask) and produces
a TechnicalArticle (JSON+LD), a companion BlogPost (JSON+LD, cross-linked to the article), Facebook/LinkedIn
social posts, and a cold outreach email.

This module lives inside the **Geek-SEO** monorepo.

## Architecture

- `content-writer/backend/` - .NET 10 Web API (`ContentWriter.Api`), EF Core repository (`ContentWriter.Infrastructure`),
  domain entities (`ContentWriter.Domain`), and the crawler/parser/LLM-provider/orchestrator layer
  (`ContentWriter.Application`).
- `frontend/src/app/content-writer/` - Next.js UI route inside the Geek-SEO frontend app.
- `frontend/src/components/content-writer/` and `frontend/src/lib/content-writer/` - UI components and API client.

LLM access goes through `IContentGenerationProvider`, implemented by `LmStudioProvider`, `OpenAiProvider`, and
`AnthropicProvider`. `IContentProviderFactory` resolves the right one per-project via keyed DI, keyed on
`Project.PreferredProvider`. LM Studio (`http://localhost:1234/v1/chat/completions`) is the default.

## Database

| Environment | Provider | Config |
|-------------|----------|--------|
| **Local (default)** | SQLite | `appsettings.Development.json` → `Data Source=contentwriter.db` |
| **Production / shared dev** | Supabase Postgres | `CONTENT_WRITER_DATABASE_URL` or `DATABASE_URL` |

Postgres tables live in the **`content_writer`** schema (same Supabase instance as `geek_seo`, separate schema).

Connection resolution order:

1. `CONTENT_WRITER_DATABASE_URL` (recommended — dedicated URI for this API)
2. `DATABASE_URL` (same Supabase project as GeekRepository)
3. `ConnectionStrings:ContentWriterDb` from `appsettings*.json`

On startup the API runs EF migrations automatically for Postgres. SQLite uses `EnsureCreated()` for frictionless local dev.

## Backend setup

```bash
cd content-writer/backend
dotnet tool install --global dotnet-ef   # if not already installed
dotnet build
```

### Local dev (SQLite — no Supabase required)

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ContentWriter.Api --urls http://localhost:5199
```

### Supabase Postgres

1. Copy `content-writer/backend/.env.example` → `.env` (or add to Geek-SEO repo root `.env` — DotNetEnv walks up).
2. Set your Supabase **direct** connection URI (port **5432**, not the transaction pooler on 6543):

   ```
   CONTENT_WRITER_DATABASE_URL=postgresql://postgres:YOUR_PASSWORD@db.YOUR_PROJECT.supabase.co:5432/postgres?sslmode=require
   ```

3. Apply migrations (also runs automatically on API startup):

   ```bash
   dotnet ef database update --project src/ContentWriter.Infrastructure --startup-project src/ContentWriter.Api
   ```

4. Start the API:

   ```bash
   dotnet run --project src/ContentWriter.Api --urls http://localhost:5199
   ```

### Import existing SQLite data into Supabase

Your local `contentwriter.db` (15 projects, generated content, etc.) can be copied once:

```bash
cd content-writer/backend
export CONTENT_WRITER_DATABASE_URL="postgresql://..."   # your Supabase URI
dotnet run --project tools/ImportSqlite/ImportSqlite.csproj
# Add --replace to truncate Postgres and re-import
```

Configure provider credentials in `appsettings.json` / `.env` under `LlmProviders`:

- `LlmProviders:LmStudio:BaseUrl` - defaults to `http://localhost:1234/v1/chat/completions`; just have LM Studio
  running with a model loaded.
- `LlmProviders:OpenAi:ApiKey` - required only if a project's preferred provider is OpenAI.
- `LlmProviders:Anthropic:ApiKey` - required only if a project's preferred provider is Anthropic.

`CompanyProfile` in `appsettings.json` controls the publisher name/logo and the base URLs used to build article
and blog slugs/links.

## Frontend setup

From the Geek-SEO `frontend/` directory:

```bash
cp .env.example .env.local   # NEXT_PUBLIC_SEO_API_URL=http://localhost:5051
npm install
npm run dev
```

Open **http://localhost:3000/content-writer** (production: **https://seo.geekatyourspot.com/content-writer**)

The Content Writer API client uses `NEXT_PUBLIC_SEO_API_URL` (Content Writer is integrated into GeekSeoBackend).

## Railway deployment

Content Writer is **hosted inside GeekSeoBackend** — one Railway project, one service.

| Service | Dockerfile | URL |
|---------|------------|-----|
| GeekSeoBackend | `Dockerfile.geekseo` (repo root) | `seo-api.geekatyourspot.com` |

Content Writer API routes (`/api/projects`, `/api/projects/{id}/generate/*`, etc.) are served by the same process on port **5051**. Do **not** deploy a separate ContentWriter Railway service.

```bash
docker build -f Dockerfile.geekseo -t geek-seo-backend .
```

Copy these Railway variables from the retired ContentWriter service onto **GeekSeoBackend**:

- `CONTENT_WRITER_DATABASE_URL` — Supabase direct Postgres URI (`content_writer` schema)
- `LlmProviders__OpenAi__ApiKey`, `LlmProviders__Anthropic__ApiKey`, etc.

The Vercel frontend uses `NEXT_PUBLIC_SEO_API_URL` (defaults Content Writer client to the same base URL).

### Local-only standalone API (optional)

For isolated backend development without GeekSeoBackend:

```bash
cd content-writer/backend
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ContentWriter.Api --urls http://localhost:5199
```

Set `NEXT_PUBLIC_CONTENT_WRITER_API_URL=http://localhost:5199` when using that mode.

## Workflow

1. Create a project (name, client URL, target keyword, preferred LLM provider).
2. Crawl the project URL - extracts JSON+LD, headings, and paragraphs; heuristically detects tone/focus.
3. Upload the manually-scraped research: 6 keyword SERP results, .edu/.gov/Wikipedia pages, local pack,
   competitor crawl (all HTML), and a People-Also-Ask text file (one question per line).
4. Generate in steps — pillar plan, pillar body, blog, social, cold outreach email — each persisted to the database.

## Retired integrations

**Site Analyzer 2** and the **URL research** pipeline (`/api/seo/url-research`) were removed from Geek-SEO in July 2026. Content Writer no longer depends on SA2 handoff or `SITE_ANALYZER2_DATABASE_URL`.

Frozen handoff fields on SEO content documents (`SiteProfileId`, `KeywordBundleJson`, etc.) may still exist in Postgres for old rows but are optional for new Content Writer projects.
