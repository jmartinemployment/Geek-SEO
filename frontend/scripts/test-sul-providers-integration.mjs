#!/usr/bin/env node
/**
 * Verifies Search Understanding Layer Tier-2 provider wiring on GeekSeoBackend.
 *
 * CI (no vendor spend): GET /health/providers — env + credential flags only.
 * Live probe (1 SERP call): SUL_LIVE=1 npm run test:integration:sul-providers
 *
 * Env:
 *   PLAYWRIGHT_API_URL or NEXT_PUBLIC_SEO_API_URL — API base (default production)
 *   INTEGRATION_USER_ID — GeekOAuth user id (default production worker / full-access id)
 */
const apiBase = (
  process.env.PLAYWRIGHT_API_URL ??
  process.env.NEXT_PUBLIC_SEO_API_URL ??
  'https://seo-api.geekatyourspot.com'
).replace(/\/$/u, '');

const devUserId =
  process.env.INTEGRATION_USER_ID ??
  process.env.NEXT_PUBLIC_DEV_USER_ID ??
  '92b274f5-2fcb-4935-ba2d-cd8c03e1b21b';

const liveProbe =
  process.env.SUL_LIVE === '1' ||
  process.env.SUL_LIVE === 'true' ||
  process.env.SUL_PROVIDERS_LIVE === '1';

const headers = {
  'X-User-Id': devUserId,
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
  console.log(`User: ${devUserId}`);

  const health = await request('GET', '/health');
  assert(health.status === 200, `health expected 200, got ${health.status}`);
  console.log('✓ health');

  const providers = await request('GET', '/health/providers');
  assert(providers.status === 200, `/health/providers expected 200, got ${providers.status}`);
  const p = providers.json;
  assert(typeof p?.serpProvider === 'string', 'missing serpProvider');
  assert(typeof p?.keywordProvider === 'string', 'missing keywordProvider');
  assert(typeof p?.credentials?.dataforseo === 'boolean', 'missing credentials.dataforseo');
  assert(typeof p?.credentials?.serpapi === 'boolean', 'missing credentials.serpapi');

  console.log(
    `✓ provider config — serp=${p.serpProvider}, keyword=${p.keywordProvider}, ` +
      `rank=${p.rankSnapshotProvider}, dataforseoCreds=${p.credentials.dataforseo}, serpapiKey=${p.credentials.serpapi}`,
  );

  if (!p.credentials.dataforseo && !p.credentials.serpapi) {
    console.warn(
      '⚠ No DataForSEO or SerpApi credentials — Niche Analyzer steps 8–9 will skip (Tier 1 fusion still runs).',
    );
  } else if (p.serpProvider === 'dataforseo' && !p.credentials.dataforseo) {
    console.warn('⚠ SERP_PROVIDER=dataforseo but DATAFORSEO_* not set.');
  } else if (p.serpProvider === 'serpapi' && !p.credentials.serpapi) {
    console.warn('⚠ SERP_PROVIDER=serpapi but SERPAPI_API_KEY not set.');
  }

  if (!liveProbe) {
    console.log('\nTier-2 live probe skipped (set SUL_LIVE=1 to call DataForSEO/SerpApi once).');
    process.exit(0);
  }

  console.log('\nLive Tier-2 probe (1 SERP request via /api/seo/serp/deep)...');
  const serp = await request(
    'GET',
    '/api/seo/serp/deep?keyword=ai+consulting&location=United%20States&languageCode=en',
  );

  if (serp.status === 200 && Array.isArray(serp.json?.organicResults)) {
    console.log(`✓ SERP live — ${serp.json.organicResults.length} organic rows (${p.serpProvider})`);
    process.exit(0);
  }

  const err = serp.json?.error ?? serp.text ?? `HTTP ${serp.status}`;
  if (String(err).includes('402') || String(err).toLowerCase().includes('payment required')) {
    console.error(
      '✗ DataForSEO account returned Payment Required (402). Credentials are set but the vendor account has no balance.',
    );
    console.error('  Fix: fund DataForSEO OR set SERP_PROVIDER=serpapi on Railway (SERPAPI_API_KEY is already flagged).');
    console.error('  Niche Analyzer steps 8–9 skip until Tier-2 calls succeed.');
    process.exit(1);
  }

  if (String(err).includes('DATAFORSEO') || String(err).includes('SERPAPI')) {
    console.error(`✗ SERP provider error: ${err.slice(0, 400)}`);
    process.exit(1);
  }

  console.error(`✗ unexpected SERP response ${serp.status}: ${String(err).slice(0, 400)}`);
  process.exit(1);
} catch (error) {
  console.error(`\n✗ ${error instanceof Error ? error.message : String(error)}`);
  process.exit(1);
}
