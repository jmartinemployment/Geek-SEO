#!/usr/bin/env node
/**
 * Human-paced full app audit — login, every sidebar route, key interactions.
 *
 *   npm run test:human:app-audit
 *   PLAYWRIGHT_HEADED=true npm run test:human:app-audit
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';
import { loginViaGeekOAuth } from './lib/geek-oauth-login.mjs';

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
const SHOT_DIR = process.env.HUMAN_AUDIT_SHOT_DIR ?? '/tmp/geek-seo-human-audit';
const email = process.env.PLAYWRIGHT_TEST_EMAIL?.trim();
const password = process.env.PLAYWRIGHT_TEST_PASSWORD?.trim();
const headed = process.env.PLAYWRIGHT_HEADED === 'true';
const slowMo = headed ? 120 : 40;

const PAGES = [
  { name: 'Dashboard', path: '/app/dashboard', heading: /dashboard/i },
  { name: 'Topical Map', path: '/app/strategy/topical-map', heading: /topical map/i },
  { name: 'Content', path: '/app/content', heading: /content/i },
  { name: 'Keywords', path: '/app/keywords', heading: /keyword/i },
  { name: 'Cannibalization', path: '/app/cannibalization', heading: /cannibal/i },
  { name: 'Rankings', path: '/app/rankings', heading: /gsc rankings|rankings/i },
  { name: 'Site Audit', path: '/app/audit', heading: /site audit/i },
  { name: 'Analytics', path: '/app/analytics', heading: /analytics/i },
  { name: 'Guided', path: '/app/guided', heading: /guided/i },
  { name: 'Bulk', path: '/app/bulk', heading: /bulk/i },
  { name: 'Calendar', path: '/app/calendar', heading: /calendar/i },
  { name: 'Deep SERP', path: '/app/serp', heading: /serp/i },
  { name: 'Planner', path: '/app/planner', heading: /planner/i },
  { name: 'Brand voice', path: '/app/brand-voice', heading: /brand voice/i },
  { name: 'Briefs', path: '/app/briefs/new', heading: /brief/i },
  { name: 'GEO', path: '/app/geo', heading: /geo/i },
  { name: 'Content guard', path: '/app/content-guard', heading: /content guard/i },
  { name: 'Settings', path: '/app/settings', heading: /settings/i },
  { name: 'Projects', path: '/app/projects', heading: /projects/i },
];

fs.mkdirSync(SHOT_DIR, { recursive: true });

function slug(name) {
  return name.toLowerCase().replace(/\s+/g, '-');
}

async function login(page) {
  await loginViaGeekOAuth(page, {
    baseUrl: BASE,
    email,
    password,
  });
}

async function auditPage(page, { name, path: pagePath, heading }) {
  const issues = [];
  const apiBefore = seoFailures.length;

  await page.goto(`${BASE}${pagePath}`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
  await page.waitForTimeout(3500);

  const finalUrl = page.url();
  if (!finalUrl.includes(pagePath.split('?')[0]) && finalUrl.includes('/auth/login')) {
    issues.push('redirected to login (session lost)');
  }

  const headingVisible = await page.getByRole('heading', { name: heading }).first().isVisible().catch(() => false);
  if (!headingVisible) {
    issues.push('expected heading not visible');
  }

  const alertText = await page.locator('[role="alert"]').allTextContents().catch(() => []);
  for (const text of alertText) {
    const trimmed = text.trim();
    if (trimmed) issues.push(`alert: ${trimmed.slice(0, 120)}`);
  }

  const stuckLoading = await page.getByText(/^Loading…$/).isVisible().catch(() => false);
  if (stuckLoading) issues.push('Loading… still visible after 3.5s');

  const newApiFailures = seoFailures.slice(apiBefore);
  for (const f of newApiFailures) {
    issues.push(`API ${f}`);
  }

  const file = path.join(SHOT_DIR, `${slug(name)}.png`);
  await page.screenshot({ path: file, fullPage: true });

  return { name, path: pagePath, issues, screenshot: file, url: finalUrl };
}

const seoFailures = [];
const consoleErrors = [];

async function main() {
  if (!email || !password || email === 'your-test-user@example.com') {
    console.error('Set PLAYWRIGHT_TEST_EMAIL/PASSWORD in frontend/.env.playwright.local');
    process.exit(1);
  }

  console.log(`Target: ${BASE}`);
  console.log(`Screenshots: ${SHOT_DIR}`);
  console.log(`Headed: ${headed}\n`);

  const browser = await chromium.launch({ headless: !headed, slowMo });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();

  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('response', (response) => {
    const url = response.url();
    if (url.includes('/api/seo/') && response.status() >= 400) {
      seoFailures.push(`${response.request().method()} ${new URL(url).pathname} → ${response.status()}`);
    }
  });

  try {
    console.log('▶ Login');
    await login(page);
    console.log(`  OK — ${page.url()}\n`);

    console.log('▶ Interactive — Topical Map generate');
    await page.goto(`${BASE}/app/strategy/topical-map`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    const genBtn = page.getByRole('button', { name: /generate|regenerate/i }).first();
    if (await genBtn.isVisible().catch(() => false)) {
      await genBtn.click();
      await page.waitForTimeout(8000);
      await page.screenshot({ path: path.join(SHOT_DIR, 'topical-map-after-generate.png'), fullPage: true });
    }

    console.log('▶ Interactive — Site audit run');
    await page.goto(`${BASE}/app/audit`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    const runBtn = page.getByRole('button', { name: /run site audit/i });
    if (await runBtn.isEnabled().catch(() => false)) {
      await runBtn.click();
      await page.waitForTimeout(5000);
      await page.screenshot({ path: path.join(SHOT_DIR, 'site-audit-after-run.png'), fullPage: true });
    }

    console.log('\n▶ Page tour');
    const results = [];
    for (const pg of PAGES) {
      const result = await auditPage(page, pg);
      const status = result.issues.length === 0 ? 'clean' : result.issues.join('; ');
      console.log(`  ${result.name} (${result.path}) — ${status}`);
      results.push(result);
    }

    const apiSummary = {};
    for (const f of seoFailures) {
      apiSummary[f] = (apiSummary[f] ?? 0) + 1;
    }

    console.log('\n=== API failures (deduped) ===');
    for (const [key, count] of Object.entries(apiSummary).sort((a, b) => b[1] - a[1])) {
      console.log(`  ${key}${count > 1 ? ` (${count}x)` : ''}`);
    }

    const uniqueConsole = [...new Set(consoleErrors)].slice(0, 15);
    if (uniqueConsole.length > 0) {
      console.log('\n=== Console errors (sample) ===');
      for (const line of uniqueConsole) console.log(`  ${line.slice(0, 160)}`);
    }

    const dirty = results.filter((r) => r.issues.length > 0);
    console.log(`\n=== Summary: ${dirty.length}/${results.length} pages with issues ===`);
    for (const r of dirty) {
      console.log(`  ${r.name}: ${r.issues.join(' | ')}`);
    }

    const reportPath = path.join(SHOT_DIR, 'report.json');
    fs.writeFileSync(
      reportPath,
      JSON.stringify({ base: BASE, results, apiSummary, consoleErrors: uniqueConsole }, null, 2),
    );
    console.log(`\nReport: ${reportPath}`);

    process.exit(dirty.length > 0 || Object.keys(apiSummary).length > 0 ? 1 : 0);
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error('FATAL:', err.message);
  process.exit(1);
});
