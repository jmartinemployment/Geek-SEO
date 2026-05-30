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
    const allowed = /→ (401|403|404|500|502|503|504)$/u;
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
  await page.getByRole('button', { name: 'Sign in' }).click();

  await page.waitForURL(
    /\/auth\/callback|\/app\/|connect\/authorize/i,
    { timeout: 30_000 },
  );

  if (page.url().includes('/auth/callback')) {
    await page.waitForURL(/\/app\//, { timeout: 30_000 });
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
