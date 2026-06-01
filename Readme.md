# Geek SEO

Surfer / ContentShake-style SEO content SaaS.

**Backlog:** [`plan-documents/TODO.md`](plan-documents/TODO.md) · **Status:** [`PROJECT_STATUS.md`](PROJECT_STATUS.md)  
**TODO:** [`plan-documents/TODO.md`](plan-documents/TODO.md) — future work  
**Status:** [`PROJECT_STATUS.md`](PROJECT_STATUS.md)  
**Architecture:** [`plan-documents/ARCHITECTURE.md`](plan-documents/ARCHITECTURE.md), [`plan-documents/PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md)

## Repo layout

| Path | Role |
|------|------|
| `frontend/` | Next.js app |
| `GeekSeo.Persistence/` | EF `geek_seo` schema + migrations (**product-owned**, M3) |
| `GeekSeoBackend/` | SEO product API + SignalR |
| `plan-documents/` | Product spec + platform decoupling plan |

**Contracts:** In-repo **`GeekSeo.Application`** + **`GeekSeo.Persistence`** — GeekSeoBackend does not reference GeekBackend. See [`PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md).

**GeekAPI is not the SEO product.** Product code belongs in **this repo**, not `GeekBackend/GeekAPI`.

## Build (local)

```bash
dotnet build GeekSEO.slnx
dotnet run --project GeekSeoBackend
```

Product Docker builds from **this repo only** (no GeekBackend clone). For full E2E with persistence, run **GeekOAuth**, **GeekAPI**, and **GeekRepository** from the sibling `GeekBackend/` repo — see [`scripts/LOCAL_DEV.md`](scripts/LOCAL_DEV.md).

## Local dev

See [`scripts/LOCAL_DEV.md`](scripts/LOCAL_DEV.md).

## Billing (PayPal)

- **Now:** sandbox checkout on production — test at `/pricing` with PayPal sandbox buyer accounts (no real charges).
- **Later (live money):** [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) — not complete until live env + plans + webhook; **also** resolve P0 items in [`docs/CODE-REVIEW.md`](docs/CODE-REVIEW.md) before charging real customers.
- **Operator login:** `SUBSCRIPTION_FULL_ACCESS_EMAILS` on GeekSeoBackend (documented in same file).

## What’s left (recommended)

In-repo product **#1–27** is complete. Still open: integration products **#28–31**, ops go-live, and security hardening — see [`docs/ROADMAP.md`](docs/ROADMAP.md).
