'use client';

import { useEffect, useRef } from 'react';
import { getHubUrl, getNicheAnalysisStatus, type NicheAnalysisStatus } from '@/lib/seo-api';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

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

function normalizeId(id: string | undefined): string | undefined {
  return id?.toLowerCase();
}

function idsMatch(a: string | undefined, b: string): boolean {
  const left = normalizeId(a);
  const right = normalizeId(b);
  return !left || left === right;
}

function msgProfileId(msg: AnalysisProgressMsg): string | undefined {
  return msg.profileId ?? msg.ProfileId;
}

function msgStatus(msg: AnalysisProgressMsg): string | undefined {
  return msg.status ?? msg.Status;
}

/**
 * Push-driven niche analysis updates via SignalR.
 * Hydrates from GET /status once on connect and after each hub event (debounced) — no interval polling.
 */
export function useNicheAnalysisSignalR(
  profileId: string | null,
  accessToken: string | null | undefined,
  onStatus: (status: NicheAnalysisStatus) => void,
  options?: {
    onComplete?: (profileId: string) => void;
    onConnectionError?: (message: string) => void;
  },
): void {
  const onStatusRef = useRef(onStatus);
  const onCompleteRef = useRef(options?.onComplete);
  const onConnectionErrorRef = useRef(options?.onConnectionError);

  useEffect(() => {
    onStatusRef.current = onStatus;
    onCompleteRef.current = options?.onComplete;
    onConnectionErrorRef.current = options?.onConnectionError;
  });

  useEffect(() => {
    if (!profileId || !accessToken) return;

    let disposed = false;
    let started = false;
    let hydrateTimer: ReturnType<typeof setTimeout> | null = null;
    let connection: import('@microsoft/signalr').HubConnection | null = null;

    async function hydrate(): Promise<NicheAnalysisStatus | null> {
      try {
        const status = await getNicheAnalysisStatus(profileId, accessToken);
        if (!disposed) onStatusRef.current(status);
        return status;
      } catch {
        if (!disposed) {
          onConnectionErrorRef.current?.('Could not refresh analysis status.');
        }
        return null;
      }
    }

    function scheduleHydrate() {
      if (hydrateTimer) clearTimeout(hydrateTimer);
      hydrateTimer = setTimeout(() => {
        hydrateTimer = null;
        void hydrate();
      }, 250);
    }

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
          if (!idsMatch(msgProfileId(raw), profileId)) return;

          const status = msgStatus(raw)?.toLowerCase();
          if (status === 'complete' && (raw.step ?? raw.Step) === 'complete') {
            void (async () => {
              const hydrated = await hydrate();
              if (!disposed && hydrated?.status === 'complete') {
                onCompleteRef.current?.(profileId);
              }
            })();
            return;
          }

          scheduleHydrate();
        });

        conn.onreconnected(() => {
          void hydrate();
        });

        connection = conn;
        await conn.start();
        started = true;
        if (disposed) {
          void conn.stop().catch(() => {});
          return;
        }
        await conn.invoke('JoinGroup', `niche-${profileId}`);
        await hydrate();
      } catch (e) {
        if (!disposed) {
          onConnectionErrorRef.current?.(
            e instanceof Error ? e.message : 'Live progress connection failed.',
          );
        }
      }
    }

    void connect();

    return () => {
      disposed = true;
      if (hydrateTimer) clearTimeout(hydrateTimer);
      const conn = connection;
      connection = null;
      if (conn && started) void conn.stop().catch(() => {});
    };
  }, [profileId, accessToken]);
}
