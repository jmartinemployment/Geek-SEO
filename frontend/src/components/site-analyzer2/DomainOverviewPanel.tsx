"use client";

import { useMemo, useState, type CSSProperties } from "react";

export type DomainOrganicPosition = {
  keyword: string;
  position: number;
  intent?: string | null;
  traffic?: number | null;
  trafficPercent?: number | null;
  volume?: number | null;
  keywordDifficulty?: number | null;
  url: string;
  serpFeatures?: string | null;
  pathMatch?: string | null;
  updatedAt?: string | null;
};

export type DomainOverview = {
  domain: string;
  siteRootUrl: string;
  analyzedUrl: string;
  requestedInput: string;
  scope: string;
  pageFetched: boolean;
  pageTitle?: string | null;
  metaDescription?: string | null;
  h2Count?: number | null;
  schemaTypes?: string[];
  httpStatus?: number | null;
  authorityScore?: number | null;
  organicTrafficEstimate?: number | null;
  organicTrafficCost?: number | null;
  organicKeywordsCount?: number | null;
  referringDomainsCount?: number | null;
  keywordsChangePercent?: number | null;
  trafficChangePercent?: number | null;
  trafficCostChangePercent?: number | null;
  totalPositionsCount?: number | null;
  researchImportCount?: number | null;
  positions?: DomainOrganicPosition[];
  message?: string | null;
  warnings?: string[];
};

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

function readCount(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function readOptionalNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function readStringList(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string" && item.trim().length > 0);
}

export function normalizeDomainOverview(value: unknown): DomainOverview | null {
  if (!value || typeof value !== "object") return null;
  const record = value as Record<string, unknown>;
  const domain = readString(record.domain) ?? readString(record.Domain);
  const siteRootUrl =
    readString(record.siteRootUrl) ??
    readString(record.SiteRootUrl) ??
    readString(record.siteUrl) ??
    readString(record.SiteUrl);
  const analyzedUrl =
    readString(record.analyzedUrl) ?? readString(record.AnalyzedUrl) ?? siteRootUrl;
  if (!domain || !siteRootUrl || !analyzedUrl) return null;

  const rawPositions = record.positions ?? record.Positions;
  const positions = (Array.isArray(rawPositions) ? rawPositions : [])
    .map((row): DomainOrganicPosition | null => {
      if (!row || typeof row !== "object") return null;
      const k = row as Record<string, unknown>;
      const keyword = readString(k.keyword) ?? readString(k.Keyword);
      const url = readString(k.url) ?? readString(k.Url);
      if (!keyword || !url) return null;
      return {
        keyword,
        position: readCount(k.position ?? k.Position),
        intent: readString(k.intent) ?? readString(k.Intent),
        traffic: readOptionalNumber(k.traffic ?? k.Traffic),
        trafficPercent: readOptionalNumber(k.trafficPercent ?? k.TrafficPercent),
        volume: readOptionalNumber(k.volume ?? k.Volume),
        keywordDifficulty: readOptionalNumber(k.keywordDifficulty ?? k.KeywordDifficulty),
        url,
        serpFeatures: readString(k.serpFeatures) ?? readString(k.SerpFeatures),
        pathMatch: readString(k.pathMatch) ?? readString(k.PathMatch),
        updatedAt: readString(k.updatedAt) ?? readString(k.UpdatedAt),
      };
    })
    .filter((row): row is DomainOrganicPosition => row !== null);

  const scope = readString(record.scope) ?? readString(record.Scope) ?? "domain";

  return {
    domain,
    siteRootUrl,
    analyzedUrl,
    requestedInput:
      readString(record.requestedInput) ?? readString(record.RequestedInput) ?? siteRootUrl,
    scope,
    pageFetched:
      record.pageFetched === true ||
      record.PageFetched === true ||
      record.homepageFetched === true ||
      record.HomepageFetched === true,
    pageTitle: readString(record.pageTitle) ?? readString(record.PageTitle),
    metaDescription: readString(record.metaDescription) ?? readString(record.MetaDescription),
    h2Count: readOptionalNumber(record.h2Count ?? record.H2Count),
    schemaTypes: readStringList(record.schemaTypes ?? record.SchemaTypes),
    httpStatus: readOptionalNumber(record.httpStatus ?? record.HttpStatus),
    authorityScore: readOptionalNumber(record.authorityScore ?? record.AuthorityScore),
    organicTrafficEstimate: readOptionalNumber(
      record.organicTrafficEstimate ?? record.OrganicTrafficEstimate,
    ),
    organicTrafficCost: readOptionalNumber(record.organicTrafficCost ?? record.OrganicTrafficCost),
    organicKeywordsCount: readOptionalNumber(
      record.organicKeywordsCount ?? record.OrganicKeywordsCount,
    ),
    referringDomainsCount: readOptionalNumber(
      record.referringDomainsCount ?? record.ReferringDomainsCount,
    ),
    keywordsChangePercent: readOptionalNumber(
      record.keywordsChangePercent ?? record.KeywordsChangePercent,
    ),
    trafficChangePercent: readOptionalNumber(
      record.trafficChangePercent ?? record.TrafficChangePercent,
    ),
    trafficCostChangePercent: readOptionalNumber(
      record.trafficCostChangePercent ?? record.TrafficCostChangePercent,
    ),
    totalPositionsCount: readOptionalNumber(
      record.totalPositionsCount ?? record.TotalPositionsCount,
    ),
    researchImportCount: readOptionalNumber(
      record.researchImportCount ?? record.ResearchImportCount,
    ),
    positions,
    message: readString(record.message) ?? readString(record.Message),
    warnings: readStringList(record.warnings ?? record.Warnings),
  };
}

function formatCompact(value: number | null | undefined, prefix = ""): string {
  if (value === null || value === undefined) return "—";
  const abs = Math.abs(value);
  if (abs >= 1_000_000) return `${prefix}${(value / 1_000_000).toFixed(1)}M`;
  if (abs >= 1_000) return `${prefix}${(value / 1_000).toFixed(1)}K`;
  return `${prefix}${value.toLocaleString()}`;
}

function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) return "";
  const sign = value > 0 ? "+" : "";
  return `${sign}${value.toFixed(2)}%`;
}

