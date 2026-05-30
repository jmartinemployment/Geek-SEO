import fs from 'node:fs';
import path from 'node:path';
import { test, expect } from '@playwright/test';
import { getTestCredentials, trackSeoApiFailures } from './auth-helpers';

const authFile = path.join(__dirname, '.auth/user.json');
const credentials = getTestCredentials();
const hasAuthState = fs.existsSync(authFile);

const skipReason = !credentials
  ? 'Add PLAYWRIGHT_TEST_EMAIL and PLAYWRIGHT_TEST_PASSWORD to frontend/.env.playwright.local (password must not be empty).'
  : !hasAuthState
    ? 'global-setup did not create e2e/.auth/user.json — check credentials and GeekOAuth (no 2FA).'
    : null;

test.describe('authenticated app', () => {
  test.skip(Boolean(skipReason), skipReason ?? '');

  test.use({ storageState: authFile });

  test('projects page loads project list or empty state', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/projects');
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible();
    await expect(page.getByText('Loading projects…')).toBeHidden({ timeout: 20_000 });

    const hasList = (await page.getByRole('listitem').count()) > 0;
    const hasEmpty = await page
      .getByText('No projects yet. Create one above to start writing.')
      .isVisible()
      .catch(() => false);

    expect(hasList || hasEmpty).toBeTruthy();
    expect(apiFailures, 'SEO API calls from projects page').toEqual([]);
  });

  test('dashboard loads for authenticated user', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/dashboard');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible({
      timeout: 20_000,
    });
    expect(apiFailures.filter((f) => f.includes(' 500'))).toEqual([]);
  });

  test('rankings page renders Google integration UI', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/rankings');
    await expect(page.getByRole('heading', { name: /GSC rankings/i })).toBeVisible();
    await expect(page.getByLabel(/project/i).first()).toBeVisible({ timeout: 15_000 });

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
    expect(apiFailures.filter((f) => !f.includes('404'))).toEqual([]);
  });

  test('analytics page renders GA4 panel', async ({ page }) => {
    await page.goto('/app/analytics');
    await expect(page.getByRole('heading', { name: 'Analytics' })).toBeVisible({
      timeout: 15_000,
    });
  });
});
