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
  computeRefreshDelayMs,
  isAuthenticated,
  shouldBootstrapSession,
} from '@/lib/auth/session-policy';
import { agentDebugLog } from '@/lib/agent-debug-log';

type AuthContextValue = {
  accessToken: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  setAccessToken: (token: string | null, expiresInSeconds?: number) => void;
  refreshAccessToken: () => Promise<string | null>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const refreshAccessTokenRef = useRef<() => Promise<string | null>>(async () => null);

  const scheduleRefresh = useCallback((expiresInSeconds: number) => {
    if (refreshTimer.current) clearTimeout(refreshTimer.current);
    refreshTimer.current = setTimeout(() => {
      void refreshAccessTokenRef.current();
    }, computeRefreshDelayMs(expiresInSeconds));
  }, []);

  const refreshAccessToken = useCallback(async () => {
    try {
      const res = await fetch('/api/auth/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ grantType: 'refresh_token' }),
      });
      if (!res.ok) {
        let errorPreview = '';
        try {
          errorPreview = (await res.clone().text()).slice(0, 200);
        } catch {
          errorPreview = '';
        }
        const sessionExpired =
          res.status === 401 || errorPreview.includes('invalid_grant') || errorPreview.includes('sessionExpired');
        // #region agent log
        agentDebugLog(
          'H-A',
          'auth-provider.tsx:refresh-failed',
          'client refresh token request failed',
          { status: res.status, sessionExpired, errorPreview },
        );
        // #endregion
        if (sessionExpired) {
          await fetch('/api/auth/logout', { method: 'POST' });
          // #region agent log
          agentDebugLog('H-A', 'auth-provider.tsx:session-cleared', 'cleared stale session after refresh failure', {});
          agentDebugLog('H-A', 'auth-provider.tsx:redirect-login', 'redirecting to login after session expiry', {
            status: res.status,
          });
          // #endregion
          setToken(null);
          globalThis.location.assign('/auth/login?reason=session-expired');
          return null;
        }
        setToken(null);
        return null;
      }
      const data = (await res.json()) as { accessToken: string | null; expiresIn: number };
      if (!data.accessToken) {
        // #region agent log
        agentDebugLog(
          'H-A',
          'auth-provider.tsx:refresh-null-token',
          'refresh returned no access token',
          { expiresIn: data.expiresIn },
        );
        // #endregion
        setToken(null);
        return null;
      }
      setToken(data.accessToken);
      scheduleRefresh(data.expiresIn);
      return data.accessToken;
    } catch {
      setToken(null);
      return null;
    }
  }, [scheduleRefresh]);

  useEffect(() => {
    refreshAccessTokenRef.current = refreshAccessToken;
  }, [refreshAccessToken]);

  const setAccessToken = useCallback(
    (token: string | null, expiresInSeconds = 900) => {
      setToken(token);
      if (token) scheduleRefresh(expiresInSeconds);
    },
    [scheduleRefresh],
  );

  const logout = useCallback(async () => {
    await fetch('/api/auth/logout', { method: 'POST' });
    setToken(null);
    if (refreshTimer.current) clearTimeout(refreshTimer.current);
  }, []);

  useEffect(() => {
    void (async () => {
      const path = globalThis.location?.pathname ?? '';
      if (shouldBootstrapSession(path, DEV_USER_ID)) {
        await refreshAccessToken();
      }
      setIsLoading(false);
    })();
    return () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
    };
  }, [refreshAccessToken]);

  const value = useMemo(
    () => ({
      accessToken,
      isLoading,
      isAuthenticated: isAuthenticated(accessToken, DEV_USER_ID),
      setAccessToken,
      refreshAccessToken,
      logout,
    }),
    [accessToken, isLoading, setAccessToken, refreshAccessToken, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
