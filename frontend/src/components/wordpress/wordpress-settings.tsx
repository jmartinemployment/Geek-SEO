'use client';

import { useEffect, useState } from 'react';
import {
  connectWordPress,
  getWordPressStatus,
  type WordPressConnectionStatus,
} from '@/lib/seo-api';

type WordPressSettingsProps = {
  projectId: string;
  accessToken: string | null;
};

export function WordPressSettings({ projectId, accessToken }: WordPressSettingsProps) {
  const [status, setStatus] = useState<WordPressConnectionStatus | null>(null);
  const [expanded, setExpanded] = useState(false);
  const [siteUrl, setSiteUrl] = useState('https://');
  const [username, setUsername] = useState('');
  const [appPassword, setAppPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    void (async () => {
      try {
        const next = await getWordPressStatus(projectId, accessToken);
        setStatus(next);
        if (next.connected) setExpanded(true);
      } catch {
        setStatus({ connected: false, defaultPostStatus: 'draft' });
      }
    })();
  }, [projectId, accessToken]);

  async function onConnect(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await connectWordPress(
        projectId,
        { siteUrl, username, applicationPassword: appPassword },
        accessToken,
      );
      setStatus(await getWordPressStatus(projectId, accessToken));
      setAppPassword('');
      setExpanded(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Connection failed');
    } finally {
      setSaving(false);
    }
  }

  if (!status) return <p className="text-sm text-zinc-500">Loading WordPress…</p>;

  if (status.connected) {
    return (
      <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-sm">
        <p className="font-medium text-green-900">WordPress connected</p>
        <p className="mt-1 text-green-800">
          {status.siteUrl} · {status.username} · default {status.defaultPostStatus}
        </p>
      </div>
    );
  }

  return (
    <div className="mt-6 rounded-lg border bg-zinc-50/50">
      <button
        type="button"
        className="flex w-full items-center justify-between px-4 py-3 text-left text-sm"
        onClick={() => setExpanded((v) => !v)}
        aria-expanded={expanded}
      >
        <span>
          <span className="font-medium text-zinc-800">WordPress publish</span>
          <span className="ml-2 text-xs text-zinc-500">(optional)</span>
        </span>
        <span className="text-zinc-400">{expanded ? '−' : '+'}</span>
      </button>
      {expanded && (
        <form onSubmit={onConnect} className="space-y-3 border-t bg-white px-4 py-4">
          <p className="text-xs text-zinc-500">
            Skip this if you do not have a WordPress site yet. You can still write, score, and export
            HTML from the editor.
          </p>
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="https://yoursite.com"
            value={siteUrl}
            onChange={(e) => setSiteUrl(e.target.value)}
            required
          />
          <input
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="WordPress username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
          />
          <input
            type="password"
            className="w-full rounded border px-3 py-2 text-sm"
            placeholder="Application password"
            value={appPassword}
            onChange={(e) => setAppPassword(e.target.value)}
            required
          />
          {error && <p className="text-xs text-red-600">{error}</p>}
          <button
            type="submit"
            disabled={saving}
            className="rounded bg-zinc-900 px-3 py-2 text-sm text-white disabled:opacity-50"
          >
            {saving ? 'Connecting…' : 'Connect WordPress'}
          </button>
        </form>
      )}
    </div>
  );
}
