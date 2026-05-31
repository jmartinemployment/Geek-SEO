#!/usr/bin/env node
/**
 * Debug session c1ee28 — Google Connect + auth flow via Playwright + fetch.
 * Writes NDJSON to .cursor/debug-c1ee28.log (and POSTs to Cursor debug ingest when running).
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const LOG_PATH = path.resolve(__dirname, '../../.cursor/debug-c1ee28.log');
const INGEST = 'http://127.0.0.1:7734/ingest/0871e8fa-3f7a-47da-bc93-ba8ad5f03982';
const SESSION = 'c1ee28';
const PROD_APP = process.env.PLAYWRIGHT_BASE_URL ?? 'https://seo.geekatyourspot.com';
const PROD_API = process.env.PLAYWRIGHT_API_URL ?? 'https://seo-api.geekatyourspot.com';
const PROJECT_ID = '39447fa9-0494-4606-b912-f404ff82a5bb';
const USER_ID = '92b274f5-2fcb-4935-ba2d-cd8c03e1b21b';

function log(hypothesisId, location, message, data = {}, runId = 'playwright') {
  const entry = {
    sessionId: SESSION,
    runId,
    hypothesisId,
    location,
    message,
    data,
    timestamp: Date.now(),
  };
  fs.appendFileSync(LOG_PATH, `${JSON.stringify(entry)}\n`);
  fetch(INGEST, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': SESSION },
    body: JSON.stringify(entry),
  }).catch(() => {});
  console.log(`[${hypothesisId}] ${message}`, data);
}

async function apiFetch(method, urlPath, { headers = {}, body } = {}) {
  const res = await fetch(`${PROD_API}${urlPath}`, {
    method,
    headers: { Accept: 'application/json', ...headers },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text.slice(0, 300) };
  }
  return { status: res.status, json };
}

async function testBackendHypotheses() {
  log('H-C', 'api/connect-url', 'fetch connect-url with X-User-Id', { projectId: PROJECT_ID });
  const connect = await apiFetch(
    'GET',
    `/api/seo/integrations/google/connect-url?projectId=${PROJECT_ID}&siteUrl=${encodeURIComponent('https://www.geekatyourspot.com/')}`,
    { headers: { 'X-User-Id': USER_ID } },
  );
  log('H-D', 'api/connect-url', 'connect-url response', {
    status: connect.status,
    hasUrl: Boolean(connect.json?.url),
    clientIdSuffix: connect.json?.url
      ? new URL(connect.json.url).searchParams.get('client_id')?.slice(-12)
      : null,
    redirectUri: connect.json?.url
      ? new URL(connect.json.url).searchParams.get('redirect_uri')
      : null,
    error: connect.json?.error ?? connect.json?.title ?? null,
  });

  if (connect.json?.url) {
    const oauthUrl = new URL(connect.json.url);
    const state = oauthUrl.searchParams.get('state');
    log('H-C', 'api/callback-fake', 'simulate callback with invalid code', { stateLen: state?.length ?? 0 });
    const cbRes = await fetch(
      `${PROD_API}/api/seo/integrations/google/callback?code=fake-code&state=${encodeURIComponent(state ?? '')}`,
      { redirect: 'manual' },
    );
    const location = cbRes.headers.get('location') ?? '';
    log('H-C', 'api/callback-fake', 'callback redirect', {
      status: cbRes.status,
      locationPreview: location.slice(0, 250),
      hasGoogleError: location.includes('google=error'),
      hasGoogleConnected: location.includes('google=connected'),
      messageParam: (() => {
        try {
          return new URL(location).searchParams.get('message')?.slice(0, 200) ?? null;
        } catch {
          return null;
        }
      })(),
    });

    log('H-E', 'api/state-reuse', 'second callback with same state (state store test)', {});
    const cbRes2 = await fetch(
      `${PROD_API}/api/seo/integrations/google/callback?code=fake-code-2&state=${encodeURIComponent(state ?? '')}`,
      { redirect: 'manual' },
    );
    const location2 = cbRes2.headers.get('location') ?? '';
    log('H-E', 'api/state-reuse', 'second callback redirect', {
      status: cbRes2.status,
      messageParam: (() => {
        try {
          return new URL(location2).searchParams.get('message')?.slice(0, 200) ?? null;
        } catch {
          return null;
        }
      })(),
    });
  }

  log('H-A', 'api/projects-unauth', 'projects without auth header');
  const unauth = await apiFetch('GET', '/api/seo/projects');
  log('H-A', 'api/projects-unauth', 'projects unauthenticated', {
    status: unauth.status,
    error: unauth.json?.error ?? null,
  });

  const authed = await apiFetch('GET', '/api/seo/projects', {
    headers: { 'X-User-Id': USER_ID },
  });
  log('H-A', 'api/projects-authed', 'projects with X-User-Id', {
    status: authed.status,
    projectCount: Array.isArray(authed.json) ? authed.json.length : null,
  });
}

async function testBrokenRefreshCookie(appBase) {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  await context.addCookies([
    {
      name: 'geekseo_refresh',
      value: 'invalid-refresh-token-for-debug',
      domain: new URL(appBase).hostname,
      path: '/',
      secure: appBase.startsWith('https'),
      httpOnly: true,
      sameSite: 'Lax',
    },
  ]);
  const page = await context.newPage();
  const authEvents = [];
  const seoEvents = [];
  page.on('response', async (response) => {
    const url = response.url();
    if (url.includes('/api/auth/token')) {
      let bodyPreview = '';
      try {
        bodyPreview = (await response.text()).slice(0, 200);
      } catch {
        bodyPreview = '';
      }
      authEvents.push({ status: response.status(), bodyPreview });
    }
    if (url.includes('seo-api.geekatyourspot.com/api/seo/')) {
      seoEvents.push({
        status: response.status(),
        path: new URL(url).pathname,
      });
    }
  });

  log('H-A', 'browser/broken-refresh', 'simulate invalid refresh cookie', { appBase, projectId: PROJECT_ID });
  const tokenPromise = page.waitForResponse(
    (response) => response.url().includes('/api/auth/token') && response.request().method() === 'POST',
    { timeout: 15_000 },
  );
  await page.goto(`${appBase}/app/projects/${PROJECT_ID}`, {
    waitUntil: 'domcontentloaded',
    timeout: 60_000,
  });
  let tokenResponse = null;
  try {
    tokenResponse = await tokenPromise;
  } catch {
    tokenResponse = null;
  }
  if (tokenResponse) {
    let bodyPreview = '';
    try {
      bodyPreview = (await tokenResponse.text()).slice(0, 200);
    } catch {
      bodyPreview = '';
    }
    authEvents.push({ status: tokenResponse.status(), bodyPreview });
  }
  await page.waitForTimeout(1500);
  log('H-A', 'browser/broken-refresh', 'captured auth/token responses', { authEvents });
  log('H-A', 'browser/broken-refresh', 'final url after stale session', { finalUrl: page.url() });
  log('H-A', 'browser/broken-refresh', 'captured seo api failures', {
    seoEvents: seoEvents.filter((e) => e.status >= 400),
  });
  await browser.close();
}

async function testGoogleOAuthPageReachability() {
  const connect = await apiFetch(
    'GET',
    `/api/seo/integrations/google/connect-url?projectId=${PROJECT_ID}`,
    { headers: { 'X-User-Id': USER_ID } },
  );
  if (!connect.json?.url) return;

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  log('H-D', 'browser/google-oauth', 'open Google consent URL (no login)', {
    host: new URL(connect.json.url).hostname,
  });

  try {
    await page.goto(connect.json.url, { waitUntil: 'domcontentloaded', timeout: 45_000 });
    await page.waitForTimeout(3000);
    const title = await page.title();
    const url = page.url();
    const bodySnippet = (await page.locator('body').innerText().catch(() => '')).slice(0, 300);
    log('H-D', 'browser/google-oauth', 'Google page loaded', {
      title,
      urlHost: new URL(url).hostname,
      bodySnippet,
      hasError: /error|blocked|redirect_uri|access denied/i.test(bodySnippet),
    });
  } catch (error) {
    log('H-D', 'browser/google-oauth', 'navigation failed', {
      error: error instanceof Error ? error.message : String(error),
    });
  }

  await browser.close();
}

async function testAuthenticatedConnectClick() {
  const email = process.env.PLAYWRIGHT_TEST_EMAIL?.trim();
  const password = process.env.PLAYWRIGHT_TEST_PASSWORD?.trim();
  if (!email || !password || email === 'your-test-user@example.com') {
    log('H-B', 'browser/auth-connect', 'skipped — set PLAYWRIGHT_TEST_EMAIL/PASSWORD in .env.playwright.local', {});
    return;
  }

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  const authEvents = [];
  const connectResponses = [];

  page.on('response', async (response) => {
    const url = response.url();
    if (url.includes('/api/auth/token')) {
      let bodyPreview = '';
      try {
        bodyPreview = (await response.text()).slice(0, 200);
      } catch {
        bodyPreview = '';
      }
      authEvents.push({ status: response.status(), bodyPreview });
    }
    if (url.includes('/api/seo/integrations/google/connect-url')) {
      let bodyPreview = '';
      try {
        bodyPreview = (await response.text()).slice(0, 200);
      } catch {
        bodyPreview = '';
      }
      connectResponses.push({ status: response.status(), bodyPreview });
    }
  });

  log('H-B', 'browser/auth-connect', 'logging in via GeekOAuth', { emailDomain: email.split('@')[1] });
  await page.goto(`${PROD_APP}/api/auth/start`, { waitUntil: 'domcontentloaded', timeout: 45_000 });
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(/\/app\//, { timeout: 45_000 });

  log('H-A', 'browser/auth-connect', 'post-login auth/token events', { authEvents });

  await page.goto(`${PROD_APP}/app/projects/${PROJECT_ID}`, { waitUntil: 'networkidle', timeout: 60_000 });
  const connectVisible = await page.getByRole('button', { name: 'Connect Google' }).isVisible().catch(() => false);
  log('H-B', 'browser/auth-connect', 'Connect Google visible after login', { connectVisible });

  if (connectVisible) {
    await Promise.all([
      page.waitForURL(/accounts\.google\.com|seo\.geekatyourspot\.com/i, { timeout: 30_000 }).catch(() => null),
      page.getByRole('button', { name: 'Connect Google' }).click(),
    ]);
    log('H-B', 'browser/auth-connect', 'after Connect Google click', {
      url: page.url(),
      onGoogle: /accounts\.google\.com/i.test(page.url()),
      connectResponses,
    });
  }

  await browser.close();
}

async function main() {
  fs.mkdirSync(path.dirname(LOG_PATH), { recursive: true });
  console.log(`Logging to ${LOG_PATH}`);
  await testBackendHypotheses();
  const localBase = process.env.PLAYWRIGHT_LOCAL_URL ?? 'http://localhost:3000';
  let localReady = false;
  try {
    const ping = await fetch(localBase, { signal: AbortSignal.timeout(2000) });
    localReady = ping.ok;
  } catch {
    localReady = false;
  }
  if (localReady) {
    log('H-A', 'main', 'broken-refresh against local instrumented app', { localBase, runId: 'post-fix' });
    await testBrokenRefreshCookie(localBase);
  } else {
    log('H-A', 'main', 'local dev not running — broken-refresh uses production', { appBase: PROD_APP });
    await testBrokenRefreshCookie(PROD_APP);
  }
  await testGoogleOAuthPageReachability();
  await testAuthenticatedConnectClick();
  console.log('Done.');
}

main().catch((error) => {
  log('H-X', 'main', 'fatal', { error: error instanceof Error ? error.message : String(error) });
  process.exit(1);
});
