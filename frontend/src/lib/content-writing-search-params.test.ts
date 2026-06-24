import { describe, expect, it } from 'vitest';
import {
  buildContentWritingSearchParams,
  contentWritingPath,
  defaultTitleForKeyword,
  parseContentWritingSearchParams,
} from './content-writing-search-params';

describe('parseContentWritingSearchParams', () => {
  it('reads projectId, analysisRunId, and keyword from the query string', () => {
    const params = new URLSearchParams(
      'projectId=d3012e49-2f5d-4b3d-a235-fc7c4b56bcd0&analysisRunId=40695b16-f5d4-4fc0-a05d-c85df54236ca&keyword=how+you+implement+AI+Content+Marketing',
    );

    expect(parseContentWritingSearchParams(params)).toEqual({
      projectId: 'd3012e49-2f5d-4b3d-a235-fc7c4b56bcd0',
      analysisRunId: '40695b16-f5d4-4fc0-a05d-c85df54236ca',
      keyword: 'how you implement AI Content Marketing',
      title: '',
      location: 'United States',
      documentId: '',
      siteProfile: '',
    });
  });

  it('reads site_profile from the query string', () => {
    const params = new URLSearchParams(
      'projectId=abc&site_profile=sp-uuid&analysisRunId=run-1',
    );

    expect(parseContentWritingSearchParams(params).siteProfile).toBe('sp-uuid');
  });

  it('falls back from legacy urlResearchId to analysisRunId', () => {
    const params = new URLSearchParams(
      'projectId=abc&urlResearchId=legacy-run-id&keyword=test',
    );

    expect(parseContentWritingSearchParams(params).analysisRunId).toBe('legacy-run-id');
  });
});

describe('contentWritingPath', () => {
  it('builds a shareable deep link', () => {
    expect(
      contentWritingPath({
        projectId: 'd3012e49-2f5d-4b3d-a235-fc7c4b56bcd0',
        analysisRunId: '40695b16-f5d4-4fc0-a05d-c85df54236ca',
        keyword: 'how you implement AI Content Marketing',
      }),
    ).toBe(
      '/content-writing?projectId=d3012e49-2f5d-4b3d-a235-fc7c4b56bcd0&analysisRunId=40695b16-f5d4-4fc0-a05d-c85df54236ca&keyword=how+you+implement+AI+Content+Marketing',
    );
  });
});

describe('defaultTitleForKeyword', () => {
  it('replaces the placeholder title with the keyword', () => {
    expect(defaultTitleForKeyword('AI Content Marketing', 'New article')).toBe(
      'AI Content Marketing',
    );
  });
});

describe('buildContentWritingSearchParams', () => {
  it('omits default location from the query string', () => {
    expect(
      buildContentWritingSearchParams({
        projectId: 'abc',
        location: 'United States',
      }).toString(),
    ).toBe('projectId=abc');
  });
});
