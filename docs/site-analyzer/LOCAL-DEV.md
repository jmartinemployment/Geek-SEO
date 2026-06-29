# Local development

Production operators use Railway + Supabase. Local dev is optional — for Api/Web changes or debugging against a shared or local database.

## Prerequisites

- .NET 10 SDK
- Node.js 20+ (Web)
- Docker (optional local Postgres)

## Database options

### Shared Supabase (simplest)

Use the same `SITE_ANALYZER2_DATABASE_URL` as production (session pooler, port **5432**). Migrations run on Api startup.

```bash
cp src/SiteAnalyzer2.Api/.env.example src/SiteAnalyzer2.Api/.env
# Edit SITE_ANALYZER2_DATABASE_URL, CORS_ORIGINS, BUSINESS_FOCUS_PROVIDER
```

### Local Postgres

```bash
docker compose up -d
```

Connection string (key=value, not `postgresql://` URI):

```text
Host=localhost;Port=5432;Database=siteanalyzer2;Username=postgres;Password=postgres
```

## Run Api

```bash
cd src/SiteAnalyzer2.Api
dotnet run --launch-profile http
```

Default: `http://localhost:5080` ([launchSettings.json](../src/SiteAnalyzer2.Api/Properties/launchSettings.json)).

Required env (via `.env` or shell):

| Variable | Example |
|----------|---------|
| `SITE_ANALYZER2_DATABASE_URL` | Supabase or local string above |
| `CORS_ORIGINS` | `http://localhost:3000` |
| `BUSINESS_FOCUS_PROVIDER` | `human` |
| `SERP_EXECUTION` | `manual` |

Optional: `GEEK_SEO_PROJECT_ID` for first SERP import on a new site URL.

## Run Web

```bash
cd src/SiteAnalyzer2.Web
cp .env.local.example .env.local
# NEXT_PUBLIC_API_URL=http://localhost:5080
npm install
npm run dev
```

Open `http://localhost:3000`. Web defaults to `http://localhost:5080` if `NEXT_PUBLIC_API_URL` is unset.

## Script import against local Api

```bash
export SITE_ANALYZER2_API_URL=http://localhost:5080
./scripts/import-serp-html.sh
```

## Tests

```bash
dotnet test
```

## Legacy (not operator workflow)

- `SiteAnalyzer2.SerpWorker` — automated Google scrape for dev/tests
- Step-gated `/runs/*` pipeline — see [archive/SiteAnalyzer-PLAN.md](archive/SiteAnalyzer-PLAN.md)

## Related

- [OPERATOR-WORKFLOW.md](OPERATOR-WORKFLOW.md)
- [RESEARCH-MODEL.md](RESEARCH-MODEL.md)
