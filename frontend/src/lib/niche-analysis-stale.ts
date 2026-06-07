import type { NicheAnalysisStatus } from '@/lib/seo-api';

/** Server marks processing runs failed after this idle window. */
export const NICHE_STALE_MS = 5 * 60 * 1000;

/** UI hint when step number stops advancing. */
export const NICHE_STALL_MS = 5 * 60 * 1000;

export function nicheStatusLastActivityIso(status: NicheAnalysisStatus): string | undefined {
  return status.progressAt ?? status.createdAt;
}

export function isNicheRunStale(status: NicheAnalysisStatus, now = Date.now()): boolean {
  if (status.status !== 'processing' && status.status !== 'queued') return false;
  const last = nicheStatusLastActivityIso(status);
  if (!last) return false;
  return now - Date.parse(last) > NICHE_STALE_MS;
}

export function isNicheStepStalled(
  status: NicheAnalysisStatus,
  lastStepNumber: number,
  lastStepChangeAt: number,
  now = Date.now(),
): boolean {
  if (status.status !== 'processing') return false;
  const step = status.stepNumber ?? 0;
  if (step <= 0 || step !== lastStepNumber) return false;
  return now - lastStepChangeAt > NICHE_STALL_MS;
}
