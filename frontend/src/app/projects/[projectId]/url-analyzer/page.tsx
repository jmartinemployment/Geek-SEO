import { redirect } from 'next/navigation';

type Props = {
  params: Promise<{ projectId: string }>;
};

export default async function ProjectUrlAnalyzerRedirectPage({ params }: Props) {
  const { projectId } = await params;
  redirect(`/projects/${encodeURIComponent(projectId)}`);
}
