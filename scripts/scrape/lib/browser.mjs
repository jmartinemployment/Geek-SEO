/**
 * Playwright Chromium — puppeteer import hangs on some environments (e.g. iCloud paths).
 */
import { chromium } from 'playwright';

const DEFAULT_UA =
  'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 GeekSEO-Research/1.0';

/**
 * @param {{ headless?: boolean }} opts
 */
export async function launchBrowser(opts = {}) {
  return chromium.launch({
    headless: opts.headless !== false,
  });
}

/**
 * @param {import('playwright').Browser} browser
 * @param {{ userAgent?: string; viewport?: { width: number; height: number } }} opts
 */
export async function newPage(browser, opts = {}) {
  const vp = opts.viewport ?? { width: 1440, height: 900 };
  const context = await browser.newContext({
    userAgent: opts.userAgent ?? DEFAULT_UA,
    viewport: vp,
  });
  return context.newPage();
}

export { DEFAULT_UA };
