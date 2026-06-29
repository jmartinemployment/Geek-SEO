"use client";

import { useEffect, useRef, useState, type CSSProperties } from "react";
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
import { contentWritingPath } from "@/lib/content-writing-search-params";
import { getSiteAnalyzer2ApiBase, siteAnalyzer2Fetch } from "@/lib/site-analyzer2-api";

const STORAGE_URL = "siteAnalyzer2.projectUrl";
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
  matchedPillarTopic?: string | null;
  matchedPillarIntent?: string | null;
  matchedPillarAngle?: string | null;
  gapTopics: string[];
  writingInstructions?: string | null;
  researchReady: boolean;
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
    matchedPillarTopic: readString(record.matchedPillarTopic) ?? readString(record.MatchedPillarTopic),
    matchedPillarIntent: readString(record.matchedPillarIntent) ?? readString(record.MatchedPillarIntent),
    matchedPillarAngle: readString(record.matchedPillarAngle) ?? readString(record.MatchedPillarAngle),
    gapTopics: readStringList(record.gapTopics ?? record.GapTopics),
    writingInstructions:
      readString(record.writingInstructions) ?? readString(record.WritingInstructions),
    researchReady: record.researchReady === true || record.ResearchReady === true,
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
    <section
      style={{
        marginTop: "1.25rem",
        padding: "1rem",
        borderRadius: 8,
        border: "1px solid #e4e4e7",
        background: "#fafafa",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", gap: ".75rem", alignItems: "start" }}>
        <div>
          <h2 style={{ fontSize: "1rem", margin: "0 0 .25rem" }}>Site profile</h2>
          <p style={{ margin: 0, fontSize: ".85rem", color: "#52525b" }}>
            {formatText(profile.displayName)} · {profile.siteUrl}
          </p>
          {!expanded && hasBusinessData ? (
            <p style={{ margin: ".35rem 0 0", fontSize: ".8rem", color: "#71717a" }}>
              {formatText(profile.primaryNiche)} · {formatList(profile.nicheTags, "—")}
            </p>
          ) : null}
          {!expanded && contentPillars.length > 0 ? (
            <p style={{ margin: ".25rem 0 0", fontSize: ".78rem", color: "#71717a" }}>
              {contentPillars.length} content pillar{contentPillars.length === 1 ? "" : "s"}
            </p>
          ) : null}
        </div>
        <div style={{ display: "flex", gap: ".4rem", flexShrink: 0 }}>
          <button
            type="button"
            onClick={toggleExpanded}
            style={{
              padding: ".35rem .65rem",
              borderRadius: 6,
              border: "1px solid #d4d4d8",
              background: "#fff",
              fontSize: ".8rem",
              cursor: "pointer",
            }}
          >
            {expanded ? "Hide" : "Show"}
          </button>
          <button
            type="button"
            onClick={onRefresh}
            disabled={loading}
            style={{
              padding: ".35rem .65rem",
              borderRadius: 6,
              border: "1px solid #d4d4d8",
              background: "#fff",
              fontSize: ".8rem",
              cursor: loading ? "not-allowed" : "pointer",
              opacity: loading ? 0.6 : 1,
            }}
          >
            {loading ? "Refreshing…" : "Refresh"}
          </button>
        </div>
      </div>

      {expanded ? (
        <>
          <dl
            style={{
              margin: ".85rem 0 0",
              display: "grid",
              gridTemplateColumns: "8.5rem 1fr",
              gap: ".45rem .75rem",
              fontSize: ".85rem",
            }}
          >
            <dt style={{ color: "#71717a", margin: 0 }}>Business profile</dt>
            <dd style={{ margin: 0 }}>
              {hasBusinessData
                ? profile.businessProfileAt
                  ? `Assembled ${formatWhen(profile.businessProfileAt)}`
                  : "Ready"
                : "Not assembled yet — click Create Site Profile"}
            </dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Business type</dt>
            <dd style={{ margin: 0 }}>{formatText(profile.businessType)}</dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Summary</dt>
            <dd style={{ margin: 0 }}>{formatText(profile.businessSummary ?? profile.businessDescription)}</dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Business identity</dt>
            <dd style={{ margin: 0 }}>{formatText(profile.primaryNiche)}</dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Niche description</dt>
            <dd style={{ margin: 0 }}>{formatText(profile.nicheDescription)}</dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Site themes</dt>
            <dd style={{ margin: 0 }}>{formatList(profile.nicheTags)}</dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Geo anchors</dt>
            <dd style={{ margin: 0 }}>{formatList(profile.geoAnchorNodes)}</dd>

            <dt style={{ color: "#71717a", margin: 0 }}>Service area</dt>
            <dd style={{ margin: 0 }}>{formatText(profile.serviceAreaDescription)}</dd>

            <dt style={{ color: "#71717a", margin: 0, alignSelf: "start" }}>Writing recommendations</dt>
            <dd style={{ margin: 0 }}>
              {profile.writingRecommendations.length > 0 ? (
                <ul style={{ margin: 0, paddingLeft: "1.1rem" }}>
                  {profile.writingRecommendations.map((item) => (
                    <li key={item} style={{ marginBottom: ".35rem" }}>
                      {item}
                    </li>
                  ))}
                </ul>
              ) : (
                <span style={{ color: "#71717a" }}>Available after Create Site Profile</span>
              )}
            </dd>

            <dt style={{ color: "#71717a", margin: 0, alignSelf: "start" }}>Content pillars</dt>
            <dd style={{ margin: 0 }}>
              {pillarsLoading ? (
                <span style={{ color: "#71717a" }}>Loading…</span>
              ) : contentPillars.length > 0 ? (
                <ul style={{ margin: 0, paddingLeft: "1.1rem" }}>
                  {contentPillars.map((pillar) => (
                    <li key={pillar.runId} style={{ marginBottom: ".35rem" }}>
                      <strong>{pillar.keyword}</strong>
                      <span style={{ color: "#71717a", fontSize: ".78rem" }}>
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
                <span style={{ color: "#71717a" }}>Saved keywords appear here after SERP import</span>
              )}
            </dd>
          </dl>

          {profile.recommendedHomepageJsonLd.length > 0 ? (
            <div style={{ marginTop: "1rem" }}>
              <h3 style={{ fontSize: ".9rem", margin: "0 0 .35rem" }}>Recommended homepage JSON-LD</h3>
              <p style={{ margin: "0 0 .75rem", fontSize: ".8rem", color: "#71717a" }}>
                Copy each block into your homepage &lt;head&gt;. Content pages get a separate TechArticle block from
                Content Writer.
              </p>
              {profile.recommendedHomepageJsonLd.map((snippet) => (
                <div
                  key={snippet.id}
                  style={{
                    marginBottom: ".85rem",
                    padding: ".75rem",
                    borderRadius: 6,
                    border: "1px solid #e4e4e7",
                    background: "#fff",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      gap: ".75rem",
                      alignItems: "start",
                      marginBottom: ".45rem",
                    }}
                  >
                    <div>
                      <p style={{ margin: 0, fontSize: ".85rem", fontWeight: 600 }}>{snippet.title}</p>
                      {snippet.description ? (
                        <p style={{ margin: ".2rem 0 0", fontSize: ".78rem", color: "#71717a" }}>
                          {snippet.description}
                        </p>
                      ) : null}
                    </div>
                    <button
                      type="button"
                      onClick={() => void copySnippet(snippet)}
                      style={{
                        padding: ".3rem .55rem",
                        borderRadius: 6,
                        border: "1px solid #d4d4d8",
                        background: "#fafafa",
                        fontSize: ".75rem",
                        cursor: "pointer",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {copiedSnippetId === snippet.id ? "Copied" : "Copy script"}
                    </button>
                  </div>
                  <pre
                    style={{
                      margin: 0,
                      padding: ".65rem",
                      borderRadius: 4,
                      background: "#f4f4f5",
                      fontSize: ".72rem",
                      lineHeight: 1.45,
                      overflowX: "auto",
                      whiteSpace: "pre-wrap",
                      wordBreak: "break-word",
                    }}
                  >
                    {snippet.scriptTag}
                  </pre>
                </div>
              ))}
            </div>
          ) : null}
        </>
      ) : null}
    </section>
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
    <section
      style={{
        marginTop: "1rem",
        padding: "1rem",
        borderRadius: 8,
        border: "1px solid #bfdbfe",
        background: "#eff6ff",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", gap: ".75rem", alignItems: "start" }}>
        <div>
          <h2 style={{ fontSize: "1rem", margin: "0 0 .25rem", color: "#1e3a8a" }}>Keyword import saved</h2>
          <p style={{ margin: 0, fontSize: ".85rem", color: "#1d4ed8" }}>
            {formatCount(summary.organicOnlyCount)} organic · {formatCount(summary.paidCount)} sponsored
            {aiLabel ? ` · ${aiLabel}` : ""} · {formatCount(summary.paaCount)} PAA/PASF
          </p>
          {filterLine ? (
            <p style={{ margin: ".25rem 0 0", fontSize: ".82rem", color: "#1e40af" }}>
              Relevance filter: {filterLine}
            </p>
          ) : null}
          {keyword ? (
            <p style={{ margin: ".25rem 0 0", fontSize: ".8rem", color: "#2563eb" }}>
              Keyword: {keyword}
            </p>
          ) : null}
          <p style={{ margin: ".25rem 0 0", fontSize: ".78rem", color: "#6b7280" }}>
            Saved {formatWhen(summary.savedAt)}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setExpanded((value) => !value)}
          style={{
            padding: ".35rem .65rem",
            borderRadius: 6,
            border: "1px solid #93c5fd",
            background: "#fff",
            fontSize: ".8rem",
            cursor: "pointer",
          }}
        >
          {expanded ? "Hide" : "Show"}
        </button>
      </div>

      {expanded ? (
        <div style={{ marginTop: ".75rem", fontSize: ".82rem", color: "#1e40af" }}>
          <p style={{ margin: "0 0 .5rem", fontWeight: 600 }}>What was saved</p>
          <ul style={{ margin: 0, paddingLeft: "1.1rem" }}>
            <li>
              {formatCount(summary.organicOnlyCount)} organic results (URLs stored for competitor crawl seeds)
            </li>
            <li>{formatCount(summary.paidCount)} sponsored results (saved, not used for crawl)</li>
            <li>
              {summary.aiOverviewCount > 0
                ? aiLabel
                : "No AI Overview block detected"}
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
          <p style={{ margin: ".75rem 0 0", fontSize: ".78rem", color: "#64748b" }}>
            Competitor crawl uses {formatCount(summary.competitorCrawlSeedCount)} included seed URL
            {summary.competitorCrawlSeedCount === 1 ? "" : "s"} (one ranking page per domain).
          </p>
        </div>
      ) : null}
    </section>
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
      <section
        style={{
          marginTop: "1rem",
          padding: "1rem",
          borderRadius: 8,
          border: "1px solid #e4e4e7",
          background: "#fafafa",
          fontSize: ".85rem",
          color: "#71717a",
        }}
      >
        Loading research focus…
      </section>
    );
  }

  if (!focus) return null;

  const readyColor = focus.researchReady ? "#065f46" : "#92400e";
  const readyBg = focus.researchReady ? "#f0fdf4" : "#fffbeb";
  const readyBorder = focus.researchReady ? "#a7f3d0" : "#fde68a";

  return (
    <section
      style={{
        marginTop: "1rem",
        padding: "1rem",
        borderRadius: 8,
        border: `1px solid ${readyBorder}`,
        background: readyBg,
      }}
    >
      <h2 style={{ fontSize: "1rem", margin: "0 0 .25rem", color: readyColor }}>
        {focus.researchReady ? "Research ready" : "Research in progress"}
      </h2>
      <p style={{ margin: "0 0 .5rem", fontSize: ".82rem", color: readyColor }}>
        Research pack for Content Writer — outlines and scoring happen in Writer, not here.
      </p>
      <p style={{ margin: "0 0 .75rem", fontSize: ".85rem", color: readyColor }}>
        Pillar: <strong>{focus.keyword}</strong>
        {focus.matchedPillarIntent ? ` · ${focus.matchedPillarIntent} intent` : ""}
      </p>

      <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#3f3f46" }}>
        Pack stats
      </p>
      <ul style={{ margin: "0 0 .85rem", paddingLeft: "1.1rem", fontSize: ".82rem", color: "#27272a" }}>
        <li>PAA questions: {focus.packStats.paaQuestionCount}</li>
        <li>Competitor pages: {focus.packStats.competitorPageCount}</li>
        <li>Competitor headings: {focus.packStats.competitorHeadingCount}</li>
        <li>Your page headings: {focus.packStats.sourceHeadingCount}</li>
        <li>Gap themes: {focus.packStats.gapTopicCount}</li>
      </ul>

      {focus.rankings.history.length > 0 ? (
        <>
          <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#3f3f46" }}>
            Rankings loop
          </p>
          {focus.rankings.hasRecapture && focus.rankings.latestDelta ? (
            <p style={{ margin: "0 0 .5rem", fontSize: ".82rem", color: "#27272a" }}>
              Latest recapture:{" "}
              <strong>
                {formatRankPosition(focus.rankings.latestDelta.previousPosition)} →{" "}
                {formatRankPosition(focus.rankings.latestDelta.currentPosition)}
              </strong>{" "}
              ({formatRankDelta(focus.rankings.latestDelta.positionChange ?? null)})
            </p>
          ) : (
            <p style={{ margin: "0 0 .5rem", fontSize: ".82rem", color: "#57534e" }}>
              Baseline captured. Re-import SERP HTML later to see position delta.
            </p>
          )}
          <ul style={{ margin: "0 0 .85rem", paddingLeft: "1.1rem", fontSize: ".82rem", color: "#27272a" }}>
            {focus.rankings.history.map((snapshot) => (
              <li key={snapshot.importSequence}>
                Import #{snapshot.importSequence}: {formatRankPosition(snapshot.targetPosition)}
                {snapshot.targetUrl ? ` · ${snapshot.targetUrl}` : ""}
              </li>
            ))}
          </ul>
        </>
      ) : null}

      <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#3f3f46" }}>
        Workflow gates
      </p>
      <ul style={{ margin: "0 0 .85rem", paddingLeft: "1.1rem", fontSize: ".82rem" }}>
        {focus.gates.map((gate) => (
          <li key={gate.id} style={{ marginBottom: ".25rem", color: gate.complete ? "#065f46" : "#78716c" }}>
            {gate.complete ? "✓" : "○"} {gate.label}
          </li>
        ))}
      </ul>

      {focus.gapTopics.length > 0 ? (
        <>
          <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#3f3f46" }}>
            Gap themes
          </p>
          <p style={{ margin: "0 0 .75rem", fontSize: ".82rem", color: "#27272a" }}>
            {formatList(focus.gapTopics)}
          </p>
        </>
      ) : null}

      {focus.writingInstructions ? (
        <>
          <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#3f3f46" }}>
            Writing brief
          </p>
          <p style={{ margin: 0, fontSize: ".82rem", color: "#27272a", lineHeight: 1.45 }}>
            {focus.writingInstructions}
          </p>
        </>
      ) : null}

      {!focus.researchReady ? (
        <p style={{ margin: ".75rem 0 0", fontSize: ".78rem", color: "#991b1b" }}>
          Research pack is not ready. Competitor crawl must finish with all gates complete before Content Writer handoff.
        </p>
      ) : null}
    </section>
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
    <section
      style={{
        marginTop: "1rem",
        padding: "1rem",
        borderRadius: 8,
        border: "1px solid #d1fae5",
        background: "#f0fdf4",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", gap: ".75rem", alignItems: "start" }}>
        <div>
          <h2 style={{ fontSize: "1rem", margin: "0 0 .25rem", color: "#065f46" }}>Competitor crawl complete</h2>
          <p style={{ margin: 0, fontSize: ".85rem", color: "#047857" }}>
            {formatCount(summary.totalPages)} pages across {formatCount(summary.domainCount)} competitor domains
          </p>
          {keyword ? (
            <p style={{ margin: ".25rem 0 0", fontSize: ".8rem", color: "#059669" }}>
              Keyword: {keyword}
            </p>
          ) : null}
          <p style={{ margin: ".25rem 0 0", fontSize: ".78rem", color: "#6b7280" }}>
            Saved {formatWhen(summary.savedAt)}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setExpanded((value) => !value)}
          style={{
            padding: ".35rem .65rem",
            borderRadius: 6,
            border: "1px solid #a7f3d0",
            background: "#fff",
            fontSize: ".8rem",
            cursor: "pointer",
          }}
        >
          {expanded ? "Hide" : "Show"}
        </button>
      </div>

      {expanded ? (
        <>
          {summary.qualityWarnings.length > 0 ? (
            <div style={{ marginTop: ".75rem" }}>
              <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#92400e" }}>
                Quality warnings
              </p>
              <ul style={{ margin: 0, paddingLeft: "1.1rem", fontSize: ".8rem", color: "#78350f" }}>
                {summary.qualityWarnings.map((warning) => (
                  <li key={warning} style={{ marginBottom: ".25rem" }}>
                    {warning}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          {domainRows.length > 0 ? (
            <div style={{ marginTop: ".75rem" }}>
              <p style={{ margin: "0 0 .35rem", fontSize: ".8rem", fontWeight: 600, color: "#065f46" }}>
                Domains crawled
              </p>
              <ul
                style={{
                  margin: 0,
                  padding: 0,
                  listStyle: "none",
                  fontSize: ".82rem",
                  color: "#064e3b",
                }}
              >
                {domainRows.map((row) => (
                  <li
                    key={row.domain}
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      gap: ".75rem",
                      padding: ".3rem 0",
                      borderBottom: "1px solid #d1fae5",
                    }}
                  >
                    <span>{row.domain}</span>
                    <span>{formatCount(row.pagesCrawled)} pages</span>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </>
      ) : null}
    </section>
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
  const [step, setStep] = useState<Step>("idle");
  const [status, setStatus] = useState<Status | null>(null);
  const [siteProfileReady, setSiteProfileReady] = useState(false);
  const [confirmedSiteUrl, setConfirmedSiteUrl] = useState("");
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
  const workflowLocked = step === "complete";
  const keywordSaved = step === "keyword_saved" || step === "complete";
  const filePickerDisabled = keywordSaved || parsing || crawling || workflowLocked;

  useEffect(() => {
    setProjectUrl(localStorage.getItem(STORAGE_URL) ?? "");
    setUrlHydrated(true);
  }, []);

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
    if (confirmedSiteUrl && normalizedProjectUrl !== confirmedSiteUrl) {
      setSiteProfileReady(false);
      setConfirmedSiteUrl("");
      setSiteProfile(null);
    }
  }, [confirmedSiteUrl, normalizedProjectUrl]);

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

  function applyLoadedProfile(profile: SiteProfile, siteUrl: string) {
    setSiteProfile(profile);
    if (profileHasBusinessData(profile)) {
      setSiteProfileReady(true);
      setConfirmedSiteUrl(siteUrl);
    }
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
      return focus;
    } catch {
      setResearchFocus(null);
      return null;
    } finally {
      setResearchFocusLoading(false);
    }
  }

  async function waitForResearchPackReady(runId: string, maxAttempts = 12): Promise<RunResearchFocus | null> {
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const focus = await loadResearchFocus(runId);
      if (focus?.researchReady) return focus;
      if (attempt < maxAttempts - 1) {
        await new Promise((resolve) => setTimeout(resolve, 2000));
      }
    }
    return null;
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

      applyLoadedProfile(profile, siteUrl);
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
    setSiteProfileReady(false);
    setConfirmedSiteUrl("");
    setSiteProfile(null);
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
      applyLoadedProfile(profile, siteUrl);
      setSiteProfileExpandToken((token) => token + 1);
      setStatus({
        kind: "ok",
        text: body.created
          ? `Site profile created and assembled for ${displayName}.`
          : "Site profile refreshed from homepage.",
      });
    } catch (e) {
      setSiteProfileReady(false);
      setConfirmedSiteUrl("");
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
    const saved = body.competitorSaved === true;
    if (!saved) {
      setStatus({
        kind: "err",
        text: [
          body.message ?? "Competitor crawl data was not saved.",
          "Keyword data is still saved. Fix issues and run competitor crawl again.",
          ...(body.qualityWarnings ?? []),
        ].join("\n"),
      });
      return;
    }

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
      setStatus({
        kind: "err",
        text: [
          buildCrawlSummaryMessage(summary),
          "Research pack assembly did not complete. Run competitor crawl again.",
        ].join("\n"),
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

    if (payload.competitorSaved === true || payload.crawlStatus === "complete") {
      crawlSettledRef.current = true;
      void finishCompetitorCrawl(payload);
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
        `/runs/${encodeURIComponent(keywordProjectId)}/competitor-crawl`,
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

  const statusStyle =
    status?.kind === "ok"
      ? { background: "#ecfdf5", border: "1px solid #a7f3d0", color: "#065f46" }
      : status?.kind === "err"
        ? { background: "#fef2f2", border: "1px solid #fecaca", color: "#991b1b" }
        : { background: "#eff6ff", border: "1px solid #bfdbfe", color: "#1e40af" };

  const btnPrimary: CSSProperties = {
    padding: ".55rem 1rem",
    borderRadius: 8,
    border: "none",
    background: "#18181b",
    color: "#fff",
    fontWeight: 600,
    cursor: "pointer",
  };

  const btnSecondary: CSSProperties = {
    ...btnPrimary,
    background: "#e4e4e7",
    color: "#18181b",
  };

  const disabled = (on: boolean): CSSProperties => ({
    opacity: on ? 0.5 : 1,
    cursor: on ? "not-allowed" : "pointer",
  });

  return (
    <>
      <main style={{ maxWidth: "32rem", margin: "0 auto", padding: "0 1rem" }}>
      <h1 style={{ fontSize: "1.35rem", margin: "0 0 .25rem" }}>Site Analyzer</h1>
      <p style={{ color: "#52525b", fontSize: ".9rem", marginBottom: "1.25rem" }}>
        Import Google results, crawl competitors, feed Content Writer.
      </p>

      <label style={{ display: "block", fontSize: ".85rem", fontWeight: 600, marginTop: "1rem" }}>
        Project URL
      </label>
      <input
        type="url"
        value={projectUrl}
        onChange={(e) => setProjectUrl(e.target.value)}
        onBlur={() => {
          const normalized = normalizeProjectUrl(projectUrl);
          if (normalized) setProjectUrl(normalized);
        }}
        disabled={workflowLocked || parsing || crawling}
        placeholder="https://geekatyourspot.com"
        style={{ width: "100%", padding: ".55rem", marginTop: ".35rem", boxSizing: "border-box" }}
      />
      <p style={{ fontSize: ".8rem", color: "#71717a", marginTop: ".3rem" }}>
        Your Geek-SEO site URL — saved in this browser.
      </p>

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
      ) : normalizedProjectUrl && !loadingSiteProfile ? (
        <p style={{ marginTop: "1rem", fontSize: ".85rem", color: "#71717a" }}>
          No site profile for this URL yet. Click Create Site Profile to register it.
        </p>
      ) : null}

      <label style={{ display: "block", fontSize: ".85rem", fontWeight: 600, marginTop: "1rem" }}>
        Saved Google page (HTML only)
      </label>
      <input
        ref={fileInputRef}
        type="file"
        accept=".html,.htm,text/html"
        disabled={filePickerDisabled}
        onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        style={{ marginTop: ".35rem" }}
      />

      <div style={{ display: "flex", flexWrap: "wrap", gap: ".6rem", marginTop: "1.25rem" }}>
        <button
          type="button"
          onClick={() => void createSiteProfile()}
          disabled={!normalizedProjectUrl || creatingSiteProfile || parsing || crawling || workflowLocked}
          title={!normalizedProjectUrl ? "Enter your site URL first" : undefined}
          style={{
            ...btnPrimary,
            ...disabled(!normalizedProjectUrl || creatingSiteProfile || parsing || crawling || workflowLocked),
          }}
        >
          {creatingSiteProfile ? "Creating…" : "Create Site Profile"}
        </button>
        <button
          type="button"
          onClick={() => void parseKeywordPage()}
          disabled={!siteProfileReady || !file || parsing || crawling || workflowLocked || keywordSaved}
          style={{
            ...btnPrimary,
            ...disabled(!siteProfileReady || !file || parsing || crawling || workflowLocked || keywordSaved),
          }}
        >
          {parsing ? "Parsing…" : "Parse keyword page"}
        </button>
        <button
          type="button"
          onClick={() => void runCompetitorCrawl()}
          disabled={!keywordSaved || parsing || crawling || workflowLocked}
          style={{
            ...btnSecondary,
            ...disabled(!keywordSaved || parsing || crawling || workflowLocked),
          }}
        >
          {crawling ? "Crawling…" : "Competitor crawl"}
        </button>
        {keywordSaved ? (
          <button
            type="button"
            onClick={startNewKeyword}
            disabled={parsing || crawling}
            style={{ ...btnSecondary, ...disabled(parsing || crawling) }}
          >
            Start new keyword
          </button>
        ) : null}
      </div>

      {keywordImportSummary && keywordSaved ? (
        <KeywordImportSummaryPanel keyword={keyword} summary={keywordImportSummary} />
      ) : null}

      {keywordSaved ? (
        <RunResearchFocusPanel focus={researchFocus} loading={researchFocusLoading} />
      ) : null}

      {competitorCrawlSummary && step === "complete" ? (
        <CompetitorCrawlSummaryPanel keyword={keyword} summary={competitorCrawlSummary} />
      ) : null}

      {keywordSaved ? (
        <div style={{ display: "flex", flexWrap: "wrap", gap: ".6rem", marginTop: ".75rem" }}>
          {step === "complete" ? (
            <button
              type="button"
              onClick={() => void copyKeywordProjectId()}
              style={{ ...btnSecondary, ...disabled(!keywordProjectId) }}
            >
              {copied ? "Copied!" : "Copy Keyword Project ID"}
            </button>
          ) : null}
          {keywordProjectId ? (
            <>
              {researchFocus?.researchReady ? (
                <a
                  href={contentWriterUrl(keywordProjectId)}
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{
                    ...btnPrimary,
                    textDecoration: "none",
                    display: "inline-block",
                    lineHeight: "1.25rem",
                  }}
                >
                  Open Content Writer
                </a>
              ) : (
                <button
                  type="button"
                  disabled
                  title="Complete all research gates before handoff"
                  style={{
                    ...btnPrimary,
                    ...disabled(true),
                  }}
                >
                  Open Content Writer
                </button>
              )}
              {researchFocus && !researchFocus.researchReady ? (
                <p style={{ fontSize: ".78rem", color: "#991b1b", margin: ".35rem 0 0", width: "100%" }}>
                  Research pack not ready — competitor crawl must complete with all gates before handoff.
                </p>
              ) : null}
            </>
          ) : (
            <p style={{ fontSize: ".8rem", color: "#71717a", margin: 0 }}>
              Project id missing from import response — check Api logs.
            </p>
          )}
        </div>
      ) : null}

      {status ? (
        <p
          style={{
            marginTop: "1.25rem",
            padding: ".75rem 1rem",
            borderRadius: 8,
            fontSize: ".9rem",
            whiteSpace: "pre-wrap",
            ...statusStyle,
          }}
        >
          {status.text}
        </p>
      ) : null}
      </main>

      <details
        style={{
          maxWidth: "32rem",
          margin: "1.5rem auto 2rem",
          padding: "0 1rem",
          fontSize: ".85rem",
          color: "#52525b",
        }}
      >
        <summary style={{ cursor: "pointer", fontWeight: 600, color: "#3f3f46" }}>
          Domain positions (optional)
        </summary>
        <p style={{ margin: ".5rem 0 .75rem", fontSize: ".78rem", color: "#71717a" }}>
          Deferred — not part of the Frase research pack. Uses your SERP import index.
        </p>
        <DomainOverviewPanel
          overview={domainOverview}
          domainInput={domainOverviewInput}
          onDomainInputChange={setDomainOverviewInput}
          onSubmit={() => void runDomainOverviewSearch()}
          loading={domainOverviewSearching}
          analyzing={domainOverviewSearching}
        />
      </details>
    </>
  );
}
