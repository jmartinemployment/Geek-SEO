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

type AuthContextValue = {
  accessToken: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  setAccessToken: (token: string | null, expiresInSeconds?: number) => void;
  refreshAccessToken: () => Promise<string | null>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const scheduleRefresh = useCallback((expiresInSeconds: number) => {
    if (refreshTimer.current) clearTimeout(refreshTimer.current);
    const ms = Math.max(30_000, (expiresInSeconds - 60) * 1000);
    refreshTimer.current = setTimeout(() => {
      void refreshAccessToken();
    }, ms);
  }, []);

  const refreshAccessToken = useCallback(async () => {
    try {
      const res = await fetch('/api/auth/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ grantType: 'refresh_token' }),
      });
      if (!res.ok) {
        setToken(null);
        return null;
      }
      const data = (await res.json()) as { accessToken: string; expiresIn: number };
      setToken(data.accessToken);
      scheduleRefresh(data.expiresIn);
      return data.accessToken;
    } catch {
      setToken(null);
      return null;
    }
  }, [scheduleRefresh]);

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
      await refreshAccessToken();
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
      isAuthenticated: Boolean(accessToken),
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
