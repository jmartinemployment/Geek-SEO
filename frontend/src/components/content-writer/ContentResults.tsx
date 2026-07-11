"use client";

import { useEffect, useState } from "react";
import {
  generateBlogContent,
  generateColdOutreachContent,
  generateImagePromptsContent,
  generateFigureImage,
  attachFigure,
  skipFigure,
  generatePendingFigures,
  generatePillarBodyContent,
  generatePillarPlanContent,
  generateSocialContent,
  exportMarkdownContent,
  publishToSite,
  listFigures,
  ApiError,
  FigureArtConflictError,
} from "@/lib/content-writer/api";
import FiguresStatusPanel from "@/components/content-writer/FiguresStatusPanel";
import {
  canWriteExportToLocalDirectory,
  pickExportDirectory,
  resolveExportDirectory,
  writeExportFilesToDirectory,
} from "@/lib/content-writer/exportToLocalDirectory";
import type { ColdOutreachEmailDraft, ContentFigureDto, ExportMarkdownResponse, FigureStatus, GeneratedContentSet, ImagePromptSection, ImagePromptsSet, PublishToSiteResponse, SiteDepartmentSlug } from "@/lib/content-writer/types";
import { CONTENT_LENGTH_TARGETS, SITE_DEPARTMENTS } from "@/lib/content-writer/types";

type Tab = "article" | "blog" | "facebook" | "linkedin" | "cold-outreach" | "image-prompts";
type GeneratingStep = "pillar-plan" | "pillar-body" | "blog" | "social" | "cold-outreach" | "image-prompts" | "all" | null;

const PILLAR_BODY_MIN_WORDS = 200;

const TABS: { id: Tab; label: string }[] = [
  { id: "article", label: "Technical Article" },
  { id: "blog", label: "Blog Post" },
  { id: "facebook", label: "Facebook" },
  { id: "linkedin", label: "LinkedIn" },
  { id: "cold-outreach", label: "Cold Outreach" },
  { id: "image-prompts", label: "Figure briefs" },
];

