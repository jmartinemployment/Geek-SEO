"use client";

import { useEffect, useRef, useState } from "react";
import {
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Circle,
  Copy,
  ExternalLink,
  Globe,
  Loader2,
  RefreshCw,
  Search,
  Upload,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useCrawlProgress } from "@/context/crawl-stream-context";
import {
  buildCrawlSummaryMessage,
  fetchCrawlProgressStatus,
  formatCount,
  readCount,
  statusResponseToProgress,
  type CompetitorCrawlProgressPayload,
  type CompetitorDomainSummary,
} from "@/lib/crawlProgressStream";
import {
  DomainOverviewPanel,
  normalizeDomainOverview,
  type DomainOverview,
} from "@/components/site-analyzer2/DomainOverviewPanel";
import { ManualResearchLanesCard } from "@/components/site-analyzer2/ManualResearchLanesCard";
import { slugifyResearchTopic } from "@/lib/manual-research-lanes";
import { contentWritingPath } from "@/lib/content-writing-search-params";
import { getSiteAnalyzer2ApiBase, siteAnalyzer2Fetch } from "@/lib/site-analyzer2-api";
import { cn } from "@/lib/utils";

const STORAGE_URL = "siteAnalyzer2.projectUrl";
const STORAGE_TOPIC_SLUG = "siteAnalyzer2.researchTopicSlug";
const STORAGE_KEYWORD_IMPORT = "siteAnalyzer2.keywordImport";
const STORAGE_COMPETITOR_CRAWL = "siteAnalyzer2.competitorCrawl";
const STORAGE_SITE_PROFILE_PANEL = "siteAnalyzer2.siteProfilePanelExpanded";

type Step = "idle" | "keyword_saved" | "complete";

type Status = { kind: "ok" | "err" | "info"; text: string };

type RecommendedJsonLdSnippet = {
  id: string;
  title: string;
  description: string;
  json: string;
  scriptTag: string;
};

type ContentPillar = {
  runId: string;
  keyword: string;
  createdAt: string;
  competitorCrawlComplete: boolean;
  gapTopicsReady: boolean;
};

type ResearchWorkflowGate = {
  id: string;
  label: string;
  complete: boolean;
};

type ResearchPackStats = {
  paaQuestionCount: number;
  competitorPageCount: number;
  competitorHeadingCount: number;
  sourceHeadingCount: number;
  gapTopicCount: number;
};

type RankingsDelta = {
  previousPosition?: number | null;
  currentPosition?: number | null;
  positionChange?: number | null;
  previousCapturedAt?: string | null;
  currentCapturedAt?: string;
};

type SerpRankSnapshot = {
  importSequence: number;
  serpCapturedAt: string;
  targetPosition: number | null;
  targetUrl: string | null;
  organicResultCount: number;
};

type RunRankingsSummary = {
  history: SerpRankSnapshot[];
  latestDelta?: RankingsDelta | null;
  hasRecapture: boolean;
};

type RunResearchFocus = {
  runId: string;
  keyword: string;
  topicSlug?: string | null;
  matchedPillarTopic?: string | null;
  matchedPillarIntent?: string | null;
  matchedPillarAngle?: string | null;
  gapTopics: string[];
  writingInstructions?: string | null;
  researchReady: boolean;
  researchMode?: string;
  gates: ResearchWorkflowGate[];
  packStats: ResearchPackStats;
  rankings: RunRankingsSummary;
};

type SiteProfile = {
  id: string;
  siteUrl: string;
  displayName?: string | null;
  createdAt: string;
  updatedAt: string;
  businessProfileAt?: string | null;
  lastRunAt?: string | null;
  businessType?: string | null;
  businessDescription?: string | null;
  businessSummary?: string | null;
  primaryNiche?: string | null;
  nicheDescription?: string | null;
  nicheTags: string[];
  geoAnchorNodes: string[];
  serviceAreaDescription?: string | null;
  writingRecommendations: string[];
  recommendedHomepageJsonLd: RecommendedJsonLdSnippet[];
};

type KeywordImportStorage = {
  projectId: string;
  keywordProjectId: string;
  keyword: string;
  keywordSaved: true;
  organicCount: number;
  organicOnlyCount: number;
  paidCount: number;
  aiOverviewCount: number;
  aiOverviewAvailable: boolean;
  paaCount: number;
  competitorCrawlSeedCount: number;
  filterApplied: boolean;
  filterIncludedCount: number;
  filterExcludedCount: number;
  filterRejectedCount: number;
  filterPendingReviewCount: number;
  filterCrawlEligibleCount: number;
  message?: string;
  targetSiteUrl: string;
  savedAt: string;
};

type CompetitorCrawlStorage = {
  keywordProjectId: string;
  competitorSaved: true;
  totalPages: number;
  domainCount: number;
  domains: CompetitorDomainSummary[];
  qualityWarnings: string[];
  message?: string;
  savedAt: string;
};


function hostnameFromProjectUrl(normalizedUrl: string): string {
  try {
    const host = new URL(normalizedUrl).hostname.toLowerCase();
    return host.startsWith("www.") ? host.slice(4) : host;
  } catch {
    return normalizedUrl;
  }
}

function normalizeProjectUrl(raw: string): string {
  const t = raw.trim();
  if (!t) return "";
  const withScheme = /^https?:\/\//i.test(t) ? t : `https://${t}`;
  try {
    const u = new URL(withScheme);
    let host = u.hostname.toLowerCase();
    if (host.startsWith("www.")) host = host.slice(4);
    // Wait for a registrable-looking host before normalizing (avoids https://www.ge/ while typing).
    if (!host.includes(".")) return "";
    const port = u.port ? `:${u.port}` : "";
    const normalized = `https://www.${host}${port}/`;
    return normalized.startsWith("https://www.") &&
      normalized.endsWith("/") &&
      normalized === normalized.toLowerCase()
      ? normalized
      : "";
  } catch {
    return "";
  }
}

function isProjectUrlReadyForLookup(normalized: string): boolean {
  if (!normalized) return false;
  try {
    const host = new URL(normalized).hostname.toLowerCase();
    const bare = host.startsWith("www.") ? host.slice(4) : host;
    return bare.includes(".");
  } catch {
    return false;
  }
}

function contentWriterUrl(analysisRunId: string): string {
  return contentWritingPath({ analysisRunId });
}

function saveKeywordImport(data: KeywordImportStorage) {
  localStorage.setItem(STORAGE_KEYWORD_IMPORT, JSON.stringify(data));
}

function loadKeywordImport(): KeywordImportStorage | null {
  const raw = localStorage.getItem(STORAGE_KEYWORD_IMPORT);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const keywordProjectId = readString(parsed.keywordProjectId);
    if (!keywordProjectId || parsed.keywordSaved !== true) return null;

    const organicOnlyCount = readCount(parsed.organicOnlyCount);
    const paidCount = readCount(parsed.paidCount);
    const organicCount = readCount(parsed.organicCount) || organicOnlyCount + paidCount;
    const paaCount = readCount(parsed.paaCount ?? parsed.relatedQueryCount);

    return {
      projectId: readString(parsed.projectId) ?? "",
      keywordProjectId,
      keyword: readString(parsed.keyword) ?? "",
      keywordSaved: true,
      organicCount,
      organicOnlyCount: organicOnlyCount || Math.max(0, organicCount - paidCount),
      paidCount,
      aiOverviewCount: readCount(parsed.aiOverviewCount),
      aiOverviewAvailable: parsed.aiOverviewAvailable === true,
      paaCount,
      competitorCrawlSeedCount: readCount(parsed.competitorCrawlSeedCount) || organicOnlyCount,
      filterApplied: parsed.filterApplied === true,
      filterIncludedCount: readCount(parsed.filterIncludedCount),
      filterExcludedCount: readCount(parsed.filterExcludedCount),
      filterRejectedCount: readCount(parsed.filterRejectedCount),
      filterPendingReviewCount: readCount(parsed.filterPendingReviewCount),
      filterCrawlEligibleCount: readCount(parsed.filterCrawlEligibleCount),
      message: readString(parsed.message) ?? undefined,
      targetSiteUrl: readString(parsed.targetSiteUrl) ?? "",
      savedAt: readString(parsed.savedAt) ?? new Date().toISOString(),
    };
  } catch {
    return null;
  }
}

function loadCompetitorCrawl(): CompetitorCrawlStorage | null {
  const raw = localStorage.getItem(STORAGE_COMPETITOR_CRAWL);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const keywordProjectId = readString(parsed.keywordProjectId);
    if (!keywordProjectId || parsed.competitorSaved !== true) return null;

    return {
      keywordProjectId,
      competitorSaved: true,
      totalPages: readCount(parsed.totalPages),
      domainCount: readCount(parsed.domainCount),
      domains: Array.isArray(parsed.domains)
        ? (parsed.domains as CompetitorDomainSummary[])
        : [],
      qualityWarnings: readStringList(parsed.qualityWarnings),
      message: readString(parsed.message) ?? undefined,
      savedAt: readString(parsed.savedAt) ?? new Date().toISOString(),
    };
  } catch {
    return null;
  }
}

function saveCompetitorCrawl(data: CompetitorCrawlStorage) {
  localStorage.setItem(STORAGE_COMPETITOR_CRAWL, JSON.stringify(data));
}

function clearOperatorData() {
  localStorage.removeItem(STORAGE_KEYWORD_IMPORT);
  localStorage.removeItem(STORAGE_COMPETITOR_CRAWL);
}

function formatWhen(iso?: string | null): string {
  if (!iso) return "—";
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleString();
}

function formatText(value?: string | null): string {
  return value?.trim() ? value.trim() : "—";
}

