# PayPal billing (Geek SEO)

Runbook for subscription checkout, sandbox testing, and switching to **live** payments later.

**Product tiers** (Starter $29, Professional $59, Team $89, Agency $149) are labels in our app. PayPal only stores **plan IDs** (`P-...`) we create via API — not pre-made tier names in their dashboard.

---

## Current production state (as configured)

| Item | Value |
|------|--------|
| Environment | **Sandbox** (`PAYPAL_ENVIRONMENT=sandbox` on Railway GeekSeoBackend) |
| Checkout | **Enabled** — `GET https://seo-api.geekatyourspot.com/api/seo/subscription/plans` → `checkout.available: true` |
| Webhook URL | `https://seo-api.geekatyourspot.com/api/seo/subscription/webhooks/paypal` |
| Operator full access | `SUBSCRIPTION_FULL_ACCESS_EMAILS=jmartinemployment@gmail.com` (no PayPal needed for your login) |
| Frontend | https://seo.geekatyourspot.com/pricing |

Sandbox = full checkout flow works; **no real money** is charged.

---

## Who gets what (simple rules)

| User | Access |
|------|--------|
| Email in `SUBSCRIPTION_FULL_ACCESS_EMAILS` | Full product (Agency gates) — always |
| Customer with active PayPal subscription | Tier matching what they paid for (from `seo_subscriptions` + webhook) |
| Signed in, no subscription | Starter limits until they subscribe |

---

## Railway variables (GeekSeoBackend)

| Variable | Purpose |
|----------|---------|
| `PAYPAL_CLIENT_ID` | PayPal app (sandbox or live) |
| `PAYPAL_CLIENT_SECRET` | Same app |
| `PAYPAL_ENVIRONMENT` | `sandbox` or `live` (not `PAYPAL_MODE`) |
| `PAYPAL_WEBHOOK_ID` | From `paypal-create-webhook.mjs` |
| `PAYPAL_PLAN_STARTER` | Plan ID `P-...` for $29/mo |
| `PAYPAL_PLAN_PROFESSIONAL` | Plan ID for $59/mo |
| `PAYPAL_PLAN_TEAM` | Plan ID for $89/mo |
| `PAYPAL_PLAN_AGENCY` | Plan ID for $149/mo |
| `SUBSCRIPTION_FULL_ACCESS_EMAILS` | Comma-separated operator emails (optional) |

Until all plan IDs + webhook + client credentials exist, `/pricing` shows checkout as deferred.

---

## Scripts (one-time per PayPal environment)

From repo root, using Railway credentials:

```bash
bash scripts/paypal-setup-sandbox.sh
```

Or manually:

```bash
cd GeekSeoBackend
railway run -- node ../scripts/paypal-create-subscription-plans.mjs
railway run -- node ../scripts/paypal-create-webhook.mjs
```

Paste output into Railway:

```bash
railway variables set \
  "PAYPAL_PLAN_STARTER=P-..." \
  "PAYPAL_PLAN_PROFESSIONAL=P-..." \
  "PAYPAL_PLAN_TEAM=P-..." \
  "PAYPAL_PLAN_AGENCY=P-..." \
  "PAYPAL_WEBHOOK_ID=..."
```

Redeploy GeekSeoBackend (or wait for auto-redeploy after variable changes).

PayPal API reference: https://developer.paypal.com/docs/subscriptions/integrate/

---

## Test checkout in sandbox (no real charges)

1. Open https://seo.geekatyourspot.com/pricing and sign in.
2. Confirm yellow banner: **PayPal is in sandbox mode**.
3. Click **Subscribe** on a plan.
4. Pay with a **PayPal sandbox Personal (buyer)** account:
   - https://developer.paypal.com → **Apps & Credentials** → **Sandbox** → **Sandbox accounts**
5. After approval, wait ~1 minute for webhook → **Settings → Subscription** should show the paid tier as `active`.

