"use client";

import { useCallback, useState } from "react";
import ProjectForm from "@/components/content-writer/ProjectForm";
import CrawlPanel from "@/components/content-writer/CrawlPanel";
import FileUploadPanel from "@/components/content-writer/FileUploadPanel";
import ContentResults from "@/components/content-writer/ContentResults";
import { getProject } from "@/lib/content-writer/api";
import type {
  CrawlSummary,
  GeneratedContentSet,
  KeywordSourceResponse,
  ProjectSummary,
} from "@/lib/content-writer/types";

export default function ContentWriterPage() {
  const [project, setProject] = useState<ProjectSummary | null>(null);
  const [crawl, setCrawl] = useState<CrawlSummary | null>(null);
  const [keywordSources, setKeywordSources] = useState<KeywordSourceResponse[]>([]);
  const [generated, setGenerated] = useState<GeneratedContentSet | null>(null);

  const canGenerate = crawl !== null && keywordSources.length > 0;

  const loadProjectState = useCallback(async (projectId: string) => {
    const detail = await getProject(projectId);
    setCrawl(detail.crawl);
    setKeywordSources(detail.keywordSources);
    setGenerated(detail.contentSet);
  }, []);

  async function handleProjectCreated(created: ProjectSummary) {
    setProject(created);
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