function formatList(values: string[], emptyLabel = "—"): string {
  const items = values.map((v) => v.trim()).filter(Boolean);
  return items.length > 0 ? items.join(", ") : emptyLabel;
}

function readString(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value.trim() : null;
}

function readStringList(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value
    .filter((item): item is string => typeof item === "string")
    .map((item) => item.trim())
    .filter(Boolean);
}

type KeywordImportApiBody = {
  projectId?: string;
  keywordProjectId?: string;
  keyword?: string;
  keywordSaved?: boolean;
  organicCount?: number;
  organicOnlyCount?: number;
  paidCount?: number;
  aiOverviewCount?: number;
  aiOverviewAvailable?: boolean;
  paaCount?: number;
  relatedQueryCount?: number;
  competitorCrawlSeedCount?: number;
  filterApplied?: boolean;
  filterIncludedCount?: number;
  filterExcludedCount?: number;
  filterRejectedCount?: number;
  filterPendingReviewCount?: number;
  filterCrawlEligibleCount?: number;
  message?: string;
  targetOrganicPosition?: number | null;
  targetOrganicUrl?: string | null;
  rankingsDelta?: {
    previousPosition?: number | null;
    currentPosition?: number | null;
    positionChange?: number | null;
    previousCapturedAt?: string | null;
    currentCapturedAt?: string;
  } | null;
  topicSlug?: string | null;
};

function formatRankPosition(position?: number | null): string {
  if (position == null || position <= 0) return "Not ranking";
  return `#${position}`;
}

function formatRankDelta(change?: number | null): string {
  if (change == null) return "—";
  if (change > 0) return `↑ ${change} (improved)`;
  if (change < 0) return `↓ ${Math.abs(change)} (declined)`;
  return "No change";
}

function normalizeRankingsDelta(value: unknown): RankingsDelta | null {
  if (!value || typeof value !== "object") return null;
  const record = value as Record<string, unknown>;
  return {
    previousPosition: readOptionalCount(record.previousPosition ?? record.PreviousPosition),
    currentPosition: readOptionalCount(record.currentPosition ?? record.CurrentPosition),
    positionChange: readOptionalCount(record.positionChange ?? record.PositionChange),
    previousCapturedAt:
      readString(record.previousCapturedAt) ?? readString(record.PreviousCapturedAt),
    currentCapturedAt:
      readString(record.currentCapturedAt) ?? readString(record.CurrentCapturedAt) ?? undefined,
  };
}

function readOptionalCount(value: unknown): number | null | undefined {
  if (value === null) return null;
  if (typeof value === "number" && Number.isFinite(value)) return value;
  return undefined;
}

function normalizeRankingsSummary(value: unknown): RunRankingsSummary {
  if (!value || typeof value !== "object") {
    return { history: [], latestDelta: null, hasRecapture: false };
  }
  const record = value as Record<string, unknown>;
  const historyRaw = record.history ?? record.History;
  const history: SerpRankSnapshot[] = Array.isArray(historyRaw)
    ? historyRaw.flatMap((item): SerpRankSnapshot[] => {
        if (!item || typeof item !== "object") return [];
        const row = item as Record<string, unknown>;
        const importSequence = readCount(row.importSequence ?? row.ImportSequence);
        const serpCapturedAt =
          readString(row.serpCapturedAt) ?? readString(row.SerpCapturedAt) ?? "";
        if (!importSequence || !serpCapturedAt) return [];
        const rawPosition = readOptionalCount(row.targetPosition ?? row.TargetPosition);
        return [
          {
            importSequence,
            serpCapturedAt,
            targetPosition: rawPosition === undefined ? null : rawPosition,
            targetUrl: readString(row.targetUrl) ?? readString(row.TargetUrl) ?? null,
            organicResultCount: readCount(row.organicResultCount ?? row.OrganicResultCount),
          },
        ];
      })
    : [];

  return {
    history,
    latestDelta: normalizeRankingsDelta(record.latestDelta ?? record.LatestDelta),
    hasRecapture: record.hasRecapture === true || record.HasRecapture === true,
  };
}

function keywordImportRankLines(body: KeywordImportApiBody): string[] {
  const lines: string[] = [];
  const delta = body.rankingsDelta;
  if (delta) {
    lines.push(
      `Rank change: ${formatRankPosition(delta.previousPosition)} → ${formatRankPosition(delta.currentPosition)} (${formatRankDelta(delta.positionChange ?? null)})`,
    );
  } else if (body.targetOrganicPosition != null) {
    lines.push(`Your domain in SERP: ${formatRankPosition(body.targetOrganicPosition)}`);
  }
  return lines;
}

function keywordImportFromApi(
  body: KeywordImportApiBody,
  targetSiteUrl: string,
  savedAt?: string,
): KeywordImportStorage {
  const organicOnlyCount = readCount(body.organicOnlyCount);
  const paidCount = readCount(body.paidCount);
  const organicCount = readCount(body.organicCount) || organicOnlyCount + paidCount;
  const paaCount = readCount(body.paaCount ?? body.relatedQueryCount);

  return {
    projectId: body.projectId ?? "",
    keywordProjectId: body.keywordProjectId ?? "",
    keyword: body.keyword ?? "",
    keywordSaved: true,
    organicCount,
    organicOnlyCount: organicOnlyCount || Math.max(0, organicCount - paidCount),
    paidCount,
    aiOverviewCount: readCount(body.aiOverviewCount),
    aiOverviewAvailable: body.aiOverviewAvailable === true,
    paaCount,
    competitorCrawlSeedCount: readCount(body.competitorCrawlSeedCount) || organicOnlyCount,
    filterApplied: body.filterApplied === true,
    filterIncludedCount: readCount(body.filterIncludedCount),
    filterExcludedCount: readCount(body.filterExcludedCount),
    filterRejectedCount: readCount(body.filterRejectedCount),
    filterPendingReviewCount: readCount(body.filterPendingReviewCount),
    filterCrawlEligibleCount: readCount(body.filterCrawlEligibleCount),
    message: body.message,
    targetSiteUrl,
    savedAt: savedAt ?? new Date().toISOString(),
  };
}

function readJsonLdSnippets(value: unknown): RecommendedJsonLdSnippet[] {
  if (!Array.isArray(value)) return [];

  return value
    .map((item) => {
      if (!item || typeof item !== "object") return null;
      const record = item as Record<string, unknown>;
      const id = readString(record.id) ?? readString(record.Id);
      const title = readString(record.title) ?? readString(record.Title);
      const description = readString(record.description) ?? readString(record.Description);
      const json = readString(record.json) ?? readString(record.Json);
      const scriptTag = readString(record.scriptTag) ?? readString(record.ScriptTag);
      if (!id || !title || !scriptTag) return null;

      return {
        id,
        title,
        description: description ?? "",
        json: json ?? "",
        scriptTag,
      };
    })
    .filter((item): item is RecommendedJsonLdSnippet => item !== null);
}

function normalizeContentPillars(value: unknown): ContentPillar[] {
  if (!Array.isArray(value)) return [];
  return value
    .map((item) => {
      if (!item || typeof item !== "object") return null;
      const record = item as Record<string, unknown>;
      const runId = readString(record.runId) ?? readString(record.RunId);
      const keyword = readString(record.keyword) ?? readString(record.Keyword);
      if (!runId || !keyword) return null;
      return {
        runId,
        keyword,
        createdAt: readString(record.createdAt) ?? readString(record.CreatedAt) ?? "",
        competitorCrawlComplete:
          record.competitorCrawlComplete === true || record.CompetitorCrawlComplete === true,
        gapTopicsReady: record.gapTopicsReady === true || record.GapTopicsReady === true,
      };
    })
    .filter((item): item is ContentPillar => item !== null);
}

function normalizePackStats(value: unknown): ResearchPackStats {
  if (!value || typeof value !== "object") {
    return {
      paaQuestionCount: 0,
      competitorPageCount: 0,
      competitorHeadingCount: 0,
      sourceHeadingCount: 0,
      gapTopicCount: 0,
    };
  }

  const record = value as Record<string, unknown>;
  return {
    paaQuestionCount: readCount(record.paaQuestionCount ?? record.PaaQuestionCount),
    competitorPageCount: readCount(record.competitorPageCount ?? record.CompetitorPageCount),
    competitorHeadingCount: readCount(record.competitorHeadingCount ?? record.CompetitorHeadingCount),
    sourceHeadingCount: readCount(record.sourceHeadingCount ?? record.SourceHeadingCount),
    gapTopicCount: readCount(record.gapTopicCount ?? record.GapTopicCount),
  };
}

function normalizeResearchFocus(value: unknown): RunResearchFocus | null {
  if (!value || typeof value !== "object") return null;
  const record = value as Record<string, unknown>;
  const runId = readString(record.runId) ?? readString(record.RunId);
  const keyword = readString(record.keyword) ?? readString(record.Keyword);
  if (!runId || !keyword) return null;

  const gatesRaw = record.gates ?? record.Gates;
  const gates: ResearchWorkflowGate[] = Array.isArray(gatesRaw)
    ? gatesRaw
        .map((gate) => {
          if (!gate || typeof gate !== "object") return null;
          const g = gate as Record<string, unknown>;
          const id = readString(g.id) ?? readString(g.Id);
          const label = readString(g.label) ?? readString(g.Label);
          if (!id || !label) return null;
          return {
            id,
            label,
            complete: g.complete === true || g.Complete === true,
          };
        })
        .filter((g): g is ResearchWorkflowGate => g !== null)
    : [];

  return {
    runId,
    keyword,
    topicSlug: readString(record.topicSlug) ?? readString(record.TopicSlug),
    matchedPillarTopic: readString(record.matchedPillarTopic) ?? readString(record.MatchedPillarTopic),
    matchedPillarIntent: readString(record.matchedPillarIntent) ?? readString(record.MatchedPillarIntent),
    matchedPillarAngle: readString(record.matchedPillarAngle) ?? readString(record.MatchedPillarAngle),
    gapTopics: readStringList(record.gapTopics ?? record.GapTopics),
    writingInstructions:
      readString(record.writingInstructions) ?? readString(record.WritingInstructions),
    researchReady: record.researchReady === true || record.ResearchReady === true,
    researchMode: readString(record.researchMode) ?? readString(record.ResearchMode) ?? undefined,
    gates,
    packStats: normalizePackStats(record.packStats ?? record.PackStats),
    rankings: normalizeRankingsSummary(record.rankings ?? record.Rankings),
  };
}

