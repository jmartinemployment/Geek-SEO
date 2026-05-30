import { test, expect } from '@playwright/test';
import { isDevUserMode } from './auth-helpers';

const baseURL = (process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000').replace(/\/$/u, '');
const apiURL = (process.env.PLAYWRIGHT_API_URL ?? 'https://geekseobackend-production.up.railway.app').replace(
  /\/$/u,
  '',
);
const devUserId = process.env.NEXT_PUBLIC_DEV_USER_ID ?? '00000000-0000-0000-0000-000000000001';
const isLocalBase = /localhost|127\.0\.0\.1/u.test(baseURL);
const devMode = isDevUserMode();
const isLocalApi = /localhost|127\.0\.0\.1/u.test(apiURL);

const skipReason = !devMode
  ? 'Set PLAYWRIGHT_USE_DEV_USER=true (see npm run test:e2e:google).'
  : !isLocalBase
    ? 'Google UI test requires PLAYWRIGHT_BASE_URL=http://localhost:3000'
    : isLocalApi
      ? 'Google UI test needs production API — run npm run test:e2e:google (not auth:local).'
      : null;

test.describe('Google GSC/GA4 integration', () => {
  test.skip(Boolean(skipReason), skipReason ?? '');

  test.describe.configure({ mode: 'serial' });

  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const create = await request.post(`${apiURL}/api/seo/projects`, {
      headers: { 'X-User-Id': devUserId, 'Content-Type': 'application/json' },
      data: {
        name: `E2E Google UI ${Date.now()}`,
        url: 'https://geekatyourspot.com',
        defaultLocation: 'United States',
      },
    });
    expect(create.ok(), `create project on ${apiURL}`).toBeTruthy();
    const body = (await create.json()) as { id: string };
    projectId = body.id;
  });

  test.afterAll(async ({ request }) => {
    if (!projectId) {
      return;
    }
    await request.delete(`${apiURL}/api/seo/projects/${projectId}`, {
      headers: { 'X-User-Id': devUserId },
    });
  });

  test('project page shows Connect Google and OAuth redirect works', async ({ page }) => {
    await page.goto(`/app/projects/${projectId}`, { waitUntil: 'domcontentloaded' });

    await expect(page.getByText(/connect google search console/i)).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole('button', { name: 'Connect Google' })).toBeVisible();

    const connectResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/seo/integrations/google/connect-url') &&
        response.status() === 200,
      { timeout: 20_000 },
    );

    await Promise.all([
      page.waitForURL(/accounts\.google\.com/i, { timeout: 30_000 }),
      connectResponse,
      page.getByRole('button', { name: 'Connect Google' }).click(),
    ]);

    const oauthUrl = new URL(page.url());
    expect(oauthUrl.hostname).toBe('accounts.google.com');
    expect(oauthUrl.searchParams.get('client_id')).toBeTruthy();
    expect(oauthUrl.searchParams.get('state')).toBeTruthy();
  });
});
