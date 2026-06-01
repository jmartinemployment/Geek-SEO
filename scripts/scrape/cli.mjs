#!/usr/bin/env node
/**
 * geek-scrape — competitor analysis, feature/component cloning, code planning
 *
 * Usage:
 *   npm run scrape -- site --url https://seranking.com/ --out ./docs/research/competitors/seranking
 *   npm run scrape -- page --url https://seranking.com/pricing.html --out ./docs/research/competitors/seranking/pricing
 *   npm run scrape -- links --url https://seranking.com/
 *
 * Requires: npx playwright install chromium  (once per machine)
 */

import { parseArgs } from 'node:util';
import { join } from 'node:path';

const HELP = `geek-scrape — competitor analysis, feature/component cloning, code planning

Commands:
  site    Scrape a whole site (URLs from sitemap.xml, many pages under pages/*/)
  page    Scrape one URL only (content.md at output root)
  links   List same-origin links from one URL

Common options:
  --url <url>           Required (real product/marketing URL — not example.com)
  --out <dir>           Output directory (required for site/page)
  --selector <css>      Root element (default: main/article/body)
  --wait <ms>           Extra wait after load for JS sites (default: 2500)
  --network             Record XHR/fetch URLs
  --full-page           Full-page screenshot (page command only)
  --max-pages <n>       Site crawl limit (default: 30)
  --delay-ms <n>        Pause between pages on site crawl (default: 1500)
  --link-crawl          If no sitemap URLs found, fall back to link discovery
  --smoke               Allow example.com (Playwright smoke test only)

Presets:
  npm run scrape:seranking              # site — crawl seranking.com
  npm run scrape:seranking:page         # page — homepage only
  npm run scrape:seranking:rank-tracker # page — one feature URL

Examples (the "--" after scrape is required — npm ignores flags without it):
  npm run scrape -- site --url "https://seranking.com/" --out ./docs/research/competitors/seranking --network
  npm run scrape -- page --url "https://seranking.com/position-tracking.html" --out ./docs/research/competitors/seranking/position-tracking --network

Smoke test only (blocked without --smoke):
  npm run scrape -- page --url "https://example.com" --out ./docs/research/competitors/_test/example --smoke

Legacy: \`crawl\` is an alias for \`site\`.

Setup (first time):
  npm run scrape:setup
`;

const { positionals, values } = parseArgs({
  allowPositionals: true,
  options: {
    url: { type: 'string' },
    out: { type: 'string' },
    selector: { type: 'string' },
    wait: { type: 'string', default: '2500' },
    network: { type: 'boolean', default: false },
    'full-page': { type: 'boolean', default: false },
    'max-pages': { type: 'string', default: '30' },
    'delay-ms': { type: 'string', default: '1500' },
    help: { type: 'boolean', short: 'h', default: false },
    smoke: { type: 'boolean', default: false },
    'link-crawl': { type: 'boolean', default: false },
  },
});

let command = positionals[0];

if (values.help || command === 'help') {
  console.log(HELP);
  process.exit(0);
}

if (!values.url) {
  const maybeUrl = positionals.find((p) => p.startsWith('http://') || p.startsWith('https://'));
  const maybeOut = positionals.find((p) => p.startsWith('./') || p.startsWith('../') || p.startsWith('/'));

  if (maybeUrl) {
    console.error(`Missing --url — npm did not forward your flags.

You likely ran:
  npm run scrape --url ${maybeUrl} --out ...

Use this instead (note "--" then "site" or "page"):
  npm run scrape -- site --url "${maybeUrl}" --out "${maybeOut ?? './docs/research/competitors/out'}"
`);
  } else {
    console.error(`Missing --url

Quick start (crawl a competitor site):
  npm run scrape:seranking

One URL only:
  npm run scrape:seranking:page

Or:
  npm run scrape -- site --url "https://seranking.com/" --out "./docs/research/competitors/seranking"
`);
  }
  process.exit(1);
}

const { assertResearchUrl } = await import('./lib/guards.mjs');
assertResearchUrl(values.url, { smoke: values.smoke });

if (!command) {
  command = 'site';
}

if (command === 'crawl') {
  console.error('Note: `crawl` is deprecated — use `site` (same behavior).\n');
  command = 'site';
}

/** @param {import('playwright').Page} page */
async function goto(page, url, waitMs) {
  console.error(`Loading ${url} …`);
  await page.goto(url, { waitUntil: 'load', timeout: 60_000 });
  const extra = Number.parseInt(String(waitMs), 10);
  if (extra > 0) {
    console.error(`Waiting ${extra}ms …`);
    await page.waitForTimeout(extra);
  }
}

