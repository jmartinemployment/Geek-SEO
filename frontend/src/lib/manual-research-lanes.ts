import {
  type ContentWriterManualResearchLane,
  type ContentWriterSerpExport,
} from '@/lib/seo-api';
import { siteAnalyzer2Fetch } from '@/lib/site-analyzer2-api';

export type ManualResearchLaneId = 'paa' | 'edu' | 'gov' | 'local' | 'wiki';

export const MANUAL_RESEARCH_LANE_ORDER: ManualResearchLaneId[] = [
  'paa',
  'edu',
  'gov',
  'local',
  'wiki',
];

export const MANUAL_RESEARCH_LANE_LABELS: Record<ManualResearchLaneId, string> = {
  paa: 'People Also Ask',
  edu: 'Research (.edu)',
  gov: 'Government (.gov)',
  local: 'Local SERP',
  wiki: 'Wikipedia',
};

const LANE_JUNK = '-template -pdf -generator -reddit -quora -course -syllabus';

export function manualResearchLaneQueryHint(
  lane: ManualResearchLaneId,
  keyword: string,
): string | null {
  const phrase = keyword.trim()
    ? `"${keyword.trim().replace(/"/g, '')}"`
    : '"your keyword"';
  switch (lane) {
    case 'wiki':
      return `Google: ${phrase} site:en.wikipedia.org ${LANE_JUNK} — results must be en.wikipedia.org (not .wiki sites)`;
    case 'gov':
      return `Google: ${phrase} (site:nist.gov OR site:ftc.gov OR site:usa.gov OR site:cdc.gov OR site:nih.gov) ${LANE_JUNK}`;
    case 'edu':
      return `Google: ${phrase} site:edu ${LANE_JUNK}`;
    default:
      return null;
  }
}

export function supplementalLanesImported(
  gates?: { id: string; complete: boolean }[],
): boolean {
  return gates?.some((g) => g.id !== 'keyword' && g.complete) ?? false;
}

export async function updateResearchTopicSlug(
  runId: string,
  topicSlug: string,
  accessToken?: string | null,
): Promise<string> {
  const res = await siteAnalyzer2Fetch(
    `/analysis-runs/${encodeURIComponent(runId)}/topic-slug`,
    accessToken,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ topicSlug }),
    },
  );
  const body = (await res.json().catch(() => ({}))) as { topicSlug?: string; error?: string };
  if (!res.ok) {
    throw new Error(body.error || res.statusText || `Could not update topic slug (${res.status})`);
  }
  return body.topicSlug ?? topicSlug;
}

export function requiredManualLanesForTopic(topicSlug: string): ManualResearchLaneId[] {
  return topicSlug.trim().toLowerCase() === 'customer-journey' ? ['gov', 'wiki'] : [];
}

export function pendingRequiredGateLabels(
  topicSlug: string,
  gates?: { id: string; label: string; complete: boolean }[],
): string[] {
  const required = requiredManualLanesForTopic(topicSlug);
  if (!gates?.length || required.length === 0) return [];

  return required
    .filter((lane) => {
      const gate = gates.find((g) => g.id === lane);
      return gate ? !gate.complete : true;
    })
    .map((lane) => MANUAL_RESEARCH_LANE_LABELS[lane]);
}

