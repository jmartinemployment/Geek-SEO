import { describe, expect, it } from 'vitest';
import {
  draftHtmlFromTitle,
  extractH1Text,
  normalizeDraftHtml,
  titleFromHtml,
} from '@/lib/content-html';

describe('content-html', () => {
  it('builds draft HTML with bold-ready h1 from title', () => {
    const html = draftHtmlFromTitle('Zapier QuickBooks Guide');
    expect(html).toContain('<h1>Zapier QuickBooks Guide</h1>');
    expect(extractH1Text(html)).toBe('Zapier QuickBooks Guide');
  });

  it('replaces placeholder h1 with working title', () => {
    const html = normalizeDraftHtml(
      '<h1>Article title</h1><p>Body</p>',
      'Real headline',
    );
    expect(extractH1Text(html)).toBe('Real headline');
  });

  it('prefers h1 text for document title metadata', () => {
    expect(
      titleFromHtml('<h1>SEO headline</h1><p>x</p>', 'Ignored field title'),
    ).toBe('SEO headline');
  });
});
