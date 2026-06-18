import {
  getNicheAnalysisStatus,
  type NicheAnalysisStatus,
  type StepStatus,
} from '@/lib/seo-api';
import type { SeoHubApi } from '@/components/signalr/seo-hub-provider';

const TERMINAL_STEP_STATUSES = new Set<StepStatus>(['complete', 'error', 'skipped']);

function normalizeId(id: string | undefined): string | undefined {
  return id?.toLowerCase();
}

function idsMatch(a: string | undefined, b: string): boolean {
  const left = normalizeId(a);
  const right = normalizeId(b);
  return !left || left === right;
}

type AnalysisProgressMsg = {
  profileId?: string;
  ProfileId?: string;
  step?: string;
  Step?: string;
  status?: string;
  Status?: string;
  message?: string;
  Message?: string;
};

function msgProfileId(msg: AnalysisProgressMsg): string | undefined {
  return msg.profileId ?? msg.ProfileId;
}

function msgStep(msg: AnalysisProgressMsg): string | undefined {
  return msg.step ?? msg.Step;
}

function msgStatus(msg: AnalysisProgressMsg): string | undefined {
  return msg.status ?? msg.Status;
}

function msgMessage(msg: AnalysisProgressMsg): string | undefined {
  return msg.message ?? msg.Message;
}

function isTerminalStepStatus(status: StepStatus | undefined): boolean {
  return status !== undefined && TERMINAL_STEP_STATUSES.has(status);
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

export type WaitForNicheStepOptions = {
  profileId: string;
  slug: string;
  accessToken?: string | null;
  hub: SeoHubApi;
  timeoutMs?: number;
  triggerRun?: () => Promise<void>;
  onProgress?: (message: string) => void;
  onStatus?: (status: NicheAnalysisStatus) => void;
};

export async function waitForNicheStepViaSignalR(
  options: WaitForNicheStepOptions,
): Promise<NicheAnalysisStatus> {
  const { profileId, slug, accessToken, hub, onProgress, onStatus, triggerRun } = options;
  const timeoutMs =
    options.timeoutMs ??
    (slug === 'site_crawl' ? 300_000 : slug === 'serp_validation' ? 900_000 : 120_000);

  return new Promise((resolve, reject) => {
    let settled = false;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;
    let leaveGroup: (() => void) | undefined;
    let unsubProgress: (() => void) | undefined;

    const cleanup = () => {
      if (timeoutId) clearTimeout(timeoutId);
      unsubProgress?.();
      leaveGroup?.();
    };

    const finishResolve = (status: NicheAnalysisStatus) => {
      if (settled) return;
      settled = true;
      cleanup();
      resolve(status);
    };

    const finishReject = (error: Error) => {
      if (settled) return;
      settled = true;
      cleanup();
      reject(error);
    };

    const hydrate = async (): Promise<NicheAnalysisStatus> => {
      const status = await getNicheAnalysisStatus(profileId, accessToken);
      onStatus?.(status);
      return status;
    };

    const settleFromDb = async (): Promise<boolean> => {
      try {
        const status = await hydrate();
        const stepState = status.stepStatuses?.[slug];
        if (isTerminalStepStatus(stepState)) {
          finishResolve(status);
          return true;
        }
        if (status.stepErrors?.[slug]) {
          throw new Error(status.stepErrors[slug] ?? `Step "${slug}" failed.`);
        }
        if (status.stepSummaries?.[slug] && stepState !== 'running') {
          finishResolve(status);
          return true;
        }
        if (status.status === 'failed' && status.step === slug) {
          throw new Error(status.errorMessage ?? `Step "${slug}" failed.`);
        }
        return false;
      } catch (e) {
        finishReject(e instanceof Error ? e : new Error(`Step "${slug}" failed.`));
        return true;
      }
    };

    const settleAfterSignal = async (detail?: string) => {
      const settledNow = await settleFromDb();
      if (settledNow || settled) return;

      await sleep(500);
      if (settled) return;

      const status = await hydrate();
      const stepState = status.stepStatuses?.[slug];
      if (isTerminalStepStatus(stepState)) {
        finishResolve(status);
        return;
      }

      finishResolve({
        ...status,
        stepStatuses: {
          ...(status.stepStatuses ?? {}),
          [slug]: 'complete',
        },
        stepSummaries: detail
          ? {
              ...(status.stepSummaries ?? {}),
              [slug]: detail,
            }
          : status.stepSummaries,
      });
    };

    timeoutId = setTimeout(() => {
      void (async () => {
        const done = await settleFromDb();
        if (!done) {
          finishReject(new Error(`Timed out waiting for step "${slug}" to finish.`));
        }
      })();
    }, timeoutMs);

    leaveGroup = hub.joinNicheProfile(profileId);

    unsubProgress = hub.subscribe('AnalysisProgress', (raw: unknown) => {
      const msg = raw as AnalysisProgressMsg;
      const pid = msgProfileId(msg);
      const step = msgStep(msg);
      if (!idsMatch(pid, profileId)) return;
      if (step && step !== slug) return;

      const detail = msgMessage(msg);
      if (detail) onProgress?.(detail);

      const status = msgStatus(msg)?.toLowerCase();
      if (status === 'running') return;

      if (status === 'complete') {
        void settleAfterSignal(detail);
        return;
      }

      if (status === 'error' || status === 'failed') {
        void (async () => {
          try {
            const hydrated = await hydrate();
            finishReject(
              new Error(hydrated.stepErrors?.[slug] ?? detail ?? `Step "${slug}" failed.`),
            );
          } catch (e) {
            finishReject(e instanceof Error ? e : new Error(`Step "${slug}" failed.`));
          }
        })();
        return;
      }

      void settleAfterSignal();
    });

    void (async () => {
      try {
        if (triggerRun) await triggerRun();
        if (!settled) void settleFromDb();
      } catch (e) {
        const done = await settleFromDb();
        if (!done) {
          finishReject(
            e instanceof Error
              ? new Error(`Live progress unavailable: ${e.message}`)
              : new Error('Live progress unavailable. Please refresh and try again.'),
          );
        }
      }
    })();
  });
}
