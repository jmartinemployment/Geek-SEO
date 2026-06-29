# Geek-SEO repository boundaries (non-negotiable)

**Architecture:** [`ARCHITECTURE.md`](../ARCHITECTURE.md)  
**Decoupling (mandatory + optional phases):** complete (M0–M9, June 2026) — see [`plan-documents/README.md`](plan-documents/README.md) retired table.

## This repo contains ONLY

| Path | Role |
|------|------|
| `frontend/` | Next.js app — PKCE login, calls product API |
| `GeekSeoBackend/` | Product API + SignalR — validates JWTs, SEO features |
| `plan-documents/` | Product specs |
| `scripts/` | Dev/deploy notes for **this** repo |

## Prohibited in this repo

Agents and contributors **must not**:

- Edit, scaffold, or document implementation steps for **any sibling platform repo**
- Reference platform services by name in code comments, errors, or “you need to add X there” implementation checklists
- Add `ProjectReference` paths into other repos (no shared `.csproj` bleed) — **M2/M7 complete**: GeekSeoBackend uses only `GeekSeo.Application` in this repo
- Configure `REPO_URL`, repository URLs, or database connection strings on **GeekSeoBackend**
- Implement OpenIddict, `oauth_users`, password hashing, or Dapper auth stores here

Platform identity and persistence are **external black boxes**. This product only consumes:

- An **OAuth issuer** URL (`NEXT_PUBLIC_AUTH_URL` / `GEEK_OAUTH_AUTHORITY`)
- A **data API** URL (`DATA_API_URL`) with a published contract

## What Geek-SEO does for security

1. **Login:** OAuth 2.1 authorization code + **PKCE** (frontend) — no passwords sent to GeekSeoBackend  
2. **API access:** **Standard JWT** validated in memory on GeekSeoBackend (`JwtBearer` + `Authority`)  
3. **Data:** Forward the **same** user `Authorization: Bearer` to `DATA_API_URL` — no second machine token for user CRUD  

See [`scripts/OAUTH_SETUP.md`](scripts/OAUTH_SETUP.md) for GeekOAuth setup.

## Coordinated platform work

Phases **M3–M6** are **complete** (May 2026). Further GeekBackend edits require a new approved phase (e.g. **O2** after **M0**). GeekSeoBackend still has **no** `DATABASE_URL` for the main `geek_seo` schema.

### Site Analyzer 2 (`sa2`) exception

When `SITE_ANALYZER2_DATABASE_URL` is set, **GeekSeoBackend** may open a **second** PostgreSQL connection for the `sa2` schema only (EF Core in `SiteAnalyzer2.Infrastructure`). This is the sole approved direct-DB exception in this repo.

- Operator API: `/api/seo/sa2/*` (in-process SA2 controllers)
- Legacy 10-step wizard: `/api/seo/site-analyzer/*` — **unchanged until SA2 is verified in-repo**
- Content Writing handoff: `analysisRunId` from SA2 export (`SA2_IN_PROCESS_REPOS=true` swaps HTTP proxy repos for in-process)

## Multi-app rule

Other Geek products must not clone the full GeekBackend repo for contracts. Prefer: in-repo product contracts (Geek SEO **M2**), or optional standalone **GeekApplication** repo (**O1**). Identity is always **GeekOAuth** (or product-specific issuer), not GeekAPI.