export default function ContentResults({
  projectId,
  canGenerate,
  result,
  onGenerated,
}: {
  projectId: string;
  canGenerate: boolean;
  result: GeneratedContentSet | null;
  onGenerated: (result: GeneratedContentSet) => void;
}) {
  const [activeTab, setActiveTab] = useState<Tab>("article");
  const [generatingStep, setGeneratingStep] = useState<GeneratingStep>(null);
  const [error, setError] = useState<string | null>(null);
  const [exportResult, setExportResult] = useState<ExportMarkdownResponse | null>(null);
  const [isExporting, setIsExporting] = useState(false);
  const [exportDepartment, setExportDepartment] = useState("");
  const [exportFolderName, setExportFolderName] = useState<string | null>(null);
  const [publishResult, setPublishResult] = useState<PublishToSiteResponse | null>(null);
  const [isPublishing, setIsPublishing] = useState(false);
  const [publishDepartment, setPublishDepartment] = useState<SiteDepartmentSlug>("accounting");
  const [figuresRefreshKey, setFiguresRefreshKey] = useState(0);

  useEffect(() => {
    if (result?.department && SITE_DEPARTMENTS.includes(result.department as SiteDepartmentSlug)) {
      setPublishDepartment(result.department as SiteDepartmentSlug);
    }
  }, [result?.department]);

  const hasPillarPlan = result?.article != null;
  const hasPillarBody = (result?.article?.wordCount ?? 0) >= PILLAR_BODY_MIN_WORDS;
  const hasBlog = result?.blog != null;
  const hasSocial = result?.facebookPost != null && result?.linkedInPost != null;
  const hasColdOutreach = result?.coldOutreachEmail != null;
  const hasImagePrompts = (result?.imagePrompts?.sections?.length ?? 0) > 0;
  const hasExportableContent =
    hasPillarBody || hasBlog || hasSocial || hasColdOutreach || hasImagePrompts;
  const isGenerating = generatingStep !== null;

  async function saveExportToLocalFolder(result: ExportMarkdownResponse) {
    if (!canWriteExportToLocalDirectory()) {
      throw new Error(
        "Use Chrome or Edge to save exports into /Users/jeffmartin/Documents/Content-Writer-Output.",
      );
    }

    const directory = await resolveExportDirectory();
    await writeExportFilesToDirectory(directory, result.files);
    setExportFolderName(directory.name);
  }

  async function runFigureBriefsStep(confirmRegenerateWithArt = false) {
    setError(null);
    setExportResult(null);
    setGeneratingStep("image-prompts");
    try {
      const next = await generateImagePromptsContent(projectId, { confirmRegenerateWithArt });
      onGenerated(next);
      setFiguresRefreshKey((k) => k + 1);
      if ((next.imagePrompts?.sections?.length ?? 0) > 0) {
        setActiveTab("image-prompts");
      }
    } catch (err) {
      if (err instanceof FigureArtConflictError && !confirmRegenerateWithArt) {
        const proceed = window.confirm(
          `${err.message}\n\nRegenerate briefs anyway? Ready figures with art are preserved by heading.`,
        );
        if (proceed) {
          await runFigureBriefsStep(true);
          return;
        }
        setError(err.message);
        return;
      }
      const message = err instanceof ApiError ? err.message : "Generation failed. Check the API logs.";
      setError(message);
    } finally {
      setGeneratingStep(null);
    }
  }

  async function runStep(step: GeneratingStep, action: () => Promise<GeneratedContentSet>) {
    setError(null);
    setExportResult(null);
    setGeneratingStep(step);
    try {
      const next = await action();
      onGenerated(next);
      setFiguresRefreshKey((k) => k + 1);
      if ((step === "cold-outreach" || step === "all") && next.coldOutreachEmail) {
        setActiveTab("cold-outreach");
      }
      if ((step === "image-prompts" || step === "all") && (next.imagePrompts?.sections?.length ?? 0) > 0) {
        setActiveTab("image-prompts");
      }
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Generation failed. Check the API logs.";
      setError(message);
    } finally {
      setGeneratingStep(null);
    }
  }

  return (
    <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
      <h2 className="text-lg font-semibold text-foreground">4. Generate Content</h2>
      <p className="mt-1 text-sm text-muted">
        Run each step separately. Steps 1–2 plan and write the pillar article; steps 3–6 build blog, social, email, and Leonardo image prompts from it.
      </p>

      <div className="mt-5 space-y-3">
        <StepRow
          step={1}
          title="Pillar plan (Technical Article)"
          description="Title, meta, keywords, and declarative H2 outline (PAA goes in a final FAQ section only)."
          done={hasPillarPlan}
          disabled={!canGenerate || isGenerating}
          isRunning={generatingStep === "pillar-plan"}
          buttonLabel={hasPillarPlan ? "Regenerate plan" : "Generate plan"}
          onClick={() => runStep("pillar-plan", () => generatePillarPlanContent(projectId))}
        />

        <StepRow
          step={2}
          title="Pillar body"
          description={`${CONTENT_LENGTH_TARGETS.pillar.definition} Target ${CONTENT_LENGTH_TARGETS.pillar.label} words — one outline section per LLM call.`}
          done={hasPillarBody}
          disabled={!hasPillarPlan || isGenerating}
          isRunning={generatingStep === "pillar-body"}
          buttonLabel={hasPillarBody ? "Regenerate body" : "Write body"}
          onClick={() => runStep("pillar-body", () => generatePillarBodyContent(projectId))}
          lockedMessage={!hasPillarPlan ? "Complete Step 1 first." : undefined}
        />

        <StepRow
          step={3}
          title="Blog content"
          description={`${CONTENT_LENGTH_TARGETS.blog.definition} Target ${CONTENT_LENGTH_TARGETS.blog.label} words — one section per LLM call, cross-linked to the pillar.`}
          done={hasBlog}
          disabled={!hasPillarBody || isGenerating}
          isRunning={generatingStep === "blog"}
          buttonLabel={hasBlog ? "Regenerate blog" : "Generate blog"}
          onClick={() => runStep("blog", () => generateBlogContent(projectId))}
          lockedMessage={!hasPillarBody ? "Complete Step 2 first." : undefined}
        />

        <StepRow
          step={4}
          title="Social content"
          description="Facebook (~40 words) and LinkedIn (~200–300 words) posts linking to the pillar."
          done={hasSocial}
          disabled={!hasPillarBody || isGenerating}
          isRunning={generatingStep === "social"}
          buttonLabel={hasSocial ? "Regenerate social" : "Generate social"}
          onClick={() => runStep("social", () => generateSocialContent(projectId))}
          lockedMessage={!hasPillarBody ? "Complete Step 2 first." : undefined}
        />

        <StepRow
          step={5}
          title="Cold outreach email"
          description={`${CONTENT_LENGTH_TARGETS.emailColdOutreach.definition} Target ${CONTENT_LENGTH_TARGETS.emailColdOutreach.label} words — subject, body, and one CTA to the pillar.`}
          done={result?.coldOutreachEmail != null}
          disabled={!hasPillarBody || isGenerating}
          isRunning={generatingStep === "cold-outreach"}
          buttonLabel={result?.coldOutreachEmail ? "Regenerate email" : "Generate email"}
          onClick={() => runStep("cold-outreach", () => generateColdOutreachContent(projectId))}
          lockedMessage={!hasPillarBody ? "Complete Step 2 first." : undefined}
        />

        <StepRow
          step={6}
          title="Figure briefs"
          description="One art-direction brief per H2 in the pillar and blog — use for Figma or manual illustration."
          done={hasImagePrompts}
          disabled={!hasBlog || isGenerating}
          isRunning={generatingStep === "image-prompts"}
          buttonLabel={hasImagePrompts ? "Regenerate briefs" : "Generate briefs"}
          onClick={() => runFigureBriefsStep()}
          lockedMessage={!hasBlog ? "Complete Step 3 (blog) first." : undefined}
        />
      </div>

      <button
        onClick={() =>
          runStep("all", async () => {
            let state = result;
            if (!state?.article) {
              state = await generatePillarPlanContent(projectId);
              onGenerated(state);
            }
            if ((state.article?.wordCount ?? 0) < PILLAR_BODY_MIN_WORDS) {
              state = await generatePillarBodyContent(projectId);
              onGenerated(state);
            }
            if (!state.blog) {
              state = await generateBlogContent(projectId);
              onGenerated(state);
            }
            if (!state.facebookPost || !state.linkedInPost) {
              state = await generateSocialContent(projectId);
              onGenerated(state);
            }
            if (!state.coldOutreachEmail) {
              state = await generateColdOutreachContent(projectId);
              onGenerated(state);
            }
            if (!state.imagePrompts?.sections?.length) {
              state = await generateImagePromptsContent(projectId);
              onGenerated(state);
            }
            return state!;
          })
        }
        disabled={
          !canGenerate ||
          isGenerating ||
          (hasPillarBody && hasBlog && hasSocial && result?.coldOutreachEmail != null && hasImagePrompts)
        }
        className="mt-4 text-sm font-medium text-brand hover:underline disabled:opacity-60"
      >
        {generatingStep === "all" ? "Generating..." : "Generate all remaining steps"}
      </button>

      {!canGenerate && (
        <p className="mt-2 text-xs text-muted">Crawl the site and upload at least one research input first.</p>
      )}
      {error && (
        <p className={`mt-4 text-sm ${error.toLowerCase().includes("timed out") ? "text-amber-700" : "text-red-600"}`}>
          {error}
        </p>
      )}

      {hasExportableContent && (
        <div className="mt-5 rounded-lg border border-border bg-background p-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="text-sm font-semibold text-foreground">Export markdown</h3>
              <p className="mt-1 text-xs text-muted">
                Saves files under{" "}
                <span className="font-mono text-foreground">
                  Content-Writer-Output/{`{department}`}/{`{target keyword}`}/
                </span>{" "}
                (Pillar, Blog, Social, Email, ImagePrompts). The first export asks you to pick the{" "}
                <span className="font-mono text-foreground">Content-Writer-Output</span> folder on your Mac;
                after that, exports write there automatically.
              </p>
              <label className="mt-2 flex flex-col gap-1 text-xs text-muted">
                Department folder (optional)
                <input
                  value={exportDepartment}
                  onChange={(e) => setExportDepartment(e.target.value)}
                  placeholder="accounting"
                  className="max-w-xs rounded-md border border-border bg-white px-2 py-1.5 text-sm text-foreground outline-none focus:border-brand"
                />
              </label>
              {canWriteExportToLocalDirectory() && (
                <button
                  type="button"
                  onClick={async () => {
                    try {
                      const directory = await pickExportDirectory();
                      setExportFolderName(directory.name);
                      setError(null);
                    } catch (err) {
                      if (err instanceof DOMException && err.name === "AbortError") return;
                      setError(err instanceof Error ? err.message : "Could not choose export folder.");
                    }
                  }}
                  className="mt-2 text-xs font-medium text-brand hover:underline"
                >
                  {exportFolderName
                    ? `Export folder: ${exportFolderName} (change)`
                    : "Choose Content-Writer-Output folder"}
                </button>
              )}
            </div>
            <button
              type="button"
              disabled={isExporting || isGenerating}
              onClick={async () => {
                setError(null);
                setExportResult(null);
                setIsExporting(true);
                try {
                  const result = await exportMarkdownContent(
                    projectId,
                    exportDepartment.trim() || undefined
                  );
                  await saveExportToLocalFolder(result);
                  setExportResult(result);
                } catch (err) {
                  if (err instanceof DOMException && err.name === "AbortError") {
                    setError("Export cancelled. Choose Content-Writer-Output folder, then try again.");
                    return;
                  }
                  const message = err instanceof ApiError ? err.message : "Export failed.";
                  setError(message);
                } finally {
                  setIsExporting(false);
                }
              }}
              className="shrink-0 rounded-md border border-brand px-3 py-2 text-sm font-semibold text-brand transition-colors hover:bg-brand/5 disabled:opacity-60"
            >
              {isExporting ? "Exporting..." : "Export all content (.md)"}
            </button>
          </div>
          {exportResult && (
            <div className="mt-3 rounded-md bg-green-50 p-3 text-xs text-green-900">
              <p className="font-medium">
                Saved {exportResult.files.length} file(s) under{" "}
                {exportFolderName ? `${exportFolderName}/` : ""}
                {exportResult.department}/
              </p>
              <ul className="mt-2 space-y-1 font-mono">
                {exportResult.files.map((file) => (
                  <li key={file.relativePath}>
                    {file.contentType}: {file.filePath ?? file.relativePath}
                  </li>
                ))}
              </ul>
              {exportResult.files.every((file) => !file.filePath) && (
                <p className="mt-2 text-amber-900">
                  Hosted API cannot write to your Mac directly. Files were saved through the browser into{" "}
                  {exportFolderName ? (
                    <span className="font-mono">{exportFolderName}</span>
                  ) : (
                    "the folder you chose"
                  )}
                  . If nothing appeared in Finder, click “Choose Content-Writer-Output folder” and export again.
                </p>
              )}
            </div>
          )}
        </div>
      )}

      {hasPillarBody && (
        <div className="mt-5 rounded-lg border border-border bg-background p-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="text-sm font-semibold text-foreground">Publish to geekatyourspot</h3>
              <p className="mt-1 text-xs text-muted">
                Pushes pillar and blog Markdown + JSON-LD to GeekAPI (
                <span className="font-mono text-foreground">geek_blog</span>
                ) and triggers ISR on the public site.
              </p>
              <label className="mt-2 flex flex-col gap-1 text-xs text-muted">
                Department (required — must match geekatyourspot.com)
                <select
                  value={publishDepartment}
                  onChange={(e) => setPublishDepartment(e.target.value as SiteDepartmentSlug)}
                  className="max-w-xs rounded-md border border-border bg-white px-2 py-1.5 text-sm text-foreground outline-none focus:border-brand"
                >
                  {SITE_DEPARTMENTS.map((department) => (
                    <option key={department} value={department}>
                      {department}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <button
              type="button"
              disabled={isPublishing || isGenerating}
              onClick={async () => {
                setError(null);
                setPublishResult(null);
                setIsPublishing(true);
                try {
                  const response = await publishToSite(projectId, publishDepartment);
                  setPublishResult(response);
                  setFiguresRefreshKey((k) => k + 1);
                } catch (err) {
                  const message = err instanceof ApiError ? err.message : "Publish failed.";
                  setError(message);
                } finally {
                  setIsPublishing(false);
                }
              }}
              className="shrink-0 rounded-md bg-brand px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-dark disabled:opacity-60"
            >
              {isPublishing ? "Publishing..." : "Publish to site"}
            </button>
          </div>
          {publishResult && (
            <div className="mt-3 rounded-md bg-green-50 p-3 text-xs text-green-900">
              <p className="font-medium">
                Published {publishResult.posts.length} post(s) under department{" "}
                <span className="font-mono">{publishResult.department}</span>
              </p>
              <ul className="mt-2 space-y-1 font-mono">
                {publishResult.posts.map((post) => (
                  <li key={post.slug}>
                    {post.created ? "Created" : "Updated"} {post.postType}: {post.publicPath}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {(result?.imagePrompts?.sections?.length ?? 0) > 0 && (
            <FiguresStatusPanel
              projectId={projectId}
              refreshKey={figuresRefreshKey}
              onFiguresChanged={() => setFiguresRefreshKey((k) => k + 1)}
            />
          )}
        </div>
      )}

      {result?.article && (
        <div className="mt-6">
          <div className="flex gap-1 border-b border-border">
            {TABS.map((tab) => {
              const unavailable =
                (tab.id === "blog" && !result.blog) ||
                ((tab.id === "facebook" || tab.id === "linkedin") && !result.facebookPost) ||
                (tab.id === "cold-outreach" && !result.coldOutreachEmail) ||
                (tab.id === "image-prompts" && (result.imagePrompts?.sections?.length ?? 0) === 0);
              const hint =
                tab.id === "image-prompts" && (result.imagePrompts?.sections?.length ?? 0) === 0
                  ? "Run Step 6 (Generate prompts) first"
                  : tab.id === "cold-outreach" && !result.coldOutreachEmail
                  ? "Run Step 5 (Generate email) first"
                  : tab.id === "blog" && !result.blog
                    ? "Run Step 3 (Generate blog) first"
                    : (tab.id === "facebook" || tab.id === "linkedin") && !result.facebookPost
                      ? "Run Step 4 (Generate social) first"
                      : undefined;

              return (
                <button
                  key={tab.id}
                  type="button"
                  title={hint}
                  onClick={() => setActiveTab(tab.id)}
                  className={`rounded-t-md px-4 py-2 text-sm font-medium transition-colors ${
                    activeTab === tab.id
                      ? "border-b-2 border-brand text-brand"
                      : unavailable
                        ? "text-muted/70 hover:text-muted"
                        : "text-muted hover:text-foreground"
                  }`}
                >
                  {tab.label}
                </button>
              );
            })}
          </div>

          <div className="pt-5">
            {activeTab === "article" && result.article && (
              <ArticleView
                title={result.article.title}
                metaDescription={result.article.metaDescription}
                bodyHtml={result.article.bodyHtml}
                url={result.articleUrl ?? ""}
                jsonLd={result.articleJsonLd ?? ""}
                keywords={result.article.keywords}
                wordCount={result.article.wordCount}
                sectionOutline={result.article.sectionOutline}
                planOnly={!hasPillarBody}
                targetLabel={`Target: ${CONTENT_LENGTH_TARGETS.pillar.label} words`}
                minWords={CONTENT_LENGTH_TARGETS.pillar.min}
              />
            )}
            {activeTab === "blog" &&
              (result.blog && result.blogUrl ? (
                <ArticleView
                  title={result.blog.title}
                  metaDescription={result.blog.metaDescription}
                  bodyHtml={result.blog.bodyHtml}
                  url={result.blogUrl}
                  jsonLd={result.blogJsonLd ?? ""}
                  keywords={result.blog.keywords}
                  wordCount={result.blog.wordCount}
                  sectionOutline={result.blog.sectionOutline}
                  targetLabel={`Target: ${CONTENT_LENGTH_TARGETS.blog.label} words`}
                  minWords={CONTENT_LENGTH_TARGETS.blog.min}
                />
              ) : (
                <EmptyTabHint message="Run Step 3 to generate the blog post." />
              ))}
            {activeTab === "facebook" &&
              (result.facebookPost ? (
                <SocialView text={result.facebookPost.text} platform="Facebook" />
              ) : (
                <EmptyTabHint message="Run Step 4 to generate social posts." />
              ))}
            {activeTab === "linkedin" &&
              (result.linkedInPost ? (
                <SocialView text={result.linkedInPost.text} platform="LinkedIn" />
              ) : (
                <EmptyTabHint message="Run Step 4 to generate social posts." />
              ))}
            {activeTab === "cold-outreach" &&
              (result.coldOutreachEmail ? (
                <ColdOutreachView email={result.coldOutreachEmail} />
              ) : (
                <EmptyTabHint message="Run Step 5 (Generate email) to create the cold outreach email." />
              ))}
            {activeTab === "image-prompts" &&
              (result.imagePrompts ? (
                <ImagePromptsView
                  prompts={result.imagePrompts}
                  projectId={projectId}
                  refreshKey={figuresRefreshKey}
                  onFiguresChanged={() => setFiguresRefreshKey((k) => k + 1)}
                />
              ) : (
                <EmptyTabHint message="Run Step 6 to generate figure briefs." />
              ))}
          </div>
        </div>
      )}
    </div>
  );
}

function EmptyTabHint({ message }: { message: string }) {
  return <p className="rounded-lg border border-dashed border-border bg-background p-4 text-sm text-muted">{message}</p>;
}

function StepRow({
  step,
  title,
  description,
  done,
  disabled,
  isRunning,
  buttonLabel,
  onClick,
  lockedMessage,
}: {
  step: number;
  title: string;
  description: string;
  done: boolean;
  disabled: boolean;
  isRunning: boolean;
  buttonLabel: string;
  onClick: () => void;
  lockedMessage?: string;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-background p-4">
      <div>
        <div className="flex items-center gap-2">
          <span className="flex h-6 w-6 items-center justify-center rounded-full bg-brand/10 text-xs font-bold text-brand">
            {step}
          </span>
          <h3 className="text-sm font-semibold text-foreground">{title}</h3>
          {done && (
            <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">Done</span>
          )}
        </div>
        <p className="mt-1 text-xs text-muted">{description}</p>
        {lockedMessage && <p className="mt-1 text-xs text-muted">{lockedMessage}</p>}
      </div>
      <button
        onClick={onClick}
        disabled={disabled}
        className="shrink-0 rounded-md bg-brand px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-dark disabled:opacity-60"
      >
        {isRunning ? "Generating..." : buttonLabel}
      </button>
    </div>
  );
}

function ArticleView({
  title,
  metaDescription,
  bodyHtml,
  url,
  jsonLd,
  keywords,
  wordCount,
  sectionOutline,
  planOnly = false,
  targetLabel,
  minWords,
}: {
  title: string;
  metaDescription: string;
  bodyHtml: string;
  url: string;
  jsonLd: string;
  keywords: string[];
  wordCount: number;
  sectionOutline?: string[];
  planOnly?: boolean;
  targetLabel?: string;
  minWords?: number;
}) {
  const [showSchema, setShowSchema] = useState(false);
  const underTarget = minWords != null && wordCount > 0 && wordCount < minWords;

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-xl font-bold text-foreground">{title}</h3>
        <div className="flex flex-wrap items-center gap-2">
          {wordCount > 0 && (
            <span
              className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                underTarget ? "bg-amber-100 text-amber-800" : "bg-brand/10 text-brand"
              }`}
            >
              {wordCount} words
            </span>
          )}
          {targetLabel && <span className="text-xs text-muted">{targetLabel}</span>}
        </div>
        {planOnly && (
          <span className="rounded-full bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-800">Plan only</span>
        )}
      </div>
      <p className="mt-1 text-sm text-muted">{metaDescription}</p>
      {url && (
        <a href={url} className="mt-1 inline-block text-sm text-brand hover:underline" target="_blank">
          {url}
        </a>
      )}

      <div className="mt-2 flex flex-wrap gap-1.5">
        {keywords.map((kw) => (
          <span key={kw} className="rounded-full bg-background px-2 py-0.5 text-xs text-muted">
            {kw}
          </span>
        ))}
      </div>

      {planOnly && sectionOutline && sectionOutline.length > 0 && (
        <div className="mt-4 rounded-lg border border-border bg-background p-4">
          <h4 className="text-sm font-semibold text-foreground">Section outline</h4>
          <ol className="mt-2 list-decimal space-y-1 pl-5 text-sm text-muted">
            {sectionOutline.map((heading) => (
              <li key={heading}>{heading}</li>
            ))}
          </ol>
          <p className="mt-3 text-xs text-muted">Run Step 2 to write the full article from this outline.</p>
        </div>
      )}

      {bodyHtml && (
        <div
          className="rendered-content mt-5 rounded-lg border border-border bg-background p-4"
          dangerouslySetInnerHTML={{ __html: bodyHtml }}
        />
      )}

      {jsonLd && (
        <>
          <button
            onClick={() => setShowSchema((v) => !v)}
            className="mt-4 text-sm font-medium text-brand hover:underline"
          >
            {showSchema ? "Hide" : "Show"} JSON+LD Schema
          </button>
          {showSchema && (
            <pre className="mt-2 max-h-96 overflow-auto rounded-lg bg-slate-900 p-4 text-xs text-slate-100">
              {jsonLd}
            </pre>
          )}
        </>
      )}
    </div>
  );
}

function countWords(text: string): number {
  return text.trim().split(/\s+/).filter(Boolean).length;
}

function SocialView({ text, platform }: { text: string; platform: "Facebook" | "LinkedIn" }) {
  const words = countWords(text);
  const chars = text.length;
  const target =
    platform === "Facebook" ? "~40 words, under 250 chars" : "200–300 words, 1,300–1,900 chars";

  return (
    <div>
      <div className="mb-2 flex flex-wrap gap-2 text-xs text-muted">
        <span className="rounded-full bg-brand/10 px-2 py-0.5 font-medium text-brand">{words} words</span>
        <span className="rounded-full bg-brand/10 px-2 py-0.5 font-medium text-brand">{chars} characters</span>
        <span>Target: {target}</span>
      </div>
      <div className="whitespace-pre-wrap rounded-lg border border-border bg-background p-4 text-sm text-foreground">
        {text}
      </div>
    </div>
  );
}

function ColdOutreachView({ email }: { email: ColdOutreachEmailDraft }) {
  const words = countWords(email.bodyText);
  const outOfRange =
    words < CONTENT_LENGTH_TARGETS.emailColdOutreach.min ||
    words > CONTENT_LENGTH_TARGETS.emailColdOutreach.max;

  return (
    <div>
      <div className="mb-2 flex flex-wrap gap-2 text-xs text-muted">
        <span
          className={`rounded-full px-2 py-0.5 font-medium ${
            outOfRange ? "bg-amber-100 text-amber-800" : "bg-brand/10 text-brand"
          }`}
        >
          {words} words
        </span>
        <span>Target: {CONTENT_LENGTH_TARGETS.emailColdOutreach.label} words</span>
      </div>
      <h3 className="text-lg font-semibold text-foreground">{email.subject}</h3>
      <div className="mt-3 whitespace-pre-wrap rounded-lg border border-border bg-background p-4 text-sm text-foreground">
        {email.bodyText}
      </div>
      <p className="mt-3 text-sm text-foreground">
        <span className="font-medium">{email.ctaLabel}: </span>
        <a href={email.ctaUrl} className="text-brand hover:underline" target="_blank" rel="noreferrer">
          {email.ctaUrl}
        </a>
      </p>
    </div>
  );
}

function formatLeonardoSettings(item: ImagePromptSection): string {
  const lines = [
    `Model: ${item.leonardoModel}`,
    `Model ID: ${item.leonardoModelId}`,
    `Dimensions: ${item.width} × ${item.height}`,
    `Style preset: ${item.stylePreset}`,
    `Alchemy: ${item.alchemy ? "On" : "Off"}`,
    `PhotoReal: ${item.photoReal ? "On" : "Off"}`,
  ];
  if (item.notes) lines.push(`Notes: ${item.notes}`);
  return lines.join("\n");
}

function formatLeonardoCopyBlock(item: ImagePromptSection): string {
  return `${formatLeonardoSettings(item)}\n\nPrompt:\n${item.prompt}`;
}

async function copyText(text: string): Promise<void> {
  await navigator.clipboard.writeText(text);
}

function ImagePromptsView({
  prompts,
  projectId,
  refreshKey,
  onFiguresChanged,
}: {
  prompts: ImagePromptsSet;
  projectId: string;
  refreshKey: number;
  onFiguresChanged: () => void;
}) {
  const [figureRows, setFigureRows] = useState<ContentFigureDto[]>([]);

  useEffect(() => {
    let cancelled = false;
    void listFigures(projectId)
      .then((response) => {
        if (!cancelled) {
          setFigureRows(response.figures);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setFigureRows([]);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, refreshKey]);

  const pillarSections = prompts.sections.filter((s) => s.sourceType === "pillar");
  const blogSections = prompts.sections.filter((s) => s.sourceType === "blog");

  function figureFor(section: ImagePromptSection) {
    return figureRows.find(
      (f) =>
        f.sourceType === section.sourceType &&
        f.heading.trim().toLowerCase() === section.heading.trim().toLowerCase()
    );
  }

  function statusFor(section: ImagePromptSection) {
    return figureFor(section)?.status;
  }

  return (
    <div className="space-y-8">
      <p className="text-sm text-muted">
        Each H2 has a figure brief. After text is published to the site, generate art from the brief (OpenAI) or
        upload a WebP from Figma.
      </p>
      {pillarSections.length > 0 && (
        <ImagePromptSectionGroup
          title="Pillar sections"
          sections={pillarSections}
          projectId={projectId}
          figureFor={figureFor}
          statusFor={statusFor}
          onFiguresChanged={onFiguresChanged}
        />
      )}
      {blogSections.length > 0 && (
        <ImagePromptSectionGroup
          title="Blog sections"
          sections={blogSections}
          projectId={projectId}
          figureFor={figureFor}
          statusFor={statusFor}
          onFiguresChanged={onFiguresChanged}
        />
      )}
    </div>
  );
}

function ImagePromptSectionGroup({
  title,
  sections,
  projectId,
  figureFor,
  statusFor,
  onFiguresChanged,
}: {
  title: string;
  sections: ImagePromptSection[];
  projectId: string;
  figureFor: (section: ImagePromptSection) => ContentFigureDto | undefined;
  statusFor: (section: ImagePromptSection) => FigureStatus | undefined;
  onFiguresChanged: () => void;
}) {
  return (
    <div className="space-y-4">
      <h3 className="text-base font-semibold text-foreground">{title}</h3>
      {sections.map((item) => (
        <ImagePromptCard
          key={`${item.sourceType}-${item.order}-${item.heading}`}
          item={item}
          projectId={projectId}
          figure={figureFor(item)}
          figureStatus={statusFor(item)}
          onFiguresChanged={onFiguresChanged}
        />
      ))}
    </div>
  );
}

function FigureStatusBadge({ status }: { status: FigureStatus }) {
  const styles: Record<FigureStatus, string> = {
    Pending: "bg-slate-100 text-slate-700",
    Ready: "bg-blue-100 text-blue-900",
    Published: "bg-green-100 text-green-800",
    Skipped: "bg-amber-100 text-amber-900",
  };
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${styles[status]}`}>
      {status}
    </span>
  );
}

function ImagePromptCard({
  item,
  projectId,
  figure,
  figureStatus,
  onFiguresChanged,
}: {
  item: ImagePromptSection;
  projectId: string;
  figure?: ContentFigureDto;
  figureStatus?: FigureStatus;
  onFiguresChanged: () => void;
}) {
  const [copied, setCopied] = useState<"prompt" | "all" | null>(null);
  const [busy, setBusy] = useState<"generate" | "upload" | "skip" | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const canAct = Boolean(figure?.geekApiSlug) && figureStatus !== "Skipped";
  const headingSlug = figure?.headingSlug;

  async function handleCopy(mode: "prompt" | "all") {
    await copyText(mode === "prompt" ? item.prompt : formatLeonardoCopyBlock(item));
    setCopied(mode);
    window.setTimeout(() => setCopied(null), 2000);
  }

  async function handleGenerate() {
    if (!headingSlug) return;
    setBusy("generate");
    setActionError(null);
    try {
      await generateFigureImage(projectId, item.sourceType, headingSlug);
      onFiguresChanged();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : "Image generation failed.");
    } finally {
      setBusy(null);
    }
  }

  async function handleUpload(file: File) {
    if (!headingSlug) return;
    setBusy("upload");
    setActionError(null);
    try {
      await attachFigure(projectId, item.sourceType, headingSlug, file);
      onFiguresChanged();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : "Upload failed.");
    } finally {
      setBusy(null);
    }
  }

  async function handleSkip() {
    if (!headingSlug) return;
    setBusy("skip");
    setActionError(null);
    try {
      await skipFigure(projectId, item.sourceType, headingSlug);
      onFiguresChanged();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : "Skip failed.");
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="rounded-lg border border-border bg-background p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <h3 className="text-base font-semibold text-foreground">
            {item.order}. {item.heading}
          </h3>
          {figureStatus && <FigureStatusBadge status={figureStatus} />}
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => handleCopy("prompt")}
            className="rounded-md border border-border px-3 py-1.5 text-xs font-medium text-foreground hover:bg-surface"
          >
            {copied === "prompt" ? "Copied!" : "Copy brief"}
          </button>
          <button
            type="button"
            disabled={!canAct || busy !== null}
            onClick={() => void handleGenerate()}
            className="rounded-md bg-brand px-3 py-1.5 text-xs font-medium text-white hover:opacity-90 disabled:opacity-50"
          >
            {busy === "generate" ? "Generating…" : "Generate image"}
          </button>
          <label
            className={`cursor-pointer rounded-md border border-border px-3 py-1.5 text-xs font-medium text-foreground hover:bg-surface ${!canAct || busy !== null ? "pointer-events-none opacity-50" : ""}`}
          >
            {busy === "upload" ? "Uploading…" : "Upload WebP"}
            <input
              type="file"
              accept="image/webp"
              className="sr-only"
              disabled={!canAct || busy !== null}
              onChange={(e) => {
                const file = e.target.files?.[0];
                e.target.value = "";
                if (file) void handleUpload(file);
              }}
            />
          </label>
          {figureStatus !== "Skipped" && (
            <button
              type="button"
              disabled={!headingSlug || busy !== null}
              onClick={() => void handleSkip()}
              className="rounded-md border border-border px-3 py-1.5 text-xs font-medium text-muted hover:bg-surface disabled:opacity-50"
            >
              {busy === "skip" ? "Skipping…" : "Skip"}
            </button>
          )}
        </div>
      </div>
      {!figure?.geekApiSlug && (
        <p className="mt-2 text-xs text-amber-800">Publish text to the site before generating or uploading art.</p>
      )}
      {figure?.imageUrl && (
        // eslint-disable-next-line @next/next/no-img-element -- figure preview from blob CDN
        <img
          src={figure.imageUrl}
          alt={figure.imageAlt}
          className="mt-3 max-h-48 rounded-md border border-border object-contain"
        />
      )}
      {actionError && (
        <p className="mt-2 text-xs text-red-700" role="alert">
          {actionError}
        </p>
      )}
      <pre className="mt-3 whitespace-pre-wrap rounded-md border border-border bg-surface p-3 text-xs text-muted">
        {formatLeonardoSettings(item)}
      </pre>
      <div className="mt-3 whitespace-pre-wrap rounded-md border border-border p-3 text-sm text-foreground">
        {item.prompt}
      </div>
    </div>
  );
}