/** Client-side check before POST — catches wrong SERP saves early. */
export function validateManualLaneFileContent(
  lane: ManualResearchLaneId,
  content: string,
  fileName?: string,
): string | null {
  if (lane === 'paa') return null;

  const lower = content.toLowerCase();
  const name = (fileName ?? '').toLowerCase();

  if (lane === 'wiki') {
    if (lower.includes('wikipedia.org')) return null;
    if (
      /https?:\/\/[a-z0-9.-]+\.wiki[\s"'/<>\\]/i.test(content)
      || name.includes('site_wiki')
      || (name.includes('site:wiki') && !name.includes('wikipedia'))
    ) {
      return 'Wrong wiki SERP: this file has .wiki sites (e.g. aisdr.wiki), not en.wikipedia.org. Use Google site:en.wikipedia.org and save Webpage, HTML only.';
    }
    return 'No wikipedia.org URLs in this file. Re-run Google with site:en.wikipedia.org, then save Webpage, HTML only.';
  }

  if (lane === 'gov' && !/\.gov[\s"'/<>\\]/i.test(content)) {
    return 'No .gov URLs found. Save a Google SERP from the government query shown below.';
  }

  if (lane === 'edu' && !/\.edu[\s"'/<>\\]/i.test(content)) {
    return 'No .edu URLs found. Save a Google SERP from the .edu query shown below.';
  }

  return null;
}

export type ManualLaneImportResult = {
  runId: string;
  lane: string;
  topicSlug: string;
  organicCount: number;
  citationEligibleCount?: number;
  researchMode?: string;
  fileCount?: number;
  paaQuestionCount?: number;
};

export async function fetchContentWriterExport(
  runId: string,
  accessToken?: string | null,
): Promise<ContentWriterSerpExport | null> {
  const res = await siteAnalyzer2Fetch(
    `/analysis-runs/${encodeURIComponent(runId)}/content-writer-export`,
    accessToken,
  );
  if (!res.ok) return null;
  return res.json() as Promise<ContentWriterSerpExport>;
}

export function slugifyResearchTopic(value: string): string {
  const slug = value
    .trim()
    .toLowerCase()
    .replace(/&/g, 'and')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return (slug.length > 80 ? slug.slice(0, 80).replace(/-+$/g, '') : slug) || 'research-topic';
}

export async function importManualResearchLane(
  runId: string,
  lane: ManualResearchLaneId,
  topicSlug: string,
  html: string,
  accessToken?: string | null,
  fileName?: string,
): Promise<ManualLaneImportResult> {
  const params = new URLSearchParams({ lane, topic: topicSlug });
  const isText = (fileName ?? '').toLowerCase().endsWith('.txt');
  const res = await siteAnalyzer2Fetch(
    `/analysis-runs/${encodeURIComponent(runId)}/serp/import-html?${params}`,
    accessToken,
    {
      method: 'POST',
      headers: {
        'Content-Type': isText ? 'text/plain; charset=utf-8' : 'text/html; charset=utf-8',
      },
      body: html,
    },
  );
  const body = (await res.json().catch(() => ({}))) as ManualLaneImportResult & {
    error?: string;
  };
  if (!res.ok) {
    console.error('[manual-research-lane] import failed', { lane, topicSlug, status: res.status, body });
    throw new Error(body.error || res.statusText || `Import failed (${res.status})`);
  }
  return body;
}

export async function importManualResearchPaaBatch(
  runId: string,
  topicSlug: string,
  files: File[],
  accessToken?: string | null,
): Promise<ManualLaneImportResult> {
  const contents = await Promise.all(
    files.map(async (file) => ({
      fileName: file.name,
      content: await file.text(),
    })),
  );
  const params = new URLSearchParams({ topic: topicSlug });
  const res = await siteAnalyzer2Fetch(
    `/analysis-runs/${encodeURIComponent(runId)}/serp/import-paa-batch?${params}`,
    accessToken,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ files: contents }),
    },
  );
  const body = (await res.json().catch(() => ({}))) as ManualLaneImportResult & {
    error?: string;
  };
  if (!res.ok) {
    console.error('[manual-research-lane] PAA batch import failed', { topicSlug, status: res.status, body });
    throw new Error(body.error || res.statusText || `Import failed (${res.status})`);
  }
  return body;
}

export function laneImportStatus(
  lane: ManualResearchLaneId,
  exportData: ContentWriterSerpExport | null,
  gates?: { id: string; complete: boolean }[],
): 'ok' | 'empty' {
  const gate = gates?.find((g) => g.id === lane);
  if (gate) return gate.complete ? 'ok' : 'empty';

  if (!exportData?.manualResearchLanes?.length) return 'empty';

  const manual = exportData.manualResearchLanes.find(
    (l) => l.lane.toLowerCase() === lane,
  );
  if (!manual) return 'empty';

  if (lane === 'paa') {
    return (manual.paaCount ?? manual.paaQuestions?.length ?? 0) > 0 ? 'ok' : 'empty';
  }
  return manual.organicCount > 0 ? 'ok' : 'empty';
}

export function summarizeManualLanes(
  exportData: ContentWriterSerpExport | null,
): ContentWriterManualResearchLane[] {
  return exportData?.manualResearchLanes ?? [];
}
