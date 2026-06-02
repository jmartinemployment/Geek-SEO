'use client';

import { useEffect, useRef, useState } from 'react';
import { getNicheAnalysisStatus, type NicheAnalysisStatus } from '@/lib/seo-api';

const SEO_API_URL = process.env.NEXT_PUBLIC_SEO_API_URL ?? 'http://localhost:5051';

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

export function AnalysisStatusListener({ profileId, accessToken, onComplete, onError }: Props) {
  const [progress, setProgress] = useState<NicheAnalysisStatus | null>(null);
  const hubRef = useRef<WebSocket | null>(null);
  const completedRef = useRef(false);

  // Fallback: poll /status for initial state or if SignalR drops
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;

    async function poll() {
      try {
        const status = await getNicheAnalysisStatus(profileId, accessToken);
        if (status.status === 'complete' && !completedRef.current) {
          completedRef.current = true;
          onComplete(profileId);
          return;
        }
        if (status.status === 'failed') {
          onError(status.errorMessage ?? 'Analysis failed');
          return;
        }
        setProgress(status);
      } catch {
        // ignore transient errors
      }
      timer = setTimeout(poll, 3_000);
    }

    // Check immediately (handles page-refresh-after-complete)
    void poll();

    return () => clearTimeout(timer);
  }, [profileId, accessToken, onComplete, onError]);

  // SignalR (primary) — connect to /hubs/seo-scoring
  useEffect(() => {
    if (completedRef.current) return;

    let connection: { invoke?: () => void; stop?: () => void } | null = null;

    async function connect() {
      try {
        const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
        const conn = new HubConnectionBuilder()
          .withUrl(`${SEO_API_URL}/hubs/seo-scoring`, {
            accessTokenFactory: () => accessToken ?? '',
          })
          .configureLogging(LogLevel.Warning)
          .withAutomaticReconnect()
          .build();

        conn.on('AnalysisProgress', (msg: NicheAnalysisStatus & { message?: string; ProfileId?: string }) => {
          const msgProfileId = msg.profileId ?? msg.ProfileId;
          if (msgProfileId && msgProfileId !== profileId) return;
          setProgress({
            profileId: msgProfileId ?? profileId,
            status: msg.status ?? (msg as { Status?: string }).Status ?? 'processing',
            step: msg.step ?? (msg as { Step?: string }).Step,
            stepNumber: msg.stepNumber ?? (msg as { StepNumber?: number }).StepNumber,
            totalSteps: msg.totalSteps ?? (msg as { TotalSteps?: number }).TotalSteps ?? 10,
            errorMessage: msg.errorMessage ?? (msg as { ErrorMessage?: string }).ErrorMessage,
          });
          if (msg.status === 'complete' && !completedRef.current) {
            completedRef.current = true;
            onComplete(profileId);
          }
          if (msg.status === 'failed') {
            onError(msg.errorMessage ?? 'Analysis failed');
          }
        });

        await conn.start();
        connection = conn as unknown as typeof connection;
      } catch {
        // SignalR unavailable — polling fallback continues
      }
    }

    void connect();

    return () => {
      void (connection as unknown as { stop?: () => Promise<void> })?.stop?.();
    };
  }, [profileId, accessToken, onComplete, onError]);

  const stepNumber = progress?.stepNumber ?? 0;
  const totalSteps = progress?.totalSteps ?? 10;
  const pct = totalSteps > 0 ? Math.round((stepNumber / totalSteps) * 100) : 0;
  const label = progress?.step
    ? (STEP_LABELS[progress.step] ?? progress.step)
    : progress?.status === 'processing'
      ? 'Analyzing…'
      : 'Queued…';

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
