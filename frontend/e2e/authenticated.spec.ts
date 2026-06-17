import fs from 'node:fs';
import path from 'node:path';
import { test, expect } from '@playwright/test';
import {
  assertSeoApiFailures,
  getTestCredentials,
  isDevUserMode,
  trackSeoApiFailures,
} from './auth-helpers';
import {
  createEphemeralContentDocument,
  deleteProject,
  devApiHeaders,
  getSeoApiBaseUrl,
} from './api-helpers';

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

  test('site audit page loads (not redirected to dashboard)', async ({ page }) => {
    const apiFailures = trackSeoApiFailures(page);

    await page.goto('/app/audit', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/app\/dashboard/u);
    await expect(page.getByRole('heading', { name: 'Site audit' })).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByRole('button', { name: /run site audit/i })).toBeVisible();

    if (!devMode) {
      expect(apiFailures.filter((f) => f.includes(' 500'))).toEqual([]);
    } else {
      assertSeoApiFailures(apiFailures);
    }
  });

  test('content editor shows optional plagiarism panel', async ({ page, request }) => {
    test.skip(!devMode, 'Uses local dev-user flow with GeekSeoBackend data gateway.');

    const probe = await request.post(`${getSeoApiBaseUrl()}/api/seo/projects`, {
      headers: devApiHeaders(),
      data: {
        name: `Gateway probe ${Date.now()}`,
        url: 'https://example.com',
        defaultLocation: 'United States',
      },
    });
    if (!probe.ok()) {
      test.skip(
        true,
        `GeekSeoBackend data gateway unavailable (${probe.status()}). Set GEEK_API_URL and GEEK_BACKEND_API_KEY in GeekSeoBackend/.env — see scripts/LOCAL_DEV.md.`,
      );
    }
    const probeProject = (await probe.json()) as { id?: string };
    if (probeProject.id) {
      await deleteProject(request, probeProject.id);
    }

    const apiFailures = trackSeoApiFailures(page);
    const { projectId, documentId } = await createEphemeralContentDocument(request);

    try {
      await page.goto(`/content-writing?documentId=${documentId}`, { waitUntil: 'domcontentloaded' });
      await expect(page.getByRole('heading', { name: /Review workspace/i })).toBeVisible({
        timeout: 25_000,
      });
      assertSeoApiFailures(apiFailures);
    } finally {
      await deleteProject(request, projectId);
    }
  });

  test('pricing page shows sandbox PayPal subscribe targets', async ({ page }) => {
    test.skip(devMode, 'PayPal checkout UI test runs against production (sandbox) only.');

    const apiFailures = trackSeoApiFailures(page);
    await page.goto('/pricing', { waitUntil: 'domcontentloaded' });

    await expect(page.getByText(/paypal is in.*sandbox mode/i)).toBeVisible({ timeout: 20_000 });
    await expect(page.getByText(/checkout coming soon/i)).toHaveCount(0);

    for (const tier of ['starter', 'professional', 'team', 'agency']) {
      await expect(page.locator(`#paypal-${tier}`)).toBeVisible({ timeout: 20_000 });
    }

    expect(apiFailures.filter((f) => f.includes('/subscription/plans'))).toEqual([]);
  });
});
