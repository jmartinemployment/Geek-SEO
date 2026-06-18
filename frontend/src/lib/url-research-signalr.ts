import { getUrlResearch, type UrlResearchFull } from '@/lib/seo-api';
import type { SeoHubApi } from '@/components/signalr/seo-hub-provider';

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

export type SubscribeUrlResearchProjectProgressOptions = {
  projectId: string;
  onProgress: (update: { urlResearchId: string; status: string; message?: string }) => void;
};

/** Subscribes to all page-research updates for a project via the shared hub. */
export function subscribeUrlResearchProjectProgress(
  hub: SeoHubApi,
  options: SubscribeUrlResearchProjectProgressOptions,
): () => void {
  const { projectId, onProgress } = options;
  const leave = hub.joinUrlResearchProject(projectId);

  const unsub = hub.subscribe('UrlResearchProgress', (raw: unknown) => {
    const msg = raw as UrlResearchProgressMsg;
    const urlResearchId = msgResearchId(msg);
    const status = msgStatus(msg);
    if (!urlResearchId || !status) return;
    const message = msg.message ?? msg.Message ?? undefined;
    onProgress({ urlResearchId, status, message });
  });

  return () => {
    unsub();
    leave();
  };
}

export type SubscribeUrlResearchProgressOptions = {
  urlResearchId: string;
  projectId: string;
  onStatus?: (status: string, message?: string) => void;
  onTerminal?: (status: string) => void;
};

export function subscribeUrlResearchProgress(
  hub: SeoHubApi,
  options: SubscribeUrlResearchProgressOptions,
): () => void {
  const { urlResearchId, projectId, onStatus, onTerminal } = options;
  const leaveResearch = hub.joinUrlResearch(urlResearchId);
  const leaveProject = hub.joinUrlResearchProject(projectId);

  const unsub = hub.subscribe('UrlResearchProgress', (raw: unknown) => {
    const msg = raw as UrlResearchProgressMsg;
    if (!idsMatch(msgResearchId(msg), urlResearchId)) return;

    const status = msgStatus(msg);
    if (!status) return;

    const message = msg.message ?? msg.Message ?? undefined;
    onStatus?.(status, message);

    if (TERMINAL.has(status)) {
      onTerminal?.(status);
    }
  });

  return () => {
    unsub();
    leaveResearch();
    leaveProject();
  };
}

export type WaitForUrlResearchOptions = {
  urlResearchId: string;
  projectId: string;
  hub: SeoHubApi;
  accessToken?: string | null;
  timeoutMs?: number;
  onStatus?: (status: string, message?: string) => void;
};

export async function waitForUrlResearchViaSignalR(
  options: WaitForUrlResearchOptions,
): Promise<UrlResearchFull> {
  const { urlResearchId, projectId, hub, accessToken, onStatus } = options;
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

        cleanup = subscribeUrlResearchProgress(hub, {
          urlResearchId,
          projectId,
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
