#!/usr/bin/env node
/**
 * Verifies optional Copyscape wiring on GeekSeoBackend.
 * Uses X-User-Id (dev user) — same header as local dev and other integration scripts.
 *
 * CI: status endpoint only (no Copyscape API spend).
 * Manual live check: COPYSCAPE_LIVE_CHECK=1 npm run test:integration:plagiarism
 */
const apiBase = (process.env.PLAYWRIGHT_API_URL ?? process.env.NEXT_PUBLIC_SEO_API_URL ?? 'https://geekseobackend-production.up.railway.app').replace(/\/$/u, '');
const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';
const liveCheck = process.env.COPYSCAPE_LIVE_CHECK === '1' || process.env.COPYSCAPE_LIVE_CHECK === 'true';

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

  const health = await request('GET', '/health');
  assert(health.status === 200, `health expected 200, got ${health.status}`);
  console.log('✓ health');

  const status = await request('GET', '/api/seo/plagiarism/status');
  assert(status.status === 200, `plagiarism status expected 200, got ${status.status}: ${JSON.stringify(status.json)}`);
  assert(typeof status.json?.configured === 'boolean', 'status missing configured boolean');
  assert(status.json?.provider === 'copyscape', `expected provider copyscape, got ${status.json?.provider}`);
  console.log(`✓ plagiarism status (configured=${status.json.configured})`);

  const onProduction = apiBase.includes('railway.app') || apiBase.includes('geekatyourspot.com');
  if (onProduction && status.json.configured) {
    console.log('  Production Copyscape credentials detected.');
  } else if (!status.json.configured) {
    console.log('  Copyscape not configured — optional; set COPYSCAPE_USERNAME and COPYSCAPE_API_KEY to enable.');
  }

  if (liveCheck && status.json.configured) {
    console.log('\nRunning live Copyscape check (charges against COPYSCAPE_SPEND_LIMIT_USD)...');

    const created = await request('POST', '/api/seo/projects', {
      name: `Plagiarism integration ${Date.now()}`,
      url: 'https://geekatyourspot.com',
      defaultLocation: 'United States',
    });
    assert(created.status === 200 || created.status === 201, `create project failed: ${created.status} ${JSON.stringify(created.json)}`);
    projectId = created.json?.id;
    assert(typeof projectId === 'string' && projectId.length > 0, 'create project missing id');
    console.log(`✓ create project (${projectId})`);

    const doc = await request('POST', '/api/seo/content', {
      projectId,
      title: 'Plagiarism integration test',
      targetKeyword: 'local seo checklist',
    });
    assert(doc.status === 200 || doc.status === 201, `create content failed: ${doc.status} ${JSON.stringify(doc.json)}`);
    const documentId = doc.json?.id;
    assert(typeof documentId === 'string', 'create content missing id');

    const sampleHtml =
      '<p>Geek SEO integration test paragraph with enough unique words to exceed the minimum ' +
      'plain-text threshold for Copyscape. This sentence is generated for automated verification only ' +
      'and should not match live web pages at high overlap percentages.</p>';

    const updated = await request('PUT', `/api/seo/content/${documentId}/content`, {
      contentHtml: sampleHtml,
      title: 'Plagiarism integration test',
    });
    assert(updated.status === 200, `update content failed: ${updated.status} ${JSON.stringify(updated.json)}`);
    console.log(`✓ seed content (${documentId})`);

    const checked = await request('POST', '/api/seo/plagiarism/check', {
      documentId,
      forceRefresh: true,
    });
    if (checked.status === 400 && String(checked.json?.error ?? '').includes('Insufficient credit')) {
      console.warn('⚠ Copyscape returned insufficient account credit — credentials work; add funds at copyscape.com');
      console.log('✓ live API path reached Copyscape (billing blocked, not a deploy bug)');
    } else {
      assert(checked.status === 200, `plagiarism check failed: ${checked.status} ${JSON.stringify(checked.json)}`);
      assert(typeof checked.json?.matchPercent === 'number', 'check result missing matchPercent');
      assert(Array.isArray(checked.json?.matches), 'check result missing matches array');
      assert(typeof checked.json?.publishBlocked === 'boolean', 'check result missing publishBlocked');
      console.log(`✓ live Copyscape check (${checked.json.matchPercent}% matched, publishBlocked=${checked.json.publishBlocked})`);

      const latest = await request('GET', `/api/seo/plagiarism/check/${documentId}`);
      assert(latest.status === 200, `get latest check failed: ${latest.status}`);
      assert(latest.json?.id === checked.json.id, 'latest check id mismatch');
      console.log('✓ persisted latest check');
    }
  } else if (liveCheck && !status.json.configured) {
    console.log('Skipping live check — Copyscape not configured on this API host.');
  } else {
    console.log('Skipping live Copyscape API call (set COPYSCAPE_LIVE_CHECK=1 to run a paid check).');
  }

  console.log('\nAll plagiarism integration checks passed.');
} catch (error) {
  console.error('\nPlagiarism integration test failed:', error.message);
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
