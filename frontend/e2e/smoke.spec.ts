import { test, expect } from '@playwright/test';

const apiBaseUrl =
  process.env.PLAYWRIGHT_API_URL?.replace(/\/$/u, '') ??
  'https://geekseobackend-production.up.railway.app';

function collectConsoleErrors(page: import('@playwright/test').Page) {
  const errors: string[] = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });
  return errors;
}

/** Next.js RSC prefetch of /api/auth/start must not fetch cross-origin authorize URL. */
function isOAuthPrefetchCorsError(text: string) {
  return (
    text.includes('blocked by CORS policy') &&
    text.includes('/api/auth/start') &&
    text.includes('seo.geekatyourspot.com')
  );
}

test.describe('production smoke', () => {
  test('home page renders marketing shell', async ({ page }) => {
    const consoleErrors = collectConsoleErrors(page);

    await page.goto('/');
    await expect(page).toHaveTitle(/Geek SEO/i);
    await expect(page.getByRole('link', { name: /sign in/i }).first()).toBeVisible();

    await expect
      .poll(() => consoleErrors.filter(isOAuthPrefetchCorsError).length, {
        message: 'OAuth start should not be prefetched cross-origin',
      })
      .toBe(0);
  });

  test('sign in navigates to GeekOAuth login', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /start free/i }).first().click();

    await page.waitForURL(/auth\.geekatyourspot\.com\/(Account\/Login|connect\/authorize)/i, {
      timeout: 20_000,
    });
    await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible();
  });

  test('OAuth start route returns redirect without client fetch', async ({ request, baseURL }) => {
    const startUrl = new URL('/api/auth/start', baseURL).toString();
    const response = await request.get(startUrl, { maxRedirects: 0 });
    expect(response.status()).toBeGreaterThanOrEqual(302);
    expect(response.status()).toBeLessThan(400);
    const location = response.headers().location ?? '';
    expect(location).toMatch(/connect\/authorize/);
  });

  test('GeekSeoBackend health', async ({ request }) => {
    const response = await request.get(`${apiBaseUrl}/health`);
    expect(response.ok()).toBeTruthy();

    const body = (await response.json()) as { service?: string; gateway?: string };
    expect(body.service).toBe('GeekSeoBackend');
    expect(body.gateway).toBe('ok');
  });

  test('pricing page shows plan catalog', async ({ page }) => {
    await page.goto('/pricing');
    await expect(page.getByRole('heading', { name: /geek seo pricing/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Starter' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Professional' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Team' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Agency' })).toBeVisible();
  });

  test('subscription plans API exposes sandbox checkout', async ({ request }) => {
    const response = await request.get(`${apiBaseUrl}/api/seo/subscription/plans`);
    expect(response.ok()).toBeTruthy();

    const body = (await response.json()) as {
      tiers?: unknown[];
      checkout?: {
        available?: boolean;
        deferred?: boolean;
        environment?: string;
        planIds?: Record<string, string>;
      };
    };
    expect(body.tiers?.length).toBeGreaterThanOrEqual(4);
    expect(body.checkout?.available).toBe(true);
    expect(body.checkout?.deferred).toBe(false);
    expect(body.checkout?.environment).toBe('sandbox');
    expect(Object.keys(body.checkout?.planIds ?? {}).length).toBeGreaterThanOrEqual(4);
  });
});
