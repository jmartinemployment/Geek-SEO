import type { SiteAnalysisStatus } from '@/lib/seo-api';

/** Server marks processing runs failed after this idle window. */
export const SITE_ANALYSIS_STALE_MS = 5 * 60 * 1000;

/** UI hint when step number stops advancing. */
export const SITE_STALL_MS = 5 * 60 * 1000;

export function siteAnalysisStatusLastActivityIso(status: SiteAnalysisStatus): string | undefined {
  return status.progressAt ?? status.createdAt;
}

export function isSiteAnalysisRunStale(status: SiteAnalysisStatus, now = Date.now()): boolean {
  if (status.status !== 'processing' && status.status !== 'queued') return false;
  const last = siteAnalysisStatusLastActivityIso(status);
  if (!last) return false;
  return now - Date.parse(last) > SITE_ANALYSIS_STALE_MS;
}

export function isSiteStepStalled(
  status: SiteAnalysisStatus,
  lastStepNumber: number,
  lastStepChangeAt: number,
  now = Date.now(),
): boolean {
  if (status.status !== 'processing') return false;
  const step = status.stepNumber ?? 0;
  if (step <= 0 || step !== lastStepNumber) return false;
  return now - lastStepChangeAt > SITE_STALL_MS;
}
