using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.DTOs;

/// <summary>Everything the orchestrator needs to know about a project to generate its content set.</summary>
public record ProjectGenerationContext(
    string ProjectName,
    string ProjectUrl,
    string TargetKeyword,
    string SiteName,
    string DetectedTone,
    string DetectedFocus,
    List<string> CrawledHeadings,
    List<string> CrawledParagraphs,
    string? JsonLdStructuredSummary,
    List<KeywordSourceSummary> KeywordSources,
    List<string> PeopleAlsoAskQuestions,
    string PublisherName,
    string PublisherLogoUrl,
    string AuthorName,
    string ArticleBaseUrl,
    string BlogBaseUrl,
    string ImplementerPositioning,
    LlmProviderType Provider);

public record KeywordSourceSummary(
    KeywordSourceCategory Category,
    string? Title,
    string SourceLabel,
    List<string> Headings,
    List<string> Paragraphs);

public record ArticleMetadataDraft(
    string Title,
    string MetaDescription,
    List<string> Keywords,
    List<string> SectionOutline);

public record BlogMetadataDraft(
    string Title,
    string MetaDescription,
    List<string> Keywords,
    List<string> SectionOutline);

public record ArticleDraft(
    string Title,
    string MetaDescription,
    string BodyHtml,
    List<string> Keywords,
    int WordCount,
    List<string> SectionOutline);

public record BlogDraft(
    string Title,
    string MetaDescription,
    string BodyHtml,
    List<string> Keywords,
    int WordCount,
    List<string> SectionOutline);

public record SocialPostDraft(string Platform, string Text);

public record GeneratedContentSet(
    ArticleDraft? Article,
    string? ArticleSlug,
    string? ArticleUrl,
    string? ArticleJsonLd,
    BlogDraft? Blog,
    string? BlogSlug,
    string? BlogUrl,
    string? BlogJsonLd,
    SocialPostDraft? FacebookPost,
    SocialPostDraft? LinkedInPost);
