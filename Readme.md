# Geek SEO

Surfer / ContentShake-style SEO content SaaS.

**Plan:** [`plan-documents/geekseo-plan.md`](plan-documents/geekseo-plan.md) — v1 **100% complete**  
**TODO:** [`plan-documents/TODO.md`](plan-documents/TODO.md) — future work  
**Status:** [`PROJECT_STATUS.md`](PROJECT_STATUS.md)  
**Architecture:** [`plan-documents/ARCHITECTURE.md`](plan-documents/ARCHITECTURE.md), [`plan-documents/PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md)

## Repo layout

| Path | Role |
|------|------|
| `frontend/` | Next.js app |
| `GeekSeo.Persistence/` | EF `geek_seo` schema + migrations (**product-owned**, M3) |
| `GeekSeoBackend/` | SEO product API + SignalR |
| `GeekBackend.commit` | *(transitional)* Pinned GeekBackend SHA for Railway Docker — **removed in PLATFORM-DECOUPLING M7** |
| `plan-documents/` | Product spec + platform decoupling plan |

**Contracts:** In-repo **`GeekSeo.Application`** + **`GeekSeo.Persistence`** — GeekSeoBackend does not reference GeekBackend. See [`PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md).

**GeekAPI is not the SEO product.** Product code belongs in **this repo**, not `GeekBackend/GeekAPI`.

## Build (local)

Clone GeekBackend next to this repo:

```
development-new/
├── Geek-SEO/
└── GeekBackend/
```

```bash
dotnet build GeekSEO.slnx
dotnet run --project GeekSeoBackend
```

Until **M7**, when GeekBackend shared contracts change, update `GeekBackend.commit` and redeploy. After **M7**, product deploys are independent of GeekBackend SHA.

## Local dev

See [`scripts/LOCAL_DEV.md`](scripts/LOCAL_DEV.md).

## Billing (PayPal)

- **Now:** sandbox checkout on production — test at `/pricing` with PayPal sandbox buyer accounts (no real charges).
- **Later (live money):** [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) — not complete until live env + plans + webhook; **also** resolve P0 items in [`docs/CODE-REVIEW.md`](docs/CODE-REVIEW.md) before charging real customers.
- **Operator login:** `SUBSCRIPTION_FULL_ACCESS_EMAILS` on GeekSeoBackend (documented in same file).

## What’s left (recommended)

In-repo product **#1–27** is complete. Still open: integration products **#28–31**, ops go-live, and security hardening — see [`docs/ROADMAP.md`](docs/ROADMAP.md).
