import { describe, expect, it } from 'vitest';
import { selectVisibleInsights } from '@/lib/insight-suggestions';
import type { ScoreSuggestion } from '@/hooks/useContentScoring';

function suggestion(id: string, pointValue: number): ScoreSuggestion {
  return {
    id,
    component: id,
    pointValue,
    actionText: id,
    proposedChange: id,
    applyMode: 'deterministic',
  };
}

describe('selectVisibleInsights', () => {
  it('pins meta_description even when lower ranked than GEO suggestions', () => {
    const visible = selectVisibleInsights([
      suggestion('geo_authority', 18),
      suggestion('geo_readability', 16),
      suggestion('word_count', 15),
      suggestion('term_coverage', 14),
      suggestion('geo_citations', 12),
      suggestion('meta_description', 8),
    ]);

    expect(visible.some((s) => s.id === 'meta_description')).toBe(true);
    expect(visible[0]?.id).toBe('meta_description');
  });

  it('returns top suggestions when nothing is pinned', () => {
    const visible = selectVisibleInsights([
      suggestion('geo_authority', 18),
      suggestion('word_count', 15),
      suggestion('term_coverage', 14),
    ]);

    expect(visible).toHaveLength(3);
    expect(visible[0]?.id).toBe('geo_authority');
  });
});
