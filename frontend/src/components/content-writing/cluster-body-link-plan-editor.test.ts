import { describe, expect, it } from 'vitest';
import { resolveBodyLinkStatus } from '@/components/content-writing/cluster-body-link-plan-editor';
import type { ContentLinkBodySlot, ContentSpokeSummary } from '@/lib/seo-api';

const spoke = (overrides: Partial<ContentSpokeSummary>): ContentSpokeSummary => ({
  id: 'spoke-1',
  title: 'Spoke title',
  publishSlug: 'spoke-slug',
  spokeSourcePhrase: 'source phrase',
  status: 'shell',
  wordCount: 0,
  ...overrides,
});

const bodySlot = (overrides: Partial<ContentLinkBodySlot>): ContentLinkBodySlot => ({
  insertAfterH2Hint: 'implementation',
  anchorText: 'guide phrase',
  priority: 1,
  ...overrides,
});

describe('resolveBodyLinkStatus', () => {
  it('returns none when no target is set', () => {
    expect(resolveBodyLinkStatus(bodySlot({ targetDocumentId: null, targetPath: null }), [])).toBe(
      'none',
    );
  });

  it('returns pending when target spoke exists but is not generated', () => {
    const spokes = [spoke({ id: 'spoke-1', publishSlug: 'guide-slug', status: 'shell' })];
    expect(
      resolveBodyLinkStatus(
        bodySlot({ targetDocumentId: 'spoke-1', targetPath: '/blog/guide-slug' }),
        spokes,
      ),
    ).toBe('pending');
  });

  it('returns linked when target spoke is generated', () => {
    const spokes = [spoke({ id: 'spoke-1', status: 'body_generated', wordCount: 900 })];
    expect(resolveBodyLinkStatus(bodySlot({ targetDocumentId: 'spoke-1' }), spokes)).toBe('linked');
  });
});
