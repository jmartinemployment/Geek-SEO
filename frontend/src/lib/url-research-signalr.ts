import { getHubUrl, getUrlResearch, type UrlResearchFull } from '@/lib/seo-api';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;
const TERMINAL = new Set(['completed', 'failed']);

export type UrlResearchProgressMsg = {
  urlResearchId?: string;
  UrlResearchId?: string;
  projectId?: string;
  ProjectId?: string;
  status?: string;
  Status?: string;
  message?: string;
  Message?: string;
  errorMessage?: string;
  ErrorMessage?: string;
};

function hubUrl(accessToken?: string | null): string {
  const base = getHubUrl();
  if (!accessToken && DEV_USER_ID) {
    return `${base}?access_token=${encodeURIComponent(DEV_USER_ID)}`;
  }
  return base;
}

function idsMatch(a: string | undefined, b: string): boolean {
  const left = a?.toLowerCase();
  const right = b.toLowerCase();
  return !left || left === right;
}

function msgResearchId(msg: UrlResearchProgressMsg): string | undefined {
  return msg.urlResearchId ?? msg.UrlResearchId;
}

function msgStatus(msg: UrlResearchProgressMsg): string | undefined {
  return (msg.status ?? msg.Status)?.toLowerCase();
}

function researchGroup(urlResearchId: string): string {
  return `url-research-${urlResearchId}`;
}

function projectGroup(projectId: string): string {
  return `url-research-project-${projectId}`;
}

export type SubscribeUrlResearchProjectProgressOptions = {
  projectId: string;
  accessToken?: string | null;
  onProgress: (update: { urlResearchId: string; status: string; message?: string }) => void;
};

/**
 * Subscribes to all page-research updates for a project (list row status patches).
 */
export async function subscribeUrlResearchProjectProgress(
  options: SubscribeUrlResearchProjectProgressOptions,
): Promise<() => void> {
  const { projectId, accessToken, onProgress } = options;

  const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl(accessToken), {
      accessTokenFactory: () => accessToken ?? '',
      withCredentials: true,
    })
    .configureLogging(LogLevel.None)
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
    .build();

  let disposed = false;

  connection.on('UrlResearchProgress', (raw: UrlResearchProgressMsg) => {
    const urlResearchId = msgResearchId(raw);
    const status = msgStatus(raw);
    if (!urlResearchId || !status) return;

    const message = raw.message ?? raw.Message ?? undefined;
    onProgress({ urlResearchId, status, message });
  });

  await connection.start();
  if (disposed) {
    await connection.stop().catch(() => {});
    return () => {};
  }

  await connection.invoke('JoinGroup', projectGroup(projectId));

  return () => {
    disposed = true;
    void connection.stop().catch(() => {});
  };
}

export type SubscribeUrlResearchProgressOptions = {
  urlResearchId: string;
  projectId: string;
  accessToken?: string | null;
  onStatus?: (status: string, message?: string) => void;
  onTerminal?: (status: string) => void;
};

/**
 * Subscribes to live page-research job updates via SignalR (no polling).
 * Returns a cleanup function that stops the hub connection.
 */
export async function subscribeUrlResearchProgress(
  options: SubscribeUrlResearchProgressOptions,
): Promise<() => void> {
  const { urlResearchId, projectId, accessToken, onStatus, onTerminal } = options;

  const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl(accessToken), {
      accessTokenFactory: () => accessToken ?? '',
      withCredentials: true,
    })
    .configureLogging(LogLevel.None)
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
    .build();

  let disposed = false;

  connection.on('UrlResearchProgress', (raw: UrlResearchProgressMsg) => {
    if (!idsMatch(msgResearchId(raw), urlResearchId)) return;

    const status = msgStatus(raw);
    if (!status) return;

    const message = raw.message ?? raw.Message ?? undefined;
    onStatus?.(status, message);

    if (TERMINAL.has(status)) {
      onTerminal?.(status);
    }
  });

  await connection.start();
  if (disposed) {
    await connection.stop().catch(() => {});
    return () => {};
  }

  await connection.invoke('JoinGroup', researchGroup(urlResearchId));
  await connection.invoke('JoinGroup', projectGroup(projectId));

  return () => {
    disposed = true;
    void connection.stop().catch(() => {});
  };
}

export type WaitForUrlResearchOptions = {
  urlResearchId: string;
  projectId: string;
  accessToken?: string | null;
  timeoutMs?: number;
  onStatus?: (status: string, message?: string) => void;
};

/**
 * Waits until a page-research job reaches a terminal state via SignalR, then hydrates once from GET.
 */
export async function waitForUrlResearchViaSignalR(
  options: WaitForUrlResearchOptions,
): Promise<UrlResearchFull> {
  const { urlResearchId, projectId, accessToken, onStatus } = options;
  const timeoutMs = options.timeoutMs ?? 15 * 60 * 1000;

  return new Promise((resolve, reject) => {
    let settled = false;
    let cleanup: (() => void) | null = null;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    const finish = async (hydrate: () => Promise<UrlResearchFull>) => {
      if (settled) return;
      settled = true;
      if (timeoutId) clearTimeout(timeoutId);
      cleanup?.();
      try {
        resolve(await hydrate());
      } catch (e) {
        reject(e instanceof Error ? e : new Error('Could not load page research.'));
      }
    };

    const fail = (error: Error) => {
      if (settled) return;
      settled = true;
      if (timeoutId) clearTimeout(timeoutId);
      cleanup?.();
      reject(error);
    };

    timeoutId = setTimeout(() => {
      void (async () => {
        try {
          const row = await getUrlResearch(urlResearchId, accessToken);
          if (TERMINAL.has(row.status)) {
            await finish(async () => row);
            return;
          }
          fail(new Error('Timed out waiting for page research to finish. Try Refresh on the list.'));
        } catch (e) {
          fail(e instanceof Error ? e : new Error('Timed out waiting for page research.'));
        }
      })();
    }, timeoutMs);

    void (async () => {
      try {
        const initial = await getUrlResearch(urlResearchId, accessToken);
        if (TERMINAL.has(initial.status)) {
          await finish(async () => initial);
          return;
        }

        onStatus?.(initial.status);

        cleanup = await subscribeUrlResearchProgress({
          urlResearchId,
          projectId,
          accessToken,
          onStatus,
          onTerminal: (status) => {
            void finish(async () => {
              const row = await getUrlResearch(urlResearchId, accessToken);
              if (status === 'failed' && row.status === 'failed') return row;
              if (status === 'completed' && row.status === 'completed') return row;
              return getUrlResearch(urlResearchId, accessToken);
            });
          },
        });
      } catch (e) {
        fail(
          e instanceof Error
            ? new Error(`Live progress unavailable: ${e.message}`)
            : new Error('Live progress unavailable. Refresh the list and try again.'),
        );
      }
    })();
  });
}
