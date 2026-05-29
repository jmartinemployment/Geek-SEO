import { listContent, listProjects, type SeoContentDocument, type SeoProject } from '@/lib/seo-api';

export type ProjectWithDocuments = SeoProject & {
  documents: SeoContentDocument[];
};

export type RecentDocument = SeoContentDocument & {
  projectName: string;
  projectUrl: string;
};

export type CopilotSuggestion = {
  id: string;
  title: string;
  detail: string;
  href: string;
};

export type DashboardData = {
  projects: ProjectWithDocuments[];
  recentDocuments: RecentDocument[];
  copilotSuggestions: CopilotSuggestion[];
};

function buildCopilotSuggestions(projects: ProjectWithDocuments[]): CopilotSuggestion[] {
  const lowScoreDocs = projects
    .flatMap((project) =>
      project.documents.map((doc) => ({
        ...doc,
        projectName: project.name,
      })),
    )
    .filter((doc) => doc.seoScore > 0 && doc.seoScore < 70)
    .slice(0, 3);

  if (lowScoreDocs.length === 0) {
    return [
      {
        id: 'welcome',
        title: 'Add your first site',
        detail: 'Create a project to unlock topical maps, audits, and content scoring.',
        href: '/app/projects',
      },
    ];
  }

  return lowScoreDocs.map((doc) => ({
    id: doc.id,
    title: `"${doc.title || 'Untitled'}" scores ${doc.seoScore}%`,
    detail: `Improve structure and topic coverage for "${doc.targetKeyword || 'your target keyword'}".`,
    href: `/app/content/${doc.id}`,
  }));
}

export async function loadDashboardData(accessToken: string | null): Promise<DashboardData> {
  const projects = await listProjects(accessToken);
  const projectsWithDocs = await Promise.all(
    projects.map(async (project) => ({
      ...project,
      documents: await listContent(project.id, accessToken),
    })),
  );

  const recentDocuments = projectsWithDocs
    .flatMap((project) =>
      project.documents.map((doc) => ({
        ...doc,
        projectName: project.name,
        projectUrl: project.url,
      })),
    )
    .slice(0, 5);

  return {
    projects: projectsWithDocs,
    recentDocuments,
    copilotSuggestions: buildCopilotSuggestions(projectsWithDocs),
  };
}
