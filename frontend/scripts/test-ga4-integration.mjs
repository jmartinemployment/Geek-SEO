#!/usr/bin/env node
/**
 * Verifies GA4 landing-pages API against GeekSeoBackend.
 *
 *   npm run test:integration:ga4
 *   GA4_LIVE=1 npm run test:integration:ga4
 *
 * Live mode requires a project with Google connected + GA4 property on the operator account.
 */
const apiBase = (
  process.env.PLAYWRIGHT_API_URL ??
  process.env.NEXT_PUBLIC_SEO_API_URL ??
  'https://seo-api.geekatyourspot.com'
).replace(/\/$/u, '');

const userId =
  process.env.INTEGRATION_USER_ID ??
  process.env.NEXT_PUBLIC_DEV_USER_ID ??
  '92b274f5-2fcb-4935-ba2d-cd8c03e1b21b';

const projectId =
  process.env.GA4_PROJECT_ID ?? 'e6275e97-6568-4e48-9bab-ee788de8fe77';

const live = process.env.GA4_LIVE === '1' || process.env.GA4_LIVE === 'true';

const headers = {
  'X-User-Id': userId,
  Accept: 'application/json',
};

async function request(method, path, body) {
  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers: body ? { ...headers, 'Content-Type': 'application/json' } : headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, text };
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

try {
  console.log(`API: ${apiBase}`);
  console.log(`User: ${userId}`);
  console.log(`Project: ${projectId}`);
  console.log(`Live GA4: ${live}\n`);

  const health = await request('GET', '/health');
  assert(health.status === 200, `health expected 200, got ${health.status}`);
  console.log('✓ health');

  const status = await request('GET', `/api/seo/integrations/google/status?projectId=${projectId}`);
  assert(status.status === 200, `google status expected 200, got ${status.status}: ${JSON.stringify(status.json)}`);
  console.log(
    `✓ google status (connected=${status.json?.connected}, ga4=${status.json?.ga4Connected}, property=${status.json?.propertyId ?? 'n/a'})`,
  );

  if (!live) {
    if (!status.json?.ga4Connected) {
      console.log('○ GA4 not connected on this project — skip live landing-pages (set GA4_LIVE=1 after connect)');
    } else {
      console.log('○ GA4 connected — run GA4_LIVE=1 to verify Analytics Data API');
    }
    console.log('\nGA4 integration checks passed (non-live).');
    process.exit(0);
  }

  assert(status.json?.ga4Connected === true, 'GA4 must be connected for GA4_LIVE=1');
  assert(status.json?.propertyId, 'GA4 propertyId missing on google status');

  const landing = await request(
    'GET',
    `/api/seo/analytics/ga4/${projectId}/landing-pages?limit=10`,
  );

  if (landing.status === 402) {
    console.warn('⚠ landing-pages gated (Professional tier) — upgrade tier or set SUBSCRIPTION_FULL_ACCESS_*');
    process.exit(0);
  }

  assert(
    landing.status === 200,
    `landing-pages expected 200, got ${landing.status}: ${landing.text?.slice(0, 500)}`,
  );

  const errorText = typeof landing.json?.error === 'string' ? landing.json.error : '';
  assert(
    !errorText.includes('403'),
    `GA4 still forbidden (check Analytics Data + Admin APIs on GCP): ${errorText}`,
  );
  assert(
    !errorText.toLowerCase().includes('service_disabled'),
    `Google API disabled: ${errorText}`,
  );

  assert(Array.isArray(landing.json?.rows), 'landing-pages response missing rows array');
  console.log(
    `✓ GA4 landing-pages (${landing.json.rows.length} rows, property=${landing.json.propertyId ?? status.json.propertyId})`,
  );

  console.log('\nAll GA4 integration checks passed (live).');
} catch (error) {
  console.error('\nGA4 integration test FAILED:', error instanceof Error ? error.message : error);
  process.exitCode = 1;
}
