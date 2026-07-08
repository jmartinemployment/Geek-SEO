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
  | "EmailTransactional";

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
}

export const CONTENT_LENGTH_TARGETS = {
  pillar: {
    min: 3000,
    max: 5000,
    label: "3,000–5,000+",
    definition:
      "Exhaustive macro-level entry points for massive topics — multiple subsections that link out to cluster articles.",
  },
  blog: {
    min: 1800,
    max: 2500,
    label: "1,800–2,500",
    definition:
      "Deep-dive articles aimed at outranking competitors — substantive depth in every section, not surface summaries.",
  },
  listicleGuide: {
    min: 1200,
    max: 1800,
    label: "1,200–1,800",
    definition:
      "Actionable step-by-step tutorials with substantial context, data, and layout formatting.",
  },
  news: {
    min: 400,
    max: 800,
    label: "400–800",
    definition: "Press releases and short announcements — timely and concise.",
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

export interface GeneratedContentSet {
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
