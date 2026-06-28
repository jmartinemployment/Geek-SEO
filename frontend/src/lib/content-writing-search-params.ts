export const CONTENT_WRITING_DEFAULT_LOCATION = 'United States';

/** SA2 handoff query string — exactly three pointers plus optional in-app fields. */
export type ContentWritingSearchParams = {
  analysisRunId: string;
  keyword: string;
  siteProfile: string;
  title: string;
  location: string;
  documentId: string;
};

type SearchParamsReader = Pick<URLSearchParams, 'get' | 'has'>;

const LEGACY_HANDOFF_PARAMS = [
  'projectId',
  'urlResearchId',
  'siteProfile',
  'url_research_id',
] as const;

/** Params from dropped handoff contracts — presence means the link is invalid. */
export function rejectedLegacyHandoffParams(
  searchParams: SearchParamsReader,
): string[] {
  return LEGACY_HANDOFF_PARAMS.filter((name) => Boolean(searchParams.get(name)?.trim()));
}

/** Read Content Writing deep-link params from the page query string. */
export function parseContentWritingSearchParams(
  searchParams: SearchParamsReader,
): ContentWritingSearchParams {
  return {
    analysisRunId: searchParams.get('analysisRunId')?.trim() ?? '',
    keyword: searchParams.get('keyword')?.trim() ?? '',
    siteProfile: searchParams.get('site_profile')?.trim() ?? '',
    title: searchParams.get('title')?.trim() ?? '',
    location:
      searchParams.get('location')?.trim() || CONTENT_WRITING_DEFAULT_LOCATION,
    documentId: searchParams.get('documentId')?.trim() ?? '',
  };
}

export function buildContentWritingSearchParams(
  params: Partial<ContentWritingSearchParams>,
): URLSearchParams {
  const query = new URLSearchParams();

  if (params.analysisRunId) query.set('analysisRunId', params.analysisRunId);
  if (params.keyword) query.set('keyword', params.keyword);
  if (params.siteProfile) query.set('site_profile', params.siteProfile);
  if (params.title) query.set('title', params.title);
  if (
    params.location &&
    params.location !== CONTENT_WRITING_DEFAULT_LOCATION
  ) {
    query.set('location', params.location);
  }
  if (params.documentId) query.set('documentId', params.documentId);

  return query;
}

export function contentWritingPath(
  params: Partial<ContentWritingSearchParams>,
): string {
  const query = buildContentWritingSearchParams(params).toString();
  return query ? `/content-writing?${query}` : '/content-writing';
}

export function isCompleteContentWritingHandoff(
  params: ContentWritingSearchParams,
): boolean {
  return Boolean(
    params.analysisRunId && params.keyword.trim() && params.siteProfile,
  );
}

export function missingContentWritingHandoffFields(
  params: ContentWritingSearchParams,
): string[] {
  const missing: string[] = [];
  if (!params.analysisRunId) missing.push('analysis run');
  if (!params.keyword.trim()) missing.push('keyword');
  if (!params.siteProfile) missing.push('site profile');
  return missing;
}

export function defaultTitleForKeyword(
  keyword: string,
  currentTitle?: string | null,
): string {
  const trimmedKeyword = keyword.trim();
  if (!trimmedKeyword) return currentTitle?.trim() || 'New article';
  const trimmedTitle = currentTitle?.trim() ?? '';
  if (!trimmedTitle || trimmedTitle === 'New article') return trimmedKeyword;
  return trimmedTitle;
}
