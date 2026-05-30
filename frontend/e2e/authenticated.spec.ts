import fs from 'node:fs';
import path from 'node:path';
import { test, expect } from '@playwright/test';
import {
  assertSeoApiFailures,
  getTestCredentials,
  isDevUserMode,
  trackSeoApiFailures,
} from './auth-helpers';

const authFile = path.join(__dirname, '.auth/user.json');
const credentials = getTestCredentials();
const hasAuthState = fs.existsSync(authFile);
const devMode = isDevUserMode();
const baseURL = (process.env.PLAYWRIGHT_BASE_URL ?? 'https://seo.geekatyourspot.com').replace(
  /\/$/u,
  '',
);
const isLocalBase = /localhost|127\.0\.0\.1/u.test(baseURL);

const skipReason = devMode
  ? !isLocalBase
    ? 'PLAYWRIGHT_USE_DEV_USER requires PLAYWRIGHT_BASE_URL=http://localhost:3000 (start: npm run dev).'
    : null
  : !credentials
    ? 'Add PLAYWRIGHT_TEST_EMAIL and PLAYWRIGHT_TEST_PASSWORD to frontend/.env.playwright.local, or run: npm run test:e2e:auth:local'
    : !hasAuthState
      ? 'global-setup did not create e2e/.auth/user.json — check credentials and GeekOAuth (no 2FA).'
      : null;

test.describe('authenticated app', () => {
  test.skip(Boolean(skipReason), skipReason ?? '');

  if (!devMode) {
    test.use({ storageState: authFile });
  }

  test('projects page loads project list or empty state', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/projects', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible();
    await expect(page.getByText('Loading projects…')).toBeHidden({ timeout: 20_000 });

    const hasList = (await page.getByRole('listitem').count()) > 0;
    const hasEmpty = await page
      .getByText('No projects yet. Create one above to start writing.')
      .isVisible()
      .catch(() => false);
    const hasError = await page
      .getByText(/failed to load|something went wrong|error/i)
      .isVisible()
      .catch(() => false);

    if (devMode && hasError) {
      test.info().annotations.push({
        type: 'warning',
        description: 'GeekSeoBackend may be degraded — start/fix localhost:5051 for full API coverage.',
      });
    } else {
      expect(hasList || hasEmpty).toBeTruthy();
    }

    assertSeoApiFailures(apiFailures);
  });

  test('dashboard loads for authenticated user', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/dashboard', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible({
      timeout: 20_000,
    });
    if (!devMode) {
      expect(apiFailures.filter((f) => f.includes(' 500'))).toEqual([]);
    } else {
      assertSeoApiFailures(apiFailures);
    }
  });

  test('rankings page renders Google integration UI', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/rankings', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: /GSC rankings/i })).toBeVisible();
    await expect(page.getByText(/^Loading…$/)).toBeHidden({ timeout: 20_000 });

    const emptyState = await page
      .getByText(/create a project first/i)
      .isVisible()
      .catch(() => false);

    if (emptyState) {
      await expect(page.getByRole('link', { name: /go to projects/i })).toBeVisible();
    } else {
      await expect(page.locator('#google-project-select')).toBeVisible({ timeout: 15_000 });

      const connectVisible = await page
        .getByRole('button', { name: /connect google/i })
        .isVisible()
        .catch(() => false);
      const connectedVisible = await page
        .getByText(/google connected/i)
        .isVisible()
        .catch(() => false);
      const dataVisible = await page
        .getByRole('table')
        .isVisible()
        .catch(() => false);

      expect(connectVisible || connectedVisible || dataVisible).toBeTruthy();
    }
    if (!devMode) {
      expect(apiFailures.filter((f) => !f.includes('404'))).toEqual([]);
    } else {
      assertSeoApiFailures(apiFailures);
    }
  });

  test('analytics page renders GA4 panel', async ({ page }) => {
    await page.goto('/app/analytics', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: 'Analytics' })).toBeVisible({
      timeout: 15_000,
    });
  });
});
