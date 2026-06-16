import {
  getHubUrl,
  getNicheAnalysisStatus,
  type NicheAnalysisStatus,
  type StepStatus,
} from '@/lib/seo-api';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

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

function hubUrl(accessToken?: string | null): string {
  const base = getHubUrl();
  if (!accessToken && DEV_USER_ID) {
    return `${base}?access_token=${encodeURIComponent(DEV_USER_ID)}`;
  }
  return base;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

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

export type WaitForNicheStepOptions = {
  profileId: string;
  slug: string;
  accessToken?: string | null;
  timeoutMs?: number;
  /** Called after the hub is connected — use this to POST run-step so events are not missed. */
  triggerRun?: () => Promise<void>;
  onProgress?: (message: string) => void;
  onStatus?: (status: NicheAnalysisStatus) => void;
};

/**
 * Waits for a manual step run to finish via SignalR, then hydrates once from GET /status.
 */
export async function waitForNicheStepViaSignalR(
  options: WaitForNicheStepOptions,
): Promise<NicheAnalysisStatus> {
  const { profileId, slug, accessToken, onProgress, onStatus, triggerRun } = options;
  const timeoutMs =
    options.timeoutMs ??
    (slug === 'site_crawl' ? 300_000 : slug === 'serp_validation' ? 900_000 : 120_000);

  return new Promise((resolve, reject) => {
    let disposed = false;
    let started = false;
    let settled = false;
    let connection: import('@microsoft/signalr').HubConnection | null = null;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    const cleanup = () => {
      disposed = true;
      if (timeoutId) clearTimeout(timeoutId);
      const conn = connection;
      connection = null;
      if (conn && started) void conn.stop().catch(() => {});
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

    const hydrateUntilTerminal = async (attempts = 5): Promise<NicheAnalysisStatus | null> => {
      for (let i = 0; i < attempts; i += 1) {
        const status = await hydrate();
        const stepState = status.stepStatuses?.[slug];
        if (isTerminalStepStatus(stepState)) return status;
        if (status.stepErrors?.[slug]) {
          throw new Error(status.stepErrors[slug] ?? `Step "${slug}" failed.`);
        }
        if (status.stepSummaries?.[slug] && stepState !== 'running') return status;
        if (status.status === 'failed' && status.step === slug) {
          throw new Error(status.errorMessage ?? `Step "${slug}" failed.`);
        }
        if (i < attempts - 1) await sleep(200);
      }
      return null;
    };

    const settleFromDb = async (): Promise<boolean> => {
      try {
        const status = await hydrateUntilTerminal();
        if (status) {
          finishResolve(status);
          return true;
        }
        return false;
      } catch (e) {
        finishReject(e instanceof Error ? e : new Error(`Step "${slug}" failed.`));
        return true;
      }
    };

    timeoutId = setTimeout(() => {
      void (async () => {
        const done = await settleFromDb();
        if (!done) {
          finishReject(new Error(`Timed out waiting for step "${slug}" to finish.`));
        }
      })();
    }, timeoutMs);

    // Fast steps can finish before the hub connects; reconcile a few times during the wait.
    for (const delayMs of [1_000, 3_000, 8_000]) {
      setTimeout(() => {
        if (!settled) void settleFromDb();
      }, delayMs);
    }

    const onTerminalSignal = () => {
      void (async () => {
        const done = await settleFromDb();
        if (!done && !settled) {
          // DB may lag the push slightly; timeout will reconcile if needed.
        }
      })();
    };

    async function connect() {
      try {
        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
        if (disposed) return;

        const conn = new HubConnectionBuilder()
          .withUrl(hubUrl(accessToken), {
            accessTokenFactory: () => accessToken ?? '',
            withCredentials: true,
          })
          .configureLogging(LogLevel.None)
          .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
          .build();

        conn.on('AnalysisProgress', (raw: AnalysisProgressMsg) => {
          const pid = msgProfileId(raw);
          const step = msgStep(raw);
          if (!idsMatch(pid, profileId)) return;
          if (step && step !== slug) return;

          const detail = msgMessage(raw);
          if (detail) onProgress?.(detail);

          const status = msgStatus(raw)?.toLowerCase();
          if (status === 'running') return;

          if (status === 'complete') {
            void (async () => {
              try {
                const hydrated = await hydrateUntilTerminal(12);
                if (hydrated) {
                  finishResolve(hydrated);
                  return;
                }

                const fallback = await hydrate();
                finishResolve({
                  ...fallback,
                  stepStatuses: {
                    ...(fallback.stepStatuses ?? {}),
                    [slug]: 'complete',
                  },
                  stepSummaries: detail
                    ? {
                        ...(fallback.stepSummaries ?? {}),
                        [slug]: detail,
                      }
                    : fallback.stepSummaries,
                });
              } catch (e) {
                finishReject(
                  e instanceof Error ? e : new Error(`Step "${slug}" failed.`),
                );
              }
            })();
            return;
          }

          if (status === 'error' || status === 'failed') {
            void (async () => {
              try {
                const hydrated = await hydrate();
                finishReject(
                  new Error(
                    hydrated.stepErrors?.[slug] ?? detail ?? `Step "${slug}" failed.`,
                  ),
                );
              } catch (e) {
                finishReject(
                  e instanceof Error ? e : new Error(`Step "${slug}" failed.`),
                );
              }
            })();
            return;
          }

          onTerminalSignal();
        });

        conn.onreconnected(() => {
          void settleFromDb();
        });

        connection = conn;
        await conn.start();
        started = true;
        if (disposed) {
          void conn.stop().catch(() => {});
          return;
        }
        await conn.invoke('JoinGroup', `niche-${profileId}`);
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
    }

    void connect();
  });
}
