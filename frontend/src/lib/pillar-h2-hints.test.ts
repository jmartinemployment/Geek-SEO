import { describe, expect, it } from 'vitest';
import { extractPillarH2Hints } from '@/lib/pillar-h2-hints';

describe('extractPillarH2Hints', () => {
  it('returns h2 ids before the FAQ section', () => {
    const hints = extractPillarH2Hints(`
      <h2 id="implementation">Implementation approach</h2>
      <p>Body copy.</p>
      <h2>Vendor selection</h2>
      <h2>Frequently Asked Questions</h2>
    `);

    expect(hints).toEqual(['implementation', 'Vendor selection']);
  });
});
