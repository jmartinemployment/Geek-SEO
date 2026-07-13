"use client";

import { Suspense, useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  generateFigureDraft,
  listImageGeneratorSections,
  type ImageGeneratorSection,
} from "@/lib/image-generator/api";

function ImageGeneratorContent() {
  const searchParams = useSearchParams();
  const initialProjectId = searchParams.get("projectId") ?? "";

  const [projectId, setProjectId] = useState(initialProjectId);
  const [sections, setSections] = useState<ImageGeneratorSection[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generatingSlug, setGeneratingSlug] = useState<string | null>(null);

  const loadSections = useCallback(async (id: string) => {
    if (!id.trim()) {
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const response = await listImageGeneratorSections(id.trim());
      setSections(response.sections);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load sections.");
      setSections([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (initialProjectId) {
      void loadSections(initialProjectId);
    }
  }, [initialProjectId, loadSections]);

  async function handleGenerate(section: ImageGeneratorSection) {
    if (!projectId.trim()) {
      return;
    }
    setGeneratingSlug(section.headingSlug);
    setError(null);
    try {
      await generateFigureDraft(projectId.trim(), section.sourceType, section.headingSlug);
      await loadSections(projectId.trim());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Generate failed.");
    } finally {
      setGeneratingSlug(null);
    }
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <h1 className="text-2xl font-semibold text-foreground">Section image generator</h1>
      <p className="mt-2 text-sm text-muted">
        One section at a time. OpenAI draft → review → refine in Figma if needed. Files save to the site image path
        when storage is configured on the API.
      </p>

      <div className="mt-6 flex flex-wrap items-end gap-3">
        <label className="flex min-w-[280px] flex-1 flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Content Writer project ID</span>
          <input
            className="rounded-md border border-border bg-surface px-3 py-2 text-foreground"
            value={projectId}
            onChange={(e) => setProjectId(e.target.value)}
            placeholder="3ab80a4b-2427-45e3-88b4-d871b9d977c8"
          />
        </label>
        <button
          type="button"
          className="rounded-md bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          disabled={loading || !projectId.trim()}
          onClick={() => void loadSections(projectId)}
        >
          {loading ? "Loading…" : "Load sections"}
        </button>
      </div>

      {error && (
        <p className="mt-4 text-sm text-red-700" role="alert">
          {error}
        </p>
      )}

      {sections.length > 0 && (
        <div className="mt-8 overflow-x-auto rounded-lg border border-border">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-border bg-slate-50 text-xs uppercase text-muted">
              <tr>
                <th className="px-3 py-2">Section</th>
                <th className="px-3 py-2">Path</th>
                <th className="px-3 py-2">Disk</th>
                <th className="px-3 py-2">Preview</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {sections.map((section) => {
                const busy = generatingSlug === section.headingSlug;
                const canGenerate =
                  section.status !== "Skipped" && Boolean(section.geekApiSlug?.trim());
                return (
                  <tr key={`${section.sourceType}-${section.headingSlug}`} className="border-b border-border">
                    <td className="px-3 py-3 align-top">
                      <div className="font-medium text-foreground">{section.heading}</div>
                      <div className="text-xs text-muted">
                        {section.sourceType} · {section.headingSlug}
                      </div>
                    </td>
                    <td className="max-w-xs px-3 py-3 align-top font-mono text-xs text-muted">
                      {section.relativePath ?? "— publish text first —"}
                    </td>
                    <td className="px-3 py-3 align-top text-xs">
                      {section.existsOnDisk || section.imageUrl ? "yes" : "missing"}
                    </td>
                    <td className="px-3 py-3 align-top">
                      {section.imageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={section.imageUrl}
                          alt=""
                          className="h-16 w-28 rounded border border-border object-cover"
                        />
                      ) : (
                        <span className="text-xs text-muted">—</span>
                      )}
                    </td>
                    <td className="px-3 py-3 align-top">
                      <button
                        type="button"
                        className="rounded border border-border px-3 py-1.5 text-xs font-medium disabled:opacity-50"
                        disabled={!canGenerate || busy || Boolean(generatingSlug)}
                        onClick={() => void handleGenerate(section)}
                      >
                        {busy ? "Generating…" : "Generate draft"}
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default function ImageGeneratorPage() {
  return (
    <Suspense fallback={<div className="p-8 text-sm text-muted">Loading…</div>}>
      <ImageGeneratorContent />
    </Suspense>
  );
}
