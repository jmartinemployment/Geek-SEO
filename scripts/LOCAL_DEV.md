# Local development (no WordPress required)

Geek SEO works end-to-end without WordPress. Publishing to WordPress is optional when you have a site later.

## Stack

| Service | Port | Repo |
|---------|------|------|
| geek-OAuth | 3001 | `../geek-OAuth` |
| GeekRepository | 5050 | `../GeekBackend/GeekRepository` |
| GeekAPI | 8080 | `../GeekBackend/GeekAPI` |
| Next.js frontend | 3000 | `frontend/` |

## 1. Database

Apply SEO migrations on your Supabase/Railway Postgres:

```bash
cd ../GeekBackend/GeekRepository
export DATABASE_URL="postgresql://..."
dotnet ef database update --context SeoDbContext
```

## 2. GeekRepository

```bash
cd ../GeekBackend/GeekRepository
export DATABASE_URL="postgresql://..."
export REPO_API_KEY="dev-repo-key-change-me"

# Optional — SERP + keyword research
export DATAFORSEO_LOGIN=...
export DATAFORSEO_PASSWORD=...

# Optional — AI writing, briefs, auto-optimize
export ANTHROPIC_API_KEY=sk-ant-...

# Skip Playwright if Chromium not installed locally
# export DISABLE_PLAYWRIGHT=true

dotnet run
```

## 3. GeekAPI

```bash
cd ../GeekBackend/GeekAPI
export REPO_URL=http://localhost:5050
export REPO_API_KEY=dev-repo-key-change-me
export AUTH_SERVER_URL=http://localhost:3001

# Dev auth bypass (use a real users.id from your DB)
export DEV_USER_ID=00000000-0000-0000-0000-000000000001

dotnet run --urls http://localhost:8080
```

## 4. Frontend

```bash
cd frontend
cp .env.example .env.local
```

`.env.local` without OAuth:

```
NEXT_PUBLIC_API_URL=http://localhost:8080
NEXT_PUBLIC_DEV_USER_ID=00000000-0000-0000-0000-000000000001
```

With OAuth (geek-OAuth on :3001):

```
NEXT_PUBLIC_API_URL=http://localhost:8080
NEXT_PUBLIC_AUTH_URL=http://localhost:3001
NEXT_PUBLIC_APP_URL=http://localhost:3000
NEXT_PUBLIC_CLIENT_ID=geekseo
NEXT_PUBLIC_REDIRECT_URI=http://localhost:3000/auth/callback
```

```bash
npm run dev
```

Open http://localhost:3000 → Dashboard, Guided flow, Editor, Calendar.

## API smoke test (no browser)

```bash
API_URL=http://localhost:8080 \
DEV_USER_ID=<your-users-table-uuid> \
node scripts/e2e-smoke.mjs
```

With AI/SERP (optional):

```bash
RUN_KEYWORD_RESEARCH=true RUN_BRIEF=true RUN_AI_TOOLS=true \
API_URL=http://localhost:8080 DEV_USER_ID=<uuid> \
node scripts/e2e-smoke.mjs
```

See `scripts/E2E_SMOKE.md` for full flag list.

## What works without WordPress

- Projects, documents, real-time SEO scoring (SignalR)
- Competitor insights (Playwright crawl when enabled)
- Keyword research (DataForSEO)
- Content briefs, guided full-article jobs
- Editor AI tools (humanize, detect, auto-optimize)
- Content calendar (status kanban)
- Export: copy HTML from editor or use “Skip publish” in guided mode
