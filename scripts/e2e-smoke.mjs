#!/usr/bin/env node
/**
 * Geek SEO API smoke test (no browser).
 *
 * Usage:
 *   API_URL=http://localhost:5051 \
 *   DEV_USER_ID=00000000-0000-0000-0000-000000000001 \
 *   node scripts/e2e-smoke.mjs
 *
 * Optional (billable external APIs on GeekSeoBackend):
 *   RUN_KEYWORD_RESEARCH=true  — DATAFORSEO_* on GeekSeoBackend
 *   RUN_BRIEF=true             — DATAFORSEO_* + ANTHROPIC_API_KEY
 *   RUN_FULL_ARTICLE=true      — ANTHROPIC_API_KEY (polls job until done or timeout)
 *   RUN_AI_TOOLS=true          — ANTHROPIC_API_KEY (humanize on sample HTML)
 *
 * WordPress is optional and off by default:
 *   RUN_WORDPRESS=true WP_SITE_URL=... WP_USERNAME=... WP_APP_PASSWORD=...
 */

const API_URL = process.env.API_URL ?? 'http://localhost:5051';
const DEV_USER_ID = process.env.DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';
const JOB_POLL_MS = Number.parseInt(process.env.JOB_POLL_MS ?? '3000', 10);
const JOB_MAX_WAIT_MS = Number.parseInt(process.env.JOB_MAX_WAIT_MS ?? '120000', 10);

const headers = {
  'Content-Type': 'application/json',
  'X-User-Id': DEV_USER_ID,
};

let passed = 0;
let failed = 0;

function log(ok, label, detail = '') {
  if (ok) {
    passed++;
    console.log(`  ✓ ${label}${detail ? ` — ${detail}` : ''}`);
  } else {
    failed++;
    console.error(`  ✗ ${label}${detail ? ` — ${detail}` : ''}`);
  }
}

function skip(label) {
  console.log(`  ○ SKIP ${label}`);
}

