#!/usr/bin/env node
/**
 * Verifies optional Copyscape wiring on GeekSeoBackend.
 * Uses X-User-Id (dev user) — same header as local dev and other integration scripts.
 */
const apiBase = (process.env.PLAYWRIGHT_API_URL ?? process.env.NEXT_PUBLIC_SEO_API_URL ?? 'https://geekseobackend-production.up.railway.app').replace(/\/$/u, '');
const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';

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

  if (status.json.configured) {
    console.log('  Copyscape credentials are set — run a manual check in the content editor.');
  } else {
    console.log('  Copyscape not configured — optional; set COPYSCAPE_USERNAME and COPYSCAPE_API_KEY to enable.');
  }

  console.log('\nAll plagiarism integration checks passed.');
} catch (error) {
  console.error('\nPlagiarism integration test failed:', error.message);
  process.exit(1);
}
