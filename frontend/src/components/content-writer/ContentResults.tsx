"use client";

import { useState } from "react";
import {
  generateBlogContent,
  generatePillarBodyContent,
  generatePillarPlanContent,
  generateSocialContent,
  ApiError,
} from "@/lib/content-writer/api";
import type { GeneratedContentSet } from "@/lib/content-writer/types";
import { CONTENT_LENGTH_TARGETS } from "@/lib/content-writer/types";

type Tab = "article" | "blog" | "facebook" | "linkedin";
type GeneratingStep = "pillar-plan" | "pillar-body" | "blog" | "social" | "all" | null;

const PILLAR_BODY_MIN_WORDS = 200;

const TABS: { id: Tab; label: string }[] = [
  { id: "article", label: "Technical Article" },
  { id: "blog", label: "Blog Post" },
  { id: "facebook", label: "Facebook" },
  { id: "linkedin", label: "LinkedIn" },
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

  const hasPillarPlan = result?.article != null;
  const hasPillarBody = (result?.article?.wordCount ?? 0) >= PILLAR_BODY_MIN_WORDS;
  const hasBlog = result?.blog != null;
  const hasSocial = result?.facebookPost != null && result?.linkedInPost != null;
  const isGenerating = generatingStep !== null;

  async function runStep(step: GeneratingStep, action: () => Promise<GeneratedContentSet>) {
    setError(null);
    setGeneratingStep(step);
    try {
      onGenerated(await action());
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
        Run each step separately. Steps 1–2 plan and write the pillar article; steps 3–4 build blog and social
        from it.
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
            }
            return state!;
          })
        }
        disabled={!canGenerate || isGenerating || (hasPillarBody && hasBlog && hasSocial)}
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

      {result?.article && (
        <div className="mt-6">
          <div className="flex gap-1 border-b border-border">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                disabled={
                  (tab.id === "article" && !result.article) ||
                  (tab.id === "blog" && !result.blog) ||
                  ((tab.id === "facebook" || tab.id === "linkedin") && !result.facebookPost)
                }
                className={`rounded-t-md px-4 py-2 text-sm font-medium transition-colors disabled:opacity-40 ${
                  activeTab === tab.id
                    ? "border-b-2 border-brand text-brand"
                    : "text-muted hover:text-foreground"
                }`}
              >
                {tab.label}
              </button>
            ))}
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
            {activeTab === "blog" && result.blog && result.blogUrl && (
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
            )}
            {activeTab === "facebook" && result.facebookPost && (
              <SocialView text={result.facebookPost.text} platform="Facebook" />
            )}
            {activeTab === "linkedin" && result.linkedInPost && (
              <SocialView text={result.linkedInPost.text} platform="LinkedIn" />
            )}
          </div>
        </div>
      )}
    </div>
  );
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
