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
  manualResearchLaneQueryHint,
  pendingRequiredGateLabels,
  slugifyResearchTopic,
  validateManualLaneFileContent,
  type ManualResearchLaneId,
} from '@/lib/manual-research-lanes';
import type { ContentWriterSerpExport } from '@/lib/seo-api';

type Gate = { id: string; label: string; complete: boolean };

type Props = {
  runId: string;
  accessToken?: string | null;
  topicSlug: string;
  keyword?: string;
  onTopicSlugChange: (value: string) => void;
  onTopicSlugBlur?: () => void | Promise<void>;
  gates?: Gate[];
  researchReady?: boolean;
  onImported: () => void;
};

export function ManualResearchLanesCard({
  runId,
  accessToken,
  topicSlug,
  keyword = '',
  onTopicSlugChange,
  onTopicSlugBlur,
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
  const [lanePreflightErrors, setLanePreflightErrors] = useState<
    Partial<Record<ManualResearchLaneId, string>>
  >({});
  const [laneError, setLaneError] = useState<string | null>(null);

  const effectiveTopicSlug =
    topicSlug.trim() || (keyword.trim() ? slugifyResearchTopic(keyword) : '');

  function resolveLaneFile(lane: ManualResearchLaneId): File | undefined {
    if (lane === 'paa') return undefined;
    return pendingFiles[lane] ?? fileRefs.current[lane]?.files?.[0] ?? undefined;
  }

  function resolvePaaFiles(): File[] {
    if (pendingPaaFiles.length > 0) return pendingPaaFiles;
    return Array.from(fileRefs.current.paa?.files ?? []);
  }

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
    if (!effectiveTopicSlug) {
      setLaneError('Enter a research topic slug or import the keyword SERP first (slug auto-derives from keyword).');
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
        await importManualResearchLane(runId, 'paa', effectiveTopicSlug, html, accessToken, file.name);
      } else {
        await importManualResearchPaaBatch(runId, effectiveTopicSlug, files, accessToken);
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
    if (!effectiveTopicSlug) {
      setLaneError('Enter a research topic slug or import the keyword SERP first (slug auto-derives from keyword).');
      return;
    }
    setLaneError(null);
    setImportingLane(lane);
    try {
      const html = await file.text();
      const preflight = validateManualLaneFileContent(lane, html, file.name);
      if (preflight) {
        setLanePreflightErrors((prev) => ({ ...prev, [lane]: preflight }));
        setLaneError(preflight);
        return;
      }
      setLanePreflightErrors((prev) => {
        const next = { ...prev };
        delete next[lane];
        return next;
      });
      await importManualResearchLane(runId, lane, effectiveTopicSlug, html, accessToken, file.name);
      setPendingFiles((prev) => {
        const next = { ...prev };
        delete next[lane];
        return next;
      });
      setLanePreflightErrors((prev) => {
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
    const entries = MANUAL_RESEARCH_LANE_ORDER.filter((lane) => {
      if (lane === 'paa') return false;
      return Boolean(resolveLaneFile(lane));
    });
    const paaFiles = resolvePaaFiles();
    if (entries.length === 0 && paaFiles.length === 0) {
      setLaneError(
        'Choose at least one lane file to import. If you already picked files, wait for preflight to finish or fix validation errors on that row.',
      );
      return;
    }
    if (!effectiveTopicSlug) {
      setLaneError('Enter a research topic slug or import the keyword SERP first (slug auto-derives from keyword).');
      return;
    }
    setImportingAll(true);
    setLaneError(null);
    try {
      if (paaFiles.length > 0) {
        await importPaaLane(paaFiles);
      }
      for (const lane of entries) {
        const file = resolveLaneFile(lane);
        if (!file) continue;
        await importLane(lane, file);
      }
    } finally {
      setImportingAll(false);
    }
  }

  const requiredHint =
    'Import any lanes you saved. Keyword is required; gov, edu, local, paa, and wiki are optional.';

  const pendingRequired = pendingRequiredGateLabels(topicSlug, gates);

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
        {keyword.trim() ? (
          <p className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-muted)]/20 px-3 py-2 text-xs text-[var(--color-text-secondary)]">
            <span className="font-semibold text-[var(--color-text-primary)]">Pillar keyword for this run:</span>{' '}
            {keyword.trim()}
            <span className="mt-1 block text-[var(--color-text-muted)]">
              Use this phrase in each Google search below — lane files must match this keyword, not a different topic.
            </span>
          </p>
        ) : null}

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
            onChange={(e) => onTopicSlugChange(e.target.value)}
            onBlur={() => void onTopicSlugBlur?.()}
            placeholder="auto-from-keyword"
            className="mt-1.5 w-full rounded-[var(--radius-button)] border border-[var(--color-border-strong)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[rgba(59,179,122,0.2)]"
          />
          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
            {requiredHint} Matches folder <code className="text-xs">research/{topicSlug || '…'}/</code>.
            Leave blank to auto-derive from keyword on import; edit anytime. Syncs to this run on blur.
          </p>
        </div>

        {pendingRequired.length > 0 ? (
          <p className="text-xs font-medium text-[var(--color-warn)]">
            Still required for Content Writer: {pendingRequired.join(', ')}
          </p>
        ) : null}

        {loadingExport ? (
          <p className="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
            <Loader2 className="size-3.5 animate-spin" />
            Loading lane status…
          </p>
        ) : null}

        <ul className="space-y-3">
          {MANUAL_RESEARCH_LANE_ORDER.map((lane) => {
            const status = laneImportStatus(lane, exportData, gates);
            const file = lane === 'paa' ? undefined : resolveLaneFile(lane);
            const paaFiles = lane === 'paa' ? resolvePaaFiles() : [];
            const busy = importingLane === lane || importingAll;
            const queryHint = manualResearchLaneQueryHint(lane, keyword);
            const preflightError = lanePreflightErrors[lane];
            const importBlocked = Boolean(preflightError);
            const statusLabel =
              status === 'ok'
                ? 'Imported'
                : status === 'na'
                  ? lane === 'wiki'
                    ? 'Skip — no Wikipedia for this keyword'
                    : 'Optional — not imported'
                  : 'Waiting on import';
            return (
              <li
                key={lane}
                className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)]/10 p-3"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex items-center gap-2">
                    {status === 'ok' ? (
                      <CheckCircle2 className="size-4 text-[var(--color-good)]" />
                    ) : status === 'na' ? (
                      <span className="inline-flex rounded-full border border-[var(--color-border)] bg-[var(--color-surface-muted)] px-1.5 py-0.5 text-[9px] font-medium uppercase tracking-wide text-[var(--color-text-muted)]">
                        na
                      </span>
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
                        const nextFile = picked[0];
                        if (!nextFile) {
                          setPendingFiles((prev) => {
                            const next = { ...prev };
                            delete next[lane];
                            return next;
                          });
                          setLanePreflightErrors((prev) => {
                            const next = { ...prev };
                            delete next[lane];
                            return next;
                          });
                          return;
                        }
                        void nextFile.text().then((html) => {
                          const err = validateManualLaneFileContent(lane, html, nextFile.name);
                          setLanePreflightErrors((prev) => {
                            const next = { ...prev };
                            if (err) next[lane] = err;
                            else delete next[lane];
                            return next;
                          });
                          if (err) {
                            setLaneError(`[${lane}] ${err}`);
                            setPendingFiles((prev) => {
                              const next = { ...prev };
                              delete next[lane];
                              return next;
                            });
                            return;
                          }
                          setPendingFiles((prev) => ({ ...prev, [lane]: nextFile }));
                          setLaneError(null);
                        });
                      }}
                    />
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={
                        (lane === 'paa' ? paaFiles.length === 0 : !file || importBlocked)
                        || busy
                        || !effectiveTopicSlug
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
                {preflightError ? (
                  <p className="mt-1 text-xs text-[var(--color-bad)]">{preflightError}</p>
                ) : null}
                {queryHint ? (
                  <p className="mt-1 text-xs text-[var(--color-text-muted)]">{queryHint}</p>
                ) : null}
                {status === 'na' && lane === 'wiki' ? (
                  <p className="mt-1 text-xs text-[var(--color-text-muted)]">
                    Skip if Google has no en.wikipedia.org results — not required for Content Writer.
                  </p>
                ) : null}
                {status !== 'ok' ? (
                  <p className="mt-1 text-[10px] text-[var(--color-text-muted)]">{statusLabel}</p>
                ) : null}
              </li>
            );
          })}
        </ul>

        <Button
          type="button"
          className="w-full"
          disabled={importingAll || importingLane !== null || !effectiveTopicSlug}
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
            Import keyword SERP to unlock Content Writer. Supplemental lanes are optional.
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
