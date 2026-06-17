import { redirect } from 'next/navigation';

type NewBriefPageProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function NewBriefPage({ searchParams }: NewBriefPageProps) {
  const params = (await searchParams) ?? {};
  const query = new URLSearchParams();
  const keyword = Array.isArray(params.keyword) ? params.keyword[0] : params.keyword;
  const projectId = Array.isArray(params.projectId) ? params.projectId[0] : params.projectId;
  const location = Array.isArray(params.location) ? params.location[0] : params.location;

  if (keyword) query.set('keyword', keyword);
  if (projectId) query.set('projectId', projectId);
  if (location) query.set('location', location);

  redirect(`/content-writing${query.size > 0 ? `?${query.toString()}` : ''}`);
}
