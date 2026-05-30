# Geek-SEO repository boundaries (non-negotiable)

**Architecture:** [`plan-documents/ARCHITECTURE.md`](plan-documents/ARCHITECTURE.md)  
**Decoupling (mandatory + optional phases):** [`plan-documents/PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md)

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
- Add `ProjectReference` paths into other repos (no shared `.csproj` bleed) — **target state** after PLATFORM-DECOUPLING **M2**; transitional reference to `GeekBackend/GeekApplication` exists until M2/M7 ship
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

When [`PLATFORM-DECOUPLING.md`](plan-documents/PLATFORM-DECOUPLING.md) phases **M3–M6** are explicitly approved, agents may edit **GeekBackend** (GeekAPI, GeekRepository, GeekApplication cleanup) as required by that phase — still **no** `DATABASE_URL` on GeekSeoBackend.

## Multi-app rule

Other Geek products must not clone the full GeekBackend repo for contracts. Prefer: in-repo product contracts (Geek SEO **M2**), or optional standalone **GeekApplication** repo (**O1**). Identity is always **GeekOAuth** (or product-specific issuer), not GeekAPI.