async function main() {
  const { basename } = await import('node:path');
  const { launchBrowser, newPage } = await import('./lib/browser.mjs');
  const { extractPage, toMarkdown } = await import('./lib/extract-page.mjs');
  const { collectSameOriginLinks } = await import('./lib/crawl.mjs');
  const { discoverUrlsFromSitemap } = await import('./lib/sitemap.mjs');
  const {
    ensureOutDir,
    writeJson,
    writeText,
    writeScrapeReport,
    writeSiteReport,
    fileExists,
    resolveOutDir,
  } = await import('./lib/output.mjs');

  const wait = values.wait;

  async function cmdPage() {
    if (!values.out) {
      console.error('page requires --out');
      process.exit(1);
    }
    await ensureOutDir(values.out);
    console.error('Launching Chromium …');
    const browser = await launchBrowser();
    const page = await newPage(browser);
    try {
      await goto(page, values.url, wait);
      console.error('Extracting page data …');
      const { data, network } = await extractPage(page, {
        selector: values.selector,
        captureNetwork: values.network,
      });

      console.error('Saving raw HTML …');
      const rawHtml = await page.content();
      await writeText(values.out, 'raw.html', rawHtml);

      console.error('Saving full page text …');
      const fullText = await page.evaluate(() =>
        (document.body?.innerText ?? '')
          .replace(/\r\n/g, '\n')
          .replace(/[ \t]+/g, ' ')
          .replace(/\n{3,}/g, '\n\n')
          .trim(),
      );
      await writeText(values.out, 'full-text.txt', fullText.slice(0, 500_000));

      const shotRel = values['full-page'] ? 'screenshots/full.png' : 'screenshots/viewport.png';
      const shotPath = join(values.out, shotRel);
      console.error(`Screenshot → ${shotPath}`);
      await page.screenshot({
        path: shotPath,
        fullPage: values['full-page'],
      });
      if (!(await fileExists(shotPath))) {
        throw new Error(`Screenshot was not written: ${shotPath}`);
      }

      await writeJson(values.out, 'page.json', data);
      await writeText(values.out, 'content.md', toMarkdown(data, { fullText }));
      await writeJson(values.out, 'links.json', data.links);
      if (values.network) await writeJson(values.out, 'network.json', network);

      const scrapedAt = new Date().toISOString();
      await writeJson(values.out, 'manifest.json', {
        tool: 'geek-scrape',
        command: 'page',
        url: values.url,
        out: values.out,
        scrapedAt,
      });

      await writeScrapeReport(values.out, {
        url: data.url,
        title: data.title,
        scrapedAt,
        files: [
          { name: 'SCRAPE-REPORT.md', description: 'This file — start here' },
          { name: 'raw.html', description: 'Full rendered HTML from the browser' },
          { name: 'full-text.txt', description: 'All visible text on the page' },
          { name: 'content.md', description: 'Research digest: meta, headings, full copy, links' },
          { name: 'page.json', description: 'Structured metadata, headings, links, DOM outline' },
          { name: 'links.json', description: 'Every link extracted from the page' },
          { name: shotRel, description: 'Screenshot proof the browser loaded the URL' },
          ...(values.network ? [{ name: 'network.json', description: 'XHR/fetch URLs' }] : []),
        ],
        stats: {
          headings: data.headings?.length ?? 0,
          links: data.links?.length ?? 0,
          textCharacters: data.textLength ?? fullText.length,
          htmlBytes: rawHtml.length,
        },
      });

      const absOut = resolveOutDir(values.out);
      console.log('');
      console.log(`✓ Page scraped: ${data.url}`);
      console.log(`  Title: ${data.title}`);
      console.log(`  Folder: ${absOut}`);
      console.log('  Open: SCRAPE-REPORT.md, raw.html, content.md, screenshots/viewport.png');
      console.log('');
      console.log('  Tip: for the whole site use `site`, e.g. npm run scrape:seranking');
    } finally {
      await browser.close();
    }
  }

  async function cmdLinks() {
    console.error('Launching Chromium …');
    const browser = await launchBrowser();
    const page = await newPage(browser);
    try {
      await goto(page, values.url, wait);
      const links = await collectSameOriginLinks(page, values.url);
      const payload = { url: values.url, count: links.length, links };
      if (values.out) {
        await ensureOutDir(values.out);
        await writeJson(values.out, 'links.json', payload);
        console.log(`Done. Wrote ${values.out}/links.json (${links.length} links)`);
      } else {
        console.log(JSON.stringify(payload, null, 2));
      }
    } finally {
      await browser.close();
    }
  }

  async function cmdSite() {
    if (!values.out) {
      console.error('site requires --out');
      process.exit(1);
    }
    const maxPages = Number.parseInt(values['max-pages'], 10);
    const delayMs = Number.parseInt(values['delay-ms'], 10);
    await ensureOutDir(values.out);

    console.error('Discovering URLs from sitemap …');
    const sitemapDiscovery = await discoverUrlsFromSitemap(values.url, {
      maxUrls: Math.max(maxPages * 4, 200),
    });

    let discovery = sitemapDiscovery.discovery;
    const queue = [...sitemapDiscovery.urls];
    const queued = new Set(queue);

    if (queue.length === 0) {
      if (values['link-crawl']) {
        console.error('No sitemap URLs found — falling back to link crawl from start URL.');
        queue.push(values.url);
        queued.add(values.url);
        discovery = 'links';
      } else {
        console.error(`No URLs found in sitemap for ${values.url}

Tried robots.txt and /sitemap.xml (and common variants).
Use --link-crawl to discover pages via HTML links instead.`);
        process.exit(1);
      }
    } else {
      console.error(
        `Sitemap: ${queue.length} URL(s) from ${sitemapDiscovery.sitemapSources.length} sitemap file(s)`,
      );
      await writeJson(values.out, 'sitemap-sources.json', {
        startUrl: values.url,
        sources: sitemapDiscovery.sitemapSources,
        urlCount: queue.length,
      });
      await writeJson(values.out, 'sitemap-urls.json', queue);
    }

    console.error(`Site scrape (up to ${maxPages} pages) …`);
    console.error('Launching Chromium …');
    const browser = await launchBrowser();
    const page = await newPage(browser);
    const visited = new Set();
    const index = [];

    try {
      while (queue.length > 0 && visited.size < maxPages) {
        const url = queue.shift();
        if (!url || visited.has(url)) continue;
        visited.add(url);

        console.error(`[${visited.size}/${maxPages}] ${url}`);
        await goto(page, url, wait);
        const { data, network } = await extractPage(page, {
          selector: values.selector,
          captureNetwork: values.network,
        });

        const slug = basename(new URL(url).pathname) || 'index';
        const safe = slug.replace(/[^a-zA-Z0-9_-]+/g, '_').slice(0, 80);
        const pageDir = `pages/${safe}-${visited.size}`;
        const fullDir = join(values.out, pageDir);
        await ensureOutDir(fullDir);
        await writeJson(fullDir, 'page.json', data);
        const pageFullText = await page.evaluate(() =>
          (document.body?.innerText ?? '')
            .replace(/\r\n/g, '\n')
            .replace(/[ \t]+/g, ' ')
            .replace(/\n{3,}/g, '\n\n')
            .trim(),
        );
        await writeText(fullDir, 'full-text.txt', pageFullText.slice(0, 500_000));
        await writeText(fullDir, 'content.md', toMarkdown(data, { fullText: pageFullText }));
        await page.screenshot({ path: join(fullDir, 'screenshot.png') });
        if (values.network) await writeJson(fullDir, 'network.json', network);

        index.push({ url, dir: pageDir, title: data.title });

        if (discovery === 'links') {
          const more = await collectSameOriginLinks(page, url);
          for (const link of more) {
            if (!visited.has(link) && !queued.has(link) && visited.size + queue.length < maxPages * 3) {
              queue.push(link);
              queued.add(link);
            }
          }
        }

        if (delayMs > 0) await page.waitForTimeout(delayMs);
      }

      const scrapedAt = new Date().toISOString();
      const siteIndex = {
        startUrl: values.url,
        discovery,
        sitemapSources: sitemapDiscovery.sitemapSources,
        pagesScraped: index.length,
        pages: index,
        scrapedAt,
      };
      await writeJson(values.out, 'site-index.json', siteIndex);
      await writeJson(values.out, 'crawl-index.json', siteIndex);
      await writeSiteReport(values.out, {
        startUrl: values.url,
        discovery,
        scrapedAt,
        pages: index,
      });
      await writeJson(values.out, 'manifest.json', {
        tool: 'geek-scrape',
        command: 'site',
        url: values.url,
        out: values.out,
        pagesScraped: index.length,
        scrapedAt,
      });

      const absOut = resolveOutDir(values.out);
      console.log('');
      console.log(`✓ Site scraped: ${index.length} pages from ${values.url}`);
      console.log(`  Folder: ${absOut}`);
      console.log('  Open: SITE-REPORT.md, site-index.json, pages/*/content.md');
      console.log('');
    } finally {
      await browser.close();
    }
  }

  const runners = { site: cmdSite, page: cmdPage, links: cmdLinks };
  const run = runners[command];
  if (!run) {
    console.error(`Unknown command: ${command}\n`);
    console.log(HELP);
    process.exit(1);
  }

  await run();
}

main().catch((err) => {
  console.error('geek-scrape failed:', err instanceof Error ? err.message : err);
  if (err instanceof Error && err.message?.includes('Executable doesn')) {
    console.error('\nRun once: npx playwright install chromium');
  }
  process.exit(1);
});
