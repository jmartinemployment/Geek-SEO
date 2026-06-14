'use client';

import { useEffect, useRef, useState } from 'react';
import { getHubUrl, getNicheAnalysisStatus, type NicheAnalysisStatus } from '@/lib/seo-api';
import { isNicheStepStalled, NICHE_STALL_MS } from '@/lib/niche-analysis-stale';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

const STEP_LABELS: Record<string, string> = {
  schema: 'Extracting schema.org data…',
  site_urls: 'Collecting site URLs…',
  nav: 'Crawling navigation menu…',
  headings: 'Reading homepage headings…',
  merging: 'Merging pillar signals…',
  page_content: 'Reading homepage content…',
  site_crawl: 'Crawling site pages…',
  internal_links: 'Extracting internal links…',
  url_patterns: 'Detecting URL patterns…',
  site_structure: 'Scanning site structure…',
  keywords: 'Enriching keyword demand…',
  serp_validation: 'Validating SERP footprint…',
  profile: 'Building niche profile…',
  local: 'Local geography…',
  coverage: 'Content coverage…',
  scoring: 'Computing authority score…',
  complete: 'Analysis complete',
  failed: 'Analysis failed',
  sitemap: 'Collecting site URLs…',
  discovery: 'Collecting site URLs…',
  validating: 'Building niche profile…',
  saving: 'Saving analysis…',
};

type Props = {
  profileId: string;
  accessToken?: string | null;
  onComplete: (profileId: string) => void;
  onError: (message: string) => void;
};

function mergeStatus(
  prev: NicheAnalysisStatus | null,
  next: NicheAnalysisStatus,
): NicheAnalysisStatus {
  const prevStep = prev?.stepNumber ?? 0;
  const nextStep = next.stepNumber ?? 0;
  if (prev && prevStep > nextStep && prev.status === 'processing')
    return { ...next, step: prev.step, stepNumber: prevStep };
  return next;
}

function hubUrl(accessToken?: string | null): string {
  const base = getHubUrl();
  if (!accessToken && DEV_USER_ID) {
    return `${base}?access_token=${encodeURIComponent(DEV_USER_ID)}`;
  }
  return base;
}

