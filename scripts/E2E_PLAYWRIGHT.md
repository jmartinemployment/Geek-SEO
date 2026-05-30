# Playwright E2E (browser)

Complements `scripts/E2E_SMOKE.md` (API-only, no browser).

## Commands (from `frontend/`)

| Command | Target | Credentials |
|---------|--------|-------------|
| `npm run test:e2e:smoke` | Production (default) | None |
| `npm run test:e2e:auth:local` | `127.0.0.1:3000` + `:5051` | `NEXT_PUBLIC_DEV_USER_ID` in `.env.local` |
| `npm run test:e2e:auth` | Production | `PLAYWRIGHT_TEST_*` in `.env.playwright.local` |

## Local authenticated (recommended for dev)

```bash
cd frontend
npm run test:e2e:auth:local
```

Starts GeekSeoBackend and Next.js if they are not already running. Expect **4 passed** (projects, dashboard, rankings, analytics).

## Production authenticated (optional)

1. Copy `frontend/.env.playwright.example` → `frontend/.env.playwright.local`
2. Set a GeekOAuth user **without 2FA**
3. Run `npm run test:e2e:auth`

## GitHub Actions

| Workflow | Trigger | Status |
|----------|---------|--------|
| `e2e-smoke.yml` | Every push/PR touching `frontend/**` | No secrets — **must stay green** |
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