Use a **separate** sandbox buyer account to test the customer path. Your operator email does not need to subscribe.

### Verify API without the UI

```bash
curl -sS https://seo-api.geekatyourspot.com/api/seo/subscription/plans | python3 -m json.tool
```

Expect: `checkout.available: true`, `checkout.deferred: false`, four entries in `checkout.planIds`.

**CI:** `npm run test:e2e:smoke` and `npm run test:integration:google` assert this on production. With GitHub secrets, `npm run test:e2e:auth` also checks `/pricing` sandbox PayPal button containers.

---

## Going live later (real money)

Do this when you are ready to charge real cards — not required for development.

### Prerequisites

- PayPal **Business** account approved for live payments.
- **Live** REST app in PayPal Developer (separate from sandbox app).
- Live `PAYPAL_CLIENT_ID` and `PAYPAL_CLIENT_SECRET`.

### Checklist

1. **PayPal Developer** → switch to **Live** (not Sandbox) → create or open your live app → copy Client ID and Secret.

2. **Railway (GeekSeoBackend)** — update credentials and environment:
   ```bash
   railway variables set \
     "PAYPAL_ENVIRONMENT=live" \
     "PAYPAL_CLIENT_ID=<live-client-id>" \
     "PAYPAL_CLIENT_SECRET=<live-secret>"
   ```
   Remove or replace old **sandbox** plan IDs and webhook ID (live uses different IDs).

3. **Create live plans + webhook** (must run against live credentials):
   ```bash
   cd GeekSeoBackend
   railway run -- node ../scripts/paypal-create-subscription-plans.mjs
   railway run -- node ../scripts/paypal-create-webhook.mjs
   ```
   Set the new `PAYPAL_PLAN_*` and `PAYPAL_WEBHOOK_ID` on Railway from script output.

4. **Redeploy** GeekSeoBackend and confirm:
   ```bash
   curl -sS https://seo-api.geekatyourspot.com/api/seo/subscription/plans
   ```
   `checkout.available` should be `true`; frontend `/pricing` should **not** show the sandbox warning banner (`environment: live`).

5. **Smoke test** with a small real subscription (or PayPal live test tools if available), then cancel in app or PayPal dashboard.

6. **Leave** `SUBSCRIPTION_FULL_ACCESS_EMAILS` set for your operator login so you are not blocked while testing live.

### Do not carry over from sandbox

- Plan IDs (`P-...`) are **environment-specific** — live plans must be created again.
- Webhook ID is **environment-specific** — register again for live.
- Sandbox buyer accounts do not work on live checkout.

---

## Optional: dev tier override (not for public production)

```bash
SUBSCRIPTION_MANUAL_TIER_ENABLED=true
```

Settings → manual tier buttons. Only for staging/QA. Not needed if `SUBSCRIPTION_FULL_ACCESS_EMAILS` covers your login.

---

## OrderStack vs Geek SEO

OrderStack (Render) uses PayPal for **commerce checkout** (`PAYPAL_MODE`, etc.). Geek SEO uses **subscription billing plans** (`P-...` IDs). The same PayPal business can own both apps, but Geek SEO plan IDs are created only via the scripts above.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| No PayPal buttons on `/pricing` | `checkout.available` false — missing env vars; run scripts |
| Buttons load but payment fails | Sandbox site must use sandbox buyer; live site must use live PayPal |
| Paid but tier not updating | Webhook URL reachable; `PAYPAL_WEBHOOK_ID` matches; GeekSeoBackend logs |
| You still see “upgrade” | Your email must be in `SUBSCRIPTION_FULL_ACCESS_EMAILS`, or you need an active subscription row |
| 402 on GSC/GA4 | Customer tier below Professional — expected for unpaid Starter |

Webhook events handled: `BILLING.SUBSCRIPTION.ACTIVATED`, `RE-ACTIVATED`, `CANCELLED`, `SUSPENDED`, `EXPIRED`.
