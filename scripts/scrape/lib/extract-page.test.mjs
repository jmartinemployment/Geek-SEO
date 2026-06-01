import assert from 'node:assert/strict';
import { isSmokeTestUrl } from './guards.mjs';
import { toMarkdown } from './extract-page.mjs';

assert.equal(isSmokeTestUrl('https://example.com/'), true);
assert.equal(isSmokeTestUrl('https://seranking.com/'), false);

const md = toMarkdown(
  {
    title: 'Rank Tracker',
    url: 'https://seranking.com/position-tracking.html',
    meta: { description: 'Track rankings daily.' },
    headings: [{ level: 1, text: 'Rank Tracker' }],
    textPreview: 'short',
    textLength: 12,
    links: [
      { href: '/pricing.html', text: 'Pricing' },
      { href: 'https://google.com', text: 'Google' },
    ],
  },
  { fullText: 'Line one.\n\nLine two with more copy.' },
);

assert.match(md, /Track rankings daily./);
assert.match(md, /## Page copy/);
assert.match(md, /pricing\.html/);

console.log('extract-page tests ok');
