#!/usr/bin/env node
/**
 * Trigger a production re-analyze for geekatyourspot.com and poll until complete.
 * Asserts sul-2.0 pillar count (50+) when finished.
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

const pollMs = Number(process.env.POLL_MS ?? 8000);
const timeoutMs = Number(process.env.TIMEOUT_MS ?? 12 * 60 * 1000);

const headers = {
  'X-User-Id': userId,
  Accept: 'application/json',
  'Content-Type': 'application/json',
};

async function request(method, path, body) {
  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
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

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

try {
  console.log(`API: ${apiBase}`);
  const projects = await request('GET', '/api/seo/projects');
  assert(projects.status === 200, `projects ${projects.status}`);
  const list = Array.isArray(projects.json) ? projects.json : [];
  const geek = list.find((p) => /geekatyourspot/i.test(p.url ?? ''));
  assert(geek, 'geekatyourspot project not found');
  console.log(`✓ project ${geek.name} (${geek.id})`);

  const analyze = await request('POST', '/api/seo/niche-analyzer/analyze', {
    projectId: geek.id,
    domain: geek.url ?? 'https://www.geekatyourspot.com',
  });
  assert(analyze.status === 200 || analyze.status === 201, `analyze ${analyze.status}`);
  const profileId = analyze.json?.profileId ?? analyze.json?.id;
  assert(profileId, 'analyze response missing profileId');
  console.log(`✓ enqueued profile ${profileId}`);

  const started = Date.now();
  let lastStep = '';

  while (Date.now() - started < timeoutMs) {
    const status = await request('GET', `/api/seo/niche-analyzer/${profileId}/status`);
    assert(status.status === 200, `status ${status.status}`);
    const s = status.json;
    const stepLabel = `${s.stepNumber ?? '?'}/${s.totalSteps ?? 14} ${s.step ?? ''}`;
    if (stepLabel !== lastStep) {
      console.log(`  … ${s.status} @ ${stepLabel} — ${s.message ?? ''}`);
      lastStep = stepLabel;
    }

    if (s.status === 'complete') {
      const profile = await request('GET', `/api/seo/niche-analyzer/${profileId}`);
      assert(profile.status === 200, `profile ${profile.status}`);
      const pillarCount =
        profile.json.pillars?.length ?? profile.json.totalPillarsIdentified ?? 0;
      console.log(
        `✓ complete — ${pillarCount} pillars (structure=${profile.json.structureStatus ?? 'n/a'} enrichment=${profile.json.enrichmentStatus ?? 'n/a'})`,
      );

      const candidates = await request(
        'GET',
        `/api/seo/niche-analyzer/${profileId}/topic-candidates?page=1&pageSize=5`,
      );
      if (candidates.status === 200) {
        console.log(`  topic-candidates total=${candidates.json?.total ?? 0}`);
      }

      const details = await request('GET', `/api/seo/niche-analyzer/${profileId}/analysis-details`);
      if (details.status === 200) {
        const sul = details.json?.fusionSnapshot?.sulVersion;
        const candidates = details.json?.fusionSnapshot?.allCandidates?.length ?? 0;
        console.log(`  fusion: sulVersion=${sul ?? 'n/a'} candidates=${candidates}`);
        if (sul) assert(sul === 'sul-2.0', `expected sul-2.0, got ${sul}`);
        if (candidates > 0) assert(candidates >= 12, `expected 12+ candidates, got ${candidates}`);
      }

      assert(pillarCount >= 50, `pillar count ${pillarCount} below 50`);
      console.log('\nRe-analyze geekatyourspot passed.');
      process.exit(0);
    }

    if (s.status === 'failed') {
      throw new Error(s.errorMessage ?? 'analysis failed');
    }

    await sleep(pollMs);
  }

  throw new Error(`timed out after ${timeoutMs}ms`);
} catch (error) {
  console.error('\nFAILED:', error.message);
  process.exit(1);
}
