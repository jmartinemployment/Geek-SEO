# Geek SEO API smoke test

Runs against **GeekSeoBackend** (no browser). WordPress is not required.

## Prerequisites

1. GeekSeoBackend on `:5051` with database migrated and `/health` OK
2. A real `users.id` for dev auth (`DEV_USER_ID` or `X-User-Id` header)

## Quick run

```bash
cd Geek-SEO

API_URL=http://localhost:5051 \
DEV_USER_ID=<your-users-table-uuid> \
node scripts/e2e-smoke.mjs
```

Covers: health, projects, content CRUD, competitors.

## Optional (billable APIs on GeekSeoBackend)

```bash
RUN_KEYWORD_RESEARCH=true \
RUN_BRIEF=true \
RUN_AI_TOOLS=true \
RUN_FULL_ARTICLE=true \
node scripts/e2e-smoke.mjs
```

Provider env vars must be set on **GeekSeoBackend**, not on this frontend repo.

## Browser E2E (Playwright)

See [`E2E_PLAYWRIGHT.md`](./E2E_PLAYWRIGHT.md) — smoke on every CI push, local auth via `npm run test:e2e:auth:local`.
