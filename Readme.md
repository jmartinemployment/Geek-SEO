# Geek SEO

Surfer / ContentShake-style SEO content SaaS.

**Plan:** [`plan-documents/GEEKSEO-PLAN.md`](plan-documents/GEEKSEO-PLAN.md)  
**Architecture / decoupling:** [`plan-documents/ARCHITECTURE.md`](plan-documents/ARCHITECTURE.md), [`plan-documents/PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md)

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
- **Later (live money):** step-by-step checklist in [`docs/PAYPAL-BILLING.md`](docs/PAYPAL-BILLING.md) — switch `PAYPAL_ENVIRONMENT=live`, live app credentials, re-run `scripts/paypal-create-subscription-plans.mjs` and `scripts/paypal-create-webhook.mjs`.
- **Operator login:** `SUBSCRIPTION_FULL_ACCESS_EMAILS` on GeekSeoBackend (documented in same file).
