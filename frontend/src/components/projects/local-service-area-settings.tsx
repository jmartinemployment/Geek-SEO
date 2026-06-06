'use client';

import { useCallback, useEffect, useState } from 'react';
import { SeoErrorBanner } from '@/components/seo/seo-error-banner';
import { updateProject, type SeoProject } from '@/lib/seo-api';

const MIN_RADIUS = 5;
const MAX_RADIUS = 100;
const DEFAULT_RADIUS = 20;

type Props = Readonly<{
  projectId: string;
  project: SeoProject;
  accessToken: string | null;
  onSaved: (project: SeoProject) => void;
}>;

export function LocalServiceAreaSettings({ projectId, project, accessToken, onSaved }: Props) {
  const [businessAddress, setBusinessAddress] = useState(project.businessAddress ?? '');
  const [serviceRadiusMiles, setServiceRadiusMiles] = useState(
    project.serviceRadiusMiles ?? DEFAULT_RADIUS,
  );
  const [localSeoEnabled, setLocalSeoEnabled] = useState(project.localSeoEnabled ?? true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setBusinessAddress(project.businessAddress ?? '');
    setServiceRadiusMiles(project.serviceRadiusMiles ?? DEFAULT_RADIUS);
    setLocalSeoEnabled(project.localSeoEnabled ?? true);
  }, [project]);

  const onSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setSaving(true);
      setError(null);
      setSaved(false);
      try {
        const radius = Math.min(MAX_RADIUS, Math.max(MIN_RADIUS, serviceRadiusMiles));
        const updated = await updateProject(
          projectId,
          {
            businessAddress: businessAddress.trim() || null,
            serviceRadiusMiles: radius,
            localSeoEnabled,
          },
          accessToken,
        );
        onSaved(updated);
        setSaved(true);
      } catch (err) {
        setError(err);
      } finally {
        setSaving(false);
      }
    },
    [accessToken, businessAddress, localSeoEnabled, onSaved, projectId, serviceRadiusMiles],
  );

  return (
    <section className="mt-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
      <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Local service area</h2>
      <p className="mt-1 text-xs text-[var(--color-text-muted)]">
        Your business address and how far you travel (default {DEFAULT_RADIUS} miles). Used to find
        location-page gaps on your site — no Google Business account needed.
      </p>

      {error ? (
        <div className="mt-3">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      <form onSubmit={onSubmit} className="mt-4 flex flex-col gap-4">
        <label className="flex items-center gap-2 text-sm text-[var(--color-text-secondary)]">
          <input
            type="checkbox"
            checked={localSeoEnabled}
            onChange={(e) => setLocalSeoEnabled(e.target.checked)}
            className="rounded border-[var(--color-border-strong)]"
          />
          Include local SEO for this project
        </label>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-text-primary)]">Business address</span>
          <textarea
            rows={2}
            value={businessAddress}
            onChange={(e) => setBusinessAddress(e.target.value)}
            placeholder="123 Main St, Fort Lauderdale, FL 33301"
            className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
            disabled={!localSeoEnabled}
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-text-primary)]">
            Service radius (miles)
          </span>
          <input
            type="number"
            min={MIN_RADIUS}
            max={MAX_RADIUS}
            value={serviceRadiusMiles}
            onChange={(e) => setServiceRadiusMiles(Number.parseInt(e.target.value, 10) || DEFAULT_RADIUS)}
            className="w-28 rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
            disabled={!localSeoEnabled}
          />
          <span className="text-xs text-[var(--color-text-muted)]">
            Between {MIN_RADIUS} and {MAX_RADIUS} miles. Default is {DEFAULT_RADIUS}.
          </span>
        </label>

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save local settings'}
          </button>
          {saved ? (
            <span className="text-xs text-green-700">Saved.</span>
          ) : null}
        </div>
      </form>
    </section>
  );
}
