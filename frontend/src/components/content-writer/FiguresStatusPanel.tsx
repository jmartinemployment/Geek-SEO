"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError, listFigures, mergeFigures } from "@/lib/content-writer/api";
import type { ContentFiguresListResponse } from "@/lib/content-writer/types";

export function FiguresStatusPanel({
  projectId,
  refreshKey,
}: {
  projectId: string;
  refreshKey: number;
}) {
  const [figures, setFigures] = useState<ContentFiguresListResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [mergingSource, setMergingSource] = useState<"pillar" | "blog" | null>(null);
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

  const { summary } = figures;
  const needsMerge = figures.figures.some((f) => f.needsFigureMerge);
  const mergeablePillar = figures.figures.some(
    (f) => f.sourceType === "pillar" && (f.status === "Ready" || f.status === "Published") && f.imageUrl
  );
  const mergeableBlog = figures.figures.some(
    (f) => f.sourceType === "blog" && (f.status === "Ready" || f.status === "Published") && f.imageUrl
  );

  async function runMerge(source: "pillar" | "blog") {
    setError(null);
    setMergeMessage(null);
    setMergingSource(source);
    try {
      const result = await mergeFigures(projectId, source);
      setMergeMessage(`Merged ${result.figuresMerged} figure(s) into ${result.publicPath}`);
      await loadFigures();
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Figure merge failed.";
      setError(message);
    } finally {
      setMergingSource(null);
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
        Attach WebP art locally with the ContentFigures CLI, then merge into the live post.
      </p>
      <div className="mt-2 flex flex-wrap gap-2">
        {mergeablePillar && (
          <button
            type="button"
            disabled={mergingSource !== null}
            onClick={() => void runMerge("pillar")}
            className="rounded-md border border-border bg-white px-2.5 py-1.5 text-xs font-medium text-foreground hover:bg-slate-100 disabled:opacity-60"
          >
            {mergingSource === "pillar" ? "Merging pillar…" : "Merge pillar figures"}
          </button>
        )}
        {mergeableBlog && (
          <button
            type="button"
            disabled={mergingSource !== null}
            onClick={() => void runMerge("blog")}
            className="rounded-md border border-border bg-white px-2.5 py-1.5 text-xs font-medium text-foreground hover:bg-slate-100 disabled:opacity-60"
          >
            {mergingSource === "blog" ? "Merging blog…" : "Merge blog figures"}
          </button>
        )}
      </div>
      {mergeMessage && <p className="mt-2 font-medium text-green-800">{mergeMessage}</p>}
    </div>
  );
}
