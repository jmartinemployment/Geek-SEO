#!/usr/bin/env node
/**
 * Extract DOM structure, computed styles, and screenshot for a page region.
 * Use when browser MCP is unavailable; feed output into deconstruct-web-feature spec.
 *
 * Usage:
 *   node scripts/deconstruct/extract-feature.mjs \
 *     --url "https://example.com/dashboard" \
 *     --selector "main" \
 *     --out docs/research/features/example-dashboard
 *
 * Requires: playwright (npx playwright install chromium)
 */

import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { parseArgs } from 'node:util';

const { values } = parseArgs({
  options: {
    url: { type: 'string' },
    selector: { type: 'string', default: 'main' },
    out: { type: 'string' },
    viewport: { type: 'string', default: '1440x900' },
  },
});

if (!values.url || !values.out) {
  console.error('Required: --url and --out');
  process.exit(1);
}

const [width, height] = values.viewport.split('x').map(Number);

async function main() {
  const { chromium } = await import('playwright');
  const outDir = values.out;
  await mkdir(join(outDir, 'screenshots'), { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width, height } });

  const requests = [];
  page.on('request', (req) => {
    if (req.resourceType() === 'xhr' || req.resourceType() === 'fetch') {
      requests.push({
        method: req.method(),
        url: req.url(),
      });
    }
  });

  await page.goto(values.url, { waitUntil: 'networkidle', timeout: 60_000 });

  const extraction = await page.evaluate((sel) => {
    const root = document.querySelector(sel) ?? document.body;
    const cs = getComputedStyle(root);
    const vars = {};
    for (const name of Array.from(document.styleSheets).flatMap((sheet) => {
      try {
        return Array.from(sheet.cssRules).map((r) => r.cssText);
      } catch {
        return [];
      }
    })) {
      if (name.includes('--')) {
        const m = name.match(/--[\w-]+/g);
        if (m) for (const v of m) vars[v] = getComputedStyle(document.documentElement).getPropertyValue(v);
      }
    }

    function summarize(el, depth) {
      if (depth > 3) return null;
      const style = getComputedStyle(el);
      return {
        tag: el.tagName.toLowerCase(),
        id: el.id || undefined,
        className: el.className?.toString?.().slice(0, 120) || undefined,
        role: el.getAttribute('role') || undefined,
        textPreview: (el.childNodes.length === 1 && el.childNodes[0].nodeType === 3)
          ? el.textContent?.trim().slice(0, 80)
          : undefined,
        rect: el.getBoundingClientRect().toJSON(),
        display: style.display,
        gridTemplateColumns: style.gridTemplateColumns,
        flexDirection: style.flexDirection,
        gap: style.gap,
        children: Array.from(el.children)
          .slice(0, 12)
          .map((c) => summarize(c, depth + 1))
          .filter(Boolean),
      };
    }

    return {
      selector: sel,
      found: !!document.querySelector(sel),
      framework: {
        nextData: !!document.getElementById('__NEXT_DATA__'),
        reactRoot: !!document.querySelector('[data-reactroot], #root, #__next'),
      },
      root: summarize(root, 0),
      cssVarsSample: Object.fromEntries(Object.entries(vars).slice(0, 40)),
      title: document.title,
      fonts: Array.from(document.fonts || []).map((f) => f.family).slice(0, 10),
    };
  }, values.selector);

  await page.screenshot({
    path: join(outDir, 'screenshots', 'desktop.png'),
    fullPage: false,
  });

  await writeFile(join(outDir, 'dom-summary.json'), JSON.stringify(extraction, null, 2));
  await writeFile(
    join(outDir, 'network-requests.json'),
    JSON.stringify(requests.slice(0, 200), null, 2),
  );

  await browser.close();

  console.log(`Wrote ${outDir}/dom-summary.json`);
  console.log(`Wrote ${outDir}/screenshots/desktop.png`);
  console.log(`Wrote ${outDir}/network-requests.json (${requests.length} xhr/fetch)`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
