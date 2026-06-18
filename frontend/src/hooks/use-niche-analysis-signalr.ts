'use client';

import { useEffect, useRef } from 'react';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import { getNicheAnalysisStatus, type NicheAnalysisStatus } from '@/lib/seo-api';

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

export function useNicheAnalysisSignalR(
  profileId: string | null,
  accessToken: string | null | undefined,
  onStatus: (status: NicheAnalysisStatus) => void,
  options?: {
    onComplete?: (profileId: string) => void;
    onConnectionError?: (message: string) => void;
  },
): void {
  const hub = useSeoHub();
  const onStatusRef = useRef(onStatus);
  const onCompleteRef = useRef(options?.onComplete);
  const onConnectionErrorRef = useRef(options?.onConnectionError);

  useEffect(() => {
    onStatusRef.current = onStatus;
    onCompleteRef.current = options?.onComplete;
    onConnectionErrorRef.current = options?.onConnectionError;
  });

  useEffect(() => {
    if (!profileId || !accessToken || !hub.isConnected) return;

    const activeProfileId = profileId;
    let hydrateTimer: ReturnType<typeof setTimeout> | null = null;

    async function hydrate(): Promise<NicheAnalysisStatus | null> {
      try {
        const status = await getNicheAnalysisStatus(activeProfileId, accessToken);
        onStatusRef.current(status);
        return status;
      } catch {
        onConnectionErrorRef.current?.('Could not refresh analysis status.');
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

    const leave = hub.joinNicheProfile(activeProfileId);
    const unsub = hub.subscribe('AnalysisProgress', (raw: unknown) => {
      const msg = raw as AnalysisProgressMsg;
      if (!idsMatch(msgProfileId(msg), activeProfileId)) return;

      const status = msgStatus(msg)?.toLowerCase();
      if (status === 'complete' && (msg.step ?? msg.Step) === 'complete') {
        void (async () => {
          const hydrated = await hydrate();
          if (hydrated?.status === 'complete') {
            onCompleteRef.current?.(activeProfileId);
          }
        })();
        return;
      }

      scheduleHydrate();
    });

    void hydrate();

    return () => {
      if (hydrateTimer) clearTimeout(hydrateTimer);
      unsub();
      leave();
    };
  }, [profileId, accessToken, hub, hub.isConnected]);
}
