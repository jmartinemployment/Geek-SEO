/**
 * @param {string} baseUrl
 * @param {string} href
 */
export function resolveUrl(baseUrl, href) {
  try {
    return new URL(href, baseUrl).href;
  } catch {
    return null;
  }
}

/**
 * @param {string} url
 * @param {string} origin
 */
export function isSameOrigin(url, origin) {
  try {
    return new URL(url).origin === origin;
  } catch {
    return false;
  }
}

/**
 * @param {import('playwright').Page} page
 * @param {string} baseUrl
 */
export async function collectSameOriginLinks(page, baseUrl) {
  const origin = new URL(baseUrl).origin;
  const hrefs = await page.evaluate(() =>
    Array.from(document.querySelectorAll('a[href]'))
      .map((a) => a.getAttribute('href'))
      .filter(Boolean),
  );

  const seen = new Set();
  const out = [];
  for (const href of hrefs) {
    const abs = resolveUrl(baseUrl, href);
    if (!abs || !isSameOrigin(abs, origin)) continue;
    const u = new URL(abs);
    u.hash = '';
    const normalized = u.href;
    if (seen.has(normalized)) continue;
    if (/\.(pdf|zip|png|jpe?g|gif|webp|svg|mp4|mp3|woff2?)$/i.test(u.pathname)) continue;
    seen.add(normalized);
    out.push(normalized);
  }
  return out;
}
