# Geek SEO — Architecture

**Master plan:** [`GEEKSEO-PLAN.md`](GEEKSEO-PLAN.md)

## Planning rule (non-negotiable)

**GeekAPI is not used for the Geek SEO product.** Not for `/api/seo`, not for SignalR scoring, not for SERP or AI. That was decided in planning before implementation.

GeekAPI may exist only as Jeff’s **internal data gateway** to GeekRepository (if at all). Browsers and the Next.js app never call GeekAPI for SEO.

---

## This repository (`Geek-SEO`)

```
Geek-SEO/
  frontend/           Next.js — seo.geekatyourspot.com
  GeekSeoBackend/     .NET SEO product API — :5051  ← MUST LIVE HERE (not built yet)
  plan-documents/
```

| Layer | Where | Port |
|-------|--------|------|
| Browser | `frontend/` | 3000 |
| SEO product API | **`GeekSeoBackend/` in this repo** | 5051 |
| Login | geek-OAuth (sibling repo) | 3001 |

---

## Data (GeekBackend — Jeff)

```
GeekSeoBackend (Geek-SEO repo)
    ↕  server-to-server only
GeekRepository (+ optional internal gateway in GeekAPI)
    ↕
Postgres — schema geek_seo
```

GeekSeoBackend has **no** direct database connection. Jeff adapts GeekRepository for Geek SEO tables.

---

## What does not happen

- SEO controllers or `SeoContentScoringHub` on **GeekAPI**
- Frontend `NEXT_PUBLIC_*` pointing at GeekAPI for product calls
- Treating GeekAPI :8080 / :5272 as “the SEO API”

---

## Frontend env

`NEXT_PUBLIC_SEO_API_URL` → GeekSeoBackend in **this repo** (e.g. `http://localhost:5051`).
