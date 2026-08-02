import { describe, expect, it } from 'vitest';
import {
  compareDecayingPagesByPillarPriority,
  matchUrlToSiteAnalysisPillar,
  normalizePagePath,
  pillarCoverageLabel,
} from '@/lib/site-url-match';
import type { SiteAnalysisPillarResult } from '@/lib/seo-api';

const pillars: SiteAnalysisPillarResult[] = [
  {
    id: '1',
    pillarTopic: 'Managed IT',
    pillarSlug: 'managed-it',
    primaryKeyword: 'managed it services',
    pageUrl: 'https://www.example.com/managed-it',
    searchIntent: 'commercial',
    searchVolume: 100,
    keywordDifficulty: 40,
    coverageStatus: 'covered',
    coverageScore: 80,
    existingPageCount: 1,
    requiredSubtopicCount: 3,
    coveredSubtopicCount: 2,
    strategicPriority: 'must_have',
    source: 'schema',
    displayOrder: 0,
    subtopics: [
      {
        id: 's1',
        subtopicTitle: 'Backup',
        targetKeyword: 'it backup',
        searchIntent: 'informational',
        searchVolume: 10,
        keywordDifficulty: 20,
        coverageStatus: 'covered',
        existingUrl: 'https://www.example.com/managed-it/backup',
        recommendedFormat: 'guide',
        recommendedWordCount: 1200,
        fixEffort: 'low',
        isQuickWin: true,
      },
    ],
    paaQuestions: [],
    relatedSearches: [],
    localPaaQuestions: [],
    localRelatedSearches: [],
  },
  {
    id: '2',
    pillarTopic: 'Cloud Migration',
    pillarSlug: 'cloud-migration',
    primaryKeyword: 'cloud migration',
    pageUrl: 'https://www.example.com/cloud-migration',
    searchIntent: 'commercial',
    searchVolume: 50,
    keywordDifficulty: 55,
    coverageStatus: 'gap',
    coverageScore: 10,
    existingPageCount: 0,
    requiredSubtopicCount: 2,
    coveredSubtopicCount: 0,
    strategicPriority: 'high_value',
    source: 'nav',
    displayOrder: 1,
    subtopics: [],
    paaQuestions: [],
    relatedSearches: [],
    localPaaQuestions: [],
    localRelatedSearches: [],
  },
];

describe('normalizePagePath', () => {
  it('strips host, query, and trailing slash', () => {
    expect(normalizePagePath('https://WWW.Example.com/managed-it/')).toBe('/managed-it');
  });
});

describe('matchUrlToSiteAnalysisPillar', () => {
  it('matches dedicated pillar page', () => {
    const match = matchUrlToSiteAnalysisPillar('https://example.com/managed-it', pillars);
    expect(match?.pillarTopic).toBe('Managed IT');
    expect(match?.matchKind).toBe('pillar_page');
  });

  it('matches subtopic existing URL', () => {
    const match = matchUrlToSiteAnalysisPillar('https://example.com/managed-it/backup', pillars);
    expect(match?.matchKind).toBe('subtopic_page');
  });

  it('returns null when no pillar matches', () => {
    expect(matchUrlToSiteAnalysisPillar('https://example.com/contact', pillars)).toBeNull();
  });
});

describe('pillarCoverageLabel', () => {
  it('uses plain language for gap status', () => {
    expect(pillarCoverageLabel('gap')).toContain('No strong page');
  });
});

describe('compareDecayingPagesByPillarPriority', () => {
  it('ranks gap pillars before covered pillars', () => {
    const gapFirst = compareDecayingPagesByPillarPriority(
      'https://example.com/cloud-migration',
      'https://example.com/managed-it',
      pillars,
    );
    expect(gapFirst).toBeLessThan(0);
  });
});
