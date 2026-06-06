import { describe, expect, it } from 'vitest';
import { buildTopicalMapCopilotSuggestions } from '@/lib/dashboard-data';
import type { SeoProject, TopicalMapResult } from '@/lib/seo-api';

const project = {
  id: 'proj-1',
  name: 'Geek At Your Spot',
  url: 'https://www.geekatyourspot.com',
} as SeoProject;

describe('buildTopicalMapCopilotSuggestions', () => {
  it('returns empty when map has no recommendations', () => {
    expect(buildTopicalMapCopilotSuggestions(project, null)).toEqual([]);
    expect(
      buildTopicalMapCopilotSuggestions(project, {
        projectId: project.id,
        generatedAt: '2026-01-01',
        topics: [],
        coveredCount: 0,
        gapCount: 0,
        partialCount: 0,
        recommendations: [],
      }),
    ).toEqual([]);
  });

  it('maps recommendations to copilot cards with topical map link', () => {
    const map: TopicalMapResult = {
      projectId: project.id,
      generatedAt: '2026-01-01',
      topics: [],
      coveredCount: 1,
      gapCount: 1,
      partialCount: 0,
      recommendations: [
        {
          name: 'managed-it',
          queries: [],
          coverage: 'gap',
          totalImpressions: 0,
          suggestedTitle: 'Managed IT Services in South Florida',
          priorityScore: 92,
        },
      ],
    };

    const result = buildTopicalMapCopilotSuggestions(project, map);
    expect(result).toHaveLength(1);
    expect(result[0].title).toContain('Managed IT Services');
    expect(result[0].href).toContain('projectId=proj-1');
    expect(result[0].detail).toContain('Geek At Your Spot');
  });
});
