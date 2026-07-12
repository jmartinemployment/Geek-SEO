"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ApiError,
  generatePendingFigures,
  listFigures,
  mergeFigures,
} from "@/lib/content-writer/api";
import type { ContentFiguresListResponse } from "@/lib/content-writer/types";

function sourceLabel(sourceType: string): string {
  if (sourceType === "pillar") return "pillar";
  if (sourceType === "blog") return "blog";
  if (sourceType.startsWith("tool/")) return sourceType.replace(/^tool\//i, "tool ");
  return sourceType;
}

export function FiguresStatusPanel({
  projectId,
  refreshKey,
  onFiguresChanged,
}: {
  projectId: string;
  refreshKey: number;
  onFiguresChanged?: () => void;
}) {
  const [figures, setFigures] = useState<ContentFiguresListResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [mergingSource, setMergingSource] = useState<string | null>(null);
  const [generatingSource, setGeneratingSource] = useState<string | null>(null);
  const [mergeMessage, setMergeMessage] = useState<string | null>(null);

  const loadFigures = useCallback(async () => {
    try {
      const response = await listFigures(projectId);
      setFigures(response);
      setError(null);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Could not load figures.";
      setError(message);
      setFigures(null);
    }
  }, [projectId]);

  useEffect(() => {
    void loadFigures();
  }, [loadFigures, refreshKey]);

  const sourceTypes = useMemo(() => {
    if (!figures) return [];
    return [...new Set(figures.figures.map((f) => f.sourceType))].sort((a, b) => {
      const rank = (s: string) =>
        s === "pillar" ? 0 : s === "blog" ? 1 : s.startsWith("tool/") ? 2 : 3;
      const diff = rank(a) - rank(b);
      return diff !== 0 ? diff : a.localeCompare(b);
    });
  }, [figures]);

  if (error) {
    return (
      <p className="mt-4 text-xs text-red-700" role="alert">
        {error}
      </p>
    );
  }

  if (!figures || figures.figures.length === 0) {
    return null;
  }

  const { summary, inAppGenerationEnabled } = figures;
  const needsMerge = figures.figures.some((f) => f.needsFigureMerge);

  async function runMerge(source: string) {
    setError(null);
    setMergeMessage(null);
    setMergingSource(source);
    try {
      const result = await mergeFigures(projectId, source);
      setMergeMessage(`Merged ${result.figuresMerged} figure(s) into ${result.publicPath}`);
      await loadFigures();
      onFiguresChanged?.();
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Figure merge failed.";
      setError(message);
    } finally {
      setMergingSource(null);
    }
  }

  async function runGenerateAll(source: string) {
    setError(null);
    setMergeMessage(null);
    setGeneratingSource(source);
    try {
      const result = await generatePendingFigures(projectId, source);
      setMergeMessage(`Generated ${result.generatedCount} ${sourceLabel(source)} figure(s) from briefs.`);
      await loadFigures();
      onFiguresChanged?.();
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Figure generation failed.";
      setError(message);
    } finally {
      setGeneratingSource(null);
    }
  }

  return (
    <div className="mt-4 rounded-md border border-border bg-slate-50 p-3 text-xs text-slate-800">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="font-semibold text-foreground">Section figures</p>
        {needsMerge && (
          <span className="rounded-full bg-amber-100 px-2 py-0.5 font-medium text-amber-900">
            Needs merge
          </span>
        )}
      </div>
      <p className="mt-1 text-muted">
        Pending {summary.pending} · Ready {summary.ready} · Published {summary.published} · Skipped{" "}
        {summary.skipped}
        {summary.missingGeekApiSlug > 0 && (
          <> · <span className="text-amber-900">{summary.missingGeekApiSlug} awaiting text publish</span></>
        )}
      </p>
      <p className="mt-2 text-muted">
        {inAppGenerationEnabled
          ? "Copy briefs to your image tool, or use in-app generation, then upload WebP per section."
          : "Copy each brief to your external image tool (Figma, Midjourney, etc.), export WebP, upload per section, then merge."}
      </p>
      <div className="mt-2 flex flex-wrap gap-2">
        {sourceTypes.map((source) => {
          const pending = figures.figures.some(
            (f) => f.sourceType === source && f.status === "Pending" && f.geekApiSlug
          );
          const mergeable = figures.figures.some(
            (f) =>
              f.sourceType === source &&
              (f.status === "Ready" || f.status === "Published") &&
              f.imageUrl
          );
          const label = sourceLabel(source);
          return (
            <span key={source} className="contents">
              {inAppGenerationEnabled && pending && (
                <button
                  type="button"
                  disabled={generatingSource !== null || mergingSource !== null}
                  onClick={() => void runGenerateAll(source)}
                  className="rounded-md border border-border bg-white px-2.5 py-1.5 text-xs font-medium text-foreground hover:bg-slate-100 disabled:opacity-60"
                >
                  {generatingSource === source ? `Generating ${label}…` : `Generate all ${label}`}
                </button>
              )}
              {mergeable && (
                <button
                  type="button"
                  disabled={mergingSource !== null || generatingSource !== null}
                  onClick={() => void runMerge(source)}
                  className="rounded-md border border-border bg-white px-2.5 py-1.5 text-xs font-medium text-foreground hover:bg-slate-100 disabled:opacity-60"
                >
                  {mergingSource === source ? `Merging ${label}…` : `Merge ${label} figures`}
                </button>
              )}
            </span>
          );
        })}
      </div>
      {mergeMessage && <p className="mt-2 font-medium text-green-800">{mergeMessage}</p>}
    </div>
  );
}

export default FiguresStatusPanel;
