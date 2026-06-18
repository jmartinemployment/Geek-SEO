import { describe, expect, it } from 'vitest';
import { formatRenderedArticleForClipboard } from '@/lib/seo-api';

describe('formatRenderedArticleForClipboard', () => {
  it('appends schema scripts when renderedHtml omits JSON-LD', () => {
    const result = formatRenderedArticleForClipboard({
      bodyHtml: '<h1>Title</h1><p>Body</p>',
      renderedHtml: '<h1>Title</h1><p>Body</p>',
      schemaScripts: [
        '<script type="application/ld+json">{"@type":"TechArticle"}</script>',
        '<script type="application/ld+json">{"@type":"FAQPage"}</script>',
      ],
      schemaTypes: ['TechArticle', 'FAQPage'],
    });

    expect(result).toContain('<p>Body</p>');
    expect(result).toContain('application/ld+json');
    expect(result).toContain('TechArticle');
    expect(result).toContain('FAQPage');
  });

  it('returns renderedHtml when it already contains JSON-LD', () => {
    const rendered =
      '<h1>Title</h1>\n<script type="application/ld+json">{"@type":"TechArticle"}</script>';
    const result = formatRenderedArticleForClipboard({
      bodyHtml: '<h1>Title</h1>',
      renderedHtml: rendered,
      schemaScripts: ['<script type="application/ld+json">{"@type":"TechArticle"}</script>'],
      schemaTypes: ['TechArticle'],
    });

    expect(result).toBe(rendered);
  });

  it('returns body only when there are no schema scripts', () => {
    const body = '<h1>Title</h1><p>Body</p>';
    const result = formatRenderedArticleForClipboard({
      bodyHtml: body,
      renderedHtml: body,
      schemaScripts: [],
      schemaTypes: [],
    });

    expect(result).toBe(body);
    expect(result).not.toContain('application/ld+json');
  });
});
