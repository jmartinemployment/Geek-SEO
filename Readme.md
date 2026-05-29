# Geek SEO

Surfer / ContentShake-style SEO content SaaS.

**Plan:** [`plan-documents/GEEKSEO-PLAN.md`](plan-documents/GEEKSEO-PLAN.md)

## Repo layout (intended)

| Path | Role |
|------|------|
| `frontend/` | Next.js app |
| `GeekSeoBackend/` | SEO product API + SignalR |
| `GeekBackend/` | Git submodule — shared `GeekApplication` contracts (same code local + Railway) |
| `plan-documents/` | Product spec |

**GeekAPI is not the SEO product.** Planning excluded GeekAPI from SEO; product code belongs in **this repo**, not `GeekBackend/GeekAPI`.

### First-time clone

```bash
git clone --recurse-submodules https://github.com/jmartinemployment/Geek-SEO.git
```

Already cloned without submodules?

```bash
git submodule update --init --recursive
```

## Status

- **Frontend:** Step 1 UI — projects, documents, editor + SignalR score sidebar.
- **GeekSeoBackend:** Product API `:5051` — providers (DataForSEO, Playwright, Claude), `ContentScoringService`, SignalR hub.
- **GeekAPI:** Thin internal proxy only — `/api/seo/internal/*` → GeekRepository `repo/seo/*`.
- **GeekRepository:** `geek_seo` schema + EF migrations — persistence only (no SERP/scoring).
- **Step 1:** Ready for E2E verification once `DATABASE_URL`, provider keys, and all three services are running locally.

## Build

From repo root (requires `GeekBackend` submodule — see above):

```bash
git submodule update --init --recursive
dotnet build GeekSEO.slnx
dotnet run --project GeekSeoBackend
```

## Local dev

See [`scripts/LOCAL_DEV.md`](scripts/LOCAL_DEV.md).
