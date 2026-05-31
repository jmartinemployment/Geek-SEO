# PayPal billing (Geek SEO)

Geek SEO tier names (**Starter**, **Professional**, **Team**, **Agency**) are **product labels in our app**. PayPal does not offer a "Starter tier" or pre-made `PAYPAL_PLAN_*` variables.

## What you need on Railway (GeekSeoBackend)

| Variable | Where it comes from |
|----------|---------------------|
| `PAYPAL_CLIENT_ID` | PayPal Developer → your app |
| `PAYPAL_CLIENT_SECRET` | Same app (or Render Secret File on OrderStack) |
| `PAYPAL_ENVIRONMENT` | `sandbox` or `live` (not `PAYPAL_MODE`) |
| `PAYPAL_WEBHOOK_ID` | You create a webhook in PayPal pointing at Geek SEO |
| `PAYPAL_PLAN_STARTER` | **Created by our script** — Plan ID like `P-...` |
| `PAYPAL_PLAN_PROFESSIONAL` | Same |
| `PAYPAL_PLAN_TEAM` | Same |
| `PAYPAL_PLAN_AGENCY` | Same |

Until all plan IDs exist, checkout stays **deferred**; the app still works via manual tier (see below).

## Enable checkout (one-time per environment)

From repo root with Railway credentials (or export vars locally):

```bash
cd GeekSeoBackend
railway run -- node ../scripts/paypal-create-subscription-plans.mjs
railway run -- node ../scripts/paypal-create-webhook.mjs
```

Then set the printed `PAYPAL_PLAN_*` and `PAYPAL_WEBHOOK_ID` on GeekSeoBackend (or use `railway variables set`).

**Sandbox** (`PAYPAL_ENVIRONMENT=sandbox`): test checkout with PayPal sandbox buyer accounts — no real money.

**Live** (`PAYPAL_ENVIRONMENT=live` + live app credentials): re-run both scripts against live API, update Railway vars, redeploy — real charges.

Docs: https://developer.paypal.com/docs/subscriptions/integrate/

## Operator full access (you)

On GeekSeoBackend (local and Railway):

```bash
SUBSCRIPTION_FULL_ACCESS_EMAILS=jmartinemployment@gmail.com
```

Sign in with GeekOAuth using that email → full Agency access. No scripts, no manual tier buttons.

## Without PayPal (staging QA only)

`SUBSCRIPTION_MANUAL_TIER_ENABLED=true` — optional; lets anyone change tier in Settings. Do not use on public production.

## OrderStack vs Geek SEO

OrderStack Render vars are for **checkout / payments** (`PAYPAL_MODE`, commerce). Geek SEO needs **billing plans** (`P-...` IDs) for recurring subscriptions — same PayPal app can work for both, but plan IDs must be created separately (script above).
