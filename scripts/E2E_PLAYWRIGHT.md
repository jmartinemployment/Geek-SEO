# Playwright E2E (browser)

Complements `scripts/E2E_SMOKE.md` (API-only, no browser).

## Commands (from `frontend/`)

| Command | Target | Credentials |
|---------|--------|-------------|
| `npm run test:e2e:smoke` | Production (default) | None |
| `npm run test:integration:google` | Production API | Dev user header (see script) |
| `npm run test:e2e:google` | `localhost:3000` + production API | `NEXT_PUBLIC_DEV_USER_ID` in `.env.local` |
| `npm run test:e2e:auth:local` | `localhost:3000` + `:5051` | `NEXT_PUBLIC_DEV_USER_ID` in `.env.local` |
| `npm run test:e2e:auth` | Production | `PLAYWRIGHT_TEST_*` in `.env.playwright.local` |

## Local authenticated (recommended for dev)

```bash
cd frontend
npm run test:e2e:auth:local
```

Starts GeekSeoBackend and Next.js if they are not already running. Expect **4 passed** (projects, dashboard, rankings, analytics).

## Google GSC/GA4 integration

Verifies connect-url and status against production GeekSeoBackend (does **not** complete Google sign-in).

```bash
cd frontend
npm run test:integration:google   # API only (~1s)
npm run test:e2e:google             # API + browser redirect to accounts.google.com
```

The UI test uses `http://localhost:3000` (not `127.0.0.1`) so production CORS allows browser calls.

## Production authenticated (optional)

1. Copy `frontend/.env.playwright.example` → `frontend/.env.playwright.local`
2. Set a GeekOAuth user **without 2FA**
3. Run `npm run test:e2e:auth`

## GitHub Actions

| Workflow | Trigger | Status |
|----------|---------|--------|
| `e2e-smoke.yml` | Every push/PR touching `frontend/**` | No secrets — **must stay green** |
| `e2e-google-integration.yml` | Push/PR touching `frontend/**` or GeekSeoBackend | API-only Google OAuth wiring — no secrets |
| `e2e-authenticated.yml` | Weekly + manual | Requires repo secrets (see below) |

### One-time: enable prod login in CI

```bash
cd Geek-SEO
gh secret set PLAYWRIGHT_TEST_EMAIL --body "your-test-user@example.com"
gh secret set PLAYWRIGHT_TEST_PASSWORD --body "your-password"
gh workflow run e2e-authenticated.yml
```

Then open Actions → **E2E authenticated (Playwright)** → latest run.

Without secrets, the authenticated workflow **skips** (does not fail).

## CI history note

Earlier smoke failures (`npm ci` out of sync) were fixed when `@playwright/test` was added to `package-lock.json` (commit `fc42364`).
