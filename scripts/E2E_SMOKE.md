# Geek SEO API smoke test

Runs against a live GeekAPI instance (no browser). **WordPress is not required.**

## Prerequisites

1. GeekRepository on `:5050` with migrations applied
2. GeekAPI on `:8080` with `REPO_URL` and `REPO_API_KEY`
3. A real `users.id` for dev auth (`DEV_USER_ID` or `X-User-Id` header)

## Quick run (core paths — no external APIs)

```bash
cd /Volumes/Seagate/development/Geek-SEO

API_URL=http://localhost:8080 \
DEV_USER_ID=<your-users-table-uuid> \
node scripts/e2e-smoke.mjs
```

Covers: health, projects, content CRUD, calendar status PATCH, competitors.

## With SERP / AI (optional)

```bash
# On GeekRepository
export DATAFORSEO_LOGIN=...
export DATAFORSEO_PASSWORD=...
export ANTHROPIC_API_KEY=sk-ant-...

API_URL=http://localhost:8080 \
DEV_USER_ID=<uuid> \
RUN_KEYWORD_RESEARCH=true \
RUN_BRIEF=true \
RUN_AI_TOOLS=true \
RUN_FULL_ARTICLE=true \
node scripts/e2e-smoke.mjs
```

`RUN_FULL_ARTICLE=true` polls `GET /api/seo/jobs/{id}` until complete (default 120s timeout).

## WordPress (optional — skip if you have no site)

```bash
RUN_WORDPRESS=true \
WP_SITE_URL=https://yoursite.com \
WP_USERNAME=admin \
WP_APP_PASSWORD=xxxx-xxxx-xxxx \
# GEEK_SEO_ENCRYPTION_KEY on GeekRepository
node scripts/e2e-smoke.mjs
```

## What it checks

| Step | Endpoint | Default run |
|------|----------|-------------|
| Health | `GET /health` | ✓ |
| Projects | `GET/POST /api/seo/projects` | ✓ |
| Content | `POST/GET/PUT /api/seo/content` | ✓ |
| Calendar status | `PATCH .../status` | ✓ |
| Competitors | `GET/POST .../competitors` | ✓ |
| Keywords | `POST .../keywords/research` | optional |
| Brief | `POST .../briefs/generate` | optional |
| AI tools | humanize, detect | optional |
| Full article | `POST .../full-article` + job poll | optional |
| WordPress | connect + publish | optional |

Exit code `0` = all executed steps passed.
