# Geek SEO

Surfer / ContentShake-style SEO content SaaS.

**Plan:** [`plan-documents/GEEKSEO-PLAN.md`](plan-documents/GEEKSEO-PLAN.md)

## Repo layout

| Path | Role |
|------|------|
| `frontend/` | Next.js app |
| `GeekSeoBackend/` | SEO product API + SignalR |
| `GeekBackend.commit` | Pinned GeekBackend git SHA for Railway Docker builds |
| `plan-documents/` | Product spec |

**GeekApplication** (shared contracts) lives in the separate **[GeekBackend](https://github.com/jmartinemployment/GeekBackend)** repo — sibling folder locally (`../GeekBackend`), cloned in Docker on Railway.

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

When GeekBackend shared contracts change, update `GeekBackend.commit` to the new SHA and push.

## Local dev

See [`scripts/LOCAL_DEV.md`](scripts/LOCAL_DEV.md).
