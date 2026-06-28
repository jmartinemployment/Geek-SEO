import { describe, expect, it } from 'vitest';
import {
  buildContentWritingSearchParams,
  contentWritingPath,
  defaultTitleForKeyword,
  isCompleteContentWritingHandoff,
  missingContentWritingHandoffFields,
  parseContentWritingSearchParams,
  rejectedLegacyHandoffParams,
} from './content-writing-search-params';

describe('parseContentWritingSearchParams', () => {
  it('reads SA2 handoff params only', () => {
    const params = new URLSearchParams(
      'analysisRunId=40695b16-f5d4-4fc0-a05d-c85df54236ca&keyword=how+you+implement+AI+Content+Marketing&site_profile=sp-uuid',
    );

    expect(parseContentWritingSearchParams(params)).toEqual({
      analysisRunId: '40695b16-f5d4-4fc0-a05d-c85df54236ca',
      keyword: 'how you implement AI Content Marketing',
      siteProfile: 'sp-uuid',
      title: '',
      location: 'United States',
      documentId: '',
    });
  });

  it('does not read dropped projectId or urlResearchId', () => {
    const params = new URLSearchParams(
      'projectId=abc&urlResearchId=legacy-run-id&analysisRunId=run-1&keyword=test',
    );

    expect(parseContentWritingSearchParams(params).analysisRunId).toBe('run-1');
    expect(rejectedLegacyHandoffParams(params)).toEqual(['projectId', 'urlResearchId']);
  });
});

describe('rejectedLegacyHandoffParams', () => {
  it('flags camelCase siteProfile', () => {
    const params = new URLSearchParams('siteProfile=bad&site_profile=good');
    expect(rejectedLegacyHandoffParams(params)).toContain('siteProfile');
  });
});

describe('contentWritingPath', () => {
  it('builds SA2 handoff deep link', () => {
    expect(
      contentWritingPath({
        analysisRunId: '40695b16-f5d4-4fc0-a05d-c85df54236ca',
        keyword: 'how you implement AI Content Marketing',
        siteProfile: 'sp-uuid',
      }),
    ).toBe(
      '/content-writing?analysisRunId=40695b16-f5d4-4fc0-a05d-c85df54236ca&keyword=how+you+implement+AI+Content+Marketing&site_profile=sp-uuid',
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
        analysisRunId: 'run-1',
        location: 'United States',
      }).toString(),
    ).toBe('analysisRunId=run-1');
  });
});

describe('isCompleteContentWritingHandoff', () => {
  it('requires run, keyword, and site profile', () => {
    expect(
      isCompleteContentWritingHandoff({
        analysisRunId: 'r',
        keyword: 'kw',
        siteProfile: 'sp',
        title: '',
        location: 'United States',
        documentId: '',
      }),
    ).toBe(true);

    expect(
      isCompleteContentWritingHandoff({
        analysisRunId: 'r',
        keyword: 'kw',
        siteProfile: '',
        title: '',
        location: 'United States',
        documentId: '',
      }),
    ).toBe(false);
  });
});

describe('missingContentWritingHandoffFields', () => {
  it('lists absent handoff fields', () => {
    expect(
      missingContentWritingHandoffFields({
        analysisRunId: '',
        keyword: '',
        siteProfile: '',
        title: '',
        location: 'United States',
        documentId: '',
      }),
    ).toEqual(['analysis run', 'keyword', 'site profile']);
  });
});
