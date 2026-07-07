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
import * as signalR from '@microsoft/signalr';
import { useAuth } from '@/components/auth/auth-provider';
import { getHubUrl } from '@/lib/seo-api';
import { ALWAYS_WIRED_USER_EVENTS } from '@/lib/seo-hub-events';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

type GroupEntry = {
  refCount: number;
  rejoin: () => Promise<void>;
  leave: () => Promise<void>;
};

export type SeoHubContextValue = {
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  whenConnected: (timeoutMs?: number) => Promise<void>;
  subscribe: (event: string, handler: (...args: unknown[]) => void) => () => void;
  joinDocument: (documentId: string) => () => void;
  joinNicheProfile: (profileId: string) => () => void;
};

export type SeoHubApi = Pick<
  SeoHubContextValue,
  | 'connection'
  | 'isConnected'
  | 'whenConnected'
  | 'subscribe'
  | 'joinDocument'
  | 'joinNicheProfile'
>;

const SeoHubContext = createContext<SeoHubContextValue | null>(null);

function hubUrl(accessToken: string | null): string {
  const base = getHubUrl();
  if (!accessToken && DEV_USER_ID) {
    return `${base}?access_token=${encodeURIComponent(DEV_USER_ID)}`;
  }
  return base;
}

function groupKey(kind: string, id: string): string {
  return `${kind}:${id}`;
}

const DEFAULT_HUB_CONNECT_TIMEOUT_MS = 15_000;

const ALWAYS_WIRED_EVENT_SET = new Set<string>(ALWAYS_WIRED_USER_EVENTS);

function waitForHubConnected(
  conn: signalR.HubConnection,
  timeoutMs: number,
): Promise<void> {
  if (conn.state === signalR.HubConnectionState.Connected) {
    return Promise.resolve();
  }

  return new Promise((resolve, reject) => {
    const deadline = Date.now() + timeoutMs;

    const poll = () => {
      if (conn.state === signalR.HubConnectionState.Connected) {
        resolve();
        return;
      }
      if (Date.now() >= deadline) {
        reject(new Error('Timed out waiting for SignalR hub connection.'));
        return;
      }
      setTimeout(poll, 50);
    };

    poll();
  });
}