export function AnalysisStatusListener({ profileId, accessToken, onComplete, onError }: Props) {
  const [progress, setProgress] = useState<NicheAnalysisStatus | null>(null);
  const [liveMessage, setLiveMessage] = useState<string | null>(null);
  const [stalled, setStalled] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const completedRef = useRef(false);
  const onCompleteRef = useRef(onComplete);
  const onErrorRef = useRef(onError);
  const stepTrackerRef = useRef({ step: 0, at: Date.now() });

  function applyStatus(status: NicheAnalysisStatus) {
    const step = status.stepNumber ?? 0;
    if (step > 0 && step !== stepTrackerRef.current.step) {
      stepTrackerRef.current = { step, at: Date.now() };
      setStalled(false);
    } else if (isNicheStepStalled(status, stepTrackerRef.current.step, stepTrackerRef.current.at)) {
      setStalled(true);
    }

    setProgress((prev) => mergeStatus(prev, status));

    if (status.status === 'complete' && !completedRef.current) {
      completedRef.current = true;
      onCompleteRef.current(profileId);
      return;
    }
    if (status.status === 'failed') {
      onErrorRef.current(status.errorMessage ?? 'Analysis failed');
    }
  }

  useEffect(() => {
    onCompleteRef.current = onComplete;
    onErrorRef.current = onError;
  }, [onComplete, onError]);

  useEffect(() => {
    completedRef.current = false;
    stepTrackerRef.current = { step: 0, at: Date.now() };
    setStalled(false);
    setLiveMessage(null);
    setConnectionError(null);
  }, [profileId]);

  // One-time hydrate on mount / profile change (e.g. tab refresh mid-run).
  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const status = await getNicheAnalysisStatus(profileId, accessToken);
        if (!cancelled) applyStatus(status);
      } catch {
        if (!cancelled) {
          setConnectionError('Could not load analysis status. Please refresh.');
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [profileId, accessToken]);

  // SignalR live progress; reconcile from DB on reconnect.
  useEffect(() => {
    let disposed = false;
    let started = false;
    let connection: import('@microsoft/signalr').HubConnection | null = null;

    async function hydrateOnce() {
      try {
        const status = await getNicheAnalysisStatus(profileId, accessToken);
        if (!disposed) applyStatus(status);
      } catch {
        if (!disposed) {
          setConnectionError('Could not refresh analysis status.');
        }
      }
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

        conn.on(
          'AnalysisProgress',
          (msg: NicheAnalysisStatus & { message?: string; Message?: string; ProfileId?: string }) => {
            const msgProfileId = msg.profileId ?? msg.ProfileId;
            if (msgProfileId && msgProfileId !== profileId) return;
            const detail = msg.message ?? msg.Message;
            if (detail) setLiveMessage(detail);

            const status = msg.status ?? (msg as { Status?: string }).Status ?? 'processing';
            if (status === 'complete' && !completedRef.current) {
              completedRef.current = true;
              void hydrateOnce();
              onCompleteRef.current(profileId);
              return;
            }
            if (status === 'failed') {
              onErrorRef.current(msg.errorMessage ?? 'Analysis failed');
              return;
            }

            applyStatus({
              profileId: msgProfileId ?? profileId,
              status: status as NicheAnalysisStatus['status'],
              step: msg.step ?? (msg as { Step?: string }).Step,
              stepNumber: msg.stepNumber ?? (msg as { StepNumber?: number }).StepNumber,
              totalSteps: msg.totalSteps ?? (msg as { TotalSteps?: number }).TotalSteps ?? 16,
              errorMessage: msg.errorMessage ?? (msg as { ErrorMessage?: string }).ErrorMessage,
            });
          },
        );

        conn.onreconnected(() => {
          setConnectionError(null);
          void hydrateOnce();
        });

        connection = conn;
        await conn.start();
        started = true;
        if (disposed) {
          void conn.stop().catch(() => {});
          return;
        }
        setConnectionError(null);
        await conn.invoke('JoinGroup', `niche-${profileId}`);
      } catch (e) {
        if (!disposed) {
          setConnectionError(
            e instanceof Error ? e.message : 'Live progress connection failed.',
          );
        }
      }
    }

    void connect();

    return () => {
      disposed = true;
      const conn = connection;
      connection = null;
      if (conn && started) void conn.stop().catch(() => {});
    };
  }, [profileId, accessToken]);

  const stepNumber = progress?.stepNumber ?? 0;
  const totalSteps = progress?.totalSteps ?? 16;
  const pct = totalSteps > 0 ? Math.round((stepNumber / totalSteps) * 100) : 0;
  const label =
    liveMessage ??
    (progress?.step
      ? (STEP_LABELS[progress.step] ?? progress.step)
      : progress?.status === 'processing'
        ? 'Analyzing…'
        : 'Queued…');

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between text-sm">
        <span className="text-[var(--color-text-secondary)]">{label}</span>
        <span className="font-medium text-[var(--color-text-primary)]">{pct}%</span>
      </div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-[var(--color-border)]">
        <div
          className="h-full rounded-full bg-[var(--color-accent)] transition-all duration-500"
          style={{ width: `${pct}%` }}
        />
      </div>
      <p className="text-xs text-[var(--color-text-muted)]">
        Step {stepNumber} of {totalSteps}
      </p>
      {connectionError ? (
        <p className="text-xs text-amber-700">{connectionError}</p>
      ) : null}
      {stalled ? (
        <p className="text-xs text-amber-700">
          This step has not changed for {Math.round(NICHE_STALL_MS / 60_000)} minutes. If nothing
          moves soon, click Re-analyze — the server will abandon stuck runs automatically.
        </p>
      ) : null}
    </div>
  );
}
