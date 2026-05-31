#!/usr/bin/env node
/**
 * Registers PayPal webhook for Geek SEO subscription events.
 * Requires PAYPAL_CLIENT_ID, PAYPAL_CLIENT_SECRET, PAYPAL_ENVIRONMENT.
 *
 * Usage:
 *   PAYPAL_WEBHOOK_URL=https://seo-api.geekatyourspot.com/api/seo/subscription/webhooks/paypal \
 *   node scripts/paypal-create-webhook.mjs
 */

const clientId = process.env.PAYPAL_CLIENT_ID;
const clientSecret = process.env.PAYPAL_CLIENT_SECRET;
const environment =
  process.env.PAYPAL_ENVIRONMENT ?? process.env.PAYPAL_MODE ?? 'sandbox';
const webhookUrl =
  process.env.PAYPAL_WEBHOOK_URL ??
  'https://seo-api.geekatyourspot.com/api/seo/subscription/webhooks/paypal';

const apiBase =
  environment === 'live'
    ? 'https://api-m.paypal.com'
    : 'https://api-m.sandbox.paypal.com';

const EVENT_TYPES = [
  'BILLING.SUBSCRIPTION.ACTIVATED',
  'BILLING.SUBSCRIPTION.RE-ACTIVATED',
  'BILLING.SUBSCRIPTION.CANCELLED',
  'BILLING.SUBSCRIPTION.SUSPENDED',
  'BILLING.SUBSCRIPTION.EXPIRED',
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
  if (!res.ok) throw new Error(`OAuth failed (${res.status}): ${await res.text()}`);
  return (await res.json()).access_token;
}

async function main() {
  if (!clientId || !clientSecret) {
    console.error('Set PAYPAL_CLIENT_ID and PAYPAL_CLIENT_SECRET.');
    process.exit(1);
  }

  const token = await getAccessToken();

  const listRes = await fetch(`${apiBase}/v1/notifications/webhooks`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (listRes.ok) {
    const list = await listRes.json();
    const existing = (list.webhooks ?? []).find((w) => w.url === webhookUrl);
    if (existing?.id) {
      console.log(`Webhook already exists: ${existing.id}`);
      console.log(`PAYPAL_WEBHOOK_ID=${existing.id}`);
      return;
    }
  }

  const res = await fetch(`${apiBase}/v1/notifications/webhooks`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      url: webhookUrl,
      event_types: EVENT_TYPES.map((name) => ({ name })),
    }),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`Create webhook failed (${res.status}): ${text}`);

  const body = JSON.parse(text);
  console.log(`Created webhook: ${body.id}`);
  console.log(`URL: ${webhookUrl}`);
  console.log(`\nPAYPAL_WEBHOOK_ID=${body.id}`);
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
