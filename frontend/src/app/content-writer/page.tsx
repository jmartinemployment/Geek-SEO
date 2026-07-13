"use client";

import { Suspense, useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import ProjectForm from "@/components/content-writer/ProjectForm";
import CrawlPanel from "@/components/content-writer/CrawlPanel";
import FileUploadPanel from "@/components/content-writer/FileUploadPanel";
import ContentResults from "@/components/content-writer/ContentResults";
import { ApiError, getProject, getRecentProjects } from "@/lib/content-writer/api";
import type {
  CrawlSummary,
  GeneratedContentSet,
  KeywordSourceResponse,
  ProjectSummary,
} from "@/lib/content-writer/types";

const LAST_PROJECT_STORAGE_KEY = "content-writer:last-project-id";

function toProjectSummary(detail: {
  id: string;
  name: string;
  projectUrl: string;
  targetKeyword: string;
  status: ProjectSummary["status"];
  preferredProvider: ProjectSummary["preferredProvider"];
  createdAtUtc: string;
}): ProjectSummary {
  return {
    id: detail.id,
    name: detail.name,
    projectUrl: detail.projectUrl,
    targetKeyword: detail.targetKeyword,
    status: detail.status,
    preferredProvider: detail.preferredProvider,
    createdAtUtc: detail.createdAtUtc,
  };
}

function ResumeProjectPanel({
  activeProjectId,
  onResume,
}: {
  activeProjectId: string | null;
  onResume: (projectId: string) => Promise<void>;
}) {
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [resumingId, setResumingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void getRecentProjects()
      .then((items) => {
        if (!cancelled) {
          setProjects(items);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setProjects([]);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (loading || projects.length === 0) {
    return null;
  }

  return (
    <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
      <h2 className="text-lg font-semibold text-foreground">Resume a project</h2>
      <p className="mt-1 text-sm text-muted">
        After a refresh, pick your existing project here — generation steps (including tool pages) only appear once a
        project is loaded.
      </p>
      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}
      <ul className="mt-4 space-y-2">
        {projects.map((item) => {
          const isActive = item.id === activeProjectId;
          const isResuming = item.id === resumingId;
          return (
            <li
              key={item.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-background px-4 py-3"
            >
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-foreground">{item.name}</p>
                <p className="truncate text-xs text-muted">{item.targetKeyword}</p>
              </div>
              <button
                type="button"
                disabled={isActive || isResuming}
                onClick={async () => {
                  setError(null);
                  setResumingId(item.id);
                  try {
                    await onResume(item.id);
                  } catch (err) {
                    setError(err instanceof ApiError ? err.message : "Could not load project.");
                  } finally {
                    setResumingId(null);
                  }
                }}
                className="shrink-0 rounded-md border border-brand px-3 py-1.5 text-sm font-semibold text-brand transition-colors hover:bg-brand/5 disabled:opacity-60"
              >
                {isActive ? "Loaded" : isResuming ? "Loading…" : "Open"}
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

function ContentWriterPageInner() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [project, setProject] = useState<ProjectSummary | null>(null);
  const [crawl, setCrawl] = useState<CrawlSummary | null>(null);
  const [keywordSources, setKeywordSources] = useState<KeywordSourceResponse[]>([]);
  const [generated, setGenerated] = useState<GeneratedContentSet | null>(null);
  const [resumeError, setResumeError] = useState<string | null>(null);
  const [autoResumeAttempted, setAutoResumeAttempted] = useState(false);

  const canGenerate = crawl !== null && keywordSources.length > 0;

  const rememberProject = useCallback(
    (summary: ProjectSummary) => {
      setProject(summary);
      if (typeof window !== "undefined") {
        localStorage.setItem(LAST_PROJECT_STORAGE_KEY, summary.id);
      }
      router.replace(`/content-writer?projectId=${encodeURIComponent(summary.id)}`, { scroll: false });
    },
    [router],
  );

  const loadProjectState = useCallback(
    async (projectId: string) => {
      const detail = await getProject(projectId);
      rememberProject(toProjectSummary(detail));
      setCrawl(detail.crawl);
      setKeywordSources(detail.keywordSources);
      setGenerated(detail.contentSet);
    },
    [rememberProject],
  );

  useEffect(() => {
    if (project || autoResumeAttempted) {
      return;
    }

    const fromUrl = searchParams.get("projectId");
    const fromStorage =
      typeof window !== "undefined" ? localStorage.getItem(LAST_PROJECT_STORAGE_KEY) : null;
    const projectId = fromUrl ?? fromStorage;
    if (!projectId) {
      setAutoResumeAttempted(true);
      return;
    }

    setAutoResumeAttempted(true);
    void loadProjectState(projectId).catch((err) => {
      setResumeError(err instanceof ApiError ? err.message : "Could not restore your last project.");
    });
  }, [autoResumeAttempted, loadProjectState, project, searchParams]);

  async function handleProjectCreated(created: ProjectSummary) {
    await loadProjectState(created.id);
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="mb-8">
        <p className="text-sm font-semibold uppercase tracking-wide text-brand">Content Writer</p>
        <h1 className="mt-1 text-3xl font-bold text-foreground">Content Writer</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted">
          Turn a crawled client site and manually-scraped SERP research into a publish-ready TechnicalArticle,
          NewsArticle tool pages (when the pillar has a Top AI Tools section), companion BlogPost, Facebook/LinkedIn
          social posts, and a cold outreach email — each with schema.org JSON+LD where applicable.
        </p>
      </div>

      <div className="flex flex-col gap-6">
        <ResumeProjectPanel activeProjectId={project?.id ?? null} onResume={loadProjectState} />
        {resumeError && <p className="text-sm text-red-600">{resumeError}</p>}

        <ProjectForm onCreated={handleProjectCreated} />

        {project && (
          <CrawlPanel projectId={project.id} projectUrl={project.projectUrl} crawl={crawl} onCrawled={setCrawl} />
        )}

        {project && (
          <FileUploadPanel projectId={project.id} keywordSources={keywordSources} onChanged={setKeywordSources} />
        )}

        {project && (
          <ContentResults
            projectId={project.id}
            canGenerate={canGenerate}
            result={generated}
            onGenerated={setGenerated}
          />
        )}
      </div>
    </div>
  );
}

export default function ContentWriterPage() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto max-w-4xl px-4 py-10 text-sm text-muted sm:px-6 lg:px-8">Loading Content Writer…</div>
      }
    >
      <ContentWriterPageInner />
    </Suspense>
  );
}
