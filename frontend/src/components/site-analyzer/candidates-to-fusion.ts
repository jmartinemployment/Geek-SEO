import type { SiteTopicProfile, TopicCandidate, TopicEvidence } from '@/lib/seo-api';

export type NicheTopicCandidateRow = {
  id: string;
  nicheProfileId: string;
  slug: string;
  name: string;
  confidence: number;
  isSelected: boolean;
  exclusionReason?: string | null;
  dedicatedPageUrl?: string | null;
  internalLinkCount: number;
  contentDepthScore: number;
  displayOrder: number;
  evidence?: TopicEvidence[] | null;
};

export type NicheTopicCandidateList = {
  items: NicheTopicCandidateRow[];
  total: number;
  page: number;
  pageSize: number;
};

function parseEvidenceJson(raw: unknown): TopicEvidence[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item) => {
    if (!item || typeof item !== 'object') return [];
    const row = item as Record<string, unknown>;
    const source = typeof row.source === 'string' ? row.source : '';
    if (!source) return [];
    return [
      {
        source,
        snippet: typeof row.snippet === 'string' ? row.snippet : undefined,
        url: typeof row.url === 'string' ? row.url : undefined,
        weight: typeof row.weight === 'number' ? row.weight : 0,
      },
    ];
  });
}

function rowToCandidate(row: NicheTopicCandidateRow): TopicCandidate {
  return {
    name: row.name,
    slug: row.slug,
    evidence: row.evidence ?? [],
    confidence: row.confidence,
    dedicatedPageUrl: row.dedicatedPageUrl ?? undefined,
    internalLinkCount: row.internalLinkCount,
  };
}

/** Build a partial fusion view from paginated candidate inventory (Phase 2 read path). */
export function fusionFromTopicCandidates(
  rows: NicheTopicCandidateRow[],
  sulVersion = 'sul-2.0',
): SiteTopicProfile | null {
  if (rows.length === 0) return null;

  const sorted = [...rows].sort((a, b) => a.displayOrder - b.displayOrder);
  const allCandidates = sorted.map(rowToCandidate);
  const selectedPillars = sorted.filter((r) => r.isSelected).map(rowToCandidate);
  const excludedCandidates = sorted.filter((r) => !r.isSelected).map(rowToCandidate);
  const exclusionReasons: Record<string, string> = {};
  for (const row of sorted) {
    if (!row.isSelected && row.exclusionReason) {
      exclusionReasons[row.slug] = row.exclusionReason;
    }
  }

  const sources = new Set<string>();
  for (const c of allCandidates) {
    for (const e of c.evidence) sources.add(e.source);
  }

  return {
    allCandidates,
    selectedPillars,
    excludedCandidates,
    exclusionReasons,
    sulVersion,
    signalSourcesPresent: [...sources],
  };
}

export { parseEvidenceJson };
