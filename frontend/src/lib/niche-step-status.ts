import type { StepStatus } from '@/lib/seo-api';

const TERMINAL_STEP_STATUSES = new Set<StepStatus>(['complete', 'error', 'skipped']);

function statusRank(status: StepStatus | undefined): number {
  switch (status) {
    case 'complete':
    case 'error':
    case 'skipped':
      return 3;
    case 'running':
      return 2;
    case 'pending':
      return 1;
    default:
      return 0;
  }
}

/** Merge status maps; terminal beats running so a refresh cannot undo a completed step. */
export function mergeStepStatuses(
  ...maps: Array<Record<string, StepStatus> | undefined>
): Record<string, StepStatus> {
  const merged: Record<string, StepStatus> = {};
  for (const map of maps) {
    if (!map) continue;
    for (const [slug, status] of Object.entries(map)) {
      const existing = merged[slug];
      merged[slug] = statusRank(status) >= statusRank(existing) ? status : existing;
    }
  }
  return merged;
}

/** Legacy 14-step runs used site_structure instead of site_crawl. */
export function isNicheStepComplete(
  slug: string,
  statuses?: Record<string, StepStatus>,
): boolean {
  if (statuses?.[slug] === 'complete') return true;
  if (slug === 'site_crawl' && statuses?.site_structure === 'complete') return true;
  return false;
}

export function isAnyNicheStepRunning(statuses?: Record<string, StepStatus>): boolean {
  return Object.values(statuses ?? {}).some((status) => status === 'running');
}

export function isTerminalStepStatus(status: StepStatus | undefined): boolean {
  return status !== undefined && TERMINAL_STEP_STATUSES.has(status);
}
