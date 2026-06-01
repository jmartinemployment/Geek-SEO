import assert from 'node:assert/strict';
import {
  isSitemapIndex,
  parseRobotsSitemaps,
  parseSitemapLocs,
} from './sitemap.mjs';

const urlset = `<?xml version="1.0"?>
<urlset>
  <url><loc>https://seranking.com/</loc></url>
  <url><loc>https://seranking.com/pricing.html</loc></url>
</urlset>`;

const index = `<?xml version="1.0"?>
<sitemapindex>
  <sitemap><loc>https://seranking.com/sitemap-pages.xml</loc></sitemap>
</sitemapindex>`;

assert.equal(parseSitemapLocs(urlset).length, 2);
assert.equal(isSitemapIndex(urlset), false);
assert.equal(isSitemapIndex(index), true);

const robots = `User-agent: *\nSitemap: https://seranking.com/sitemap.xml\n`;
assert.deepEqual(parseRobotsSitemaps(robots, 'https://seranking.com'), [
  'https://seranking.com/sitemap.xml',
]);

console.log('sitemap tests ok');
