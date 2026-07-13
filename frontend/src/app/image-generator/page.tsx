"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { getRecentProjects } from "@/lib/content-writer/api";
import type { ProjectSummary } from "@/lib/content-writer/types";
import {
  generateFigureDraft,
  generateFromBrief,
  listImageGeneratorSections,
  type ImageGeneratorSection,
} from "@/lib/image-generator/api";

function ImageGeneratorContent() {
  const formRef = useRef<HTMLDivElement>(null);
  const briefRef = useRef<HTMLTextAreaElement>(null);

  const [heading, setHeading] = useState("");
  const [briefText, setBriefText] = useState("");
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [downloadName, setDownloadName] = useState("figure-draft.avif");
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [sections, setSections] = useState<ImageGeneratorSection[]>([]);
  const [loadingSections, setLoadingSections] = useState(false);
  const [generatingSlug, setGeneratingSlug] = useState<string | null>(null);

  useEffect(() => {
    void getRecentProjects()
      .then(setProjects)
      .catch(() => setProjects([]));
  }, []);

  const loadSections = useCallback(async (id: string) => {
    if (!id) {
      setSections([]);
      return;
    }
    setLoadingSections(true);
    setError(null);
    try {
      const response = await listImageGeneratorSections(id);
      setSections(response.sections);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load sections.");
      setSections([]);
    } finally {
      setLoadingSections(false);
    }
  }, []);

  useEffect(() => {
    if (selectedProjectId) {
      void loadSections(selectedProjectId);
    } else {
      setSections([]);
    }
  }, [selectedProjectId, loadSections]);

  function useSectionBrief(section: ImageGeneratorSection) {
    setError(null);
    const nextHeading = section.heading?.trim() ?? "";
    const nextBrief = section.briefText?.trim() ?? "";

    setHeading(nextHeading);
    setBriefText(nextBrief);

    if (!nextBrief) {
      setStatusMessage(null);
      setError(`“${nextHeading || section.headingSlug}” has no brief text. Re-run Step 6 in Content Writer.`);
      return;
    }

    setStatusMessage(`Loaded brief: ${nextHeading || section.headingSlug}`);
    requestAnimationFrame(() => {
      formRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
      briefRef.current?.focus();
    });
  }

  async function handleGenerateFromBrief() {
    if (!briefText.trim()) {
      setError("Paste a figure brief first — or click Use brief under a project section.");
      return;
    }
    setGenerating(true);
    setError(null);
    setStatusMessage(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
    try {
      const result = await generateFromBrief(heading.trim() || "Section figure", briefText.trim());
      const bytes = Uint8Array.from(atob(result.imageBase64), (c) => c.charCodeAt(0));
      const blob = new Blob([bytes], { type: "image/avif" });
      setPreviewUrl(URL.createObjectURL(blob));
      setDownloadName(result.fileName);
      setStatusMessage("Draft ready — preview below.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Generate failed.");
    } finally {
      setGenerating(false);
    }
  }

  async function handleSaveToSitePath(section: ImageGeneratorSection) {
    if (!selectedProjectId) {
      return;
    }
    setGeneratingSlug(section.headingSlug);
    setError(null);
    try {
      await generateFigureDraft(selectedProjectId, section.sourceType, section.headingSlug);
      await loadSections(selectedProjectId);
      setStatusMessage(`Saved draft for ${section.heading}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save to site path failed.");
    } finally {
      setGeneratingSlug(null);
    }
  }

  const selectedProject = projects.find((p) => p.id === selectedProjectId);

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <h1 className="text-2xl font-semibold text-foreground">Image generator</h1>
      <p className="mt-2 text-sm text-muted">
        Paste a figure brief and generate a draft. Review it, refine in Figma AI if you want, then save the AVIF to
        your site path.
      </p>

      <div ref={formRef} className="mt-8 space-y-4 rounded-lg border border-border bg-surface p-4">
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Section title (optional)</span>
          <input
            className="rounded-md border border-border px-3 py-2 text-foreground"
            value={heading}
            onChange={(e) => setHeading(e.target.value)}
            placeholder="e.g. Overview of Zoho Books"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Figure brief</span>
          <textarea
            ref={briefRef}
            className="min-h-[240px] rounded-md border border-border px-3 py-2 text-foreground"
            value={briefText}
            onChange={(e) => setBriefText(e.target.value)}
            placeholder="Paste the full art-direction brief here, or Use brief from a project section below…"
          />
        </label>

        <button
          type="button"
          className="rounded-md bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          disabled={generating || !briefText.trim()}
          onClick={() => void handleGenerateFromBrief()}
        >
          {generating ? "Generating draft…" : "Generate draft"}
        </button>
      </div>

      {statusMessage && (
        <p className="mt-4 text-sm text-emerald-800" role="status">
          {statusMessage}
        </p>
      )}

      {error && (
        <p className="mt-4 text-sm text-red-700" role="alert">
          {error}
        </p>
      )}

      {previewUrl && (
        <div className="mt-8 rounded-lg border border-border p-4">
          <p className="text-sm font-medium text-foreground">Draft preview</p>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={previewUrl} alt="" className="mt-3 w-full rounded border border-border" />
          <a
            href={previewUrl}
            download={downloadName}
            className="mt-3 inline-block text-sm font-medium text-[var(--color-accent)] underline"
          >
            Download AVIF
          </a>
        </div>
      )}

      <div className="mt-10 rounded-lg border border-border p-4">
        <p className="text-sm font-medium text-foreground">Or pick briefs from Content Writer</p>
        <p className="mt-1 text-xs text-muted">Choose a project by name — no IDs.</p>

        <label className="mt-3 flex flex-col gap-1 text-sm">
          <span className="text-muted">Project</span>
          <select
            className="rounded-md border border-border bg-surface px-3 py-2 text-foreground"
            value={selectedProjectId}
            onChange={(e) => setSelectedProjectId(e.target.value)}
          >
            <option value="">Select a project…</option>
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name} — {p.targetKeyword}
              </option>
            ))}
          </select>
        </label>

        {loadingSections && <p className="mt-3 text-xs text-muted">Loading sections…</p>}

        {selectedProject && sections.length > 0 && (
          <ul className="mt-4 max-h-64 space-y-2 overflow-y-auto text-sm">
            {sections.map((section) => (
              <li
                key={`${section.sourceType}-${section.headingSlug}`}
                className="flex flex-wrap items-center justify-between gap-2 rounded border border-border px-3 py-2"
              >
                <div className="min-w-0 flex-1">
                  <span className="font-medium">{section.heading}</span>
                  {!section.briefText.trim() && (
                    <span className="ml-2 text-xs text-amber-800">no brief</span>
                  )}
                </div>
                <div className="flex gap-3">
                  <button
                    type="button"
                    className="text-xs font-medium text-[var(--color-accent)] underline"
                    onClick={() => useSectionBrief(section)}
                  >
                    Use brief
                  </button>
                  {section.relativePath && (
                    <button
                      type="button"
                      className="text-xs underline disabled:opacity-50"
                      disabled={
                        generatingSlug === section.headingSlug ||
                        section.status === "Skipped" ||
                        !section.geekApiSlug
                      }
                      onClick={() => void handleSaveToSitePath(section)}
                    >
                      {generatingSlug === section.headingSlug ? "Saving…" : "Save to site path"}
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}

        {selectedProject && !loadingSections && sections.length === 0 && (
          <p className="mt-3 text-xs text-muted">No figure briefs on this project yet — run Step 6 in Content Writer.</p>
        )}
      </div>
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
