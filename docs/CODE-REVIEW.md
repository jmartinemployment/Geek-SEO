# Geek SEO — code review log

**Date:** June 1, 2026  
**Scope:** Auth, billing, persistence, usage gates (static review; tests not re-run)  
**Related:** [`ROADMAP.md`](./ROADMAP.md) § Security and billing hardening

---

## Verdict

| Stage | Ready? |
|-------|--------|
| Sandbox PayPal + operator-only prod | Acceptable with env discipline (no dev bypass headers on prod) |
| PayPal **live** / real money | **Not ready** — P0 items below |
| Public paid SaaS | **Not ready** — P0 + P1 |

---

## Strengths

- OAuth PKCE + httpOnly refresh cookie; access token via server route (`frontend/src/app/api/auth/token/route.ts`).
- PayPal webhook signature verification before DB writes (`PayPalBillingService.cs`).
- Centralized tier (`SeoFeatureGateMiddleware`) and usage (`SeoUsageGateMiddleware`) gates.
- `UserId` on core entities; subscription unique per user in `GeekSeo.Persistence`.
- Service-layer ownership checks on projects, Google OAuth, scoring.

---

## Critical (fix before live money)

### 1. `X-User-Id` impersonation

`UserIdResolver` accepts `X-User-Id` without a production guard when JWT `sub` is missing (`GeekSeoBackend/Auth/UserIdResolver.cs`). Controllers rely on `ICurrentUserContext`, not `[Authorize]`. Any caller who can reach GeekSeoBackend can spoof another user’s GUID.

**Also forwarded** to GeekAPI as `X-Geek-User-Id` (`GeekDataGatewayHandler.cs`).

**Fix:** Reject header unless explicit dev mode; require valid JWT in production; verify GeekAPI ignores spoofed headers from the public internet.

### 2. PayPal `custom_id` client-controlled

`pricing-checkout.tsx` sets `custom_id` from the browser; webhooks trust it (`PayPalBillingService.cs`). Attacker can attribute payment to another user’s id.

**Fix:** Server-create or server-record pending subscription ↔ `userId`; validate on webhook.

### 3. Usage / tier gates fail open

On subscription/metering errors, middleware defaults to Starter tier or allows the request (`SeoFeatureGateMiddleware`, `SeoUsageGateMiddleware`).

**Fix:** Fail closed (503) for gated/metered routes when billing/usage unavailable.

### 4. Double usage increment

Middleware increments on success for metered POSTs; `SiteAuditService` and `PlagiarismService` also increment — possible **2×** burn per call.

**Fix:** Increment in one layer only.

---

## Important (fix soon)

| Issue | Location |
|-------|----------|
| JWT `ValidateAudience = false` | `SeoBackendExtensions.cs` |
| `SUBSCRIPTION_MANUAL_TIER_ENABLED` on prod | `SubscriptionController` |
| Google OAuth state in memory | `InMemoryGoogleOAuthStateStore` — breaks multi-instance |
| Internal APIs pass `userId` in query | Repositories — depends on GeekAPI enforcement |
| SignalR `JoinDocument` without access check | `SeoContentScoringHub` |
| PayPal webhook idempotency | `PayPalBillingService` |
| Large migration risk | `20260531155403_AddContentGuardTables` |

---

## Minor

- `agent-debug-log.ts` — localhost debug posts; ensure not used in production builds.
- App middleware gates on refresh cookie only — shell may load before access refresh.
- CORS `AllowCredentials` — keep origin list tight.

---

## Recommended fix order

1. Auth header hardening (`X-User-Id`)  
2. PayPal user binding  
3. Metering fail-closed + single increment  
4. Google OAuth state store (Redis/DB)  
5. Staging DB migration dry-run  
