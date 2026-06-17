import {
  getDashboardOverview,
  getTopicalMap,
  type SeoContentDocument,
  type SeoProject,
  type TopicalMapResult,
  type TopicalMapTopic,
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

function topicalRecommendationDetail(projectName: string, topic: TopicalMapTopic): string {
  if (topic.coverage === 'gap') {
    return `${projectName} does not have a strong page for this topic yet — add it to your content plan.`;
  }
  if (topic.coverage === 'partial') {
    return `You have partial coverage — a dedicated article could capture more searches for this topic.`;
  }
  return `High-priority topic from your saved topical map.`;
}

export function buildTopicalMapCopilotSuggestions(
  project: SeoProject,
  map: TopicalMapResult | null,
  limit = 2,
): CopilotSuggestion[] {
  if (!map?.recommendations?.length) return [];

  return map.recommendations.slice(0, limit).map((topic, index) => ({
    id: `topical-rec-${project.id}-${index}-${topic.name}`,
    title: `Write next: ${topic.suggestedTitle ?? topic.name}`,
    detail: topicalRecommendationDetail(project.name, topic),
    href: `/app/strategy/topical-map?projectId=${encodeURIComponent(project.id)}`,
  }));
}

function buildUrlAnalyzerCopilotSuggestion(project: SeoProject): CopilotSuggestion {
  return {
    id: `url-analyzer-${project.id}`,
    title: `Research SERP for ${project.name}`,
    detail: 'Run keyword-level SERP research (PAA, PASF, competitor outlines) for your next article.',
    href: '/url-analyzer',
  };
}

function buildCopilotSuggestions(
  projects: ProjectWithDocuments[],
  nicheSuggestion: CopilotSuggestion | null,
  topicalSuggestions: CopilotSuggestion[],
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

  const merged = [
    ...(nicheSuggestion ? [nicheSuggestion] : []),
    ...topicalSuggestions,
    ...docSuggestions,
  ];

  return dedupeCopilotSuggestions(merged).slice(0, 4);
}

function dedupeCopilotSuggestions(suggestions: CopilotSuggestion[]): CopilotSuggestion[] {
  const seen = new Set<string>();
  return suggestions.filter((s) => {
    if (seen.has(s.id)) return false;
    seen.add(s.id);
    return true;
  });
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

async function loadPrimaryCopilotInputs(
  accessToken: string | null,
  projects: ProjectWithDocuments[],
): Promise<{ nicheSuggestion: CopilotSuggestion | null; topicalSuggestions: CopilotSuggestion[] }> {
  if (!accessToken || projects.length === 0) {
    return { nicheSuggestion: null, topicalSuggestions: [] };
  }

  const project = projects[0];

  try {
    const map = await getTopicalMap(project.id, accessToken).catch(() => null);
    const topicalSuggestions = buildTopicalMapCopilotSuggestions(project, map);
    const nicheSuggestion = buildUrlAnalyzerCopilotSuggestion(project);
    return { nicheSuggestion, topicalSuggestions };
  } catch {
    return { nicheSuggestion: null, topicalSuggestions: [] };
  }
}

export async function loadDashboardData(accessToken: string | null): Promise<DashboardData> {
  const overview = await getDashboardOverview(accessToken);
  const projects = mapOverviewToProjects(overview);
  const { nicheSuggestion, topicalSuggestions } = await loadPrimaryCopilotInputs(
    accessToken,
    projects,
  );

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
    copilotSuggestions: buildCopilotSuggestions(projects, nicheSuggestion, topicalSuggestions),
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
