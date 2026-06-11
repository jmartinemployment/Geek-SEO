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
  const [defaultLocation, setDefaultLocation] = useState(project.defaultLocation ?? 'United States');
  const [businessAddress, setBusinessAddress] = useState(project.businessAddress ?? '');
  const [serviceRadiusMiles, setServiceRadiusMiles] = useState(
    project.serviceRadiusMiles ?? DEFAULT_RADIUS,
  );
  const [localSeoEnabled, setLocalSeoEnabled] = useState(project.localSeoEnabled ?? true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setDefaultLocation(project.defaultLocation ?? 'United States');
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
            defaultLocation: defaultLocation.trim() || 'United States',
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
    [accessToken, businessAddress, defaultLocation, localSeoEnabled, onSaved, projectId, serviceRadiusMiles],
  );

  return (
    <section className="mt-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
      <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Location & service area</h2>
      <p className="mt-1 text-xs text-[var(--color-text-muted)]">
        Target market for SERP analysis and local SEO. Used for niche analysis, keyword research, and competitor detection.
      </p>

      {error ? (
        <div className="mt-3">
          <SeoErrorBanner error={error} />
        </div>
      ) : null}

      <form onSubmit={onSubmit} className="mt-4 flex flex-col gap-4">

        {/* Default location — drives SERP targeting */}
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium text-[var(--color-text-primary)]">Target market location</span>
          <input
            type="text"
            value={defaultLocation}
            onChange={(e) => setDefaultLocation(e.target.value)}
            placeholder="Fort Lauderdale, Florida, United States"
            className="rounded-lg border border-[var(--color-border-strong)] px-3 py-2 text-sm"
          />
          <span className="text-xs text-[var(--color-text-muted)]">
            Used for SERP and competitor analysis. Format: City, State, Country (e.g. "Fort Lauderdale, Florida, United States").
            Leave as "United States" for national targeting.
          </span>
        </label>

        <div className="border-t border-[var(--color-border)] pt-4">
          <p className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wide mb-3">
            Local service area
          </p>

          <div className="flex flex-col gap-4">
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
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-accent-hover)] disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save settings'}
          </button>
          {saved ? (
            <span className="text-xs text-green-700">Saved.</span>
          ) : null}
        </div>
      </form>
    </section>
  );
}
