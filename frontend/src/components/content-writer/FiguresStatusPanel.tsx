"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError, listFigures } from "@/lib/content-writer/api";

export function FiguresStatusPanel({
  projectId,
  refreshKey,
}: {
  projectId: string;
  refreshKey: number;
}) {
  const [figureCount, setFigureCount] = useState<number | null>(null);
  const [missingSlugCount, setMissingSlugCount] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const loadFigures = useCallback(async () => {
    try {
      const response = await listFigures(projectId);
      setFigureCount(response.figures.length);
      setMissingSlugCount(response.summary.missingGeekApiSlug);
      setError(null);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Could not load figures.";
      setError(message);
      setFigureCount(null);
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

  if (figureCount === null || figureCount === 0) {
    return null;
  }

  return (
    <div className="mt-4 rounded-md border border-border bg-slate-50 p-3 text-xs text-slate-800">
      <p className="font-semibold text-foreground">Section figure briefs</p>
      <p className="mt-1 text-muted">
        {figureCount} brief{figureCount === 1 ? "" : "s"} saved for layout slots outside post body.
        {missingSlugCount > 0 && (
          <>
            {" "}
            <span className="text-amber-900">{missingSlugCount} awaiting text publish</span>
          </>
        )}
      </p>
      <p className="mt-2 text-muted">
        Generate AVIF with SectionFigures (<code className="text-foreground">export-jobs</code>{" "}
        → <code className="text-foreground">plan</code> → <code className="text-foreground">generate-one</code> per
        section), commit under{" "}
        <code className="text-foreground">public/images/</code>, then deploy geekatyourspot. Optional: upload AVIF
        per section below.
      </p>
    </div>
  );
}

export default FiguresStatusPanel;
