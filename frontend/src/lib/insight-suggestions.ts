import type { ScoreSuggestion } from '@/hooks/useContentScoring';

/** Always surface quick deterministic SEO fixes even when GEO suggestions rank higher. */
const PINNED_INSIGHT_IDS = ['meta_description', 'title_keyword'] as const;

export function selectVisibleInsights(
  suggestions: ScoreSuggestion[],
  maxTotal = 6,
): ScoreSuggestion[] {
  const sorted = [...suggestions].sort((a, b) => b.pointValue - a.pointValue);
  const pinned = PINNED_INSIGHT_IDS.flatMap((id) => {
    const match = sorted.find((s) => s.id === id);
    return match ? [match] : [];
  });
  const pinnedIds = new Set(pinned.map((s) => s.id));
  const rest = sorted.filter((s) => !pinnedIds.has(s.id));
  const slotsForRest = Math.max(0, maxTotal - pinned.length);
  return [...pinned, ...rest.slice(0, slotsForRest)];
}
