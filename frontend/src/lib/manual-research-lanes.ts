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

export type ManualLaneImportResult = {
  runId: string;
  lane: string;
  topicSlug: string;
  organicCount: number;
  citationEligibleCount?: number;
  researchMode?: string;
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