async function api(method, path, body) {
  const res = await fetch(`${API_URL}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let json;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { ok: res.ok, status: res.status, json, text };
}

async function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function pollJob(jobId) {
  const started = Date.now();
  while (Date.now() - started < JOB_MAX_WAIT_MS) {
    const res = await api('GET', `/api/seo/jobs/${jobId}`);
    if (!res.ok) return res;
    const status = res.json?.status ?? 'unknown';
    const progress = res.json?.progressPercent ?? 0;
    if (status === 'completed') return { ok: true, json: res.json };
    if (status === 'failed') {
      return { ok: false, text: res.json?.errorMessage ?? 'job failed', json: res.json };
    }
    process.stdout.write(`    … job ${status} (${progress}%)\n`);
    await sleep(JOB_POLL_MS);
  }
  return { ok: false, text: `timeout after ${JOB_MAX_WAIT_MS}ms` };
}

async function main() {
  console.log(`Geek SEO smoke test → ${API_URL} (user ${DEV_USER_ID})\n`);

  const health = await fetch(`${API_URL}/health`).catch(() => null);
  log(health?.ok, 'GeekSeoBackend health', health ? String(health.status) : 'unreachable');

  const projectsBefore = await api('GET', '/api/seo/projects');
  log(projectsBefore.ok, 'GET /api/seo/projects');

  const stamp = Date.now();
  const created = await api('POST', '/api/seo/projects', {
    name: `Smoke ${stamp}`,
    url: 'https://example.com',
    defaultLocation: 'United States',
  });
  log(created.ok, 'POST /api/seo/projects');
  const projectId = created.json?.id;
  if (!projectId) {
    console.error('\nAborting: no project id');
    process.exit(1);
  }

  const doc = await api('POST', '/api/seo/content', {
    projectId,
    title: 'Smoke test article',
    targetKeyword: 'local seo services',
    targetLocation: 'United States',
  });
  log(doc.ok, 'POST /api/seo/content', doc.ok ? `id=${doc.json?.id}` : doc.text);
  const documentId = doc.json?.id;

  if (documentId) {
    const getDoc = await api('GET', `/api/seo/content/${documentId}`);
    log(getDoc.ok, 'GET /api/seo/content/{id}');

    const sampleHtml =
      '<h1>Local SEO Services Guide</h1><p>Professional local seo services help small businesses rank in maps and organic search.</p>';

    const putContent = await api('PUT', `/api/seo/content/${documentId}/content`, {
      title: 'Smoke test article',
      contentHtml: sampleHtml,
      targetKeyword: 'local seo services',
      targetLocation: 'United States',
    });
    log(putContent.ok, 'PUT /api/seo/content/{id}/content');

    const statusWriting = await api('PATCH', `/api/seo/content/${documentId}/status`, {
      status: 'writing',
    });
    log(statusWriting.ok, 'PATCH status → writing', statusWriting.json?.status);

    const statusReview = await api('PATCH', `/api/seo/content/${documentId}/status`, {
      status: 'review',
    });
    log(statusReview.ok, 'PATCH status → review', statusReview.json?.status);

    const list = await api('GET', `/api/seo/content?projectId=${projectId}`);
    log(list.ok, 'GET /api/seo/content?projectId', list.ok ? `${list.json?.length ?? 0} docs` : list.text);

    const competitors = await api('GET', `/api/seo/content/${documentId}/competitors`);
    log(competitors.ok, 'GET competitors');

    const crawl = await api('POST', `/api/seo/content/${documentId}/competitors/crawl`);
    log(crawl.ok || crawl.status === 400, 'POST competitors/crawl', String(crawl.status));
  }

  if (process.env.RUN_KEYWORD_RESEARCH === 'true') {
    const kw = await api('POST', '/api/seo/keywords/research', {
      projectId,
      seedKeyword: 'plumber',
      location: 'United States',
      resultCount: 10,
    });
    log(kw.ok, 'POST /api/seo/keywords/research', kw.ok ? `${kw.json?.length ?? 0} keywords` : kw.text);
  } else {
    skip('keyword research (RUN_KEYWORD_RESEARCH=true)');
  }

  if (process.env.RUN_BRIEF === 'true') {
    const brief = await api('POST', '/api/seo/briefs/generate', {
      projectId,
      keyword: 'local seo services',
      location: 'United States',
    });
    log(
      brief.ok,
      'POST /api/seo/briefs/generate',
      brief.ok ? `${brief.json?.recommendedTerms?.length ?? 0} terms` : brief.text,
    );
  } else {
    skip('content brief (RUN_BRIEF=true)');
  }

  if (process.env.RUN_AI_TOOLS === 'true' && documentId) {
    const humanize = await api('POST', '/api/seo/writing/humanize', {
      documentId,
      contentHtml: '<h1>Test</h1><p>Sample paragraph for humanize.</p>',
    });
    log(humanize.ok, 'POST /api/seo/writing/humanize');

    const detect = await api('POST', '/api/seo/writing/detect', {
      documentId,
      contentHtml: '<h1>Test</h1><p>Sample paragraph for detection.</p>',
    });
    log(detect.ok, 'POST /api/seo/writing/detect', detect.ok ? `${detect.json?.aiProbability}` : detect.text);
  } else {
    skip('AI tools (RUN_AI_TOOLS=true)');
  }

  if (process.env.RUN_FULL_ARTICLE === 'true') {
    const job = await api('POST', '/api/seo/writing/full-article', {
      projectId,
      keyword: 'smoke test keyword',
      location: 'United States',
    });
    const accepted = job.status === 202 || job.ok;
    log(accepted, 'POST /api/seo/writing/full-article', String(job.status));
    if (accepted && job.json?.jobId) {
      const done = await pollJob(job.json.jobId);
      log(done.ok, 'GET /api/seo/jobs/{id} poll', done.ok ? `doc=${done.json?.resultId}` : done.text);
    }
  } else {
    skip('full article (RUN_FULL_ARTICLE=true)');
  }

  if (process.env.RUN_WORDPRESS === 'true' && process.env.WP_SITE_URL) {
    const connect = await api('POST', `/api/seo/wordpress/connect?projectId=${projectId}`, {
      siteUrl: process.env.WP_SITE_URL,
      username: process.env.WP_USERNAME,
      applicationPassword: process.env.WP_APP_PASSWORD,
      defaultPostStatus: 'draft',
    });
    log(connect.ok, 'POST /api/seo/wordpress/connect');

    if (connect.ok && documentId) {
      const pub = await api('POST', `/api/seo/wordpress/publish?documentId=${documentId}`, {
        postStatus: 'draft',
      });
      log(pub.ok, 'POST /api/seo/wordpress/publish', pub.json?.url ?? pub.text);
    }
  } else {
    skip('WordPress (RUN_WORDPRESS=true — not required for core product)');
  }

  console.log(`\nDone: ${passed} passed, ${failed} failed`);
  process.exit(failed > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error('FATAL:', err);
  process.exit(1);
});
