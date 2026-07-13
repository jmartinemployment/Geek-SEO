export const SITE_DEPARTMENTS = [
  "accounting",
  "customer-service",
  "human-resources",
  "marketing",
  "sales",
] as const;

export type SiteDepartmentSlug = (typeof SITE_DEPARTMENTS)[number];

export type LlmProviderType = "LmStudio" | "OpenAi" | "Anthropic";

export type ProjectStatus =
  | "Draft"
  | "Crawling"
  | "ReadyForGeneration"
  | "Generating"
  | "Completed"
  | "Failed";

export type KeywordSourceCategory =
  | "KeywordResult"
  | "EduDomain"
  | "GovDomain"
  | "Wikipedia"
  | "Local"
  | "PeopleAlsoAsk"
  | "CompetitorCrawl";

export type GeneratedContentType =
  | "TechnicalArticle"
  | "BlogPost"
  | "SocialFacebook"
  | "SocialLinkedIn"
  | "EmailColdOutreach"
  | "EmailNewsletter"
  | "EmailStoryNurture"
  | "EmailTransactional"
  | "ImagePromptPillarFigure"
  | "ImagePromptSocialFacebook"
  | "ImagePromptSocialLinkedIn"
  | "ImagePromptSection";

export interface ProjectSummary {
  id: string;
  name: string;
  projectUrl: string;
  targetKeyword: string;
  status: ProjectStatus;
  preferredProvider: LlmProviderType;
  createdAtUtc: string;
}

export interface CrawlSummary {
  siteName: string;
  pagesCrawled: number;
  detectedTone: string;
  detectedFocus: string;
  headingCount: number;
  paragraphCount: number;
  jsonLdBlockCount: number;
}

export interface KeywordSourceResponse {
  id: string;
  category: KeywordSourceCategory;
  originalFileName: string;
  extractedTitle: string | null;
  headingCount: number;
  paragraphCount: number;
  questionCount: number;
}

export interface GeneratedContentResponse {
  id: string;
  contentType: GeneratedContentType;
  title: string;
  slug: string;
  metaDescription: string | null;
  keywords: string[];
  wordCount: number;
  bodyHtml: string;
  jsonLdSchema: string | null;
  relatedArticleUrl: string | null;
  createdAtUtc: string;
}

export interface ProjectDetail extends ProjectSummary {
  crawl: CrawlSummary | null;
  keywordSources: KeywordSourceResponse[];
  generatedContent: GeneratedContentResponse[];
  contentSet: GeneratedContentSet | null;
}

export interface ArticleDraft {
  title: string;
  metaDescription: string;
  bodyHtml: string;
  keywords: string[];
  wordCount: number;
  sectionOutline: string[];
  homeUseCaseExcerpt?: string;
  heroExcerpt?: string;
  newspaperExcerpt?: string;
  pillarPageUseCaseExcerpt?: string;
  advertisement?: string | null;
}

export const CONTENT_LENGTH_TARGETS = {
  pillar: {
    min: 3000,
    label: "3,000+",
    definition:
      "Pillar / cornerstone TechnicalArticles — broad complete-guide depth (3,000+ words) for topical authority and long-tail keywords.",
  },
  blog: {
    min: 1500,
    max: 2500,
    label: "1,500–2,500",
    definition:
      "Blog postings — listicles, how-tos, and evergreen guides with headers, bullets, and internal linking depth.",
  },
  listicleGuide: {
    min: 1200,
    max: 1800,
    label: "1,200–1,800",
    definition:
      "Actionable step-by-step tutorials with substantial context, data, and layout formatting.",
  },
  news: {
    min: 600,
    max: 1000,
    label: "600–1,000",
    definition:
      "News articles — timely, focused copy with core details upfront for quick indexing and skimming readers.",
  },
  tool: {
    min: 1500,
    max: 2500,
    hardMax: 2500,
    label: "1,500–2,500",
    definition:
      "TechnicalArticle tool guides — comprehensive platform coverage (1,500–2,500 words) with distinct presentation copy and JSON-LD citing the pillar.",
  },
  emailColdOutreach: {
    min: 50,
    max: 125,
    label: "50–125",
    definition:
      "High response rates; pitch a single, clear call-to-action.",
  },
  emailNewsletter: {
    min: 200,
    max: 400,
    label: "200–400",
    definition: "Summarize external links; drive traffic back to your website.",
  },
  emailStoryNurture: {
    min: 500,
    max: 1000,
    label: "500–1,000",
    definition: "Build deep trust; treats email like an exclusive blog post.",
  },
  emailTransactional: {
    min: 1,
    max: 49,
    label: "Under 50",
    definition: "Deliver critical data; highly functional with zero fluff.",
  },
} as const;

