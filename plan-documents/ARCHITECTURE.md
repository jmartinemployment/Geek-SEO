# Geek SEO — Architecture (single responsibility)

**Master plan:** [`GEEKSEO-PLAN.md`](GEEKSEO-PLAN.md)

Each tier has **one job**. No duplicate token issuers, no `REPO_URL` on product hosts, no reference tokens that hit the DB per request.

---

## Three tiers

| Tier | Responsibility | Host |
|------|----------------|------|
| **Identity** | Users (`oauth_users`), OpenIddict, **standard JWTs** (signed, self-contained), PKCE for browser, `client_credentials` only for true M2M on GeekAPI | **GeekAPI** (issuer) |
| **Data** | PostgreSQL `geek_seo` — Dapper/EF, **only** via `REPO_URL` on this tier | **GeekRepository** |
| **Product** | Geek SEO API + SignalR — **validates JWT in memory** (`JwtBearer` + `Authority`), forwards same user token to data gateway | **GeekSeoBackend** (this repo) |

Browser → **GeekSeoBackend only** (`NEXT_PUBLIC_SEO_API_URL`). Never GeekRepository. Never GeekAPI product routes.

---

## Identity (GeekAPI — Jeff)

- **Standard JWTs**, not reference tokens (no per-request token introspection DB round-trip).
- **One user store:** `oauth_users` + Dapper OpenIddict stores — not Supabase Auth as a second source of truth.
- **Flows:** Authorization code + **PKCE** (SPA/Electron), `client_credentials` for GeekAPI → GeekRepository (`geekapi`).

GeekSeoBackend **does not** issue tokens, seed OpenIddict, or store `GEEK_SEO_BACKEND_CLIENT_SECRET` for user data calls.

---

## Product API (GeekSeoBackend — this repo)

```csharp
// User auth only — verify JWT locally against issuer (Step 3 pattern)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = GEEK_OAUTH_AUTHORITY; // GeekAPI issuer URL
        options.Audience = "geekseo";             // product API audience
        options.RequireHttpsMetadata = true;
    });
```

Data calls: **forward the user's `Authorization: Bearer` header** to GeekAPI internal routes (`ForwardUserAuthorizationHandler`). Same principal end-to-end — not a second machine identity for routine CRUD.

| Env | Purpose |
|-----|---------|
| `GEEK_OAUTH_AUTHORITY` | JWT issuer (GeekAPI) |
| `GEEK_API_URL` | Internal data gateway base URL |
| ~~`REPO_URL`~~ | **Never on GeekSeoBackend** |
| ~~`GEEK_SEO_BACKEND_CLIENT_SECRET`~~ | **Not for user-scoped data** (M2M only on GeekAPI if ever needed) |

---

## Data path

```
Browser
  → Bearer JWT
GeekSeoBackend (:5051)     [validate JWT in memory]
  → same Bearer JWT
GeekAPI /api/seo/internal/*
  → geekapi client_credentials + REPO_URL
GeekRepository repo/seo/*
  → Postgres geek_seo
```

---

## Frontend (this repo)

`frontend/` — PKCE login against issuer; calls GeekSeoBackend with access token only.

---

## GeekBackend (Jeff — changes by request only)

- GeekAPI: issuer + `/api/seo/internal/*` (accept forwarded user JWT, proxy with `geekapi` + `REPO_URL`)
- GeekRepository: schema + `repo/seo/*`
- Shared **contracts** for SEO domain: must live in one place (Geek-SEO library or agreed shared package) before controllers compile again
