import { expect, type Page } from '@playwright/test';

export type TestCredentials = {
  email: string;
  password: string;
};

const AUTH_ORIGIN = 'https://auth.geekatyourspot.com';

function resolveAuthUrl(location: string | undefined): string | null {
  if (!location) return null;
  if (location.startsWith('http://') || location.startsWith('https://')) return location;
  return `${AUTH_ORIGIN}${location.startsWith('/') ? location : `/${location}`}`;
}

function isAppUrl(url: string): boolean {
  return /seo\.geekatyourspot\.com\/app\//i.test(url);
}

function isCallbackUrl(url: string): boolean {
  return /seo\.geekatyourspot\.com\/auth\/callback/i.test(url);
}

async function maybeClickConsent(page: Page): Promise<void> {
  const consent = page.getByRole('button', { name: /^(allow|authorize|accept|yes)$/i });
  if (await consent.isVisible().catch(() => false)) {
    await consent.click();
  }
}

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
  const base = baseURL.replace(/\/$/u, '');
  const deadline = Date.now() + 120_000;

  await page.goto(new URL('/api/auth/start', base).toString(), {
    waitUntil: 'domcontentloaded',
    timeout: 60_000,
  });

  await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible({
    timeout: 20_000,
  });

  if (/TwoFactor/i.test(page.url())) {
    throw new Error('PLAYWRIGHT_TEST_* user has 2FA enabled; use a test account without 2FA.');
  }

  await page.locator('#Input_Email').fill(credentials.email);
  await page.locator('#Input_Password').fill(credentials.password);

  const [loginResponse] = await Promise.all([
    page.waitForResponse(
      (response) =>
        response.request().method() === 'POST' && response.url().includes('/Account/Login'),
      { timeout: 30_000 },
    ),
    page.locator('form').first().evaluate((form) => {
      if (form instanceof HTMLFormElement) form.requestSubmit();
    }),
  ]);

  if (loginResponse.status() >= 400) {
    throw new Error(`GeekOAuth login POST failed with HTTP ${loginResponse.status()}.`);
  }

  const nextUrl = resolveAuthUrl(loginResponse.headers()['location']);
  if (nextUrl) {
    await page.goto(nextUrl, { waitUntil: 'domcontentloaded', timeout: 60_000 });
  }

  while (Date.now() < deadline) {
    const url = page.url();
    if (isAppUrl(url)) {
      break;
    }
    if (isCallbackUrl(url)) {
      await page.waitForURL(/\/app\//, { waitUntil: 'domcontentloaded', timeout: 60_000 });
      break;
    }
    if (/connect\/authorize/i.test(url)) {
      await maybeClickConsent(page);
      await page.waitForTimeout(500);
      continue;
    }
    if (/auth\.geekatyourspot\.com\/Account\/Login/i.test(url)) {
      const body = await page.locator('body').innerText();
      if (/invalid|incorrect password|login failed/i.test(body)) {
        throw new Error(
          'GeekOAuth rejected PLAYWRIGHT_TEST_EMAIL/PASSWORD — update frontend/.env.playwright.local.',
        );
      }
      const returnUrl = new URL(url).searchParams.get('ReturnUrl');
      if (returnUrl) {
        await page.goto(resolveAuthUrl(returnUrl) ?? returnUrl, {
          waitUntil: 'domcontentloaded',
          timeout: 60_000,
        });
        continue;
      }
    }
    await page.waitForTimeout(400);
  }

  if (!isAppUrl(page.url())) {
    throw new Error(`OAuth login did not reach /app (last URL: ${page.url()}).`);
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
