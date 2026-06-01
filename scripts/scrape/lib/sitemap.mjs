import { DEFAULT_UA } from './browser.mjs';
import { isSameOrigin, resolveUrl } from './crawl.mjs';

const ASSET_PATH =
  /\.(pdf|zip|png|jpe?g|gif|webp|svg|ico|mp4|mp3|woff2?|ttf|eot|css|js)$/i;

const LOC_RE = /<loc>\s*([^<]+?)\s*<\/loc>/gi;

/**
 * @param {string} xml
 */
export function parseSitemapLocs(xml) {
  const locs = [];
  for (const match of xml.matchAll(LOC_RE)) {
    const loc = match[1]?.trim();
    if (loc) locs.push(loc);
  }
  return locs;
}

/**
 * @param {string} xml
 */
export function isSitemapIndex(xml) {
  return /<sitemapindex[\s>]/i.test(xml);
}

/**
 * @param {string} url
 */
export function normalizePageUrl(url) {
  const u = new URL(url);
  u.hash = '';
  return u.href;
}

/**
 * @param {string} url
 * @param {string} origin
 */
export function isScrapableSitemapUrl(url, origin) {
  try {
    const u = new URL(url);
    if (!isSameOrigin(u.href, origin)) return false;
    if (ASSET_PATH.test(u.pathname)) return false;
    if (u.protocol !== 'http:' && u.protocol !== 'https:') return false;
    return true;
  } catch {
    return false;
  }
}

/**
 * @param {string} url
 */
async function fetchSitemapXml(url) {
  const res = await fetch(url, {
    headers: {
      'User-Agent': DEFAULT_UA,
      Accept: 'application/xml,text/xml,*/*',
    },
    redirect: 'follow',
  });
  if (!res.ok) {
    throw new Error(`HTTP ${res.status}`);
  }
  return res.text();
}

/**
 * @param {string} robotsText
 * @param {string} origin
 */
export function parseRobotsSitemaps(robotsText, origin) {
  const out = [];
  for (const line of robotsText.split('\n')) {
    const match = /^sitemap:\s*(.+)$/i.exec(line.trim());
    if (!match?.[1]) continue;
    const abs = resolveUrl(origin, match[1].trim());
    if (abs) out.push(abs);
  }
  return out;
}

/**
 * @param {string} origin
 */
export function defaultSitemapCandidates(origin) {
  return [
    `${origin}/sitemap.xml`,
    `${origin}/sitemap_index.xml`,
    `${origin}/sitemap-index.xml`,
  ];
}

/**
 * @param {string} startUrl
 * @param {{ maxUrls?: number; maxSitemapFiles?: number }} [opts]
 * @returns {Promise<{ urls: string[]; sitemapSources: string[]; discovery: 'sitemap' | 'none' }>}
 */
export async function discoverUrlsFromSitemap(startUrl, opts = {}) {
  const maxUrls = opts.maxUrls ?? 2000;
  const maxSitemapFiles = opts.maxSitemapFiles ?? 40;

  const start = new URL(startUrl);
  const origin = start.origin;
  const pageUrls = new Set();
  const sitemapSources = [];
  const fetchedSitemaps = new Set();

  /** @param {string} sitemapUrl */
  async function processSitemap(sitemapUrl) {
    const normalized = normalizePageUrl(sitemapUrl);
    if (fetchedSitemaps.has(normalized)) return;
    if (fetchedSitemaps.size >= maxSitemapFiles) return;

    fetchedSitemaps.add(normalized);
    sitemapSources.push(normalized);

    let xml;
    try {
      xml = await fetchSitemapXml(normalized);
    } catch {
      return;
    }

    const locs = parseSitemapLocs(xml);
    if (isSitemapIndex(xml)) {
      for (const loc of locs) {
        const child = resolveUrl(normalized, loc);
        if (!child) continue;
        if (fetchedSitemaps.size >= maxSitemapFiles) break;
        await processSitemap(child);
      }
      return;
    }

    for (const loc of locs) {
      const abs = resolveUrl(normalized, loc);
      if (!abs || !isScrapableSitemapUrl(abs, origin)) continue;
      pageUrls.add(normalizePageUrl(abs));
      if (pageUrls.size >= maxUrls) return;
    }
  }

  const entryPoints = new Set(defaultSitemapCandidates(origin));

  try {
    const robotsUrl = `${origin}/robots.txt`;
    const robotsRes = await fetch(robotsUrl, {
      headers: { 'User-Agent': DEFAULT_UA },
      redirect: 'follow',
    });
    if (robotsRes.ok) {
      const robotsText = await robotsRes.text();
      for (const sm of parseRobotsSitemaps(robotsText, origin)) {
        entryPoints.add(sm);
      }
    }
  } catch {
    // robots.txt optional
  }

  for (const entry of entryPoints) {
    if (fetchedSitemaps.size >= maxSitemapFiles) break;
    await processSitemap(entry);
    if (pageUrls.size >= maxUrls) break;
  }

  if (pageUrls.size === 0) {
    return { urls: [], sitemapSources, discovery: 'none' };
  }

  const startNormalized = normalizePageUrl(startUrl);
  const urls = [...pageUrls];
  urls.sort((a, b) => a.localeCompare(b));
  if (pageUrls.has(startNormalized)) {
    const rest = urls.filter((u) => u !== startNormalized);
    urls.length = 0;
    urls.push(startNormalized, ...rest);
  } else {
    urls.unshift(startNormalized);
  }

  return {
    urls: urls.slice(0, maxUrls),
    sitemapSources: [...new Set(sitemapSources)],
    discovery: 'sitemap',
  };
}
