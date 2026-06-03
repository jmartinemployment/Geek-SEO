'use client';

import { useEffect, useRef, useState } from 'react';
import { getHubUrl, getNicheAnalysisStatus, type NicheAnalysisStatus } from '@/lib/seo-api';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

const STEP_LABELS: Record<string, string> = {
  schema: 'Extracting schema.org data…',
  sitemap: 'Parsing sitemap…',
  nav: 'Crawling navigation menu…',
  headings: 'Reading homepage headings…',
  merging: 'Merging pillar signals…',
  validating: 'Validating pillars…',
  scoring: 'Computing authority score…',
  saving: 'Saving analysis…',
  complete: 'Analysis complete',
  failed: 'Analysis failed',
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
  const completedRef = useRef(false);
  const onCompleteRef = useRef(onComplete);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onCompleteRef.current = onComplete;
    onErrorRef.current = onError;
  }, [onComplete, onError]);

  useEffect(() => {
    completedRef.current = false;
  }, [profileId]);

  // Fallback: poll /status if SignalR is unavailable or the tab was refreshed mid-run
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;
    let cancelled = false;

    async function poll() {
      if (cancelled || completedRef.current) return;
      try {
        const status = await getNicheAnalysisStatus(profileId, accessToken);
        if (status.status === 'complete' && !completedRef.current) {
          completedRef.current = true;
          onCompleteRef.current(profileId);
          return;
        }
        if (status.status === 'failed') {
          onErrorRef.current(status.errorMessage ?? 'Analysis failed');
          return;
        }
        setProgress((prev) => mergeStatus(prev, status));
      } catch {
        // ignore transient errors
      }
      if (!cancelled && !completedRef.current) {
        timer = setTimeout(poll, 3_000);
      }
    }

    void poll();

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [profileId, accessToken]);

  // SignalR live progress (optional; polling remains the source of truth)
  useEffect(() => {
    let disposed = false;
    let connection: import('@microsoft/signalr').HubConnection | null = null;

    async function connect() {
      try {
        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
        if (disposed) return;

        const conn = new HubConnectionBuilder()
          .withUrl(hubUrl(accessToken), {
            accessTokenFactory: () => accessToken ?? '',
            withCredentials: true,
          })
          .configureLogging(LogLevel.Error)
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
            setProgress((prev) =>
              mergeStatus(prev, {
                profileId: msgProfileId ?? profileId,
                status,
                step: msg.step ?? (msg as { Step?: string }).Step,
                stepNumber: msg.stepNumber ?? (msg as { StepNumber?: number }).StepNumber,
                totalSteps: msg.totalSteps ?? (msg as { TotalSteps?: number }).TotalSteps ?? 10,
                errorMessage: msg.errorMessage ?? (msg as { ErrorMessage?: string }).ErrorMessage,
              }),
            );

            if (status === 'complete' && !completedRef.current) {
              completedRef.current = true;
              onCompleteRef.current(profileId);
            }
            if (status === 'failed') {
              onErrorRef.current(msg.errorMessage ?? 'Analysis failed');
            }
          },
        );

        connection = conn;
        await conn.start();
        if (disposed) {
          await conn.stop();
        }
      } catch {
        // Polling fallback continues
      }
    }

    void connect();

    return () => {
      disposed = true;
      const conn = connection;
      connection = null;
      if (conn) {
        void conn.stop();
      }
    };
  }, [profileId, accessToken]);

  const stepNumber = progress?.stepNumber ?? 0;
  const totalSteps = progress?.totalSteps ?? 10;
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
    </div>
  );
}
