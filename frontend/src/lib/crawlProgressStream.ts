import { siteAnalyzer2Headers } from '@/lib/site-analyzer2-api';

export type CompetitorDomainSummary = {
  domain: string;
  pagesCrawled: number;
};

export type CompetitorCrawlProgressPayload = {
  runId: string;
  sequenceNumber?: number;
  crawlStatus?: string;
  competitorSaved?: boolean;
  totalPages?: number;
  domainCount?: number;
  domains?: CompetitorDomainSummary[];
  message?: string;
  assemblyError?: string;
  qualityWarnings?: string[];
};

export type CrawlStatusResponse = {
  crawlStatus?: string;
  competitorSaved?: boolean;
  totalPages?: number;
  domainCount?: number;
  domains?: CompetitorDomainSummary[];
  message?: string;
  assemblyError?: string;
  qualityWarnings?: string[];
};

export type CrawlProgressCatchupItem = {
  sequenceNumber: number;
  payload: string;
};

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

/** Parse API counts as base-10 integers (handles numeric strings and PascalCase fields). */
export function readCount(value: unknown): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return Math.max(0, Math.trunc(value));
  }

  if (typeof value === "string") {
    const trimmed = value.trim();
    if (!trimmed) return 0;
    const parsed = Number.parseInt(trimmed, 10);
    return Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
  }

  return 0;
}

export function formatCount(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(Math.max(0, value));
}

export function readDomainSummaries(value: unknown): CompetitorDomainSummary[] {
  if (!Array.isArray(value)) return [];

  return value
    .map((item) => {
      if (!item || typeof item !== "object") return null;
      const record = item as Record<string, unknown>;
      const domain = readString(record.domain) ?? readString(record.Domain);
      const pagesCrawled = readCount(record.pagesCrawled ?? record.PagesCrawled);
      if (!domain) return null;
      return { domain, pagesCrawled };
    })
    .filter((item): item is CompetitorDomainSummary => item !== null);
}

export function normalizeCrawlStatusResponse(
  runId: string,
  body: Record<string, unknown>,
): CompetitorCrawlProgressPayload {
  const totalPages = readCount(body.totalPages ?? body.TotalPages);
  const domainCount = readCount(body.domainCount ?? body.DomainCount);
  const domains = readDomainSummaries(body.domains ?? body.Domains);
  const competitorSaved = body.competitorSaved === true || body.CompetitorSaved === true;
  const crawlStatus = readString(body.crawlStatus ?? body.CrawlStatus) ?? undefined;

  return {
    runId,
    crawlStatus,
    competitorSaved,
    totalPages,
    domainCount: domainCount > 0 ? domainCount : domains.length,
    domains,
    message: readString(body.message ?? body.Message) ?? undefined,
    assemblyError: readString(body.assemblyError ?? body.AssemblyError) ?? undefined,
    qualityWarnings: readStringList(body.qualityWarnings ?? body.QualityWarnings),
  };
}

export function buildCrawlProgressStreamUrl(
  apiBaseUrl: string,
  runId: string,
  accessToken?: string | null,
): string {
  const base = `${apiBaseUrl}/runs/${encodeURIComponent(runId)}/competitor-crawl/progress-stream`;
  if (!accessToken) return base;
  const params = new URLSearchParams({ access_token: accessToken });
  return `${base}?${params}`;
}

export function buildCrawlProgressStatusUrl(apiBaseUrl: string, runId: string): string {
  return `${apiBaseUrl}/runs/${encodeURIComponent(runId)}/competitor-crawl/status`;
}

export function buildCrawlProgressCatchupUrl(apiBaseUrl: string, runId: string, lastSeq: number): string {
  const params = new URLSearchParams({ lastSeq: String(lastSeq) });
  return `${apiBaseUrl}/runs/${encodeURIComponent(runId)}/competitor-crawl/progress-catchup?${params}`;
}

export function parseCrawlProgressPayload(raw: string): CompetitorCrawlProgressPayload | null {
  try {
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const runId = readString(parsed.runId) ?? readString(parsed.RunId);
    if (!runId) return null;

    const normalized = normalizeCrawlStatusResponse(runId, parsed);
    const sequenceNumber = readCount(parsed.sequenceNumber ?? parsed.SequenceNumber);
    return sequenceNumber > 0 ? { ...normalized, sequenceNumber } : normalized;
  } catch {
    return null;
  }
}

export function statusResponseToProgress(
  runId: string,
  status: CrawlStatusResponse,
): CompetitorCrawlProgressPayload {
  return normalizeCrawlStatusResponse(runId, status as Record<string, unknown>);
}

export function shouldReplaceCrawlProgress(
  current: CompetitorCrawlProgressPayload | undefined,
  incoming: CompetitorCrawlProgressPayload,
): boolean {
  if (!current) return true;

  const incomingSeq = incoming.sequenceNumber ?? 0;
  const currentSeq = current.sequenceNumber ?? 0;
  if (incomingSeq > 0 && currentSeq > 0 && incomingSeq !== currentSeq) {
    return incomingSeq > currentSeq;
  }

  if (incoming.crawlStatus === "failed") return true;
  if (incoming.crawlStatus === "pages_saved") return true;
  if (incoming.crawlStatus === "complete" && incoming.competitorSaved === true) return true;
  if (current.competitorSaved === true || current.crawlStatus === "failed") return false;

  return (incoming.totalPages ?? 0) >= (current.totalPages ?? 0);
}

export async function fetchCrawlProgressStatus(
  apiBaseUrl: string,
  runId: string,
  accessToken?: string | null,
): Promise<CrawlStatusResponse | null> {
  try {
    const res = await fetch(buildCrawlProgressStatusUrl(apiBaseUrl, runId), {
      headers: siteAnalyzer2Headers(accessToken),
    });
    if (!res.ok) return null;
    const body = (await res.json()) as Record<string, unknown>;
    return normalizeCrawlStatusResponse(runId, body);
  } catch {
    return null;
  }
}

export async function fetchCrawlProgressCatchup(
  apiBaseUrl: string,
  runId: string,
  lastSeq: number,
  accessToken?: string | null,
): Promise<CrawlProgressCatchupItem[]> {
  try {
    const res = await fetch(buildCrawlProgressCatchupUrl(apiBaseUrl, runId, lastSeq), {
      headers: siteAnalyzer2Headers(accessToken),
    });
    if (!res.ok) return [];
    return (await res.json()) as CrawlProgressCatchupItem[];
  } catch {
    return [];
  }
}

export function buildCrawlSummaryMessage(payload: {
  totalPages?: number;
  domainCount?: number;
  message?: string;
}): string {
  const totalPages = readCount(payload.totalPages);
  const domainCount = readCount(payload.domainCount);
  if (totalPages > 0 && domainCount > 0) {
    return `Saved ${formatCount(totalPages)} pages across ${formatCount(domainCount)} competitor domains.`;
  }

  return payload.message?.trim() || "Competitor crawl data saved.";
}
