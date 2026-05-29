'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  disconnectGoogle,
  getGoogleConnectUrl,
  getGoogleIntegrationStatus,
  type GoogleIntegrationStatus,
} from '@/lib/seo-api';

type GoogleSettingsProps = Readonly<{
  projectId: string;
  accessToken: string | null;
}>;

export function GoogleSettings({ projectId, accessToken }: GoogleSettingsProps) {
  const [status, setStatus] = useState<GoogleIntegrationStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const refresh = useCallback(async () => {
    try {
      setError(null);
      setStatus(await getGoogleIntegrationStatus(projectId, accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not load Google status');
      setStatus({ connected: false, gscConnected: false, ga4Connected: false });
    }
  }, [projectId, accessToken]);

  useEffect(() => {
    const timer = setTimeout(() => {
      void refresh();
    }, 0);
    return () => clearTimeout(timer);
  }, [refresh]);

  async function onConnect() {
    setLoading(true);
    setError(null);
    try {
      const { url } = await getGoogleConnectUrl(projectId, accessToken);
      globalThis.location.href = url;
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not start Google connect');
      setLoading(false);
    }
  }

  async function onDisconnect() {
    setLoading(true);
    setError(null);
    try {
      await disconnectGoogle(projectId, accessToken);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Disconnect failed');
    } finally {
      setLoading(false);
    }
  }

  if (!status) return <p className="text-sm text-[var(--color-text-secondary)]">Loading Google integration…</p>;

  if (status.connected) {
    return (
      <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-sm">
        <p className="font-medium text-green-900">Google connected</p>
        {status.siteUrl ? (
          <p className="mt-1 text-green-800">Search Console: {status.siteUrl}</p>
        ) : null}
        {status.propertyId ? (
          <p className="mt-1 text-green-800">GA4 property: {status.propertyId}</p>
        ) : null}
        <button
          type="button"
          className="mt-3 text-xs text-red-700 underline"
          disabled={loading}
          onClick={() => void onDisconnect()}
        >
          Disconnect Google
        </button>
        {error ? <p className="mt-2 text-xs text-red-600">{error}</p> : null}
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm">
      <p className="font-medium text-amber-950">Connect Google Search Console &amp; Analytics</p>
      <p className="mt-1 text-amber-900/90">
        Required for rankings and analytics. You will be redirected to Google to authorize read-only access.
      </p>
      <button
        type="button"
        disabled={loading}
        onClick={() => void onConnect()}
        className="mt-3 rounded-lg bg-[var(--color-accent)] px-4 py-2 text-xs text-white disabled:opacity-60"
      >
        {loading ? 'Redirecting…' : 'Connect Google'}
      </button>
      {error ? <p className="mt-2 text-xs text-red-600">{error}</p> : null}
    </div>
  );
}
