#!/usr/bin/env node
/**
 * Creates Geek SEO subscription product + four billing plans in PayPal (sandbox or live).
 *
 * PayPal does NOT provide PAYPAL_PLAN_STARTER etc. — this script generates them via API
 * and prints the env vars to paste into Railway / GeekSeoBackend .env.
 *
 * Requires:
 *   PAYPAL_CLIENT_ID
 *   PAYPAL_CLIENT_SECRET
 *   PAYPAL_ENVIRONMENT=sandbox   (or PAYPAL_MODE=sandbox)
 *
 * Usage (from repo root):
 *   export PAYPAL_CLIENT_ID=...
 *   export PAYPAL_CLIENT_SECRET=...
 *   export PAYPAL_ENVIRONMENT=sandbox
 *   node scripts/paypal-create-subscription-plans.mjs
 */

const clientId = process.env.PAYPAL_CLIENT_ID;
const clientSecret = process.env.PAYPAL_CLIENT_SECRET;
const environment =
  process.env.PAYPAL_ENVIRONMENT ?? process.env.PAYPAL_MODE ?? 'sandbox';
const apiBase =
  environment === 'live'
    ? 'https://api-m.paypal.com'
    : 'https://api-m.sandbox.paypal.com';

const TIERS = [
  { env: 'PAYPAL_PLAN_STARTER', name: 'Geek SEO Starter', price: '29.00' },
  { env: 'PAYPAL_PLAN_PROFESSIONAL', name: 'Geek SEO Professional', price: '59.00' },
  { env: 'PAYPAL_PLAN_TEAM', name: 'Geek SEO Team', price: '89.00' },
  { env: 'PAYPAL_PLAN_AGENCY', name: 'Geek SEO Agency', price: '149.00' },
];

async function getAccessToken() {
  const credentials = Buffer.from(`${clientId}:${clientSecret}`).toString('base64');
  const res = await fetch(`${apiBase}/v1/oauth2/token`, {
    method: 'POST',
    headers: {
      Authorization: `Basic ${credentials}`,
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body: 'grant_type=client_credentials',
  });
  if (!res.ok) {
    throw new Error(`OAuth failed (${res.status}): ${await res.text()}`);
  }
  const data = await res.json();
  return data.access_token;
}

async function paypalPost(token, path, body) {
  const res = await fetch(`${apiBase}${path}`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
      'PayPal-Request-Id': crypto.randomUUID(),
    },
    body: JSON.stringify(body),
  });
  const text = await res.text();
  let json;
  try {
    json = text ? JSON.parse(text) : {};
  } catch {
    json = { raw: text };
  }
  if (!res.ok) {
    throw new Error(`POST ${path} failed (${res.status}): ${text}`);
  }
  return json;
}

async function main() {
  if (!clientId || !clientSecret) {
    console.error('Set PAYPAL_CLIENT_ID and PAYPAL_CLIENT_SECRET first.');
    process.exit(1);
  }

  console.log(`PayPal API: ${apiBase}\n`);

  const token = await getAccessToken();

  const product = await paypalPost(token, '/v1/catalogs/products', {
    name: 'Geek SEO',
    description: 'Geek SEO SaaS subscription',
    type: 'SERVICE',
    category: 'SOFTWARE',
  });
  const productId = product.id;
  if (!productId) {
    throw new Error(`No product id in response: ${JSON.stringify(product)}`);
  }
  console.log(`Created catalog product: ${productId}\n`);

  const planIds = {};

  for (const tier of TIERS) {
    const plan = await paypalPost(token, '/v1/billing/plans', {
      product_id: productId,
      name: tier.name,
      description: `${tier.name} monthly subscription`,
      billing_cycles: [
        {
          frequency: { interval_unit: 'MONTH', interval_count: 1 },
          tenure_type: 'REGULAR',
          sequence: 1,
          total_cycles: 0,
          pricing_scheme: {
            fixed_price: { value: tier.price, currency_code: 'USD' },
          },
        },
      ],
      payment_preferences: {
        auto_bill_outstanding: true,
        payment_failure_threshold: 3,
      },
    });

    const activateRes = await fetch(`${apiBase}/v1/billing/plans/${plan.id}/activate`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });
    if (!activateRes.ok) {
      console.warn(`Warning: activate ${plan.id} returned ${activateRes.status}`);
    }

    planIds[tier.env] = plan.id;
    console.log(`${tier.env}=${plan.id}  (${tier.name} $${tier.price}/mo)`);
  }

  console.log('\n--- Paste into Railway (GeekSeoBackend) ---\n');
  console.log(`PAYPAL_ENVIRONMENT=${environment}`);
  console.log(`PAYPAL_CLIENT_ID=${clientId}`);
  console.log('PAYPAL_CLIENT_SECRET=<keep your existing secret>');
  console.log('PAYPAL_WEBHOOK_ID=<run: node scripts/paypal-create-webhook.mjs>');
  for (const tier of TIERS) {
    console.log(`${tier.env}=${planIds[tier.env]}`);
  }
  console.log('\nWebhook URL:');
  console.log('  https://seo-api.geekatyourspot.com/api/seo/subscription/webhooks/paypal');
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