function formatTableNumber(value: number | null | undefined): string {
  if (value === null || value === undefined) return "—";
  return value.toLocaleString();
}

function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "—";
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric" });
}

const intentStyle: Record<string, CSSProperties> = {
  N: { background: "#dbeafe", color: "#1d4ed8" },
  T: { background: "#dcfce7", color: "#15803d" },
  C: { background: "#ffedd5", color: "#c2410c" },
  I: { background: "#f3e8ff", color: "#7e22ce" },
};

function IntentBadge({ intent }: { intent?: string | null }) {
  if (!intent) return <span style={{ color: "#a1a1aa" }}>—</span>;
  const key = intent.trim().charAt(0).toUpperCase();
  const style = intentStyle[key] ?? { background: "#f4f4f5", color: "#52525b" };
  return (
    <span
      title={intent}
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: "1.35rem",
        height: "1.35rem",
        borderRadius: 4,
        fontSize: ".7rem",
        fontWeight: 700,
        ...style,
      }}
    >
      {key}
    </span>
  );
}

function KdDot({ kd }: { kd?: number | null }) {
  if (kd === null || kd === undefined) return <span>—</span>;
  const color = kd < 30 ? "#22c55e" : kd < 60 ? "#eab308" : "#ef4444";
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: ".3rem" }}>
      <span
        style={{
          width: ".45rem",
          height: ".45rem",
          borderRadius: "50%",
          background: color,
          flexShrink: 0,
        }}
      />
      {kd}%
    </span>
  );
}

