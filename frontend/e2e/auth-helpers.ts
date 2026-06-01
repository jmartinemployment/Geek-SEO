import { expect, type Page } from '@playwright/test';

export type TestCredentials = {
  email: string;
  password: string;
};

export function getTestCredentials(): TestCredentials | null {
  const email = process.env.PLAYWRIGHT_TEST_EMAIL?.trim();
  const password = process.env.PLAYWRIGHT_TEST_PASSWORD?.trim();
  if (!email || !password || email === 'your-test-user@example.com') {
    return null;
  }
  return { email, password };
}

/** Localhost Next.js with NEXT_PUBLIC_DEV_USER_ID — no GeekOAuth login. */
export function isDevUserMode(): boolean {
  return process.env.PLAYWRIGHT_USE_DEV_USER === 'true';
}

export function assertSeoApiFailures(failures: string[]) {
  if (isDevUserMode()) {
    const allowed = /→ (400|401|403|404|500|502|503|504)$/u;
    const blocking = failures.filter((f) => !allowed.test(f));
    expect(blocking, 'unexpected SEO API failures (dev mode)').toEqual([]);
    return;
  }
  expect(failures, 'SEO API calls').toEqual([]);
}

/**
 * Full PKCE login: /api/auth/start → GeekOAuth → callback → /app/projects.
 * Requires a GeekOAuth user without 2FA (or 2FA already satisfied).
 */
export async function loginViaGeekOAuth(
  page: Page,
  baseURL: string,
  credentials: TestCredentials,
) {
  await page.goto(new URL('/api/auth/start', baseURL).toString());

  await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible({
    timeout: 20_000,
  });

  if (/TwoFactor/i.test(page.url())) {
    throw new Error('PLAYWRIGHT_TEST_* user has 2FA enabled; use a test account without 2FA.');
  }

  await page.getByLabel('Email').fill(credentials.email);
  await page.getByLabel('Password').fill(credentials.password);

  await Promise.all([
    page.waitForURL(/\/auth\/callback|\/app\/|connect\/authorize/i, {
      timeout: 60_000,
      waitUntil: 'domcontentloaded',
    }),
    page.getByRole('button', { name: 'Sign in' }).click(),
  ]);

  if (/connect\/authorize/i.test(page.url())) {
    const consent = page.getByRole('button', { name: /^(allow|authorize|accept)$/i });
    if (await consent.isVisible().catch(() => false)) {
      await consent.click();
      await page.waitForURL(/\/auth\/callback|\/app\//, {
        timeout: 60_000,
        waitUntil: 'domcontentloaded',
      });
    }
  }

  if (page.url().includes('/auth/callback')) {
    await page.waitForURL(/\/app\//, {
      timeout: 60_000,
      waitUntil: 'domcontentloaded',
    });
  }

  if (/auth\.geekatyourspot\.com/i.test(page.url())) {
    const body = await page.locator('body').innerText();
    if (/invalid|incorrect password|login failed/i.test(body)) {
      throw new Error('GeekOAuth rejected PLAYWRIGHT_TEST_EMAIL/PASSWORD — update frontend/.env.playwright.local.');
    }
    throw new Error(
      'OAuth login did not complete (still on auth.geekatyourspot.com). '
        + 'Use a test account without 2FA and verify credentials in frontend/.env.playwright.local.',
    );
  }

  await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible({
    timeout: 30_000,
  });
}

export function trackSeoApiFailures(page: Page) {
  const failures: string[] = [];
  page.on('response', (response) => {
    const url = response.url();
    if (!url.includes('/api/seo/')) {
      return;
    }
    if (response.status() >= 400) {
      const path = new URL(url).pathname;
      failures.push(`${response.request().method()} ${path} → ${response.status()}`);
    }
  });
  return failures;
}
