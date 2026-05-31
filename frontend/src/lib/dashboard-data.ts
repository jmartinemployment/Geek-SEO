import {
  getDashboardOverview,
  type SeoContentDocument,
  type SeoProject,
} from '@/lib/seo-api';

export type ProjectSiteMetrics = {
  seoScore: number | null;
  siteHealthScore: number | null;
  latestAuditAt: string | null;
};

export type ProjectWithDocuments = SeoProject & {
  documents: SeoContentDocument[];
  metrics: ProjectSiteMetrics;
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

function mapOverviewToProjects(
  overview: Awaited<ReturnType<typeof getDashboardOverview>>,
): ProjectWithDocuments[] {
  return overview.projects.map((entry) => ({
    ...entry.project,
    documents: entry.documents,
    metrics: {
      seoScore: entry.latestAuditScore,
      siteHealthScore: entry.latestAuditScore,
      latestAuditAt: entry.latestAuditAt,
    },
  }));
}

export async function loadDashboardData(accessToken: string | null): Promise<DashboardData> {
  const overview = await getDashboardOverview(accessToken);
  const projects = mapOverviewToProjects(overview);

  const projectById = new Map(projects.map((p) => [p.id, p]));
  const recentDocuments = overview.recentDocuments.slice(0, 5).map((doc) => {
    const project = projectById.get(doc.projectId);
    return {
      ...doc,
      projectName: project?.name ?? 'Project',
      projectUrl: project?.url ?? '',
    };
  });

  return {
    projects,
    recentDocuments,
    copilotSuggestions: buildCopilotSuggestions(projects),
  };
}

export async function loadAllContentDocuments(
  accessToken: string | null,
): Promise<{ projects: ProjectWithDocuments[]; allDocuments: RecentDocument[] }> {
  const overview = await getDashboardOverview(accessToken);
  const projects = mapOverviewToProjects(overview);
  const allDocuments = projects
    .flatMap((project) =>
      project.documents.map((doc) => ({
        ...doc,
        projectName: project.name,
        projectUrl: project.url,
      })),
    )
    .toSorted((a, b) => (a.title || '').localeCompare(b.title || ''));

  return { projects, allDocuments };
}
