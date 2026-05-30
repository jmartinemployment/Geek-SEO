#!/usr/bin/env node
/**
 * Verifies Google GSC/GA4 backend wiring without completing OAuth in a browser.
 * Uses X-User-Id (dev user) against GeekSeoBackend — same header the local dev UI uses.
 */
const apiBase = (process.env.PLAYWRIGHT_API_URL ?? process.env.NEXT_PUBLIC_SEO_API_URL ?? 'https://geekseobackend-production.up.railway.app').replace(/\/$/u, '');
const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';

const headers = {
  'X-User-Id': devUserId,
  'Content-Type': 'application/json',
  Accept: 'application/json',
};

async function request(method, path, body) {
  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers,
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

  const health = await request('GET', '/health');
  assert(health.status === 200, `health expected 200, got ${health.status}`);
  console.log('✓ health');

  const created = await request('POST', '/api/seo/projects', {
    name: `Google integration ${Date.now()}`,
    url: 'https://geekatyourspot.com',
    defaultLocation: 'United States',
  });
  assert(created.status === 200 || created.status === 201, `create project failed: ${created.status} ${JSON.stringify(created.json)}`);
  projectId = created.json?.id;
  assert(typeof projectId === 'string' && projectId.length > 0, 'create project missing id');
  console.log(`✓ create project (${projectId})`);

  const status = await request('GET', `/api/seo/integrations/google/status?projectId=${projectId}`);
  assert(status.status === 200, `google status expected 200, got ${status.status}`);
  assert(status.json?.connected === false, 'expected disconnected status before OAuth');
  console.log('✓ google status (disconnected)');

  const connect = await request(
    'GET',
    `/api/seo/integrations/google/connect-url?projectId=${projectId}&siteUrl=${encodeURIComponent('https://geekatyourspot.com')}`,
  );
  assert(connect.status === 200, `connect-url expected 200, got ${connect.status}: ${JSON.stringify(connect.json)}`);
  const oauthUrl = new URL(connect.json.url);
  assert(oauthUrl.hostname === 'accounts.google.com', `unexpected OAuth host: ${oauthUrl.hostname}`);
  assert(oauthUrl.searchParams.get('client_id'), 'OAuth URL missing client_id');
  assert(oauthUrl.searchParams.get('state'), 'OAuth URL missing state');
  assert(oauthUrl.searchParams.get('redirect_uri')?.includes('/api/seo/integrations/google/callback'), 'unexpected redirect_uri');
  console.log('✓ google connect-url (valid OAuth consent URL)');

  const subscription = await request('GET', '/api/seo/subscription');
  assert(subscription.status === 200, `subscription expected 200, got ${subscription.status}: ${JSON.stringify(subscription.json)}`);
  assert(typeof subscription.json?.tier === 'string', 'subscription tier should be a string');
  console.log(`✓ subscription tier (${subscription.json.tier})`);

  console.log('\nAll Google integration API checks passed.');
} catch (error) {
  console.error('\nGoogle integration test FAILED:', error instanceof Error ? error.message : error);
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
