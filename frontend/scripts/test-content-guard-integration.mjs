#!/usr/bin/env node
/**
 * Verifies Content Guard API wiring on GeekSeoBackend (Professional tier gate).
 * Uses X-User-Id (INTEGRATION_USER_ID or NEXT_PUBLIC_DEV_USER_ID).
 *
 * Live scan (GSC decay analysis, no WP draft): CONTENT_GUARD_LIVE=1 npm run test:integration:content-guard
 */
const apiBase = (process.env.PLAYWRIGHT_API_URL ?? process.env.NEXT_PUBLIC_SEO_API_URL ?? 'https://seo-api.geekatyourspot.com').replace(/\/$/u, '');
const devUserId = process.env.INTEGRATION_USER_ID ?? process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';
const liveScan = process.env.CONTENT_GUARD_LIVE === '1' || process.env.CONTENT_GUARD_LIVE === 'true';

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
  return { status: response.status, json };
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

let projectId;

try {
  console.log(`API: ${apiBase}`);
  console.log(`User: ${devUserId}`);

  const unauth = await fetch(`${apiBase}/api/seo/content-guard/00000000-0000-0000-0000-000000000001/policy`);
  assert(unauth.status === 401, `unauthenticated policy expected 401, got ${unauth.status}`);
  console.log('✓ unauthenticated policy returns 401');

  const health = await request('GET', '/health');
  assert(health.status === 200, `health expected 200, got ${health.status}`);
  console.log('✓ health');

  const subscription = await request('GET', '/api/seo/subscription');
  assert(subscription.status === 200, `subscription expected 200, got ${subscription.status}: ${JSON.stringify(subscription.json)}`);
  console.log(`✓ subscription tier (${subscription.json?.tier ?? 'unknown'})`);

  const created = await request('POST', '/api/seo/projects', {
    name: `Content Guard integration ${Date.now()}`,
    url: 'https://geekatyourspot.com',
    defaultLocation: 'United States',
  });
  assert(created.status === 200 || created.status === 201, `create project failed: ${created.status}`);
  projectId = created.json?.id;
  assert(typeof projectId === 'string', 'create project missing id');
  console.log(`✓ create project (${projectId})`);

  const policyMissing = await request('GET', `/api/seo/content-guard/${projectId}/policy`);
  if (policyMissing.status === 402) {
    assert(policyMissing.json?.requiredTier === 'professional', 'expected professional tier gate');
    console.log('✓ content guard gated (Professional tier required for dev user)');
  } else {
    assert(policyMissing.status === 404, `policy without upsert expected 404, got ${policyMissing.status}`);
    console.log('✓ policy not found before upsert');

    const upsert = await request('PUT', `/api/seo/content-guard/${projectId}/policy`, {
      enabled: true,
      autoPatch: false,
    });
    assert(upsert.status === 200, `upsert policy failed: ${upsert.status} ${JSON.stringify(upsert.json)}`);
    console.log('✓ upsert policy');

    const runs = await request('GET', `/api/seo/content-guard/${projectId}/runs`);
    assert(runs.status === 200, `list runs failed: ${runs.status}`);
    assert(Array.isArray(runs.json), 'runs should be array');
    console.log(`✓ list runs (${runs.json.length})`);

    if (liveScan) {
      const scan = await request('POST', `/api/seo/content-guard/${projectId}/scan`);
      assert(scan.status === 202, `scan expected 202 Accepted, got ${scan.status}: ${JSON.stringify(scan.json)}`);
      console.log('✓ decay scan accepted (check runs in UI; WP draft requires WordPress connection + autoPatch)');
    } else {
      console.log('Skipping live scan (set CONTENT_GUARD_LIVE=1 to POST /scan).');
    }
  }

  console.log('\nAll Content Guard integration checks passed.');
} catch (error) {
  console.error('\nContent Guard integration test failed:', error.message);
  process.exitCode = 1;
} finally {
  if (projectId) {
    const deleted = await request('DELETE', `/api/seo/projects/${projectId}`);
    if (deleted.status === 204 || deleted.status === 200) {
      console.log('✓ cleaned up test project');
    } else {
      console.warn(`cleanup delete returned ${deleted.status}`);
    }
  }
}
