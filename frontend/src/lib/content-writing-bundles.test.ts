import { describe, expect, it } from 'vitest';
import {
  parseContentWriterKeywordBundle,
  parseSiteWritingFocus,
  type ContentWriterSerpExport,
} from '@/lib/seo-api';

describe('parseContentWriterKeywordBundle', () => {
  const bundle: ContentWriterSerpExport = {
    runId: '7a9c36d8-0ecf-4c36-9387-5d02c28de201',
    projectId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    keyword: 'widget repair',
    targetSiteUrl: 'https://example.com',
    status: 'completed',
    serpSeResultsCount: 1_250_000,
    serp: [
      {
        position: 1,
        type: 'organic',
        title: 'Widget Repair',
        url: 'https://c1.com',
        domain: 'c1.com',
      },
    ],
  };

  it('parses frozen keyword bundle JSON', () => {
    const parsed = parseContentWriterKeywordBundle(JSON.stringify(bundle));
    expect(parsed?.keyword).toBe('widget repair');
    expect(parsed?.serp).toHaveLength(1);
  });

  it('returns null for empty or invalid JSON', () => {
    expect(parseContentWriterKeywordBundle(null)).toBeNull();
    expect(parseContentWriterKeywordBundle('')).toBeNull();
    expect(parseContentWriterKeywordBundle('{not json')).toBeNull();
  });
});

describe('parseSiteWritingFocus', () => {
  it('parses site focus snapshot JSON', () => {
    const focus = parseSiteWritingFocus(
      JSON.stringify({ siteName: 'Example', siteUrl: 'https://example.com' }),
    );
    expect(focus?.siteName).toBe('Example');
  });
});
