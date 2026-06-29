'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import {
  buildCrawlProgressStreamUrl,
  fetchCrawlProgressCatchup,
  fetchCrawlProgressStatus,
  parseCrawlProgressPayload,
  shouldReplaceCrawlProgress,
  statusResponseToProgress,
  type CompetitorCrawlProgressPayload,
} from '@/lib/crawlProgressStream';
import { getSiteAnalyzer2ApiBase } from '@/lib/site-analyzer2-api';

type RunProgressMap = Record<string, CompetitorCrawlProgressPayload>;

type CrawlStreamContextValue = {
  subscribeToRun: (runId: string) => void;
  unsubscribeFromRun: (runId: string) => void;
  runProgress: RunProgressMap;
};

const CrawlStreamContext = createContext<CrawlStreamContextValue | undefined>(undefined);

export function CrawlStreamProvider({
  children,
  accessToken,
}: {
  children: ReactNode;
  accessToken?: string | null;
}) {
  const [runProgress, setRunProgress] = useState<RunProgressMap>({});
  const connectionsRef = useRef<Record<string, { sse: EventSource; count: number }>>({});
  const activeRunsRef = useRef<Set<string>>(new Set());
  const sequenceTrackerRef = useRef<Record<string, number>>({});
  const apiBase = getSiteAnalyzer2ApiBase();

  const applyProgress = useCallback((runId: string, payload: CompetitorCrawlProgressPayload) => {
    if (payload.sequenceNumber) {
      sequenceTrackerRef.current[runId] = payload.sequenceNumber;
    }

    setRunProgress((prev) => {
      const current = prev[runId];
      if (!shouldReplaceCrawlProgress(current, payload)) return prev;
      return { ...prev, [runId]: payload };
    });
  }, []);

  const syncAfterReconnect = useCallback(
    async (runId: string) => {
      const lastSeq = sequenceTrackerRef.current[runId] ?? 0;

      if (lastSeq > 0) {
        const missed = await fetchCrawlProgressCatchup(apiBase, runId, lastSeq, accessToken);
        for (const item of missed) {
          const payload = parseCrawlProgressPayload(item.payload);
          if (!payload) continue;
          applyProgress(runId, { ...payload, sequenceNumber: item.sequenceNumber });
        }
        return;
      }

      const status = await fetchCrawlProgressStatus(apiBase, runId, accessToken);
      if (!status) return;
      applyProgress(runId, statusResponseToProgress(runId, status));
    },
    [accessToken, apiBase, applyProgress],
  );

  const subscribeToRun = useCallback(
    (runId: string) => {
      if (!runId) return;

      const active = connectionsRef.current[runId];
      if (active) {
        active.count += 1;
        return;
      }

      activeRunsRef.current.add(runId);

      const sse = new EventSource(buildCrawlProgressStreamUrl(apiBase, runId, accessToken));

      sse.onopen = () => {
        void syncAfterReconnect(runId);
      };

      sse.onmessage = (event) => {
        const payload = parseCrawlProgressPayload(event.data);
        if (!payload) return;
        applyProgress(runId, payload);
      };

      connectionsRef.current[runId] = { sse, count: 1 };
    },
    [accessToken, apiBase, applyProgress, syncAfterReconnect],
  );

  const unsubscribeFromRun = useCallback((runId: string) => {
    const active = connectionsRef.current[runId];
    if (!active) return;

    active.count -= 1;
    if (active.count > 0) return;

    active.sse.close();
    delete connectionsRef.current[runId];
    activeRunsRef.current.delete(runId);

    setRunProgress((prev) => {
      if (!(runId in prev)) return prev;
      const next = { ...prev };
      delete next[runId];
      return next;
    });
  }, []);

  useEffect(() => {
    const onOnline = () => {
      for (const runId of activeRunsRef.current) {
        void syncAfterReconnect(runId);
      }
    };

    window.addEventListener('online', onOnline);
    return () => window.removeEventListener('online', onOnline);
  }, [syncAfterReconnect]);

  useEffect(() => {
    const connections = connectionsRef.current;
    return () => {
      for (const connection of Object.values(connections)) {
        connection.sse.close();
      }
      connectionsRef.current = {};
      activeRunsRef.current.clear();
    };
  }, []);

  const value = useMemo(
    () => ({ subscribeToRun, unsubscribeFromRun, runProgress }),
    [subscribeToRun, unsubscribeFromRun, runProgress],
  );

  return <CrawlStreamContext.Provider value={value}>{children}</CrawlStreamContext.Provider>;
}

export function useCrawlStream() {
  const context = useContext(CrawlStreamContext);
  if (!context) {
    throw new Error('useCrawlStream must be used inside CrawlStreamProvider');
  }
  return context;
}

export function useCrawlProgress(runId: string): CompetitorCrawlProgressPayload | null {
  const { subscribeToRun, unsubscribeFromRun, runProgress } = useCrawlStream();

  useEffect(() => {
    if (!runId) return undefined;

    subscribeToRun(runId);
    return () => unsubscribeFromRun(runId);
  }, [runId, subscribeToRun, unsubscribeFromRun]);

  if (!runId) return null;
  return runProgress[runId] ?? null;
}
