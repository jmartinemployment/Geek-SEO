'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { CheckCircle2, Loader2, Upload } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import {
  fetchContentWriterExport,
  importManualResearchLane,
  importManualResearchPaaBatch,
  laneImportStatus,
  MANUAL_RESEARCH_LANE_LABELS,
  MANUAL_RESEARCH_LANE_ORDER,
  type ManualResearchLaneId,
} from '@/lib/manual-research-lanes';
import type { ContentWriterSerpExport } from '@/lib/seo-api';

type Gate = { id: string; label: string; complete: boolean };

type Props = {
  runId: string;
  accessToken?: string | null;
  topicSlug: string;
  topicSlugLocked?: boolean;
  onTopicSlugChange: (value: string) => void;
  gates?: Gate[];
  researchReady?: boolean;
  onImported: () => void;
};

export function ManualResearchLanesCard({
  runId,
  accessToken,
  topicSlug,
  topicSlugLocked = false,
  onTopicSlugChange,
  gates,
  researchReady,
  onImported,
}: Props) {
  const fileRefs = useRef<Partial<Record<ManualResearchLaneId, HTMLInputElement | null>>>({});
  const [exportData, setExportData] = useState<ContentWriterSerpExport | null>(null);
  const [loadingExport, setLoadingExport] = useState(false);
  const [importingLane, setImportingLane] = useState<ManualResearchLaneId | null>(null);
  const [importingAll, setImportingAll] = useState(false);
  const [pendingFiles, setPendingFiles] = useState<Partial<Record<ManualResearchLaneId, File>>>({});
  const [pendingPaaFiles, setPendingPaaFiles] = useState<File[]>([]);
  const [laneError, setLaneError] = useState<string | null>(null);

  const refreshExport = useCallback(async () => {
    if (!runId) return;
    setLoadingExport(true);
    try {
      const data = await fetchContentWriterExport(runId, accessToken);
      setExportData(data);
    } finally {
      setLoadingExport(false);
    }
  }, [runId, accessToken]);

  useEffect(() => {
    void refreshExport();
  }, [refreshExport]);

  async function importPaaLane(files: File[]) {
    if (!topicSlug.trim()) {
      setLaneError('Enter a research topic slug first (e.g. customer-journey).');
      return;
    }
    if (files.length === 0) {
      setLaneError('Choose at least one PAA file to import.');
      return;
    }
    setLaneError(null);
    setImportingLane('paa');
    try {
      if (files.length === 1) {
        const file = files[0]!;
        const html = await file.text();
        await importManualResearchLane(runId, 'paa', topicSlug.trim(), html, accessToken, file.name);
      } else {
        await importManualResearchPaaBatch(runId, topicSlug.trim(), files, accessToken);
      }
      setPendingPaaFiles([]);
      await refreshExport();
      onImported();
    } catch (e) {
      setLaneError(e instanceof Error ? e.message : String(e));
    } finally {
      setImportingLane(null);
    }
  }

  async function importLane(lane: ManualResearchLaneId, file: File) {
    if (lane === 'paa') {
      await importPaaLane([file]);
      return;
    }
    if (!topicSlug.trim()) {
      setLaneError('Enter a research topic slug first (e.g. customer-journey).');
      return;
    }
    setLaneError(null);
    setImportingLane(lane);
    try {
      const html = await file.text();
      await importManualResearchLane(runId, lane, topicSlug.trim(), html, accessToken, file.name);
      setPendingFiles((prev) => {
        const next = { ...prev };
        delete next[lane];
        return next;
      });
      await refreshExport();
      onImported();
    } catch (e) {
      setLaneError(e instanceof Error ? e.message : String(e));
    } finally {
      setImportingLane(null);
    }
  }

  async function importAllPending() {
    const entries = MANUAL_RESEARCH_LANE_ORDER.filter(
      (lane) => lane !== 'paa' && pendingFiles[lane],
    );
    if (entries.length === 0 && pendingPaaFiles.length === 0) {
      setLaneError('Choose at least one file to import.');
      return;
    }
    setImportingAll(true);
    setLaneError(null);
    try {
      if (pendingPaaFiles.length > 0) {
        await importPaaLane(pendingPaaFiles);
      }
      for (const lane of entries) {
        const file = pendingFiles[lane];
        if (!file) continue;
        await importLane(lane, file);
      }
    } finally {
      setImportingAll(false);
    }
  }

  const requiredHint =
    topicSlug.trim().toLowerCase() === 'customer-journey'
      ? 'Required for this topic: gov + wiki. Optional: paa, edu, local.'
      : 'Import any lanes you saved. gov + wiki are required only for customer-journey.';

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Upload className="size-4 text-[var(--color-metric-blue)]" />
          Google research lanes
        </CardTitle>
        <CardDescription>
          Upload your saved Google HTML for each lane — same files as in{' '}
          <code className="text-xs">research/&lt;topic&gt;/</code>. For <strong>PAA</strong>, select
          one or more <code className="text-xs">.txt</code> lists (one question per line) and/or saved
          Google HTML files.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div>
          <label
            htmlFor="research-topic-slug"
            className="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-muted)]"
          >
            Research topic slug
          </label>
          <input
            id="research-topic-slug"
            type="text"
            value={topicSlug}
            readOnly={topicSlugLocked}
            onChange={(e) => onTopicSlugChange(e.target.value)}
            placeholder="customer-journey"
            className={cn(
              'mt-1.5 w-full rounded-[var(--radius-button)] border border-[var(--color-border-strong)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[rgba(59,179,122,0.2)]',
              topicSlugLocked && 'cursor-not-allowed bg-[var(--color-surface-muted)]/30 text-[var(--color-text-secondary)]',
            )}
          />
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
            {topicSlugLocked
              ? 'Locked to this keyword run — matches your research/ folder name, not the pillar keyword phrase.'
              : requiredHint}
          </p>
        </div>

        {loadingExport ? (
          <p className="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
            <Loader2 className="size-3.5 animate-spin" />
            Loading lane status…
          </p>
        ) : null}

        <ul className="space-y-3">
          {MANUAL_RESEARCH_LANE_ORDER.map((lane) => {
            const status = laneImportStatus(lane, exportData, gates);
            const file = lane === 'paa' ? undefined : pendingFiles[lane];
            const paaFiles = lane === 'paa' ? pendingPaaFiles : [];
            const busy = importingLane === lane || importingAll;
            return (
              <li
                key={lane}
                className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/10 p-3"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex items-center gap-2">
                    {status === 'ok' ? (
                      <CheckCircle2 className="size-4 text-[var(--color-good)]" />
                    ) : (
                      <span className="size-4 rounded-full border border-[var(--color-border)]" />
                    )}
                    <span className="text-sm font-medium">{MANUAL_RESEARCH_LANE_LABELS[lane]}</span>
                    <span className="text-[10px] uppercase text-[var(--color-text-muted)]">
                      {lane}
                    </span>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <input
                      ref={(el) => {
                        fileRefs.current[lane] = el;
                      }}
                      type="file"
                      multiple={lane === 'paa'}
                      accept={
                        lane === 'paa' ? '.html,.htm,.txt,text/html,text/plain' : '.html,.htm,text/html'
                      }
                      className="max-w-[200px] text-xs file:mr-2 file:rounded file:border-0 file:bg-white file:px-2 file:py-1 file:text-xs"
                      disabled={busy}
                      onChange={(e) => {
                        const picked = Array.from(e.target.files ?? []);
                        if (lane === 'paa') {
                          setPendingPaaFiles(picked);
                          return;
                        }
                        setPendingFiles((prev) =>
                          picked[0] ? { ...prev, [lane]: picked[0] } : prev,
                        );
                      }}
                    />
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={
                        (lane === 'paa' ? paaFiles.length === 0 : !file) || busy || !topicSlug.trim()
                      }
                      onClick={() => {
                        if (lane === 'paa') {
                          void importPaaLane(paaFiles);
                          return;
                        }
                        if (file) void importLane(lane, file);
                      }}
                    >
                      {importingLane === lane ? (
                        <Loader2 className="size-3.5 animate-spin" />
                      ) : (
                        'Import'
                      )}
                    </Button>
                  </div>
                </div>
                {lane === 'paa' && paaFiles.length > 0 ? (
                  <ul className="mt-1 space-y-0.5 text-xs text-[var(--color-text-secondary)]">
                    {paaFiles.map((f) => (
                      <li key={`${f.name}-${f.lastModified}`} className="truncate">
                        {f.name}
                      </li>
                    ))}
                  </ul>
                ) : null}
                {file ? (
                  <p className="mt-1 truncate text-xs text-[var(--color-text-secondary)]">
                    {file.name}
                  </p>
                ) : null}
              </li>
            );
          })}
        </ul>

        <Button
          type="button"
          className="w-full"
          disabled={importingAll || importingLane !== null || !topicSlug.trim()}
          onClick={() => void importAllPending()}
        >
          {importingAll ? (
            <>
              <Loader2 className="size-4 animate-spin" />
              Importing lanes…
            </>
          ) : (
            'Import all selected lanes'
          )}
        </Button>

        {researchReady ? (
          <p className="text-xs text-[var(--color-good)]">
            Research lanes ready — you can open Content Writer.
          </p>
        ) : (
          <p className="text-xs text-[var(--color-text-secondary)]">
            Finish required lanes, then Content Writer will use your saved research.
          </p>
        )}

        {laneError ? (
          <p className={cn('text-xs text-[var(--color-bad)] whitespace-pre-wrap')}>{laneError}</p>
        ) : null}

        {exportData?.researchMode === 'manual' ? (
          <p className="text-xs text-[var(--color-text-muted)]">Mode: manual research (saved HTML)</p>
        ) : null}
      </CardContent>
    </Card>
  );
}
