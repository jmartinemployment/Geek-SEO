# E2E tests (Playwright)

Smoke tests against deployed Geek SEO (default: production). No mock data.

## Run

```bash
cd frontend
npm run test:e2e
```

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
