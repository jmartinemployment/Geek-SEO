const AUTH_ORIGIN = 'https://auth.geekatyourspot.com';

function resolveAuthUrl(location) {
  if (!location) return null;
  if (location.startsWith('http://') || location.startsWith('https://')) return location;
  return `${AUTH_ORIGIN}${location.startsWith('/') ? location : `/${location}`}`;
}

function isAppUrl(url) {
  return /seo\.geekatyourspot\.com\/app\//i.test(url);
}

function isCallbackUrl(url) {
  return /seo\.geekatyourspot\.com\/auth\/callback/i.test(url);
}

async function maybeClickConsent(page) {
  const consent = page.getByRole('button', { name: /^(allow|authorize|accept|yes)$/i });
  if (await consent.isVisible().catch(() => false)) {
    await consent.click();
  }
}

/**
 * PKCE login for production: /api/auth/start → auth login POST → follow authorize → callback → /app.
 * Playwright must follow the login POST Location header manually; waitForURL + click races ERR_ABORTED.
 */
export async function loginViaGeekOAuth(page, { baseUrl, email, password, timeoutMs = 120_000 }) {
  const deadline = Date.now() + timeoutMs;

  await page.goto(`${baseUrl.replace(/\/$/u, '')}/api/auth/start`, {
    waitUntil: 'domcontentloaded',
    timeout: 60_000,
  });

  if (/TwoFactor/i.test(page.url())) {
    throw new Error('Account has 2FA enabled — use a GeekOAuth user without 2FA for Playwright.');
  }

  await page.getByRole('heading', { name: /sign in/i }).waitFor({ state: 'visible', timeout: 20_000 });
  await page.locator('#Input_Email').fill(email);
  await page.locator('#Input_Password').fill(password);

  const [loginResponse] = await Promise.all([
    page.waitForResponse(
      (response) =>
        response.request().method() === 'POST' && response.url().includes('/Account/Login'),
      { timeout: 30_000 },
    ),
    page.locator('form').first().evaluate((form) => form.requestSubmit()),
  ]);

  if (loginResponse.status() >= 400) {
    throw new Error(`GeekOAuth login POST failed with HTTP ${loginResponse.status()}.`);
  }

  const location = loginResponse.headers()['location'];
  const nextUrl = resolveAuthUrl(location);
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
        throw new Error('GeekOAuth rejected the email/password — check frontend/.env.playwright.local.');
      }
      // Session cookie is set but authorize redirect was missed — retry authorize from ReturnUrl.
      const returnUrl = new URL(url).searchParams.get('ReturnUrl');
      if (returnUrl) {
        await page.goto(resolveAuthUrl(returnUrl), { waitUntil: 'domcontentloaded', timeout: 60_000 });
        continue;
      }
    }
    await page.waitForTimeout(400);
  }

  if (!isAppUrl(page.url())) {
    throw new Error(`OAuth login did not reach /app (last URL: ${page.url()}).`);
  }

  await page.getByRole('heading', { name: 'Projects' }).waitFor({ state: 'visible', timeout: 30_000 });
}