export interface ToolDraft {
  title: string;
  displayTitle: string;
  departmentListExcerpt: string;
  heroExcerpt: string;
  newspaperExcerpt: string;
  toolPageExcerpt: string;
  metaDescription: string;
  advertisement: string | null;
  bodyHtml: string;
  slug: string;
  sourceAppName: string;
  sourceAppOrder: number;
  wordCount: number;
  jsonLdSchema: string | null;
}

export interface ColdOutreachEmailDraft {
  subject: string;
  bodyText: string;
  ctaLabel: string;
  ctaUrl: string;
}

export interface SocialPostDraft {
  platform: string;
  text: string;
}

export interface ImagePromptSection {
  sourceType: string;
  heading: string;
  order: number;
  prompt: string;
  width: number;
  height: number;
  leonardoModel: string;
  leonardoModelId: string;
  stylePreset: string;
  alchemy: boolean;
  photoReal: boolean;
  notes: string | null;
}

export interface ImagePromptsSet {
  sections: ImagePromptSection[];
}

export interface ExportMarkdownFile {
  contentType: string;
  relativePath: string;
  filePath: string | null;
  markdown: string;
}

export interface ExportMarkdownResponse {
  department: string;
  files: ExportMarkdownFile[];
}

export interface PublishedGeekPostResponse {
  postType: string;
  slug: string;
  postId: number;
  created: boolean;
  publicPath: string;
}

export interface PublishToSiteResponse {
  department: string;
  posts: PublishedGeekPostResponse[];
}

export type FigureStatus = "Pending" | "Ready" | "Skipped" | "Published";

export interface ContentFigureDto {
  id: string;
  sourceType: string;
  sectionOrder: number;
  headingSlug: string;
  heading: string;
  briefText: string;
  status: FigureStatus;
  skipReason: string | null;
  imageUrl: string | null;
  imageAlt: string;
  geekApiSlug: string | null;
  geekPostId: number | null;
}

export interface ContentFiguresSummary {
  pending: number;
  ready: number;
  skipped: number;
  published: number;
  missingGeekApiSlug: number;
}

export interface ContentFiguresListResponse {
  projectId: string;
  figures: ContentFigureDto[];
  summary: ContentFiguresSummary;
  inAppGenerationEnabled: boolean;
}

export interface FigureGenerateResponse {
  sourceType: string;
  generatedCount: number;
  figures: ContentFigureDto[];
}

export interface GeneratedContentSet {
  department: string;
  article: ArticleDraft | null;
  articleSlug: string | null;
  articleUrl: string | null;
  articleJsonLd: string | null;
  blog: ArticleDraft | null;
  blogSlug: string | null;
  blogUrl: string | null;
  blogJsonLd: string | null;
  facebookPost: SocialPostDraft | null;
  linkedInPost: SocialPostDraft | null;
  coldOutreachEmail: ColdOutreachEmailDraft | null;
  imagePrompts: ImagePromptsSet | null;
  tools: ToolDraft[] | null;
  toolsGenerationOutcome: string | null;
}

export interface LmStudioHealthStatus {
  isReachable: boolean;
  modelId: string | null;
  message: string | null;
}

export const KEYWORD_SOURCE_CATEGORIES: { value: KeywordSourceCategory; label: string }[] = [
  { value: "KeywordResult", label: "Keyword SERP Result" },
  { value: "EduDomain", label: ".edu Domain" },
  { value: "GovDomain", label: ".gov Domain" },
  { value: "Wikipedia", label: "Wikipedia" },
  { value: "Local", label: "Local Pack" },
  { value: "CompetitorCrawl", label: "Competitor Crawl" },
  { value: "PeopleAlsoAsk", label: "People Also Ask (text)" },
];

export const PROVIDER_OPTIONS: { value: LlmProviderType; label: string }[] = [
  { value: "LmStudio", label: "LM Studio (local dev only)" },
  { value: "OpenAi", label: "OpenAI" },
  { value: "Anthropic", label: "Anthropic (Claude)" },
];
