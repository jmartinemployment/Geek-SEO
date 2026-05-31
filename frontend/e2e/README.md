# E2E tests (Playwright)

Smoke tests against deployed Geek SEO (default: production). No mock data.

## Run

```bash
cd frontend
npm run test:e2e          # all specs
npm run test:e2e:smoke    # public smoke only (pricing catalog + sandbox checkout API)
npm run test:integration:google  # Google connect-url API (production backend)
npm run test:integration:plagiarism  # Copyscape status (optional provider)
npm run test:integration:site-audit  # Site audit tier gate (production backend)
npm run test:e2e:google   # Google UI + OAuth redirect (local Next + prod API)
```

### Authenticated tests (optional)

Requires a **GeekOAuth** user without 2FA. Copy `.env.playwright.example` → `.env.playwright.local` and set:

- `PLAYWRIGHT_TEST_EMAIL`
- `PLAYWRIGHT_TEST_PASSWORD`

Playwright auto-loads `frontend/.env.playwright.local` (you can still `source` it if you prefer).

```bash
npm run test:e2e:auth
```

### Local authenticated tests (no password)

If `frontend/.env.local` has `NEXT_PUBLIC_DEV_USER_ID` and GeekSeoBackend is reachable:

```bash
npm run test:e2e:auth:local
```

Starts backend/frontend if needed (restarts Next on `:3000` if occupied), then runs authenticated specs against `http://localhost:3000`. Includes site audit page and optional plagiarism panel on the content editor (dev API setup).

`PLAYWRIGHT_TEST_PASSWORD` must be non-empty for production OAuth login. Without credentials, tests are **skipped** (not “No tests found”).

`global-setup.ts` logs in once and saves `e2e/.auth/user.json` so the authenticated project reuses the session.

## CI (GitHub Actions)

| Workflow | When | Secrets |
|----------|------|---------|
| `e2e-smoke.yml` | Push/PR to `main` (frontend changes) | None |
| `e2e-google-integration.yml` | Push/PR (frontend or GeekSeoBackend) | None |
| `e2e-seo-api-integration.yml` | Push/PR (frontend, GeekSeoBackend, GeekSeo.Application) | None |
| `e2e-authenticated.yml` | Weekly + manual | `PLAYWRIGHT_TEST_EMAIL`, `PLAYWRIGHT_TEST_PASSWORD` in repo secrets |

## Targets

| Variable | Default |
|----------|---------|
| `PLAYWRIGHT_BASE_URL` | `https://seo.geekatyourspot.com` |
| `PLAYWRIGHT_API_URL` | `https://geekseobackend-production.up.railway.app` |

Local app + API:

```bash
PLAYWRIGHT_BASE_URL=http://localhost:3000 \
PLAYWRIGHT_API_URL=http://localhost:5051 \
npm run test:e2e
```

## UI mode

```bash
npm run test:e2e:ui
```
