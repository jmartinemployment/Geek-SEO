# Geek SEO

Surfer / ContentShake-style SEO content SaaS.

**Plan:** [`plan-documents/GEEKSEO-PLAN.md`](plan-documents/GEEKSEO-PLAN.md)

## Repo layout (intended)

| Path | Role |
|------|------|
| `frontend/` | Next.js app |
| `GeekSeoBackend/` | SEO product API + SignalR |
| `plan-documents/` | Product spec |

**GeekAPI is not the SEO product.** Planning excluded GeekAPI from SEO; product code belongs in **this repo**, not `GeekBackend/GeekAPI`.

## Status

- **Frontend:** Step 1 UI — projects, documents, editor + SignalR score sidebar.
- **GeekSeoBackend:** .NET project in `GeekSeoBackend/` — `:5051`, `/api/seo/*`, `/hubs/seo-scoring`.
- **Data:** Jeff — GeekRepository (`geek_seo`); GeekSeoBackend calls `REPO_URL` (not GeekAPI product routes).

## Local dev

See [`scripts/LOCAL_DEV.md`](scripts/LOCAL_DEV.md).
