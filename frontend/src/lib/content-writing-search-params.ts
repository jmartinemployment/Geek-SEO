export const CONTENT_WRITING_DEFAULT_LOCATION = 'United States';

export type ContentWritingSearchParams = {
  projectId: string;
  analysisRunId: string;
  keyword: string;
  title: string;
  location: string;
  documentId: string;
  siteProfile: string;
};

type SearchParamsReader = Pick<URLSearchParams, 'get'>;

/** Read Content Writing deep-link params from the page query string. */
export function parseContentWritingSearchParams(
  searchParams: SearchParamsReader,
): ContentWritingSearchParams {
  const projectId = searchParams.get('projectId')?.trim() ?? '';
  const analysisRunId =
    searchParams.get('analysisRunId')?.trim() ??
    searchParams.get('urlResearchId')?.trim() ??
    '';
  const keyword = searchParams.get('keyword')?.trim() ?? '';
  const title = searchParams.get('title')?.trim() ?? '';
  const location =
    searchParams.get('location')?.trim() || CONTENT_WRITING_DEFAULT_LOCATION;
  const documentId = searchParams.get('documentId')?.trim() ?? '';
  const siteProfile =
    searchParams.get('site_profile')?.trim() ??
    searchParams.get('siteProfile')?.trim() ??
    '';

  return { projectId, analysisRunId, keyword, title, location, documentId, siteProfile };
}

export function buildContentWritingSearchParams(
  params: Partial<ContentWritingSearchParams>,
): URLSearchParams {
  const query = new URLSearchParams();

  if (params.projectId) query.set('projectId', params.projectId);
  if (params.analysisRunId) query.set('analysisRunId', params.analysisRunId);
  if (params.keyword) query.set('keyword', params.keyword);
  if (params.title) query.set('title', params.title);
  if (
    params.location &&
    params.location !== CONTENT_WRITING_DEFAULT_LOCATION
  ) {
    query.set('location', params.location);
  }
  if (params.documentId) query.set('documentId', params.documentId);
  if (params.siteProfile) query.set('site_profile', params.siteProfile);

  return query;
}

export function contentWritingPath(
  params: Partial<ContentWritingSearchParams>,
): string {
  const query = buildContentWritingSearchParams(params).toString();
  return query ? `/content-writing?${query}` : '/content-writing';
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