export function SeoHubProvider({ children }: { children: ReactNode }) {
  const { accessToken, isAuthenticated } = useAuth();
  const tokenRef = useRef(accessToken);
  tokenRef.current = accessToken;

  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const groupsRef = useRef<Map<string, GroupEntry>>(new Map());
  const eventHandlersRef = useRef<Map<string, Set<(...args: unknown[]) => void>>>(new Map());
  const rootListenersRef = useRef<Map<string, (...args: unknown[]) => void>>(new Map());

  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  const attachRootListener = useCallback((conn: signalR.HubConnection, event: string) => {
    if (rootListenersRef.current.has(event)) return;
    const rootListener = (...args: unknown[]) => {
      const set = eventHandlersRef.current.get(event);
      if (!set) return;
      for (const h of set) h(...args);
    };
    rootListenersRef.current.set(event, rootListener);
    conn.on(event, rootListener);
  }, []);

  const wirePendingEventListeners = useCallback(
    (conn: signalR.HubConnection) => {
      for (const event of ALWAYS_WIRED_USER_EVENTS) {
        attachRootListener(conn, event);
      }
      for (const [event, handlers] of eventHandlersRef.current) {
        if (handlers.size > 0) attachRootListener(conn, event);
      }
    },
    [attachRootListener],
  );

  const invokeWhenConnected = useCallback(async (fn: () => Promise<void>) => {
    const conn = connectionRef.current;
    if (!conn || conn.state !== signalR.HubConnectionState.Connected) return;
    await fn();
  }, []);

  const rejoinAllGroups = useCallback(async () => {
    const conn = connectionRef.current;
    if (!conn || conn.state !== signalR.HubConnectionState.Connected) return;

    const entries = [...groupsRef.current.values()].filter((e) => e.refCount > 0);
    await Promise.all(
      entries.map((entry) =>
        entry.rejoin().catch(() => {
          /* reconnect will retry on next onreconnected */
        }),
      ),
    );
  }, []);

  const registerGroup = useCallback(
    (
      key: string,
      rejoin: () => Promise<void>,
      leave: () => Promise<void>,
    ): (() => void) => {
      const map = groupsRef.current;
      const existing = map.get(key);
      if (existing) {
        existing.refCount += 1;
        return () => {
          const entry = map.get(key);
          if (!entry) return;
          entry.refCount -= 1;
          if (entry.refCount <= 0) {
            map.delete(key);
            void invokeWhenConnected(entry.leave).catch(() => {});
          }
        };
      }

      map.set(key, { refCount: 1, rejoin, leave });
      void invokeWhenConnected(rejoin).catch(() => {});

      return () => {
        const entry = map.get(key);
        if (!entry) return;
        entry.refCount -= 1;
        if (entry.refCount <= 0) {
          map.delete(key);
          void invokeWhenConnected(entry.leave).catch(() => {});
        }
      };
    },
    [invokeWhenConnected],
  );

  const subscribe = useCallback(
    (event: string, handler: (...args: unknown[]) => void) => {
      let handlers = eventHandlersRef.current.get(event);
      if (!handlers) {
        handlers = new Set();
        eventHandlersRef.current.set(event, handlers);
      }

      handlers.add(handler);

      const conn = connectionRef.current;
      if (conn) attachRootListener(conn, event);

      return () => {
        const set = eventHandlersRef.current.get(event);
        if (!set) return;
        set.delete(handler);
        if (set.size === 0) {
          eventHandlersRef.current.delete(event);
          if (!ALWAYS_WIRED_EVENT_SET.has(event)) {
            const root = rootListenersRef.current.get(event);
            if (root && connectionRef.current) {
              connectionRef.current.off(event, root);
            }
            rootListenersRef.current.delete(event);
          }
        }
      };
    },
    [attachRootListener],
  );

  const whenConnected = useCallback((timeoutMs = DEFAULT_HUB_CONNECT_TIMEOUT_MS) => {
    const conn = connectionRef.current;
    if (!conn) {
      return Promise.reject(new Error('SignalR hub is not initialized.'));
    }
    return waitForHubConnected(conn, timeoutMs);
  }, []);

  const joinDocument = useCallback(
    (documentId: string) =>
      registerGroup(
        groupKey('document', documentId),
        async () => {
          await connectionRef.current!.invoke('JoinDocument', documentId);
        },
        async () => {
          await connectionRef.current!.invoke('LeaveDocument', documentId);
        },
      ),
    [registerGroup],
  );

  const joinNicheProfile = useCallback(
    (profileId: string) =>
      registerGroup(
        groupKey('niche', profileId),
        async () => {
          await connectionRef.current!.invoke('JoinNicheProfile', profileId);
        },
        async () => {
          await connectionRef.current!.invoke('LeaveNicheProfile', profileId);
        },
      ),
    [registerGroup],
  );

  useEffect(() => {
    const canConnect = isAuthenticated || Boolean(DEV_USER_ID);
    if (!canConnect) {
      void connectionRef.current?.stop();
      connectionRef.current = null;
      setConnection(null);
      groupsRef.current.clear();
      eventHandlersRef.current.clear();
      rootListenersRef.current.clear();
      setIsConnected(false);
      return;
    }

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl(accessToken), {
        accessTokenFactory: () => tokenRef.current ?? '',
        withCredentials: true,
      })
      .configureLogging(signalR.LogLevel.Warning)
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
      .build();

    conn.onreconnected(() => {
      setIsConnected(true);
      wirePendingEventListeners(conn);
      void rejoinAllGroups();
    });

    conn.onclose(() => {
      setIsConnected(false);
    });

    connectionRef.current = conn;
    setConnection(conn);

    void (async () => {
      try {
        await conn.start();
        setIsConnected(true);
        wirePendingEventListeners(conn);
        await rejoinAllGroups();
      } catch {
        setIsConnected(false);
      }
    })();

    return () => {
      void conn.stop();
      connectionRef.current = null;
      setConnection(null);
      groupsRef.current.clear();
      rootListenersRef.current.clear();
      setIsConnected(false);
    };
  }, [accessToken, isAuthenticated, rejoinAllGroups, wirePendingEventListeners]);

  const value = useMemo<SeoHubContextValue>(
    () => ({
      connection,
      isConnected,
      whenConnected,
      subscribe,
      joinDocument,
      joinNicheProfile,
    }),
    [
      connection,
      isConnected,
      whenConnected,
      subscribe,
      joinDocument,
      joinNicheProfile,
    ],
  );

  return <SeoHubContext.Provider value={value}>{children}</SeoHubContext.Provider>;
}

export function useSeoHub(): SeoHubContextValue {
  const ctx = useContext(SeoHubContext);
  if (!ctx) throw new Error('useSeoHub must be used within SeoHubProvider');
  return ctx;
}

export function useSeoHubOptional(): SeoHubContextValue | null {
  return useContext(SeoHubContext);
}
