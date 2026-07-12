"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiError, generatePendingFigures, listFigures } from "@/lib/content-writer/api";
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
  const [generatingSource, setGeneratingSource] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

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

  async function runGenerateAll(source: string) {
    setError(null);
    setStatusMessage(null);
    setGeneratingSource(source);
    try {
      const result = await generatePendingFigures(projectId, source);
      setStatusMessage(
        `Saved ${result.generatedCount} ${sourceLabel(source)} figure(s) to site paths.`,
      );
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
      <p className="font-semibold text-foreground">Section figures</p>
      <p className="mt-1 text-muted">
        Pending {summary.pending} · Ready {summary.ready} · Published {summary.published} · Skipped{" "}
        {summary.skipped}
        {summary.missingGeekApiSlug > 0 && (
          <>
            {" "}
            · <span className="text-amber-900">{summary.missingGeekApiSlug} awaiting text publish</span>
          </>
        )}
      </p>
      <p className="mt-2 text-muted">
        {inAppGenerationEnabled
          ? "Section images live in layout slots outside post body. Generate & save or upload AVIF to each path shown."
          : "Section images live in layout slots outside post body. Save AVIF to each path shown (upload or CLI)."}
      </p>
      <div className="mt-2 flex flex-wrap gap-2">
        {sourceTypes.map((source) => {
          const pending = figures.figures.some(
            (f) => f.sourceType === source && f.status === "Pending" && f.geekApiSlug,
          );
          const label = sourceLabel(source);
          return inAppGenerationEnabled && pending ? (
            <button
              key={source}
              type="button"
              disabled={generatingSource !== null}
              onClick={() => void runGenerateAll(source)}
              className="rounded-md border border-border bg-white px-2.5 py-1.5 text-xs font-medium text-foreground hover:bg-slate-100 disabled:opacity-60"
            >
              {generatingSource === source ? `Generating ${label}…` : `Generate & save all ${label}`}
            </button>
          ) : null;
        })}
      </div>
      {statusMessage && <p className="mt-2 font-medium text-green-800">{statusMessage}</p>}
    </div>
  );
}

export default FiguresStatusPanel;
