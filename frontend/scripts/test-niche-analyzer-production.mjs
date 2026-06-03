#!/usr/bin/env node
/**
 * Production niche-analyzer API check — latest profile must include pillars when complete.
 * Uses X-User-Id (Jeff's production user by default).
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

const headers = {
  'X-User-Id': userId,
  Accept: 'application/json',
};

async function request(method, path) {
  const response = await fetch(`${apiBase}${path}`, { method, headers });
  const text = await response.text();
  let json = null;
  if (text) {
    try {
      json = JSON.parse(text);
    } catch {
      json = { raw: text.slice(0, 500) };
    }
  }
  return { status: response.status, json };
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

try {
  console.log(`API: ${apiBase}`);
  console.log(`User: ${userId}`);

  const health = await request('GET', '/health');
  assert(health.status === 200, `health ${health.status}`);
  console.log('✓ health');

  const projects = await request('GET', '/api/seo/projects');
  assert(projects.status === 200, `projects ${projects.status}`);
  const list = Array.isArray(projects.json) ? projects.json : [];
  assert(list.length > 0, 'no projects for user');
  const geek = list.find((p) => /geekatyourspot/i.test(p.url ?? '')) ?? list[0];
  const projectId = geek.id;
  console.log(`✓ project ${geek.name} (${projectId}) url=${geek.url}`);
  if (/\s/.test(geek.url ?? '') || /https?:\/\/\s*https?:\/\//i.test(geek.url ?? '')) {
    console.warn(
      '⚠ project url looks malformed (spaces or double scheme) — backend normalizer should still pick a valid host on next analyze',
    );
  }

  const latest = await request('GET', `/api/seo/niche-analyzer/project/${projectId}/latest`);
  if (latest.status === 204) {
    console.log('○ no niche profile yet (204)');
    process.exit(0);
  }
  assert(latest.status === 200, `latest ${latest.status} ${JSON.stringify(latest.json)}`);
  const p = latest.json;
  console.log(
    `  latest profile: status=${p.status} pillars=${p.pillars?.length ?? 0} total=${p.totalPillarsIdentified} discovery=${p.discoveryMethod}`,
  );

  if (p.status === 'complete') {
    assert(
      (p.pillars?.length ?? 0) > 0 || p.totalPillarsIdentified === 0,
      `complete profile has totalPillars=${p.totalPillarsIdentified} but pillars array empty — GetLatest graph bug`,
    );
    if (p.totalPillarsIdentified > 0) {
      assert(
        (p.pillars?.length ?? 0) >= p.totalPillarsIdentified,
        `pillars length ${p.pillars?.length} < totalPillarsIdentified ${p.totalPillarsIdentified}`,
      );
    }
    console.log('✓ latest complete profile includes pillar rows');

    const byId = await request('GET', `/api/seo/niche-analyzer/${p.id}`);
    assert(byId.status === 200, `getById ${byId.status}`);
    assert(
      (byId.json.pillars?.length ?? 0) === (p.pillars?.length ?? 0),
      `getById pillars ${byId.json.pillars?.length} !== latest pillars ${p.pillars?.length}`,
    );
    console.log('✓ getById pillar count matches latest');

    const matrix = await request('GET', `/api/seo/niche-analyzer/${p.id}/coverage-matrix`);
    assert(matrix.status === 200, `coverage-matrix ${matrix.status}`);
    console.log(`✓ coverage-matrix rows=${Array.isArray(matrix.json) ? matrix.json.length : 0}`);

    const progress = await request(
      'GET',
      `/api/seo/niche-analyzer/project/${projectId}/progress?months=12`,
    );
    assert(progress.status === 200, `progress ${progress.status}`);
    const points = Array.isArray(progress.json) ? progress.json : [];
    console.log(`✓ progress points=${points.length}`);
  } else {
    console.log(`○ latest status is ${p.status} — skipping pillar assertions`);
  }

  console.log('\nAll niche-analyzer production checks passed.');
} catch (error) {
  console.error('\nFAILED:', error.message);
  process.exit(1);
}