function ChangePill({ value }: { value?: number | null }) {
  if (value === null || value === undefined) return null;
  const negative = value < 0;
  return (
    <span
      style={{
        fontSize: ".72rem",
        fontWeight: 600,
        color: negative ? "#dc2626" : "#16a34a",
      }}
    >
      {formatPercent(value)}
    </span>
  );
}

function MetricCard({
  label,
  value,
  change,
  currency,
}: {
  label: string;
  value: number | null | undefined;
  change?: number | null;
  currency?: boolean;
}) {
  return (
    <div
      style={{
        flex: "1 1 10rem",
        padding: "1rem 1.1rem",
        border: "1px solid #e4e4e7",
        borderRadius: 8,
        background: "#fff",
      }}
    >
      <div style={{ fontSize: ".72rem", color: "#71717a", textTransform: "uppercase", letterSpacing: ".03em" }}>
        {label}
      </div>
      <div style={{ display: "flex", alignItems: "baseline", gap: ".5rem", marginTop: ".35rem" }}>
        <span style={{ fontSize: "1.65rem", fontWeight: 700, color: "#18181b", lineHeight: 1 }}>
          {formatCompact(value, currency ? "$" : "")}
        </span>
        <ChangePill value={change} />
      </div>
    </div>
  );
}

function PathMatchBadge({ match }: { match?: string | null }) {
  if (!match) return <span style={{ color: "#a1a1aa" }}>—</span>;
  const style =
    match === "exact"
      ? { background: "#dcfce7", color: "#15803d" }
      : match === "strong"
        ? { background: "#dbeafe", color: "#1d4ed8" }
        : { background: "#f4f4f5", color: "#52525b" };
  return (
    <span
      style={{
        fontSize: ".7rem",
        fontWeight: 600,
        padding: ".15rem .4rem",
        borderRadius: 4,
        textTransform: "uppercase",
        ...style,
      }}
    >
      {match}
    </span>
  );
}

const NAV_TABS = ["Positions"] as const;

