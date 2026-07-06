import {
  getDashboardOverview,
  getTopicalMap,
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
  metrics: ProjectSiteMetrics;
};

export type CopilotSuggestion = {
  id: string;
  title: string;
  detail: string;
  href: string;
};

export type DashboardData = {
  projects: ProjectWithDocuments[];
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
    href: `/strategy/topical-map?projectId=${encodeURIComponent(project.id)}`,
  }));
}

function buildCopilotSuggestions(
  projects: ProjectWithDocuments[],
  topicalSuggestions: CopilotSuggestion[],
): CopilotSuggestion[] {
  const welcome =
    projects.length === 0
      ? [
          {
            id: 'welcome',
            title: 'Add your first site',
            detail: 'Create a project to unlock topical maps, audits, and strategy tools.',
            href: '/projects',
          },
        ]
      : [];

  return dedupeCopilotSuggestions([...topicalSuggestions, ...welcome]).slice(0, 4);
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
    metrics: {
      seoScore: entry.latestAuditScore,
      siteHealthScore: entry.latestAuditScore,
      latestAuditAt: entry.latestAuditAt,
    },
  }));
}

async function loadTopicalCopilotSuggestions(
  accessToken: string | null,
  projects: ProjectWithDocuments[],
): Promise<CopilotSuggestion[]> {
  if (!accessToken || projects.length === 0) {
    return [];
  }

  const project = projects[0];

  try {
    const map = await getTopicalMap(project.id, accessToken).catch(() => null);
    return buildTopicalMapCopilotSuggestions(project, map);
  } catch {
    return [];
  }
}

export async function loadDashboardData(accessToken: string | null): Promise<DashboardData> {
  const overview = await getDashboardOverview(accessToken);
  const projects = mapOverviewToProjects(overview);
  const topicalSuggestions = await loadTopicalCopilotSuggestions(accessToken, projects);

  return {
    projects,
    copilotSuggestions: buildCopilotSuggestions(projects, topicalSuggestions),
  };
}