function normalizeProfileFromApi(body: Record<string, unknown>): SiteProfile | null {
  const id = readString(body.id) ?? readString(body.Id);
  const siteUrl = readString(body.siteUrl) ?? readString(body.SiteUrl);
  if (!id || !siteUrl) return null;

  return {
    id,
    siteUrl,
    displayName: readString(body.displayName) ?? readString(body.DisplayName),
    createdAt: readString(body.createdAt) ?? readString(body.CreatedAt) ?? "",
    updatedAt: readString(body.updatedAt) ?? readString(body.UpdatedAt) ?? "",
    businessProfileAt: readString(body.businessProfileAt) ?? readString(body.BusinessProfileAt),
    lastRunAt: readString(body.lastRunAt) ?? readString(body.LastRunAt),
    businessType: readString(body.businessType) ?? readString(body.BusinessType),
    businessDescription: readString(body.businessDescription) ?? readString(body.BusinessDescription),
    businessSummary: readString(body.businessSummary) ?? readString(body.BusinessSummary),
    primaryNiche: readString(body.primaryNiche) ?? readString(body.PrimaryNiche),
    nicheDescription: readString(body.nicheDescription) ?? readString(body.NicheDescription),
    nicheTags: readStringList(body.nicheTags ?? body.NicheTags),
    geoAnchorNodes: readStringList(body.geoAnchorNodes ?? body.GeoAnchorNodes),
    serviceAreaDescription:
      readString(body.serviceAreaDescription) ?? readString(body.ServiceAreaDescription),
    writingRecommendations: readStringList(body.writingRecommendations ?? body.WritingRecommendations),
    recommendedHomepageJsonLd: readJsonLdSnippets(
      body.recommendedHomepageJsonLd ?? body.RecommendedHomepageJsonLd,
    ),
  };
}

function profileHasBusinessData(profile: SiteProfile): boolean {
  return Boolean(
    profile.businessProfileAt ||
      profile.businessType?.trim() ||
      profile.businessSummary?.trim() ||
      profile.businessDescription?.trim() ||
      profile.primaryNiche?.trim() ||
      profile.nicheTags.length > 0,
  );
}

