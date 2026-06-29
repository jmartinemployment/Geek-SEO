import { describe, expect, it } from 'vitest';
import { resolveFaqLinkStatus } from '@/components/content-writing/cluster-faq-plan-editor';
import type { ContentLinkFaqItem, ContentSpokeSummary } from '@/lib/seo-api';

const spoke = (overrides: Partial<ContentSpokeSummary>): ContentSpokeSummary => ({
  id: 'spoke-1',
  title: 'Spoke title',
  publishSlug: 'spoke-slug',
  spokeSourcePhrase: 'source phrase',
  status: 'shell',
  wordCount: 0,
  ...overrides,
});

const faqItem = (overrides: Partial<ContentLinkFaqItem>): ContentLinkFaqItem => ({
  question: 'Question?',
  source: 'paa',
  targetDocumentId: null,
  targetPath: null,
  anchorText: null,
  ...overrides,
});

describe('resolveFaqLinkStatus', () => {
  it('returns none when no target is set', () => {
    expect(resolveFaqLinkStatus(faqItem({}), [])).toBe('none');
  });

  it('returns pending when target spoke exists but is not generated', () => {
    const spokes = [spoke({ id: 'spoke-1', status: 'shell', wordCount: 0 })];
    expect(
      resolveFaqLinkStatus(
        faqItem({ targetDocumentId: 'spoke-1', targetPath: '/blog/spoke-slug' }),
        spokes,
      ),
    ).toBe('pending');
  });

  it('returns linked when target spoke is body_generated', () => {
    const spokes = [spoke({ id: 'spoke-1', status: 'body_generated', wordCount: 1200 })];
    expect(resolveFaqLinkStatus(faqItem({ targetDocumentId: 'spoke-1' }), spokes)).toBe('linked');
  });

  it('returns linked when target spoke has substantial word count', () => {
    const spokes = [spoke({ id: 'spoke-1', status: 'shell', wordCount: 500 })];
    expect(resolveFaqLinkStatus(faqItem({ targetDocumentId: 'spoke-1' }), spokes)).toBe('linked');
  });

  it('matches spoke by target path slug when document id is absent', () => {
    const spokes = [spoke({ publishSlug: 'my-slug', status: 'body_generated' })];
    expect(
      resolveFaqLinkStatus(faqItem({ targetPath: '/blog/my-slug' }), spokes),
    ).toBe('linked');
  });
});
