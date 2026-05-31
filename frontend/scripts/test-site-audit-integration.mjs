#!/usr/bin/env node
/**
 * Verifies site audit API wiring on GeekSeoBackend (Professional tier gate).
 * Uses X-User-Id dev user — expects 402 unless SUBSCRIPTION_FULL_ACCESS_USER_IDS includes dev user.
 *
 * Live crawl: SITE_AUDIT_LIVE=1 npm run test:integration:site-audit
 */
const apiBase = (process.env.PLAYWRIGHT_API_URL ?? process.env.NEXT_PUBLIC_SEO_API_URL ?? 'https://geekseobackend-production.up.railway.app').replace(/\/$/u, '');
const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';
const liveAudit = process.env.SITE_AUDIT_LIVE === '1' || process.env.SITE_AUDIT_LIVE === 'true';

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

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

let projectId;

try {
  console.log(`API: ${apiBase}`);
  console.log(`User: ${devUserId}`);

  const health = await request('GET', '/health');
  assert(health.status === 200, `health expected 200, got ${health.status}`);
  console.log('✓ health');

  const subscription = await request('GET', '/api/seo/subscription');
  assert(subscription.status === 200, `subscription expected 200, got ${subscription.status}`);
  const tier = subscription.json?.tier ?? 'unknown';
  console.log(`✓ subscription tier (${tier})`);

  const created = await request('POST', '/api/seo/projects', {
    name: `Site audit integration ${Date.now()}`,
    url: 'https://geekatyourspot.com',
    defaultLocation: 'United States',
  });
  assert(created.status === 200 || created.status === 201, `create project failed: ${created.status}`);
  projectId = created.json?.id;
  assert(typeof projectId === 'string', 'create project missing id');
  console.log(`✓ create project (${projectId})`);

  const list = await request('GET', `/api/seo/audit/site?projectId=${projectId}`);
  if (list.status === 402) {
    assert(list.json?.requiredTier === 'professional', 'expected professional tier gate');
    console.log('✓ site audit list gated (Professional tier required for dev user)');
  } else {
    assert(list.status === 200, `list audits unexpected status ${list.status}: ${JSON.stringify(list.json)}`);
    assert(Array.isArray(list.json), 'list audits should return array');
    console.log(`✓ list site audits (${list.json.length} rows)`);
  }

  const start = await request('POST', '/api/seo/audit/site', { projectId });
  if (start.status === 402) {
    assert(start.json?.requiredTier === 'professional', 'expected professional tier gate on start');
    console.log('✓ site audit start gated (Professional tier required for dev user)');
    if (liveAudit) {
      console.warn('Skipping live crawl — dev user lacks Professional/Agency tier on this API host.');
    }
  } else if (start.status === 202 || start.status === 200) {
    const auditId = start.json?.id;
    assert(typeof auditId === 'string', 'start audit missing id');
    console.log(`✓ started site audit (${auditId})`);

    if (liveAudit) {
      let detail;
      for (let attempt = 0; attempt < 20; attempt += 1) {
        await sleep(3000);
        const got = await request('GET', `/api/seo/audit/site/${auditId}`);
        assert(got.status === 200, `get audit failed: ${got.status}`);
        detail = got.json;
        if (detail?.status !== 'running') break;
      }
      assert(detail?.status === 'completed' || detail?.status === 'failed', `audit did not finish: ${detail?.status}`);
      console.log(`✓ site audit finished (status=${detail.status}, pages=${detail.pagesCrawled ?? 0})`);
      if (detail.status === 'failed') {
        console.warn(`  failure reason: ${detail.errorMessage ?? 'unknown'}`);
      }
    } else {
      console.log('Skipping live crawl poll (set SITE_AUDIT_LIVE=1 to wait for completion).');
    }
  } else {
    throw new Error(`unexpected start status ${start.status}: ${JSON.stringify(start.json)}`);
  }

  console.log('\nAll site audit integration checks passed.');
} catch (error) {
  console.error('\nSite audit integration test failed:', error.message);
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
