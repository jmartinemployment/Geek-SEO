#!/usr/bin/env bash
# Create Geek SEO PayPal subscription plans + webhook using Railway GeekSeoBackend credentials.
# See docs/PAYPAL-BILLING.md
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root/GeekSeoBackend"
echo "Using Railway project linked from GeekSeoBackend/"
railway run -- node ../scripts/paypal-create-subscription-plans.mjs
railway run -- node ../scripts/paypal-create-webhook.mjs
echo ""
echo "Copy PAYPAL_PLAN_* and PAYPAL_WEBHOOK_ID from output into Railway if this is a fresh environment."