export function DomainOverviewPanel({
  overview,
  domainInput,
  onDomainInputChange,
  onSubmit,
  loading,
  analyzing,
}: {
  overview: DomainOverview | null;
  domainInput: string;
  onDomainInputChange: (value: string) => void;
  onSubmit: () => void;
  loading: boolean;
  analyzing: boolean;
}) {
  const [keywordFilter, setKeywordFilter] = useState("");
  const [showPageSnapshot, setShowPageSnapshot] = useState(true);
  const busy = loading || analyzing;
  const canSubmit = Boolean(domainInput.trim()) && !busy;

  const positions = overview?.positions ?? [];
  const filteredPositions = useMemo(() => {
    const q = keywordFilter.trim().toLowerCase();
    if (!q) return positions;
    return positions.filter((row) => row.keyword.toLowerCase().includes(q));
  }, [positions, keywordFilter]);

  const positionCount =
    overview?.totalPositionsCount ?? (positions.length > 0 ? positions.length : null);
  const hasOwnedPositions = positions.length > 0;
  const bestPosition = useMemo(() => {
    const ranks = positions.map((row) => row.position).filter((p) => p > 0);
    return ranks.length > 0 ? Math.min(...ranks) : null;
  }, [positions]);

  return (
    <section
      style={{
        margin: "0 -1rem 1.5rem",
        padding: "0 1rem 1.25rem",
        borderBottom: "1px solid #e4e4e7",
        background: "#fafafa",
      }}
    >
      <div style={{ maxWidth: "72rem", margin: "0 auto" }}>
        <div style={{ display: "flex", alignItems: "center", gap: ".75rem", marginBottom: ".85rem" }}>
          <h2 style={{ fontSize: "1.1rem", margin: 0, fontWeight: 600, color: "#18181b", flexShrink: 0 }}>
            Domain positions
          </h2>
          <div style={{ flex: 1, display: "flex", gap: ".5rem" }}>
            <input
              type="text"
              value={domainInput}
              onChange={(e) => onDomainInputChange(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && canSubmit) onSubmit();
              }}
              placeholder="ramp.com or https://ramp.com/expense-management"
              style={{
                flex: 1,
                padding: ".55rem .75rem",
                border: "1px solid #d4d4d8",
                borderRadius: 6,
                fontSize: ".9rem",
                background: "#fff",
              }}
            />
            <button
              type="button"
              onClick={onSubmit}
              disabled={!canSubmit}
              style={{
                padding: ".55rem 1.25rem",
                borderRadius: 6,
                border: "none",
                background: canSubmit ? "#2563eb" : "#a1a1aa",
                color: "#fff",
                fontSize: ".9rem",
                fontWeight: 600,
                cursor: canSubmit ? "pointer" : "not-allowed",
                whiteSpace: "nowrap",
              }}
            >
              {analyzing ? "Analyzing…" : loading ? "Loading…" : "Search"}
            </button>
          </div>
        </div>

        <p style={{ margin: "0 0 .85rem", fontSize: ".82rem", color: "#71717a" }}>
          Keywords where this domain appeared in your imported Google results. Grows as you import more SERPs below.
        </p>

        <nav
          style={{
            display: "flex",
            gap: ".15rem",
            borderBottom: "1px solid #e4e4e7",
            marginBottom: "1rem",
            overflowX: "auto",
          }}
        >
          {NAV_TABS.map((tab) => (
              <span
                key={tab}
                style={{
                  padding: ".55rem .85rem",
                  borderBottom: "2px solid #2563eb",
                  color: "#2563eb",
                  fontSize: ".82rem",
                  fontWeight: 600,
                }}
              >
                {tab}
              </span>
            ))}
        </nav>

        {overview?.message ? (
          <div
            style={{
              marginBottom: ".85rem",
              padding: ".65rem .85rem",
              borderRadius: 6,
              background: hasOwnedPositions ? "#eff6ff" : "#fffbeb",
              border: `1px solid ${hasOwnedPositions ? "#bfdbfe" : "#fde68a"}`,
              fontSize: ".82rem",
              color: hasOwnedPositions ? "#1e40af" : "#92400e",
            }}
          >
            {overview.message}
          </div>
        ) : null}

        {overview?.warnings && overview.warnings.length > 0 ? (
          <ul
            style={{
              margin: "0 0 .85rem",
              padding: ".55rem .85rem .55rem 1.75rem",
              borderRadius: 6,
              background: "#fff7ed",
              border: "1px solid #fed7aa",
              fontSize: ".8rem",
              color: "#9a3412",
            }}
          >
            {overview.warnings.map((w) => (
              <li key={w}>{w}</li>
            ))}
          </ul>
        ) : null}

        {overview ? (
          <>
            <div style={{ display: "flex", flexWrap: "wrap", gap: ".75rem", marginBottom: "1rem" }}>
              <MetricCard
                label="Keywords tracked"
                value={overview.organicKeywordsCount ?? positionCount}
              />
              <MetricCard
                label="SERP imports"
                value={overview.researchImportCount}
              />
              <MetricCard label="Best position" value={bestPosition} />
              {overview.pageFetched ? (
                <MetricCard label="H2 on page" value={overview.h2Count} />
              ) : null}
            </div>

            {overview.pageFetched ? (
              <div
                style={{
                  marginBottom: "1rem",
                  padding: "1rem",
                  border: "1px solid #bfdbfe",
                  borderRadius: 8,
                  background: "#eff6ff",
                }}
              >
                <div style={{ fontSize: ".78rem", fontWeight: 600, color: "#1d4ed8", marginBottom: ".5rem" }}>
                  Page snapshot · {overview.scope === "url" ? "URL" : "domain root"}
                </div>
                <div style={{ fontSize: ".9rem", fontWeight: 600, color: "#18181b" }}>
                  {overview.pageTitle?.trim() || "—"}
                </div>
                {overview.metaDescription?.trim() ? (
                  <p style={{ margin: ".35rem 0 0", fontSize: ".82rem", color: "#3f3f46" }}>
                    {overview.metaDescription.trim()}
                  </p>
                ) : null}
                <div style={{ marginTop: ".5rem", fontSize: ".78rem", color: "#52525b" }}>
                  <a href={overview.analyzedUrl} target="_blank" rel="noopener noreferrer" style={{ color: "#2563eb" }}>
                    {overview.analyzedUrl}
                  </a>
                  {" · "}
                  HTTP {formatTableNumber(overview.httpStatus)}
                  {overview.schemaTypes && overview.schemaTypes.length > 0
                    ? ` · Schema: ${overview.schemaTypes.join(", ")}`
                    : ""}
                </div>
              </div>
            ) : null}

            <div
              style={{
                display: "flex",
                flexWrap: "wrap",
                gap: ".5rem",
                alignItems: "center",
                marginBottom: ".65rem",
              }}
            >
              <input
                type="search"
                value={keywordFilter}
                onChange={(e) => setKeywordFilter(e.target.value)}
                placeholder="Filter by keyword"
                style={{
                  flex: "1 1 12rem",
                  maxWidth: "16rem",
                  padding: ".4rem .6rem",
                  border: "1px solid #d4d4d8",
                  borderRadius: 6,
                  fontSize: ".82rem",
                }}
              />
            </div>

            <div
              style={{
                border: "1px solid #e4e4e7",
                borderRadius: 8,
                background: "#fff",
                overflow: "hidden",
              }}
            >
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: ".65rem .85rem",
                  borderBottom: "1px solid #f4f4f5",
                  fontSize: ".82rem",
                  color: "#52525b",
                  flexWrap: "wrap",
                  gap: ".5rem",
                }}
              >
                <span>
                  Positions from your imports:{" "}
                  <strong style={{ color: "#18181b" }}>
                    {positionCount != null ? positionCount.toLocaleString() : "—"}
                  </strong>
                </span>
                <span style={{ fontSize: ".75rem", color: "#a1a1aa" }}>
                  {overview.domain} ·{" "}
                  <a
                    href={overview.siteRootUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    style={{ color: "#2563eb" }}
                  >
                    {overview.siteRootUrl}
                  </a>
                </span>
              </div>

              <div style={{ overflowX: "auto" }}>
                <table style={{ width: "100%", borderCollapse: "collapse", fontSize: ".8rem" }}>
                  <thead>
                    <tr style={{ textAlign: "left", color: "#71717a", background: "#fafafa" }}>
                      {["Keyword", "Intent", "Position", "URL match", "URL", "Source", "Imported"].map((col) => (
                        <th
                          key={col}
                          style={{
                            padding: ".5rem .65rem",
                            borderBottom: "1px solid #e4e4e7",
                            fontWeight: 600,
                            whiteSpace: "nowrap",
                          }}
                        >
                          {col}
                        </th>
                      ))}
                  </tr>
                  </thead>
                  <tbody>
                    {filteredPositions.length > 0 ? (
                      filteredPositions.map((row) => (
                        <tr key={`${row.keyword}-${row.position}-${row.url}`}>
                          <td style={{ padding: ".55rem .65rem", borderBottom: "1px solid #f4f4f5" }}>
                            {row.keyword}
                          </td>
                          <td style={{ padding: ".55rem .65rem", borderBottom: "1px solid #f4f4f5" }}>
                            <IntentBadge intent={row.intent} />
                          </td>
                          <td style={{ padding: ".55rem .65rem", borderBottom: "1px solid #f4f4f5" }}>
                            {row.position > 0 ? row.position : "—"}
                          </td>
                          <td style={{ padding: ".55rem .65rem", borderBottom: "1px solid #f4f4f5" }}>
                            <PathMatchBadge match={row.pathMatch} />
                          </td>
                          <td
                            style={{
                              padding: ".55rem .65rem",
                              borderBottom: "1px solid #f4f4f5",
                              maxWidth: "18rem",
                              wordBreak: "break-all",
                            }}
                          >
                            <a
                              href={row.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              style={{ color: "#2563eb" }}
                            >
                              {row.url.replace(/^https?:\/\/(www\.)?/, "")}
                            </a>
                          </td>
                          <td
                            style={{
                              padding: ".55rem .65rem",
                              borderBottom: "1px solid #f4f4f5",
                              color: "#71717a",
                              fontSize: ".75rem",
                            }}
                          >
                            {row.serpFeatures ?? "—"}
                          </td>
                          <td
                            style={{
                              padding: ".55rem .65rem",
                              borderBottom: "1px solid #f4f4f5",
                              whiteSpace: "nowrap",
                            }}
                          >
                            {formatDate(row.updatedAt)}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td
                          colSpan={7}
                          style={{
                            padding: "2rem 1rem",
                            textAlign: "center",
                            color: "#71717a",
                            fontSize: ".85rem",
                          }}
                        >
                          No positions yet. Import a Google results page below for keywords where this domain ranks.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div style={{ marginTop: "1rem" }}>
                <button
                  type="button"
                  onClick={() => setShowPageSnapshot((v) => !v)}
                  style={{
                    padding: ".4rem 0",
                    border: "none",
                    background: "transparent",
                    color: "#2563eb",
                    fontSize: ".82rem",
                    fontWeight: 600,
                    cursor: "pointer",
                  }}
                >
                  {showPageSnapshot ? "▾" : "▸"} Page snapshot
                  {overview.pageFetched ? "" : " (run Analyze on a URL path)"}
                </button>
                {showPageSnapshot ? (
                  <div
                    style={{
                      marginTop: ".5rem",
                      padding: ".85rem",
                      border: "1px solid #e4e4e7",
                      borderRadius: 8,
                      background: "#fff",
                      fontSize: ".82rem",
                      color: "#3f3f46",
                    }}
                  >
                    <div style={{ marginBottom: ".35rem" }}>
                      <strong>Analyzed URL:</strong>{" "}
                      <a
                        href={overview.analyzedUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        style={{ color: "#2563eb" }}
                      >
                        {overview.analyzedUrl}
                      </a>
                    </div>
                    {overview.pageFetched ? (
                      <>
                        <div>
                          <strong>Title:</strong> {overview.pageTitle?.trim() || "—"}
                        </div>
                        {overview.metaDescription?.trim() ? (
                          <div>
                            <strong>Meta:</strong> {overview.metaDescription.trim()}
                          </div>
                        ) : null}
                        <div>
                          <strong>H2 count:</strong> {formatTableNumber(overview.h2Count)}
                        </div>
                        <div>
                          <strong>HTTP:</strong> {formatTableNumber(overview.httpStatus)}
                        </div>
                        <div>
                          <strong>Schema:</strong>{" "}
                          {overview.schemaTypes && overview.schemaTypes.length > 0
                            ? overview.schemaTypes.join(", ")
                            : "—"}
                        </div>
                      </>
                    ) : (
                      <p style={{ margin: 0, color: "#71717a" }}>
                        Enter a full URL path and click Search to fetch HTML structure (e.g. ramp.com/expense-management).
                      </p>
                    )}
                  </div>
                ) : null}
              </div>
          </>
        ) : busy ? (
          <p style={{ margin: 0, fontSize: ".85rem", color: "#71717a" }}>Loading domain…</p>
        ) : (
          <p style={{ margin: 0, fontSize: ".85rem", color: "#71717a" }}>
            Enter a domain to see where it ranks in SERPs you have already imported.
          </p>
        )}
      </div>
    </section>
  );
}