function WorkflowStrip({
  siteProfileReady,
  keywordSaved,
  step,
  researchReady,
  keyword,
}: {
  siteProfileReady: boolean;
  keywordSaved: boolean;
  step: Step;
  researchReady: boolean;
  keyword: string;
}) {
  const steps = [
    {
      id: "profile",
      label: "Site profile",
      done: siteProfileReady,
      active: !siteProfileReady,
    },
    {
      id: "keyword",
      label: "Keyword import",
      done: keywordSaved,
      active: siteProfileReady && !keywordSaved,
    },
    {
      id: "crawl",
      label: "Competitor crawl",
      done: step === "complete",
      active: keywordSaved && step !== "complete",
    },
    {
      id: "research",
      label: "Research pack",
      done: researchReady,
      active: step === "complete" && !researchReady,
    },
  ];

  return (
    <Card className="overflow-hidden">
      <CardContent className="px-4 py-3 sm:px-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-wrap items-center gap-2 sm:gap-0">
            {steps.map((item, index) => (
              <div key={item.id} className="flex items-center">
                <div
                  className={`flex items-center gap-2 rounded-full px-2.5 py-1 text-xs font-medium sm:px-3 ${
                    item.done
                      ? "text-[var(--color-good)]"
                      : item.active
                        ? "bg-[rgba(59,179,122,0.1)] text-[var(--color-accent)]"
                        : "text-[var(--color-text-muted)]"
                  }`}
                >
                  {item.done ? (
                    <CheckCircle2 className="size-3.5 shrink-0" />
                  ) : item.active ? (
                    <Loader2 className="size-3.5 shrink-0 animate-spin" />
                  ) : (
                    <Circle className="size-3.5 shrink-0" />
                  )}
                  <span>{item.label}</span>
                </div>
                {index < steps.length - 1 ? (
                  <div className="mx-1 hidden h-px w-6 bg-[var(--color-border)] sm:block lg:w-10" />
                ) : null}
              </div>
            ))}
          </div>
          {keyword ? (
            <Badge variant="accent" className="shrink-0 self-start sm:self-center">
              {keyword}
            </Badge>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}

function MetricTile({
  label,
  value,
  hint,
}: {
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white px-4 py-3 shadow-[var(--shadow-card)]">
      <p className="text-xs font-medium uppercase tracking-wide text-[var(--color-text-muted)]">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">
        {value}
      </p>
      {hint ? (
        <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">{hint}</p>
      ) : null}
    </div>
  );
}

function SiteProfilePanel({
  profile,
  loading,
  onRefresh,
  expandToken = 0,
  contentPillars = [],
  pillarsLoading = false,
}: {
  profile: SiteProfile;
  loading: boolean;
  onRefresh: () => void;
  expandToken?: number;
  contentPillars?: ContentPillar[];
  pillarsLoading?: boolean;
}) {
  const [expanded, setExpanded] = useState(true);
  const [copiedSnippetId, setCopiedSnippetId] = useState<string | null>(null);
  const hasBusinessData = profileHasBusinessData(profile);

  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_SITE_PROFILE_PANEL);
      if (stored === "0") setExpanded(false);
      else if (stored === "1") setExpanded(true);
    } catch {
      // ignore
    }
  }, []);

  useEffect(() => {
    if (expandToken > 0) setExpanded(true);
  }, [expandToken]);

  function toggleExpanded() {
    setExpanded((prev) => {
      const next = !prev;
      try {
        localStorage.setItem(STORAGE_SITE_PROFILE_PANEL, next ? "1" : "0");
      } catch {
        // ignore
      }
      return next;
    });
  }

  async function copySnippet(snippet: RecommendedJsonLdSnippet) {
    try {
      await navigator.clipboard.writeText(snippet.scriptTag);
      setCopiedSnippetId(snippet.id);
      window.setTimeout(() => setCopiedSnippetId((current) => (current === snippet.id ? null : current)), 2000);
    } catch {
      // ignore
    }
  }

  return (
    <Card>
      <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 pb-0">
        <div className="min-w-0 flex-1">
          <CardTitle>Site profile</CardTitle>
          <CardDescription className="truncate">
            {formatText(profile.displayName)} · {profile.siteUrl}
          </CardDescription>
          {!expanded && hasBusinessData ? (
            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              {formatText(profile.primaryNiche)} · {formatList(profile.nicheTags, "—")}
            </p>
          ) : null}
          {!expanded && contentPillars.length > 0 ? (
            <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">
              {contentPillars.length} content pillar{contentPillars.length === 1 ? "" : "s"}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 gap-2">
          <Button type="button" variant="outline" size="sm" onClick={toggleExpanded}>
            {expanded ? (
              <>
                <ChevronUp className="size-3.5" />
                Hide
              </>
            ) : (
              <>
                <ChevronDown className="size-3.5" />
                Show
              </>
            )}
          </Button>
          <Button type="button" variant="outline" size="sm" onClick={onRefresh} disabled={loading}>
            <RefreshCw className={`size-3.5 ${loading ? "animate-spin" : ""}`} />
            {loading ? "Refreshing…" : "Refresh"}
          </Button>
        </div>
      </CardHeader>

      {expanded ? (
        <CardContent className="space-y-4">
          <dl className="grid gap-x-4 gap-y-2 text-sm sm:grid-cols-[9rem_1fr]">
            <dt className="text-[var(--color-text-muted)]">Business profile</dt>
            <dd>
              {hasBusinessData
                ? profile.businessProfileAt
                  ? `Assembled ${formatWhen(profile.businessProfileAt)}`
                  : "Ready"
                : "Not assembled yet — click Create Site Profile"}
            </dd>

            <dt className="text-[var(--color-text-muted)]">Business type</dt>
            <dd>{formatText(profile.businessType)}</dd>

            <dt className="text-[var(--color-text-muted)]">Summary</dt>
            <dd>{formatText(profile.businessSummary ?? profile.businessDescription)}</dd>

            <dt className="text-[var(--color-text-muted)]">Business identity</dt>
            <dd>{formatText(profile.primaryNiche)}</dd>

            <dt className="text-[var(--color-text-muted)]">Niche description</dt>
            <dd>{formatText(profile.nicheDescription)}</dd>

            <dt className="text-[var(--color-text-muted)]">Site themes</dt>
            <dd>{formatList(profile.nicheTags)}</dd>

            <dt className="text-[var(--color-text-muted)]">Geo anchors</dt>
            <dd>{formatList(profile.geoAnchorNodes)}</dd>

            <dt className="text-[var(--color-text-muted)]">Service area</dt>
            <dd>{formatText(profile.serviceAreaDescription)}</dd>

            <dt className="self-start text-[var(--color-text-muted)]">Writing recommendations</dt>
            <dd>
              {profile.writingRecommendations.length > 0 ? (
                <ul className="list-disc space-y-1 pl-4">
                  {profile.writingRecommendations.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              ) : (
                <span className="text-[var(--color-text-muted)]">
                  Available after Create Site Profile
                </span>
              )}
            </dd>

            <dt className="self-start text-[var(--color-text-muted)]">Content pillars</dt>
            <dd>
              {pillarsLoading ? (
                <span className="text-[var(--color-text-muted)]">Loading…</span>
              ) : contentPillars.length > 0 ? (
                <ul className="list-disc space-y-1 pl-4">
                  {contentPillars.map((pillar) => (
                    <li key={pillar.runId}>
                      <strong>{pillar.keyword}</strong>
                      <span className="text-xs text-[var(--color-text-muted)]">
                        {" "}
                        · {formatWhen(pillar.createdAt)}
                        {pillar.gapTopicsReady
                          ? " · gaps ready"
                          : pillar.competitorCrawlComplete
                            ? " · crawl done"
                            : ""}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : (
                <span className="text-[var(--color-text-muted)]">
                  Saved keywords appear here after SERP import
                </span>
              )}
            </dd>
          </dl>

          {profile.recommendedHomepageJsonLd.length > 0 ? (
            <div className="space-y-3 border-t border-[var(--color-border)] pt-4">
              <div>
                <h3 className="text-sm font-semibold">Recommended homepage JSON-LD</h3>
                <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
                  Copy each block into your homepage &lt;head&gt;. Content pages get a separate
                  TechArticle block from Content Writer.
                </p>
              </div>
              {profile.recommendedHomepageJsonLd.map((snippet) => (
                <div
                  key={snippet.id}
                  className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3"
                >
                  <div className="mb-2 flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold">{snippet.title}</p>
                      {snippet.description ? (
                        <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">
                          {snippet.description}
                        </p>
                      ) : null}
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => void copySnippet(snippet)}
                    >
                      <Copy className="size-3.5" />
                      {copiedSnippetId === snippet.id ? "Copied" : "Copy script"}
                    </Button>
                  </div>
                  <pre className="overflow-x-auto rounded-md bg-white p-3 text-xs leading-relaxed whitespace-pre-wrap break-words">
                    {snippet.scriptTag}
                  </pre>
                </div>
              ))}
            </div>
          ) : null}
        </CardContent>
      ) : null}
    </Card>
  );
}

function KeywordImportSummaryPanel({
  keyword,
  summary,
}: {
  keyword: string;
  summary: KeywordImportStorage;
}) {
  const [expanded, setExpanded] = useState(true);
  const aiLabel =
    summary.aiOverviewCount === 0
      ? null
      : summary.aiOverviewAvailable
        ? "AI Overview (content captured)"
        : "AI Overview (unavailable)";

  const filterLine =
    summary.filterApplied === true
      ? `${formatCount(summary.filterCrawlEligibleCount)} crawl-eligible · ${formatCount(summary.filterIncludedCount)} included · ${formatCount(summary.filterRejectedCount)} rejected · ${formatCount(summary.filterExcludedCount)} excluded · ${formatCount(summary.filterPendingReviewCount)} pending`
      : null;

  return (
    <Card className="border-[rgba(26,110,191,0.25)] bg-[rgba(26,110,191,0.04)]">
      <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 pb-0">
        <div>
          <CardTitle className="text-[var(--color-metric-blue)]">Keyword import saved</CardTitle>
          <CardDescription className="text-[var(--color-metric-blue)]/80">
            {formatCount(summary.organicOnlyCount)} organic · {formatCount(summary.paidCount)} sponsored
            {aiLabel ? ` · ${aiLabel}` : ""} · {formatCount(summary.paaCount)} PAA/PASF
          </CardDescription>
          {filterLine ? (
            <p className="mt-1 text-xs text-[var(--color-metric-blue)]/70">Relevance filter: {filterLine}</p>
          ) : null}
          {keyword ? (
            <p className="mt-1 text-xs font-medium text-[var(--color-metric-blue)]">Keyword: {keyword}</p>
          ) : null}
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">Saved {formatWhen(summary.savedAt)}</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => setExpanded((value) => !value)}>
          {expanded ? (
            <>
              <ChevronUp className="size-3.5" />
              Hide
            </>
          ) : (
            <>
              <ChevronDown className="size-3.5" />
              Show
            </>
          )}
        </Button>
      </CardHeader>

      {expanded ? (
        <CardContent>
          <p className="mb-2 text-sm font-semibold text-[var(--color-metric-blue)]">What was saved</p>
          <ul className="list-disc space-y-1 pl-4 text-sm text-[var(--color-text-primary)]">
            <li>
              {formatCount(summary.organicOnlyCount)} organic results (URLs stored for competitor crawl seeds)
            </li>
            <li>{formatCount(summary.paidCount)} sponsored results (saved, not used for crawl)</li>
            <li>
              {summary.aiOverviewCount > 0 ? aiLabel : "No AI Overview block detected"}
            </li>
            <li>{formatCount(summary.paaCount)} People Also Ask / People also search for suggestions</li>
            {summary.filterApplied ? (
              <>
                <li>{formatCount(summary.filterCrawlEligibleCount)} crawl-eligible (included + on-topic pending)</li>
                <li>{formatCount(summary.filterIncludedCount)} auto-included for crawl</li>
                <li>{formatCount(summary.filterRejectedCount)} rejected (no pillar keyword word in URL, title, or snippet)</li>
                <li>{formatCount(summary.filterExcludedCount)} excluded (reference, owned, .gov, etc.)</li>
                {readCount(summary.filterPendingReviewCount) > 0 ? (
                  <li>{formatCount(summary.filterPendingReviewCount)} pending manual review</li>
                ) : null}
              </>
            ) : (
              <li>Relevance filter not applied yet — re-import or refresh after deploy.</li>
            )}
          </ul>
          <p className="mt-3 text-xs text-[var(--color-text-secondary)]">
            Competitor crawl uses {formatCount(summary.competitorCrawlSeedCount)} included seed URL
            {summary.competitorCrawlSeedCount === 1 ? "" : "s"} (one ranking page per domain).
          </p>
        </CardContent>
      ) : null}
    </Card>
  );
}

function RunResearchFocusPanel({
  focus,
  loading,
}: {
  focus: RunResearchFocus | null;
  loading: boolean;
}) {
  if (loading) {
    return (
      <Card>
        <CardContent className="flex items-center gap-2 py-6 text-sm text-[var(--color-text-secondary)]">
          <Loader2 className="size-4 animate-spin" />
          Loading research focus…
        </CardContent>
      </Card>
    );
  }

  if (!focus) return null;

  const ready = focus.researchReady;

  return (
    <Card
      className={
        ready
          ? "border-[rgba(34,197,94,0.3)] bg-[rgba(34,197,94,0.04)]"
          : "border-[rgba(245,158,11,0.3)] bg-[rgba(245,158,11,0.04)]"
      }
    >
      <CardHeader className="pb-2">
        <CardTitle className={ready ? "text-[var(--color-good)]" : "text-[var(--color-warn)]"}>
          {ready ? "Research ready" : "Research in progress"}
        </CardTitle>
        <CardDescription>
          Research pack for Content Writer — outlines and scoring happen in Writer, not here.
        </CardDescription>
        <p className="text-sm">
          Pillar: <strong>{focus.keyword}</strong>
          {focus.matchedPillarIntent ? ` · ${focus.matchedPillarIntent} intent` : ""}
        </p>
      </CardHeader>

      <CardContent className="grid gap-4 sm:grid-cols-2">
        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
            Pack stats
          </p>
          <ul className="space-y-1 text-sm">
            <li>PAA questions: {focus.packStats.paaQuestionCount}</li>
            <li>Competitor pages: {focus.packStats.competitorPageCount}</li>
            <li>Competitor headings: {focus.packStats.competitorHeadingCount}</li>
            <li>Your page headings: {focus.packStats.sourceHeadingCount}</li>
            <li>Gap themes: {focus.packStats.gapTopicCount}</li>
          </ul>
        </div>

        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
            Workflow gates
          </p>
          <ul className="space-y-1 text-sm">
            {focus.gates.map((gate) => (
              <li
                key={gate.id}
                className={gate.complete ? "text-[var(--color-good)]" : "text-[var(--color-text-muted)]"}
              >
                {gate.complete ? "✓" : "○"} {gate.label}
              </li>
            ))}
          </ul>
        </div>

        {focus.rankings.history.length > 0 ? (
          <div className="sm:col-span-2">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
              Rankings loop
            </p>
            {focus.rankings.hasRecapture && focus.rankings.latestDelta ? (
              <p className="mb-2 text-sm">
                Latest recapture:{" "}
                <strong>
                  {formatRankPosition(focus.rankings.latestDelta.previousPosition)} →{" "}
                  {formatRankPosition(focus.rankings.latestDelta.currentPosition)}
                </strong>{" "}
                ({formatRankDelta(focus.rankings.latestDelta.positionChange ?? null)})
              </p>
            ) : (
              <p className="mb-2 text-sm text-[var(--color-text-secondary)]">
                Baseline captured. Re-import SERP HTML later to see position delta.
              </p>
            )}
            <ul className="list-disc space-y-1 pl-4 text-sm">
              {focus.rankings.history.map((snapshot) => (
                <li key={snapshot.importSequence}>
                  Import #{snapshot.importSequence}: {formatRankPosition(snapshot.targetPosition)}
                  {snapshot.targetUrl ? ` · ${snapshot.targetUrl}` : ""}
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        {focus.gapTopics.length > 0 ? (
          <div className="sm:col-span-2">
            <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
              Gap themes
            </p>
            <p className="text-sm">{formatList(focus.gapTopics)}</p>
          </div>
        ) : null}

        {focus.writingInstructions ? (
          <div className="sm:col-span-2">
            <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
              Writing brief
            </p>
            <p className="text-sm leading-relaxed">{focus.writingInstructions}</p>
          </div>
        ) : null}

        {!ready ? (
          <p className="text-xs text-[var(--color-bad)] sm:col-span-2">
            Research pack is not ready. Competitor crawl must finish with all gates complete before
            Content Writer handoff.
          </p>
        ) : null}
      </CardContent>
    </Card>
  );
}

function CompetitorCrawlSummaryPanel({
  keyword,
  summary,
}: {
  keyword: string;
  summary: CompetitorCrawlStorage;
}) {
  const [expanded, setExpanded] = useState(true);
  const domainRows =
    summary.domains.length > 0
      ? summary.domains
      : summary.domainCount > 0
        ? [{ domain: "—", pagesCrawled: summary.totalPages }]
        : [];

  return (
    <Card className="border-[rgba(34,197,94,0.3)] bg-[rgba(34,197,94,0.04)]">
      <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 pb-0">
        <div>
          <CardTitle className="text-[var(--color-good)]">Competitor crawl complete</CardTitle>
          <CardDescription>
            {formatCount(summary.totalPages)} pages across {formatCount(summary.domainCount)} competitor domains
          </CardDescription>
          {keyword ? (
            <p className="mt-1 text-xs font-medium text-[var(--color-good)]">Keyword: {keyword}</p>
          ) : null}
          <p className="mt-0.5 text-xs text-[var(--color-text-muted)]">Saved {formatWhen(summary.savedAt)}</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => setExpanded((value) => !value)}>
          {expanded ? (
            <>
              <ChevronUp className="size-3.5" />
              Hide
            </>
          ) : (
            <>
              <ChevronDown className="size-3.5" />
              Show
            </>
          )}
        </Button>
      </CardHeader>

      {expanded ? (
        <CardContent className="space-y-4">
          {summary.qualityWarnings.length > 0 ? (
            <div>
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-warn)]">
                Quality warnings
              </p>
              <ul className="list-disc space-y-1 pl-4 text-sm text-[var(--color-warn)]">
                {summary.qualityWarnings.map((warning) => (
                  <li key={warning}>{warning}</li>
                ))}
              </ul>
            </div>
          ) : null}

          {domainRows.length > 0 ? (
            <div>
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]">
                Domains crawled
              </p>
              <ul className="divide-y divide-[var(--color-border)] text-sm">
                {domainRows.map((row) => (
                  <li key={row.domain} className="flex justify-between gap-3 py-2">
                    <span>{row.domain}</span>
                    <span className="text-[var(--color-text-secondary)]">
                      {formatCount(row.pagesCrawled)} pages
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </CardContent>
      ) : null}
    </Card>
  );
}

function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function SiteAnalyzer2Workspace({ accessToken }: { accessToken: string | null }) {
  const API = getSiteAnalyzer2ApiBase();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [projectUrl, setProjectUrl] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [keywordProjectId, setKeywordProjectId] = useState("");
  const [geekSeoProjectId, setGeekSeoProjectId] = useState("");
  const [keyword, setKeyword] = useState("");
  const [researchTopicSlug, setResearchTopicSlug] = useState("customer-journey");
  const [step, setStep] = useState<Step>("idle");
  const [status, setStatus] = useState<Status | null>(null);
  const [siteProfile, setSiteProfile] = useState<SiteProfile | null>(null);
  const [loadingSiteProfile, setLoadingSiteProfile] = useState(false);
  const [creatingSiteProfile, setCreatingSiteProfile] = useState(false);
  const [siteProfileExpandToken, setSiteProfileExpandToken] = useState(0);
  const [competitorCrawlSummary, setCompetitorCrawlSummary] = useState<CompetitorCrawlStorage | null>(null);
  const [keywordImportSummary, setKeywordImportSummary] = useState<KeywordImportStorage | null>(null);
  const [contentPillars, setContentPillars] = useState<ContentPillar[]>([]);
  const [pillarsLoading, setPillarsLoading] = useState(false);
  const [researchFocus, setResearchFocus] = useState<RunResearchFocus | null>(null);
  const [researchFocusLoading, setResearchFocusLoading] = useState(false);
  const [domainOverview, setDomainOverview] = useState<DomainOverview | null>(null);
  const [domainOverviewSearching, setDomainOverviewSearching] = useState(false);
  const [domainOverviewInput, setDomainOverviewInput] = useState("");
  const [parsing, setParsing] = useState(false);
  const [crawling, setCrawling] = useState(false);
  const [copied, setCopied] = useState(false);
  const [urlHydrated, setUrlHydrated] = useState(false);
  const skipNextProfileLoadRef = useRef(false);
  const crawlSettledRef = useRef(false);
  const competitorSummaryHydratedRef = useRef(false);
  const profileLoadAbortRef = useRef<AbortController | null>(null);

  const crawlProgress = useCrawlProgress(crawling ? keywordProjectId : "");

  const normalizedProjectUrl = normalizeProjectUrl(projectUrl);
  const siteProfileReady = Boolean(
    siteProfile &&
      normalizedProjectUrl &&
      normalizeProjectUrl(siteProfile.siteUrl) === normalizedProjectUrl,
  );
  const workflowLocked = step === "complete";
  const keywordSaved = step === "keyword_saved" || step === "complete";
  const filePickerDisabled = keywordSaved || parsing || crawling || workflowLocked;

  useEffect(() => {
    setProjectUrl(localStorage.getItem(STORAGE_URL) ?? "");
    setResearchTopicSlug(localStorage.getItem(STORAGE_TOPIC_SLUG) ?? "customer-journey");
    setUrlHydrated(true);
  }, []);

  useEffect(() => {
    if (!urlHydrated) return;
    localStorage.setItem(STORAGE_TOPIC_SLUG, researchTopicSlug.trim() || "customer-journey");
  }, [researchTopicSlug, urlHydrated]);

  useEffect(() => {
    if (!urlHydrated) return;

    const keywordImport = loadKeywordImport();
    if (!keywordImport?.keywordProjectId) return;

    setKeywordProjectId(keywordImport.keywordProjectId);
    setGeekSeoProjectId(keywordImport.projectId);
    setKeyword(keywordImport.keyword);
    setKeywordImportSummary(keywordImport);

    const stored = loadCompetitorCrawl();
    if (stored) {
      setCompetitorCrawlSummary(stored);
      setStep("complete");
      return;
    }

    setStep("keyword_saved");

    void (async () => {
      try {
        const status = await fetchCrawlProgressStatus(API, keywordImport.keywordProjectId, accessToken);
        if (!status || status.crawlStatus !== "running") return;

        crawlSettledRef.current = false;
        setCrawling(true);
        setStatus({
          kind: "info",
          text: status.message ?? "Resuming competitor crawl stream…",
        });
      } catch {
        // ignore resume errors; operator can retry manually
      }
    })();
  }, [urlHydrated]);

  useEffect(() => {
    if (!urlHydrated || step !== "complete" || !keywordProjectId) return;
    if (competitorSummaryHydratedRef.current) return;
    competitorSummaryHydratedRef.current = true;

    void (async () => {
      const status = await fetchCrawlProgressStatus(API, keywordProjectId, accessToken);
      if (!status || !status.competitorSaved) return;

      const existing = loadCompetitorCrawl();
      const refreshed: CompetitorCrawlStorage = {
        keywordProjectId,
        competitorSaved: true,
        totalPages: readCount(status.totalPages),
        domainCount: readCount(status.domainCount),
        domains: status.domains ?? [],
        qualityWarnings: status.qualityWarnings ?? [],
        message: status.message,
        savedAt: existing?.savedAt ?? new Date().toISOString(),
      };

      setCompetitorCrawlSummary(refreshed);
      saveCompetitorCrawl(refreshed);
    })();
  }, [urlHydrated, step, keywordProjectId]);

  useEffect(() => {
    if (!urlHydrated || !keywordProjectId || !keywordSaved) return;

    void (async () => {
      try {
        const params = new URLSearchParams({ keywordProjectId });
        const res = await siteAnalyzer2Fetch(`/imports/keyword-page/summary?${params}`, accessToken);
        if (!res.ok) return;

        const body = (await res.json()) as KeywordImportApiBody;
        const existing = loadKeywordImport();
        const refreshed = keywordImportFromApi(
          body,
          existing?.targetSiteUrl ?? normalizeProjectUrl(projectUrl),
          existing?.savedAt,
        );
        setKeywordImportSummary(refreshed);
        saveKeywordImport(refreshed);
      } catch {
        // keep localStorage snapshot when summary refresh fails
      }
    })();
  }, [urlHydrated, keywordProjectId, keywordSaved, projectUrl]);

  useEffect(() => {
    if (!keywordProjectId || !keywordSaved) {
      setResearchFocus(null);
      return;
    }

    void loadResearchFocus(keywordProjectId);
  }, [keywordProjectId, keywordSaved, step, competitorCrawlSummary?.savedAt]);

  useEffect(() => {
    if (!urlHydrated) return;

    if (!normalizedProjectUrl || !isProjectUrlReadyForLookup(normalizedProjectUrl)) {
      setSiteProfile(null);
      return;
    }

    if (skipNextProfileLoadRef.current) {
      skipNextProfileLoadRef.current = false;
      return;
    }

    const timer = window.setTimeout(() => {
      void loadSiteProfile(normalizedProjectUrl);
    }, 400);

    return () => window.clearTimeout(timer);
  }, [normalizedProjectUrl, urlHydrated]);

  function applyLoadedProfile(profile: SiteProfile) {
    setSiteProfile(profile);
  }

  async function loadContentPillars(siteUrl: string, signal?: AbortSignal) {
    setPillarsLoading(true);
    try {
      const params = new URLSearchParams({ siteUrl });
      const res = await siteAnalyzer2Fetch(`/sites/content-pillars?${params}`, accessToken, { signal });
      if (!res.ok) {
        setContentPillars([]);
        return;
      }
      const body: unknown = await res.json().catch(() => null);
      setContentPillars(normalizeContentPillars(body));
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") return;
      setContentPillars([]);
    } finally {
      setPillarsLoading(false);
    }
  }

  useEffect(() => {
    if (normalizedProjectUrl) {
      setDomainOverviewInput((current) => current || normalizedProjectUrl);
    }
  }, [normalizedProjectUrl]);

  async function runDomainOverviewSearch() {
    const domain = domainOverviewInput.trim();
    if (!domain) return;

    setDomainOverviewSearching(true);
    try {
      const res = await siteAnalyzer2Fetch(`/domain-overview/analyze`, accessToken, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ domain }),
      });
      const body = (await res.json().catch(() => ({}))) as Record<string, unknown>;
      if (!res.ok) {
        throw new Error(readString(body.error) ?? res.statusText ?? "Search failed");
      }

      const overview = normalizeDomainOverview(body);
      if (overview) setDomainOverview(overview);
    } catch (e) {
      setDomainOverview(null);
      setStatus({ kind: "err", text: e instanceof Error ? e.message : String(e) });
    } finally {
      setDomainOverviewSearching(false);
    }
  }

  async function loadResearchFocus(runId: string): Promise<RunResearchFocus | null> {
    setResearchFocusLoading(true);
    try {
      const res = await siteAnalyzer2Fetch(`/analysis-runs/${encodeURIComponent(runId)}/research-focus`, accessToken);
      if (!res.ok) {
        setResearchFocus(null);
        return null;
      }
      const body: unknown = await res.json().catch(() => null);
      const focus = normalizeResearchFocus(body);
      setResearchFocus(focus);
      if (focus?.topicSlug) {
        setResearchTopicSlug(focus.topicSlug);
      }
      return focus;
    } catch {
      setResearchFocus(null);
      return null;
    } finally {
      setResearchFocusLoading(false);
    }
  }

  async function waitForResearchPackReady(runId: string, maxAttempts = 12): Promise<RunResearchFocus | null> {
    let lastFocus: RunResearchFocus | null = null;
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const focus = await loadResearchFocus(runId);
      lastFocus = focus;
      if (focus?.researchReady) return focus;
      if (attempt < maxAttempts - 1) {
        await new Promise((resolve) => setTimeout(resolve, 2000));
      }
    }
    return lastFocus;
  }

  async function loadSiteProfile(siteUrl: string) {
    profileLoadAbortRef.current?.abort();
    const controller = new AbortController();
    profileLoadAbortRef.current = controller;

    setLoadingSiteProfile(true);
    try {
      const params = new URLSearchParams({ siteUrl });
      const res = await siteAnalyzer2Fetch(`/sites?${params}`, accessToken, { signal: controller.signal });
      if (res.status === 404) {
        setSiteProfile(null);
        return;
      }

      const body: unknown = await res.json().catch(() => null);
      if (!res.ok || !body || typeof body !== "object") {
        const error =
          body && typeof body === "object" && "error" in body
            ? String((body as { error?: unknown }).error ?? "Could not load site profile")
            : `Could not load site profile (${res.status})`;
        setStatus({ kind: "err", text: error });
        return;
      }

      const profile = normalizeProfileFromApi(body as Record<string, unknown>);
      if (!profile) {
        setStatus({ kind: "err", text: "Site profile response was missing required fields." });
        return;
      }

      applyLoadedProfile(profile);
      void loadContentPillars(siteUrl, controller.signal);
    } catch (e) {
      if (e instanceof DOMException && e.name === "AbortError") return;
      setStatus({
        kind: "err",
        text: e instanceof Error ? e.message : "Could not load site profile.",
      });
    } finally {
      if (profileLoadAbortRef.current === controller) {
        setLoadingSiteProfile(false);
      }
    }
  }

  function persistUrl() {
    const normalized = normalizeProjectUrl(projectUrl);
    if (normalized) setProjectUrl(normalized);
    localStorage.setItem(STORAGE_URL, normalized || projectUrl.trim());
  }

  function startNewKeyword() {
    clearOperatorData();
    setKeywordProjectId("");
    setGeekSeoProjectId("");
    setKeyword("");
    setStep("idle");
    setFile(null);
    setCopied(false);
    setCompetitorCrawlSummary(null);
    setKeywordImportSummary(null);
    competitorSummaryHydratedRef.current = false;
    setStatus(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }

  async function createSiteProfile() {
    persistUrl();
    const siteUrl = normalizeProjectUrl(projectUrl);

    if (!siteUrl) {
      setStatus({ kind: "err", text: "Enter a valid project URL (e.g. https://geekatyourspot.com)." });
      return;
    }

    const displayName = hostnameFromProjectUrl(siteUrl);
    setCreatingSiteProfile(true);
    setStatus({ kind: "info", text: "Fetching homepage and building site profile…" });

    try {
      const res = await siteAnalyzer2Fetch(`/sites`, accessToken, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ siteUrl, displayName }),
      });
      const body = (await res.json().catch(() => ({}))) as {
        error?: string;
        profile?: SiteProfile;
        created?: boolean;
      };

      if (!res.ok) {
        throw new Error(body.error || res.statusText || "Could not create site profile");
      }

      if (!body.profile) {
        throw new Error("Site profile response was missing profile data.");
      }

      const profile = normalizeProfileFromApi(body.profile as unknown as Record<string, unknown>);
      if (!profile) {
        throw new Error("Site profile response was missing required fields.");
      }

      skipNextProfileLoadRef.current = true;
      applyLoadedProfile(profile);
      setSiteProfileExpandToken((token) => token + 1);
      setStatus({
        kind: "ok",
        text: body.created
          ? `Site profile created and assembled for ${displayName}.`
          : "Site profile refreshed from homepage.",
      });
    } catch (e) {
      setSiteProfile(null);
      setStatus({ kind: "err", text: e instanceof Error ? e.message : String(e) });
    } finally {
      setCreatingSiteProfile(false);
    }
  }

  async function parseKeywordPage() {
    if (!siteProfileReady) {
      setStatus({ kind: "err", text: "Create a site profile before parsing a keyword page." });
      return;
    }

    persistUrl();
    const targetSiteUrl = normalizeProjectUrl(projectUrl);

    if (!targetSiteUrl) {
      setStatus({ kind: "err", text: "Enter a valid project URL (e.g. https://geekatyourspot.com)." });
      return;
    }
    if (!file) {
      setStatus({ kind: "err", text: "Choose the Chrome save: Webpage, HTML only." });
      return;
    }

    setParsing(true);
    setStatus({ kind: "info", text: "Parsing keyword page and saving to database…" });

    try {
      const html = await file.text();
      const params = new URLSearchParams({ targetSiteUrl });
      if (researchTopicSlug.trim()) {
        params.set("topic", researchTopicSlug.trim());
      }
      const res = await siteAnalyzer2Fetch(`/imports/keyword-page?${params}`, accessToken, {
        method: "POST",
        headers: { "Content-Type": "text/html; charset=utf-8" },
        body: html,
      });
      const body = (await res.json().catch(() => ({}))) as KeywordImportApiBody & { error?: string };

      if (!res.ok && res.status !== 422) {
        throw new Error(body.error || res.statusText || "Import failed");
      }

      const id = body.keywordProjectId ?? "";
      const projectId = body.projectId ?? "";
      const saved = body.keywordSaved === true;

      if (!saved) {
        setStatus({
          kind: "err",
          text: body.message ?? "Keyword data was not saved. Choose your HTML file and try again.",
        });
        return;
      }

      const kw = body.keyword ?? "";
      clearOperatorData();
      const importSummary = keywordImportFromApi(body, targetSiteUrl);
      saveKeywordImport(importSummary);
      setKeywordImportSummary(importSummary);

      setKeywordProjectId(id);
      setGeekSeoProjectId(projectId);
      setKeyword(kw);
      if (body.topicSlug?.trim()) {
        setResearchTopicSlug(body.topicSlug.trim());
      } else if (kw) {
        setResearchTopicSlug(slugifyResearchTopic(kw));
      }
      setStep("keyword_saved");
      void loadContentPillars(targetSiteUrl);
      void loadResearchFocus(id);
      setStatus({
        kind: "ok",
        text: [
          `Keyword: ${kw || "—"}`,
          `${formatCount(importSummary.organicOnlyCount)} organic · ${formatCount(importSummary.paidCount)} sponsored · ${formatCount(importSummary.paaCount)} PAA/PASF`,
          importSummary.aiOverviewCount > 0
            ? importSummary.aiOverviewAvailable
              ? "AI Overview content captured."
              : "AI Overview block present (no content)."
            : "No AI Overview block.",
          ...keywordImportRankLines(body),
          body.rankingsDelta
            ? "SERP recapture saved. Rank delta updated."
            : "Keyword data saved. Run competitor crawl when ready.",
        ].join("\n"),
      });
    } catch (e) {
      setStatus({ kind: "err", text: e instanceof Error ? e.message : String(e) });
    } finally {
      setParsing(false);
    }
  }

  async function finishCompetitorCrawl(body: CompetitorCrawlProgressPayload) {
    persistUrl();
    const saved = body.competitorSaved === true;
    let totalPages = readCount(body.totalPages);
    let domainCount = readCount(body.domainCount);
    let domains = body.domains ?? [];
    let qualityWarnings = body.qualityWarnings ?? [];
    let message = body.message;

    if (keywordProjectId && (totalPages === 0 || domains.length === 0)) {
      const status = await fetchCrawlProgressStatus(API, keywordProjectId, accessToken);
      if (status) {
        totalPages = readCount(status.totalPages) || totalPages;
        domainCount = readCount(status.domainCount) || domainCount;
        domains = status.domains ?? domains;
        qualityWarnings = status.qualityWarnings ?? qualityWarnings;
        message = status.message ?? message;
      }
    }

    if (!saved) {
      const assemblyError =
        body.assemblyError ??
        (message && !/research pack ready/i.test(message) ? message : null) ??
        body.message;
      console.error("[competitor-crawl] pages saved but research pack incomplete", {
        runId: keywordProjectId,
        crawlStatus: body.crawlStatus,
        competitorSaved: body.competitorSaved,
        message: body.message,
        assemblyError,
        totalPages,
        domainCount,
      });
      setStatus({
        kind: "err",
        text: [
          buildCrawlSummaryMessage({ totalPages, domainCount, message }),
          assemblyError ??
            "Research pack assembly did not complete. Check that the site profile exists and target-site crawl finished.",
          "Keyword data is still saved. Fix issues and run competitor crawl again.",
          ...qualityWarnings,
        ]
          .filter(Boolean)
          .join("\n"),
      });
      return;
    }

    const summary: CompetitorCrawlStorage = {
      keywordProjectId,
      competitorSaved: true,
      totalPages,
      domainCount: domainCount > 0 ? domainCount : domains.length,
      domains,
      qualityWarnings,
      message,
      savedAt: new Date().toISOString(),
    };

    saveCompetitorCrawl(summary);
    setCompetitorCrawlSummary(summary);

    const focus = await waitForResearchPackReady(keywordProjectId);
    if (!focus?.researchReady) {
      const pendingGates = focus?.gates.filter((gate) => !gate.complete).map((gate) => gate.label) ?? [];
      const isManual = focus?.researchMode === "manual";

      if (isManual) {
        setStatus({
          kind: "ok",
          text: [
            buildCrawlSummaryMessage(summary),
            "Competitor crawl saved.",
            pendingGates.length > 0
              ? `Finish manual research lanes before Content Writer: ${pendingGates.join(", ")}.`
              : "Finish remaining manual research lanes before Content Writer.",
          ].join("\n"),
        });
        return;
      }

      console.error("[competitor-crawl] saved but research gates incomplete", {
        runId: keywordProjectId,
        pendingGates,
        packStats: focus?.packStats,
      });
      setStatus({
        kind: "err",
        text: [
          buildCrawlSummaryMessage(summary),
          focus?.packStats.gapTopicCount
            ? "Research pack is partially assembled."
            : "Research pack assembly did not complete.",
          pendingGates.length > 0
            ? `Still needed: ${pendingGates.join(", ")}.`
            : "Run competitor crawl again.",
          focus?.packStats.gapTopicCount
            ? `Gap themes saved: ${focus.packStats.gapTopicCount}.`
            : "",
        ]
          .filter(Boolean)
          .join("\n"),
      });
      return;
    }

    setStep("complete");
    if (normalizedProjectUrl) {
      void loadSiteProfile(normalizedProjectUrl);
      void loadContentPillars(normalizedProjectUrl);
    }
    setStatus({
      kind: "ok",
      text: [
        buildCrawlSummaryMessage(summary),
        "Research pack ready — you can open Content Writer.",
      ].join("\n"),
    });
  }

  function applyCrawlProgress(payload: CompetitorCrawlProgressPayload) {
    if (
      keywordProjectId &&
      String(payload.runId).toLowerCase() !== keywordProjectId.toLowerCase()
    ) {
      return;
    }

    if (payload.crawlStatus === "running") {
      setStatus({
        kind: "info",
        text:
          payload.message ??
          `Competitor crawl running… ${formatCount(readCount(payload.totalPages))} pages saved so far.`,
      });
      return;
    }

    if (payload.crawlStatus === "failed") {
      crawlSettledRef.current = true;
      console.error("[competitor-crawl] failed", payload);
      setStatus({
        kind: "err",
        text: [
          payload.message ?? "Competitor crawl failed.",
          "Keyword data is still saved. Fix issues and run competitor crawl again.",
          ...(payload.qualityWarnings ?? []),
        ]
          .filter(Boolean)
          .join("\n"),
      });
      return;
    }

    if (
      payload.crawlStatus === "pages_saved" ||
      (payload.crawlStatus === "complete" && payload.competitorSaved === true)
    ) {
      crawlSettledRef.current = true;
      void finishCompetitorCrawl(payload);
      return;
    }

    if (payload.crawlStatus === "complete" && payload.competitorSaved !== true) {
      crawlSettledRef.current = true;
      void finishCompetitorCrawl({ ...payload, competitorSaved: false, crawlStatus: "pages_saved" });
    }
  }

  useEffect(() => {
    if (!crawling || !crawlProgress) return;
    applyCrawlProgress(crawlProgress);
  }, [crawlProgress, crawling]);

  async function runCompetitorCrawl() {
    if (!keywordProjectId) {
      setStatus({ kind: "err", text: "Parse a keyword page first." });
      return;
    }

    setCrawling(true);
    crawlSettledRef.current = false;
    setStatus({ kind: "info", text: "Starting competitor crawl…" });

    try {
      const deadline = Date.now() + 12 * 60 * 1000;

      const startRes = await siteAnalyzer2Fetch(
        `/runs/${encodeURIComponent(keywordProjectId)}/competitor-crawl?force=true`,
        accessToken,
        { method: "POST" },
      );
      const startBody = (await startRes.json().catch(() => ({}))) as {
        error?: string;
        crawlStatus?: string;
        message?: string;
        competitorSaved?: boolean;
        totalPages?: number;
        domainCount?: number;
        qualityWarnings?: string[];
      };

      if (!startRes.ok && startRes.status !== 202) {
        throw new Error(startBody.error || startRes.statusText || "Crawl failed to start");
      }

      if (startRes.ok && startBody.competitorSaved) {
        crawlSettledRef.current = true;
        await finishCompetitorCrawl(
          statusResponseToProgress(keywordProjectId, startBody as Record<string, unknown>),
        );
        return;
      }

      const syncStatus = await fetchCrawlProgressStatus(API, keywordProjectId, accessToken);
      if (!syncStatus) {
        throw new Error("Crawl status check failed");
      }
      applyCrawlProgress(statusResponseToProgress(keywordProjectId, syncStatus));
      if (crawlSettledRef.current) return;

      setStatus({
        kind: "info",
        text: startBody.message ?? "Competitor crawl running…",
      });

      while (!crawlSettledRef.current && Date.now() < deadline) {
        await sleep(500);
      }

      if (!crawlSettledRef.current) {
        const finalStatus = await fetchCrawlProgressStatus(API, keywordProjectId, accessToken);
        if (!finalStatus) return;
        if (finalStatus.competitorSaved === true) {
          await finishCompetitorCrawl(statusResponseToProgress(keywordProjectId, finalStatus));
          return;
        }
        if (finalStatus.crawlStatus === "failed") {
          setStatus({
            kind: "err",
            text: [
              finalStatus.message ?? "Competitor crawl failed.",
              "Keyword data is still saved. Fix issues and run competitor crawl again.",
            ].join("\n"),
          });
          return;
        }

        setStatus({
          kind: "err",
          text: "Competitor crawl is still running after 12 minutes. Refresh the page and check again shortly.",
        });
      }
    } catch (e) {
      setStatus({ kind: "err", text: e instanceof Error ? e.message : String(e) });
    } finally {
      setCrawling(false);
    }
  }

  async function copyKeywordProjectId() {
    if (!keywordProjectId) return;
    try {
      await navigator.clipboard.writeText(keywordProjectId);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      setStatus({ kind: "err", text: "Could not copy to clipboard." });
    }
  }

  const statusBannerClass =
    status?.kind === "ok"
      ? "border-[rgba(34,197,94,0.3)] bg-[rgba(34,197,94,0.06)] text-[var(--color-good)]"
      : status?.kind === "err"
        ? "border-[rgba(239,68,68,0.3)] bg-[rgba(239,68,68,0.06)] text-[var(--color-bad)]"
        : "border-[rgba(26,110,191,0.25)] bg-[rgba(26,110,191,0.06)] text-[var(--color-metric-blue)]";

  const showMetrics = keywordSaved && keywordImportSummary;

  const parseDisabledReason = keywordSaved
    ? "Keyword already imported — use Start new keyword to parse another SERP."
    : workflowLocked
      ? "Workflow complete for this keyword."
      : !siteProfileReady
        ? "Create a site profile for this URL first."
        : !file
          ? "Choose a saved Google SERP HTML file."
          : parsing
            ? "Parsing in progress…"
            : crawling
              ? "Wait for competitor crawl to finish."
              : null;

  return (
    <div className="mx-auto flex h-full min-h-0 w-full max-w-[1600px] flex-1 flex-col gap-5 overflow-hidden">
      <header className="flex shrink-0 flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-[var(--color-text-primary)]">
            Site Analyzer
          </h1>
          <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
            Import Google results, crawl competitors, and hand off to Content Writer.
          </p>
        </div>
        {keywordSaved && keywordProjectId ? (
          <div className="flex flex-wrap gap-2">
            {step === "complete" ? (
              <Button type="button" variant="outline" size="sm" onClick={() => void copyKeywordProjectId()}>
                <Copy className="size-3.5" />
                {copied ? "Copied!" : "Copy project ID"}
              </Button>
            ) : null}
            {researchFocus?.researchReady ? (
              <a
                href={contentWriterUrl(keywordProjectId)}
                target="_blank"
                rel="noopener noreferrer"
                className={cn(buttonVariants({ size: "sm" }))}
              >
                <ExternalLink className="size-3.5" />
                Open Content Writer
              </a>
            ) : (
              <Button
                type="button"
                size="sm"
                disabled
                title="Import required research lanes (gov + wiki for customer-journey), or complete competitor crawl"
              >
                <ExternalLink className="size-3.5" />
                Open Content Writer
              </Button>
            )}
          </div>
        ) : null}
      </header>

      <div className="shrink-0">
        <WorkflowStrip
          siteProfileReady={siteProfileReady}
          keywordSaved={keywordSaved}
          step={step}
          researchReady={researchFocus?.researchReady ?? false}
          keyword={keyword}
        />
      </div>

      {showMetrics ? (
        <div className="grid shrink-0 grid-cols-2 gap-3 sm:grid-cols-4">
          <MetricTile
            label="Organic results"
            value={formatCount(keywordImportSummary.organicOnlyCount)}
            hint={`${formatCount(keywordImportSummary.paidCount)} sponsored`}
          />
          <MetricTile
            label="PAA / PASF"
            value={formatCount(keywordImportSummary.paaCount)}
            hint="People also ask"
          />
          <MetricTile
            label="Competitor pages"
            value={
              competitorCrawlSummary
                ? formatCount(competitorCrawlSummary.totalPages)
                : crawling
                  ? "…"
                  : "—"
            }
            hint={
              competitorCrawlSummary
                ? `${formatCount(competitorCrawlSummary.domainCount)} domains`
                : keywordSaved
                  ? "Run competitor crawl"
                  : undefined
            }
          />
          <MetricTile
            label="Gap themes"
            value={
              researchFocus
                ? String(researchFocus.packStats.gapTopicCount)
                : researchFocusLoading
                  ? "…"
                  : "—"
            }
            hint={researchFocus?.researchReady ? "Research ready" : "In progress"}
          />
        </div>
      ) : null}

      {status ? (
        <div
          className={`shrink-0 rounded-[var(--radius-card)] border px-4 py-3 text-sm whitespace-pre-wrap ${statusBannerClass}`}
        >
          {status.text}
        </div>
      ) : null}

      <div className="grid min-h-0 flex-1 grid-cols-12 items-stretch gap-5 overflow-y-auto lg:overflow-hidden">
        <aside className="col-span-12 flex min-h-0 flex-col gap-5 lg:col-span-4 lg:h-full lg:overflow-y-auto">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Globe className="size-4 text-[var(--color-accent)]" />
                Project setup
              </CardTitle>
              <CardDescription>Your site URL — saved in this browser.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="project-url"
                  className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]"
                >
                  Project URL
                </label>
                <input
                  id="project-url"
                  type="url"
                  value={projectUrl}
                  onChange={(e) => setProjectUrl(e.target.value)}
                  onBlur={() => {
                    const normalized = normalizeProjectUrl(projectUrl);
                    if (normalized) setProjectUrl(normalized);
                  }}
                  disabled={workflowLocked || parsing || crawling}
                  placeholder="https://geekatyourspot.com"
                  className="mt-1.5 w-full rounded-[var(--radius-button)] border border-[var(--color-border-strong)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[rgba(59,179,122,0.2)] disabled:opacity-50"
                />
              </div>

              {!siteProfile && normalizedProjectUrl && !loadingSiteProfile ? (
                <p className="text-sm text-[var(--color-text-secondary)]">
                  No site profile yet. Create one to continue.
                </p>
              ) : null}

              <Button
                type="button"
                className="w-full"
                onClick={() => void createSiteProfile()}
                disabled={
                  !normalizedProjectUrl || creatingSiteProfile || parsing || crawling || workflowLocked
                }
                title={!normalizedProjectUrl ? "Enter your site URL first" : undefined}
              >
                {creatingSiteProfile ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    Creating…
                  </>
                ) : (
                  "Create Site Profile"
                )}
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Upload className="size-4 text-[var(--color-metric-blue)]" />
                Keyword import
              </CardTitle>
              <CardDescription>
                Chrome save: Webpage, HTML only — from your Google SERP.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="keyword-html"
                  className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]"
                >
                  Saved Google page
                </label>
                <input
                  id="keyword-html"
                  ref={fileInputRef}
                  type="file"
                  accept=".html,.htm,text/html"
                  disabled={filePickerDisabled}
                  onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                  className="mt-1.5 block w-full text-sm file:mr-3 file:rounded-[var(--radius-button)] file:border-0 file:bg-[var(--color-surface-muted)] file:px-3 file:py-1.5 file:text-sm file:font-medium disabled:opacity-50"
                />
                {file ? (
                  <p className="mt-1.5 truncate text-xs text-[var(--color-text-secondary)]">{file.name}</p>
                ) : null}
              </div>

              <div className="flex flex-col gap-2">
                <Button
                  type="button"
                  className="w-full"
                  onClick={() => void parseKeywordPage()}
                  disabled={
                    !siteProfileReady || !file || parsing || crawling || workflowLocked || keywordSaved
                  }
                  title={parseDisabledReason ?? undefined}
                >
                  {parsing ? (
                    <>
                      <Loader2 className="size-4 animate-spin" />
                      Parsing…
                    </>
                  ) : (
                    "Parse keyword page"
                  )}
                </Button>
                {parseDisabledReason ? (
                  <p className="text-xs text-[var(--color-text-secondary)]">{parseDisabledReason}</p>
                ) : null}
                <Button
                  type="button"
                  variant="outline"
                  className="w-full"
                  onClick={() => void runCompetitorCrawl()}
                  disabled={!keywordSaved || parsing || crawling || workflowLocked}
                >
                  {crawling ? (
                    <>
                      <Loader2 className="size-4 animate-spin" />
                      Crawling…
                    </>
                  ) : (
                    "Competitor crawl"
                  )}
                </Button>
                {keywordSaved ? (
                  <Button
                    type="button"
                    variant="ghost"
                    className="w-full"
                    onClick={startNewKeyword}
                    disabled={parsing || crawling}
                  >
                    Start new keyword
                  </Button>
                ) : null}
              </div>

              {researchFocus && !researchFocus.researchReady ? (
                <p className="text-xs text-[var(--color-bad)]">
                  Import required Google research lanes below (gov + wiki for customer-journey), or
                  complete competitor crawl for the full SA2 path.
                </p>
              ) : null}
              {keywordSaved && !keywordProjectId ? (
                <p className="text-xs text-[var(--color-text-secondary)]">
                  Project id missing from import response — check API logs.
                </p>
              ) : null}
            </CardContent>
          </Card>

          {keywordSaved && keywordProjectId ? (
            <ManualResearchLanesCard
              runId={keywordProjectId}
              accessToken={accessToken}
              topicSlug={researchTopicSlug}
              keyword={keyword}
              topicSlugLocked={Boolean(keywordProjectId)}
              onTopicSlugChange={setResearchTopicSlug}
              gates={researchFocus?.gates}
              researchReady={researchFocus?.researchReady}
              onImported={() => void loadResearchFocus(keywordProjectId)}
            />
          ) : null}
        </aside>

        <aside className="col-span-12 flex min-h-0 flex-col gap-5 lg:col-span-8 lg:h-full lg:overflow-y-auto">
          {siteProfile ? (
            <SiteProfilePanel
              profile={siteProfile}
              loading={loadingSiteProfile}
              expandToken={siteProfileExpandToken}
              contentPillars={contentPillars}
              pillarsLoading={pillarsLoading}
              onRefresh={() => {
                if (normalizedProjectUrl) {
                  void loadSiteProfile(normalizedProjectUrl);
                  void loadContentPillars(normalizedProjectUrl);
                }
              }}
            />
          ) : (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center justify-center py-12 text-center">
                <Globe className="mb-3 size-10 text-[var(--color-text-muted)]" />
                <p className="text-sm font-medium text-[var(--color-text-primary)]">
                  Site profile will appear here
                </p>
                <p className="mt-1 max-w-sm text-sm text-[var(--color-text-secondary)]">
                  Enter your project URL and create a site profile to see business identity, niche tags,
                  and content pillars.
                </p>
              </CardContent>
            </Card>
          )}

          {keywordImportSummary && keywordSaved ? (
            <KeywordImportSummaryPanel keyword={keyword} summary={keywordImportSummary} />
          ) : null}

          {keywordSaved ? (
            <RunResearchFocusPanel focus={researchFocus} loading={researchFocusLoading} />
          ) : null}

          {competitorCrawlSummary && step === "complete" ? (
            <CompetitorCrawlSummaryPanel keyword={keyword} summary={competitorCrawlSummary} />
          ) : null}

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Search className="size-4 text-[var(--color-badge-purple)]" />
                Domain positions
              </CardTitle>
              <CardDescription>
                Optional — not part of the research pack. Uses your SERP import index.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <DomainOverviewPanel
                overview={domainOverview}
                domainInput={domainOverviewInput}
                onDomainInputChange={setDomainOverviewInput}
                onSubmit={() => void runDomainOverviewSearch()}
                loading={domainOverviewSearching}
                analyzing={domainOverviewSearching}
              />
            </CardContent>
          </Card>
        </aside>
      </div>
    </div>
  );
}
