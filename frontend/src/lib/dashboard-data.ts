import {
  getDashboardOverview,
  getLatestNicheProfile,
  type NicheProfileResult,
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

function buildNicheCopilotSuggestion(
  project: SeoProject,
  profile: NicheProfileResult | null,
): CopilotSuggestion | null {
  if (!profile || profile.status !== 'complete') {
    return {
      id: `niche-run-${project.id}`,
      title: `Map niche pillars for ${project.name}`,
      detail: 'Run the niche analyzer to discover coverage gaps before planning new content.',
      href: '/app/strategy/niche-analyzer',
    };
  }

  if (profile.pillarsGap <= 0) return null;

  return {
    id: `niche-gaps-${project.id}`,
    title: `${profile.pillarsGap} pillar gap${profile.pillarsGap === 1 ? '' : 's'} on ${project.name}`,
    detail: `${profile.primaryNiche} — build a topical map from your latest fusion snapshot.`,
    href: `/app/strategy/topical-map?projectId=${encodeURIComponent(project.id)}&mode=niche&autogen=1`,
  };
}

function buildCopilotSuggestions(
  projects: ProjectWithDocuments[],
  nicheSuggestion: CopilotSuggestion | null,
): CopilotSuggestion[] {
  const lowScoreDocs = projects
    .flatMap((project) =>
      project.documents.map((doc) => ({
        ...doc,
        projectName: project.name,
      })),
    )
    .filter((doc) => doc.seoScore > 0 && doc.seoScore < 70)
    .slice(0, 3);

  const docSuggestions =
    lowScoreDocs.length === 0
      ? projects.length === 0
        ? [
            {
              id: 'welcome',
              title: 'Add your first site',
              detail: 'Create a project to unlock topical maps, audits, and content scoring.',
              href: '/app/projects',
            },
          ]
        : []
      : lowScoreDocs.map((doc) => ({
          id: doc.id,
          title: `"${doc.title || 'Untitled'}" scores ${doc.seoScore}%`,
          detail: `Improve structure and topic coverage for "${doc.targetKeyword || 'your target keyword'}".`,
          href: `/app/content/${doc.id}`,
        }));

  const merged = nicheSuggestion ? [nicheSuggestion, ...docSuggestions] : docSuggestions;
  return merged.slice(0, 4);
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

async function loadPrimaryNicheSuggestion(
  accessToken: string | null,
  projects: ProjectWithDocuments[],
): Promise<CopilotSuggestion | null> {
  if (!accessToken || projects.length === 0) return null;

  try {
    const profile = await getLatestNicheProfile(projects[0].id, accessToken);
    return buildNicheCopilotSuggestion(projects[0], profile);
  } catch {
    return null;
  }
}

export async function loadDashboardData(accessToken: string | null): Promise<DashboardData> {
  const overview = await getDashboardOverview(accessToken);
  const projects = mapOverviewToProjects(overview);
  const nicheSuggestion = await loadPrimaryNicheSuggestion(accessToken, projects);

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
    copilotSuggestions: buildCopilotSuggestions(projects, nicheSuggestion),
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
