import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  MANUAL_RESEARCH_LANE_ORDER,
  validateManualLaneFileContent,
} from './manual-research-lanes';

const repoRoot = join(process.cwd(), '..');

describe('manual-research-lanes wiki validation', () => {
  it('includes wiki as last supplemental lane', () => {
    expect(MANUAL_RESEARCH_LANE_ORDER).toContain('wiki');
    expect(MANUAL_RESEARCH_LANE_ORDER.at(-1)).toBe('wiki');
  });

  it('wiki preflight passes on saved wikipedia Google SERP fixture', () => {
    const fixture = readFileSync(
      join(
        repoRoot,
        'research',
        'customer-journey',
        'wiki',
        'wikipedia ai customer journey - Google Search.html',
      ),
      'utf8',
    );
    expect(validateManualLaneFileContent('wiki', fixture, 'wiki-serp.html')).toBeNull();
  });

  it('wiki preflight rejects keyword SERP with no wikipedia.org hosts', () => {
    const fixture = readFileSync(
      join(
        repoRoot,
        'GeekSiteAnalyzer.Tests',
        'fixtures',
        'serp',
        'ai marketing Customer Journeys - Google Search.html',
      ),
      'utf8',
    );
    expect(validateManualLaneFileContent('wiki', fixture, 'keyword-serp.html')).toMatch(
      /No wikipedia\.org URLs/,
    );
  });

  it('wiki preflight rejects .wiki TLD junk', () => {
    const html = '<a href="https://aisdr.wiki/foo">bad</a>';
    expect(validateManualLaneFileContent('wiki', html, 'site_wiki.html')).toMatch(/Wrong wiki SERP/);
  });
});
