# E2E tests (Playwright)

Smoke tests against deployed Geek SEO (default: production). No mock data.

## Run

```bash
cd frontend
npm run test:e2e          # all specs
npm run test:e2e:smoke    # public smoke only (no credentials)
```

### Authenticated tests (optional)

Requires a **GeekOAuth** user without 2FA. Copy `.env.playwright.example` → `.env.playwright.local` and set:

- `PLAYWRIGHT_TEST_EMAIL`
- `PLAYWRIGHT_TEST_PASSWORD`

Load env when running (zsh/bash):

```bash
set -a && source .env.playwright.local && set +a && npm run test:e2e:auth
```

Without credentials, authenticated specs are **skipped** (not failed).

`global-setup.ts` logs in once and saves `e2e/.auth/user.json` so the authenticated project reuses the session.

## CI (GitHub Actions)

| Workflow | When | Secrets |
|----------|------|---------|
| `e2e-smoke.yml` | Push/PR to `main` (frontend changes) | None |
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
