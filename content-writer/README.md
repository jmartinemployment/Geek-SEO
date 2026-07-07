# Content Writer

AI-assisted content generation for IT consulting projects. Takes a crawled client site plus manually-scraped
SERP research (keyword results, .edu/.gov/Wikipedia, local pack, competitor crawl, People-Also-Ask) and produces
a TechnicalArticle (JSON+LD), a companion BlogPost (JSON+LD, cross-linked to the article), and Facebook/LinkedIn
social posts.

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
cp .env.example .env.local   # set NEXT_PUBLIC_CONTENT_WRITER_API_URL=http://localhost:5199
npm install
npm run dev
```

Open **http://localhost:3000/content-writer** (production: **https://seo.geekatyourspot.com/content-writer**)

## Railway deployment

`content-writer/Dockerfile` and `content-writer/railway.toml` deploy the API as a separate service.

Required Railway variables:

- `CONTENT_WRITER_DATABASE_URL` — Supabase direct Postgres URI
- `ASPNETCORE_ENVIRONMENT=Production`
- LLM provider keys as needed (`LlmProviders__OpenAi__ApiKey`, etc.)

Point the Vercel frontend at the deployed API via `NEXT_PUBLIC_CONTENT_WRITER_API_URL`.

## Workflow

1. Create a project (name, client URL, target keyword, preferred LLM provider).
2. Crawl the project URL - extracts JSON+LD, headings, and paragraphs; heuristically detects tone/focus.
3. Upload the manually-scraped research: 6 keyword SERP results, .edu/.gov/Wikipedia pages, local pack,
   competitor crawl (all HTML), and a People-Also-Ask text file (one question per line).
4. Generate in steps — pillar plan, pillar body, blog, social — each persisted to the database.
