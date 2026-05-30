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

**Contracts (transitional → in-repo):** GeekSeoBackend currently references **[GeekBackend](https://github.com/jmartinemployment/GeekBackend)/GeekApplication** for SEO types. **Target:** `GeekSeo.Application` in this repo only (**M2**); Railway build without cloning GeekBackend (**M7**). See [`PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md).

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
