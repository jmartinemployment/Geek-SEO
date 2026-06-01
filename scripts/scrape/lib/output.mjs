import { access } from 'node:fs/promises';
import { mkdir, writeFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';

/**
 * @param {string} outDir
 */
export async function ensureOutDir(outDir) {
  await mkdir(join(outDir, 'screenshots'), { recursive: true });
  await mkdir(join(outDir, 'pages'), { recursive: true });
}

/**
 * @param {string} outDir
 * @param {string} name
 * @param {unknown} data
 */
export async function writeJson(outDir, name, data) {
  await writeFile(join(outDir, name), `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

/**
 * @param {string} outDir
 * @param {string} name
 * @param {string} text
 */
export async function writeText(outDir, name, text) {
  await writeFile(join(outDir, name), text, 'utf8');
}

/**
 * @param {string} filePath
 */
export async function fileExists(filePath) {
  try {
    await access(filePath);
    return true;
  } catch {
    return false;
  }
}

/**
 * @param {string} outDir
 * @param {{ url: string; title: string; scrapedAt: string; files: { name: string; description: string }[]; stats: Record<string, string | number> }} report
 */
export async function writeScrapeReport(outDir, report) {
  const abs = resolve(outDir);
  const lines = [
    '# geek-scrape report',
    '',
    `**URL:** ${report.url}`,
    `**Title:** ${report.title}`,
    `**Scraped:** ${report.scrapedAt}`,
    `**Folder:** \`${abs}\``,
    '',
    '## Files (open these)',
    '',
  ];
  for (const f of report.files) {
    lines.push(`- **${f.name}** — ${f.description}`);
  }
  lines.push('', '## Stats', '');
  for (const [k, v] of Object.entries(report.stats)) {
    lines.push(`- ${k}: ${v}`);
  }
  lines.push(
    '',
    '## Not the Example.com test folder',
    '',
    'If you see `docs/research/competitors/_test/example`, that is only a smoke test for example.com — not your competitor scrape.',
    '',
  );
  await writeText(outDir, 'SCRAPE-REPORT.md', lines.join('\n'));
}

/**
 * @param {string} outDir
 * @param {{ startUrl: string; scrapedAt: string; discovery?: string; pages: { url: string; dir: string; title: string }[] }} report
 */
export async function writeSiteReport(outDir, report) {
  const abs = resolve(outDir);
  const lines = [
    '# geek-scrape site report',
    '',
    `**Start URL:** ${report.startUrl}`,
    `**Pages scraped:** ${report.pages.length}`,
    `**Discovery:** ${report.discovery ?? 'sitemap'}`,
    `**Scraped:** ${report.scrapedAt}`,
    `**Folder:** \`${abs}\``,
    '',
    '> URLs come from `sitemap.xml` (and nested sitemaps / robots.txt). See `sitemap-urls.json` and `sitemap-sources.json`.',
    '',
    '> Each page is under `pages/<slug-N>/` with its own `content.md`, `page.json`, `full-text.txt`, and `screenshot.png`.',
    '',
    '## Pages',
    '',
    '| # | Title | URL | Folder |',
    '|---|-------|-----|--------|',
  ];

  report.pages.forEach((p, i) => {
    const title = (p.title ?? '').replace(/\|/g, '\\|').slice(0, 80);
    lines.push(`| ${i + 1} | ${title || '—'} | ${p.url} | \`${p.dir}/\` |`);
  });

  lines.push(
    '',
    '## Machine index',
    '',
    '- `site-index.json` — same list for scripts',
    '',
    '## One URL only',
    '',
    'Use the `page` command (e.g. `npm run scrape:seranking:page`), not `site`.',
    '',
  );

  await writeText(outDir, 'SITE-REPORT.md', lines.join('\n'));
}

/** @deprecated Use writeSiteReport */
export const writeCrawlReport = writeSiteReport;

export { resolve as resolveOutDir };
