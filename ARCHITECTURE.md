# Geek SEO — Architecture (single responsibility)

**Backlog:** [`TODO.md`](TODO.md) · **Status:** [`PROJECT_STATUS.md`](../PROJECT_STATUS.md)  
**Providers:** [`SEO-PROVIDER-STRATEGY.md`](SEO-PROVIDER-STRATEGY.md) — `ISerpProvider`, `IKeywordProvider`, `IRankSnapshotProvider`, `ICrawlerProvider`

Each tier has **one job**. No duplicate token issuers, no `REPO_URL` on product hosts, no reference tokens that hit the DB per request.

---

## Terminology

| Term | Meaning |
|------|---------|
| **Geek SEO (product)** | This repo — UI, GeekSeoBackend, product rules. |
| **GeekOAuth** | JWT issuer — `auth.geekatyourspot.com` (or Railway URL). Login + `geek_oauth` database. |
| **GeekAPI** | Data **gateway** — `/api/seo/internal/*` proxies to GeekRepository. **Not** the issuer. |
| **GeekRepository (service)** | **Only** deployed app with Postgres credentials for schema **`geek_seo`**. |
| **Persistence code** | **`GeekSeo.Persistence/`** in this repo — EF entities, `SeoDbContext`, migrations (**M3**). GeekRepository executes SQL only. |

---

## Four tiers (runtime)

| Tier | Responsibility | Host |
|------|----------------|------|
| **Identity** | OAuth 2.1 + PKCE, issue **standard JWTs**, user credentials | **GeekOAuth** |
| **Gateway** | Validate/proxy; `client_credentials` to GeekRepository for internal routes | **GeekAPI** |
| **Data** | PostgreSQL schema **`geek_seo`** — EF only on this tier | **GeekRepository** |
| **Product** | SEO API + SignalR, workers, providers, scoring, **schema design** (`dotnet ef`) | **Geek-SEO** (`GeekSeoBackend`, `GeekSeo.Persistence`) |

Browser → **GeekSeoBackend** (`NEXT_PUBLIC_SEO_API_URL`). Never GeekRepository directly. Never GeekAPI for product features except as HTTP gateway behind GeekSeoBackend.

---

## Identity (GeekOAuth)

- **Issuer:** `GEEK_OAUTH_AUTHORITY` / `NEXT_PUBLIC_AUTH_URL` → GeekOAuth (e.g. `https://auth.geekatyourspot.com`).
- **Client:** `geekseo` (public, PKCE).
- **Flows:** Authorization code + **PKCE** in `frontend/`; tokens exchanged via Next.js routes (`/api/auth/start`, `/api/auth/token`) — **not** GeekAPI `/api/auth/*`.

GeekSeoBackend **does not** issue tokens, host login UI, or store platform `users` rows.

**User records:** GeekOAuth Postgres table **`asp_net_users`** (`GeekOAuth/src/GeekOAuth.Server/Data/identity_tables.sql`). JWT `sub` = `id`. Background jobs use `WORKER_SERVICE_USER_ID` on GeekSeoBackend (same id). **Not** Supabase `auth.users`. Production today: single user (`jmartinemployment@gmail.com`). **TODO later:** dedicated `@geekatyourspot.com` service account — see `PROJECT_STATUS.md` → *Identity & worker service account*.

Legacy platform auth **APIs** (`GeekAPI /api/auth/*`, `SyncHub`, `repo/auth/*`) were **removed** (M4–M6, May 2026). Auth **database tables** may remain until optional phase **O2** after **M0** consumer audit.

---

## Product API (GeekSeoBackend — this repo)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = GEEK_OAUTH_AUTHORITY; // GeekOAuth issuer URL
        options.Audience = "geekseo";
        options.RequireHttpsMetadata = true;
    });
```

Data calls: forward the user's `Authorization: Bearer` to GeekAPI internal routes.

| Env | Purpose |
|-----|---------|
| `GEEK_OAUTH_AUTHORITY` | JWT issuer (**GeekOAuth**) |
| `GEEK_API_URL` | GeekAPI base URL (internal SEO proxy) |
| `GEEK_BACKEND_API_KEY` | Dev/service key for internal proxy (not end-user passwords) |
| ~~`REPO_URL`~~ | **Never on GeekSeoBackend** |
| ~~`DATABASE_URL`~~ | **Never on GeekSeoBackend** |

### Contracts (`GeekSeo.Application` — M2 complete)

SEO interfaces, models, and in-process services (e.g. `ContentScoringService`) live in **`GeekSeo.Application`**. **GeekSeoBackend** references only that project — **not** `GeekBackend/GeekApplication`. Product Docker builds **without** cloning GeekBackend (M7).

---

## Data path

```
Browser
  → PKCE → GeekOAuth (access token)
  → Bearer JWT → GeekSeoBackend (:5051)     [validate JWT in memory]
  → same Bearer JWT
GeekAPI /api/seo/internal/*
  → service auth + REPO_URL
GeekRepository repo/seo/*
  → Postgres (schema geek_seo)
```

**JSON** is the contract between GeekSeoBackend and GeekRepository. Shared C# types are a convenience today; after M3 they may diverge intentionally (duplicate types, same JSON).

---

## Frontend (this repo)

`frontend/` — PKCE against **GeekOAuth**; API calls to **GeekSeoBackend** with access token only.

---

## GeekBackend (sibling repo — platform gateway + data plane)

| Piece | Role |
|-------|------|
| **GeekAPI** | `/api/seo/internal/*` proxy; content reads; holds `REPO_URL` — **no** `/api/auth/*` |
| **GeekRepository** | `geek_seo` runtime + `repo/seo/*`; clones **Geek-SEO** at `Geek-SEO.commit` |
| **GeekApplication** | Content contracts + `Result` only (auth types removed M6) |

**Railway:** GeekAPI → `./Dockerfile`; GeekRepository → `./Dockerfile.repository` + `railway.geekrepository.toml` (see GeekBackend `README.md`).

**M3:** `GeekSeo.Persistence` in this repo — `dotnet ef` here; GeekRepository applies migrations at startup.

Optional **O1** (standalone contracts repo), **O2** (drop auth DB tables after **M0**) — both complete as of June 2026.

---

## Multi-app / future products

Additional Geek apps should:

- Use **GeekOAuth** (or their own issuer) — not GeekAPI as issuer.
- Keep **one data service** with DB credentials per schema.
- Avoid `ProjectReference` to the full GeekBackend repo for product builds.
