import { redirect } from 'next/navigation';

type SearchParams = Record<string, string | string[] | undefined>;

function buildQueryString(searchParams: SearchParams): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === 'string') query.set(key, value);
    else if (Array.isArray(value)) value.forEach((v) => query.set(key, v));
  }
  const qs = query.toString();
  return qs ? `?${qs}` : '';
}

type Props = {
  searchParams: Promise<SearchParams>;
};

export default async function UrlAnalyzerRedirectPage({ searchParams }: Props) {
  const params = await searchParams;
  redirect(`/site-analyzer${buildQueryString(params)}`);
}
