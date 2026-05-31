#!/usr/bin/env node
/**
 * Human-paced Playwright audit — production login + Connect Google click.
 * Loads credentials from frontend/.env.playwright.local
 *
 *   npm run test:human:google-connect
 *   PLAYWRIGHT_HEADED=true npm run test:human:google-connect
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FRONTEND = path.resolve(__dirname, '..');

function loadEnvFile(envPath) {
  if (!fs.existsSync(envPath)) return;
  for (const line of fs.readFileSync(envPath, 'utf8').split('\n')) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const eq = trimmed.indexOf('=');
    if (eq < 0) continue;
    const key = trimmed.slice(0, eq).trim();
    let value = trimmed.slice(eq + 1).trim();
    if (
      (value.startsWith('"') && value.endsWith('"')) ||
      (value.startsWith("'") && value.endsWith("'"))
    ) {
      value = value.slice(1, -1);
    }
    if (key && (process.env[key] === undefined || process.env[key] === '')) {
      process.env[key] = value;
    }
  }
}

loadEnvFile(path.join(FRONTEND, '.env.playwright.local'));

const BASE = (process.env.PLAYWRIGHT_BASE_URL ?? 'https://seo.geekatyourspot.com').replace(/\/$/u, '');
const PROJECT_ID = process.env.GOOGLE_CONNECT_PROJECT_ID ?? '39447fa9-0494-4606-b912-f404ff82a5bb';
const SHOT_DIR = '/tmp/google-connect-human';
const email = process.env.PLAYWRIGHT_TEST_EMAIL?.trim();
const password = process.env.PLAYWRIGHT_TEST_PASSWORD?.trim();
const useDevUser = process.env.PLAYWRIGHT_USE_DEV_USER === 'true';
const staleOnly = process.env.HUMAN_STALE_ONLY === 'true';
const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';
const headed = process.env.PLAYWRIGHT_HEADED === 'true';

fs.mkdirSync(SHOT_DIR, { recursive: true });

function step(label) {
  console.log(`\n▶ ${label}`);
}

async function screenshot(page, name) {
  const file = path.join(SHOT_DIR, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log(`  screenshot: ${file}`);
}

async function navigateToGoogleConnectUi(page, effectiveBase, projectId) {
  await page.goto(`${effectiveBase}/app/projects/${projectId}`, {
    waitUntil: 'domcontentloaded',
    timeout: 60_000,
  });
  await page.waitForTimeout(2000);

  const connectBtn = page.getByRole('button', { name: 'Connect Google' });
  if (await connectBtn.isVisible().catch(() => false)) {
    console.log(`  Google connect UI: ${page.url()}`);
    return;
  }

  step('Fallback — open GSC rankings (Google panel)');
  await page.goto(`${effectiveBase}/app/rankings`, {
    waitUntil: 'domcontentloaded',
    timeout: 60_000,
  });
  await page.waitForTimeout(1500);
  const select = page.locator('#google-project-select');
  if (await select.isVisible().catch(() => false)) {
    await select.selectOption(projectId);
    await page.waitForTimeout(800);
  }
  console.log(`  Google connect UI: ${page.url()}`);
}

async function loginViaGeekOAuth(page) {
  step('Open GeekOAuth sign-in');
  await page.goto(`${BASE}/api/auth/start`, { waitUntil: 'domcontentloaded', timeout: 45_000 });
  await page.waitForTimeout(800);
  await screenshot(page, '01-auth-start');

  if (/TwoFactor/i.test(page.url())) {
    throw new Error('Test account has 2FA — use a GeekOAuth user without 2FA.');
  }

  await page.getByLabel('Email').click();
  await page.waitForTimeout(300);
  await page.getByLabel('Email').fill(email);
  await page.waitForTimeout(400);
  await page.getByLabel('Password').click();
  await page.waitForTimeout(300);
  await page.getByLabel('Password').fill(password);
  await screenshot(page, '02-credentials-filled');
  await page.waitForTimeout(500);
  await page.getByRole('button', { name: 'Sign in' }).click();

  try {
    await page.waitForURL(/\/auth\/callback|\/app\//, { timeout: 45_000 });
  } catch {
    await screenshot(page, '02b-login-failed');
    const stillOnLogin = /\/Account\/Login/i.test(page.url());
    if (stillOnLogin) {
      throw new Error(
        'GeekOAuth login did not complete — check PLAYWRIGHT_TEST_EMAIL/PASSWORD in .env.playwright.local (2FA must be off).',
      );
    }
    throw new Error(`GeekOAuth login timed out at ${page.url()}`);
  }
  if (page.url().includes('/auth/callback')) {
    await page.waitForURL(/\/app\//, { timeout: 45_000 });
  }
  await page.waitForTimeout(1200);
  await screenshot(page, '03-after-login');
  console.log(`  logged in: ${page.url()}`);
}

async function main() {
  const hasCredentials =
    email && password && email !== 'your-test-user@example.com' && password.length > 0;

  if (!useDevUser && !hasCredentials && !staleOnly) {
    console.error(
      'Set PLAYWRIGHT_TEST_EMAIL/PASSWORD in .env.playwright.local, HUMAN_STALE_ONLY=true for stale-cookie-only, or PLAYWRIGHT_USE_DEV_USER=true on localhost:3000',
    );
    process.exit(1);
  }

  const effectiveBase = useDevUser
    ? (process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000').replace(/\/$/u, '')
    : BASE;

  console.log(`Target: ${effectiveBase}`);
  console.log(`Mode: ${useDevUser ? 'dev-user (no OAuth login)' : 'GeekOAuth login'}`);
  console.log(`Project: ${PROJECT_ID}`);
  console.log(`Screenshots: ${SHOT_DIR}`);
  console.log(`Headed: ${headed}`);

  const browser = await chromium.launch({
    headless: !headed,
    slowMo: headed ? 120 : 50,
  });

  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: 'en-US',
  });
  const page = await context.newPage();

  const authEvents = [];
  const seoFailures = [];
  const consoleErrors = [];

  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('response', async (response) => {
    const url = response.url();
    if (url.includes('/api/auth/token')) {
      let bodyPreview = '';
      try {
        bodyPreview = (await response.text()).slice(0, 160);
      } catch {
        bodyPreview = '';
      }
      authEvents.push({ status: response.status(), bodyPreview });
    }
    if (url.includes('/api/seo/') && response.status() >= 400) {
      seoFailures.push(`${response.request().method()} ${new URL(url).pathname} → ${response.status()}`);
    }
  });

  try {
    if (!useDevUser) {
      step('Simulate stale refresh cookie');
      await context.addCookies([
        {
          name: 'geekseo_refresh',
          value: 'invalid-refresh-token-human-test',
          domain: new URL(BASE).hostname,
          path: '/',
          secure: BASE.startsWith('https'),
          httpOnly: true,
          sameSite: 'Lax',
        },
      ]);
      await page.goto(`${BASE}/app/projects/${PROJECT_ID}`, {
        waitUntil: 'domcontentloaded',
        timeout: 60_000,
      });
      await page.waitForTimeout(2500);
      await screenshot(page, '00-stale-cookie');
      const staleUrl = page.url();
      const staleAuth = authEvents.filter((e) => e.status === 401 || e.bodyPreview.includes('sessionExpired'));
      console.log(`  URL after stale cookie: ${staleUrl}`);
      console.log(`  Auth/token events: ${JSON.stringify(authEvents.slice(-3))}`);
      if (staleAuth.length > 0) {
        console.log('  PASS stale session → 401/sessionExpired');
      } else if (/auth\.geekatyourspot|\/auth\/login|\/api\/auth\/start/i.test(staleUrl)) {
        console.log('  PASS redirected toward login');
      } else {
        console.log('  WARN stale cookie recovery unclear — continuing');
      }

      if (staleOnly) {
        console.log('\nDone (stale-cookie-only). Screenshots in ' + SHOT_DIR);
        return;
      }

      authEvents.length = 0;
      seoFailures.length = 0;
      await context.clearCookies();
    } else if (staleOnly) {
      console.log('HUMAN_STALE_ONLY ignored in dev-user mode');
      return;
    }

    let activeProjectId = PROJECT_ID;

    if (useDevUser) {
      step('Dev-user — create ephemeral project for connect flow');
      const apiUrl = (process.env.PLAYWRIGHT_API_URL ?? 'https://seo-api.geekatyourspot.com').replace(/\/$/u, '');
      const createRes = await fetch(`${apiUrl}/api/seo/projects`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Id': devUserId,
        },
        body: JSON.stringify({
          name: `Human Google UI ${Date.now()}`,
          url: 'https://geekatyourspot.com',
          defaultLocation: 'United States',
        }),
      });
      if (!createRes.ok) {
        throw new Error(`Could not create dev project: ${createRes.status}`);
      }
      const created = await createRes.json();
      activeProjectId = created.id;
      console.log(`  dev project: ${activeProjectId}`);
      step('Dev-user mode — skip OAuth, open Google connect UI');
      await navigateToGoogleConnectUi(page, effectiveBase, activeProjectId);
      await screenshot(page, '04-project-page');
      console.log('  dev-user: using NEXT_PUBLIC_DEV_USER_ID for API auth');
    } else {
      await loginViaGeekOAuth(page);
      step('Open Google connect UI');
      await navigateToGoogleConnectUi(page, effectiveBase, activeProjectId);
      await screenshot(page, '04-project-page');

      const token400 = authEvents.filter((e) => e.status === 400);
      console.log(`  Auth/token after project load: ${JSON.stringify(authEvents)}`);
      if (token400.length > 0) {
        console.log('  FAIL still seeing 400 on /api/auth/token');
      } else {
        console.log('  PASS no auth/token 400 on project load');
      }
    }

    step('Connect Google');
    const connectBtn = page.getByRole('button', { name: 'Connect Google' });
    const connected = await page.getByText(/google connected/i).isVisible().catch(() => false);
    const connectVisible = await connectBtn.isVisible().catch(() => false);

    if (connected) {
      console.log('  PASS Google already connected');
      await screenshot(page, '05-already-connected');
    } else if (!connectVisible) {
      await screenshot(page, '05-no-connect-button');
      throw new Error('Connect Google button not visible');
    } else {
      await connectBtn.scrollIntoViewIfNeeded();
      await page.waitForTimeout(600);
      await screenshot(page, '05-before-connect-click');

      const connectApiPromise = page
        .waitForResponse(
          (r) => r.url().includes('/api/seo/integrations/google/connect-url'),
          { timeout: 20_000 },
        )
        .catch(() => null);

      await Promise.all([
        page.waitForURL(/accounts\.google\.com|google=error/i, { timeout: 35_000 }).catch(() => null),
        connectBtn.click(),
      ]);

      const connectApi = await connectApiPromise;
      if (connectApi) {
        console.log(`  connect-url → ${connectApi.status()}`);
        if (connectApi.status() !== 200) {
          let body = '';
          try {
            body = (await connectApi.text()).slice(0, 200);
          } catch {
            body = '';
          }
          console.log(`  FAIL connect-url: ${body}`);
          process.exitCode = 1;
        }
      } else {
        console.log('  WARN no connect-url response within 20s');
      }
      await page.waitForTimeout(2000);
      await screenshot(page, '06-after-connect-click');

      const finalUrl = page.url();
      console.log(`  Final URL: ${finalUrl}`);
      if (/accounts\.google\.com/i.test(finalUrl)) {
        console.log('  PASS reached Google OAuth page');
      } else if (/google=error/i.test(finalUrl)) {
        console.log(`  FAIL google=error — ${finalUrl}`);
        process.exitCode = 1;
      } else {
        console.log('  WARN unexpected URL after Connect Google');
      }
    }

    if (seoFailures.length > 0) {
      console.log(`\nSEO API failures:\n  ${seoFailures.join('\n  ')}`);
    } else {
      console.log('\nPASS no SEO API 4xx/5xx during flow');
    }

    if (consoleErrors.length > 0) {
      console.log(`\nConsole errors (${consoleErrors.length}):`);
      for (const err of consoleErrors.slice(0, 8)) console.log(`  • ${err.slice(0, 200)}`);
    }

    console.log(`\nDone. Screenshots in ${SHOT_DIR}`);
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error('FATAL:', error.message);
  process.exit(1);
});
