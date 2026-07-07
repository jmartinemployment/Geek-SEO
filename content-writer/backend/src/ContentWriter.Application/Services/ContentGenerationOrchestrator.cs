using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.JsonLd;
using ContentWriter.Application.Services.PromptBuilders;
using ContentWriter.Application.Services.SchemaBuilders;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentWriter.Application.Services;

public class ContentGenerationOrchestrator : IContentGenerationOrchestrator
{
    private const int MaxPeopleAlsoAskQuestions = 12;

    private readonly IProjectRepository _projectRepository;
    private readonly IContentProviderFactory _providerFactory;
    private readonly IContentPromptBuilder _promptBuilder;
    private readonly IJsonLdParserService _jsonLdParser;
    private readonly ITechnicalArticleSchemaBuilder _articleSchemaBuilder;
    private readonly IBlogPostingSchemaBuilder _blogSchemaBuilder;
    private readonly CompanyProfileOptions _companyProfile;
    private readonly ILogger<ContentGenerationOrchestrator> _logger;

    public ContentGenerationOrchestrator(
        IProjectRepository projectRepository,
        IContentProviderFactory providerFactory,
        IContentPromptBuilder promptBuilder,
        IJsonLdParserService jsonLdParser,
        ITechnicalArticleSchemaBuilder articleSchemaBuilder,
        IBlogPostingSchemaBuilder blogSchemaBuilder,
        IOptions<CompanyProfileOptions> companyProfile,
        ILogger<ContentGenerationOrchestrator> logger)
    {
        _projectRepository = projectRepository;
        _providerFactory = providerFactory;
        _promptBuilder = promptBuilder;
        _jsonLdParser = jsonLdParser;
        _articleSchemaBuilder = articleSchemaBuilder;
        _blogSchemaBuilder = blogSchemaBuilder;
        _companyProfile = companyProfile.Value;
        _logger = logger;
    }

    public async Task<GeneratedContentSet> GeneratePillarPlanAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForGenerationAsync(projectId, cancellationToken);
        var context = BuildContext(project);
        var provider = _providerFactory.Get(project.PreferredProvider);

        _logger.LogInformation("Generating pillar plan for project {ProjectId} via {Provider}", projectId, provider.ProviderType);

        RemoveGeneratedContents(project,
            GeneratedContentType.TechnicalArticle,
            GeneratedContentType.BlogPost,
            GeneratedContentType.SocialFacebook,
            GeneratedContentType.SocialLinkedIn);

        var metadata = await GenerateArticleMetadataAsync(provider, context, cancellationToken);
        var articleSlug = SlugHelper.Slugify(metadata.Title);

        await AddContentAsync(project, provider.ProviderType, new GeneratedContent
        {
            ProjectId = project.Id,
            ContentType = GeneratedContentType.TechnicalArticle,
            Title = metadata.Title,
            Slug = articleSlug,
            MetaDescription = metadata.MetaDescription,
            Keywords = metadata.Keywords,
            SectionOutline = metadata.SectionOutline,
            WordCount = 0,
            BodyHtml = string.Empty,
            GeneratedByProvider = provider.ProviderType,
            GeneratedByModel = ResolveModelName(project.PreferredProvider)
        }, cancellationToken);

        await SaveProjectAsync(project, ProjectStatus.ReadyForGeneration, cancellationToken);
        return Assemble(project);
    }

    public async Task<GeneratedContentSet> GeneratePillarBodyAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForGenerationAsync(projectId, cancellationToken);
        var articleRow = RequireGeneratedContent(project, GeneratedContentType.TechnicalArticle,
            "Generate the pillar plan (Step 1) before writing the article body.");

        var context = BuildContext(project);
        var provider = _providerFactory.Get(project.PreferredProvider);
        var metadata = ToMetadataDraft(articleRow);
        var (bodyMetadata, faqQuestions) = PrepareBodyInput(metadata, context.PeopleAlsoAskQuestions, context.TargetKeyword);
        if (!articleRow.SectionOutline.SequenceEqual(bodyMetadata.SectionOutline))
        {
            articleRow.SectionOutline = bodyMetadata.SectionOutline;
        }

        var isRegeneration = !string.IsNullOrWhiteSpace(articleRow.BodyHtml) && articleRow.WordCount > 0;

        _logger.LogInformation(
            "Generating pillar body for project {ProjectId} via {Provider} (regeneration={IsRegeneration}, faqCount={FaqCount})",
            projectId, provider.ProviderType, isRegeneration, faqQuestions.Count);

        var bodyHtml = await GenerateArticleBodyAsync(provider, context, bodyMetadata, faqQuestions, isRegeneration, cancellationToken);
        var wordCount = HtmlWordCounter.Count(bodyHtml);
        var articleUrl = CombineUrl(context.ArticleBaseUrl, articleRow.Slug);
        var placeholderBlogUrl = CombineUrl(context.BlogBaseUrl, $"{articleRow.Slug}-blog");

        var now = DateTime.UtcNow;
        var articleMetadata = new ContentMetadata(
            metadata.Title, metadata.MetaDescription, context.AuthorName, context.PublisherName,
            context.PublisherLogoUrl, articleUrl, context.PublisherLogoUrl, now, now, metadata.Keywords, wordCount);
        var softwareApplications = ToolsSectionHtmlParser.ExtractApplications(bodyHtml, metadata.SectionOutline);
        articleRow.BodyHtml = bodyHtml;
        articleRow.WordCount = wordCount;
        articleRow.JsonLdSchema = _articleSchemaBuilder.Build(articleMetadata, placeholderBlogUrl, softwareApplications);
        articleRow.RelatedArticleUrl = placeholderBlogUrl;
        articleRow.GeneratedByProvider = provider.ProviderType;
        articleRow.GeneratedByModel = ResolveModelName(project.PreferredProvider);

        await SaveProjectAsync(project, ProjectStatus.ReadyForGeneration, cancellationToken);
        return Assemble(project);
    }

    public async Task<GeneratedContentSet> GeneratePillarAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await GeneratePillarPlanAsync(projectId, cancellationToken);
        return await GeneratePillarBodyAsync(projectId, cancellationToken);
    }

    public async Task<GeneratedContentSet> GenerateBlogAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForGenerationAsync(projectId, cancellationToken);
        var articleRow = RequireCompletePillar(project);

        var context = BuildContext(project);
        var provider = _providerFactory.Get(project.PreferredProvider);
        var article = GeneratedContentSetAssembler.ToArticleDraft(articleRow);
        var articleUrl = CombineUrl(context.ArticleBaseUrl, articleRow.Slug);

        _logger.LogInformation("Generating blog content for project {ProjectId} via {Provider}", projectId, provider.ProviderType);

        RemoveGeneratedContents(project, GeneratedContentType.BlogPost);

        var blog = await GenerateBlogDraftAsync(provider, context, article, cancellationToken);
        var blogSlug = SlugHelper.Slugify(blog.Title);
        var blogUrl = CombineUrl(context.BlogBaseUrl, blogSlug);

        var now = DateTime.UtcNow;
        var blogMetadata = new ContentMetadata(
            blog.Title, blog.MetaDescription, context.AuthorName, context.PublisherName,
            context.PublisherLogoUrl, blogUrl, context.PublisherLogoUrl, now, now, blog.Keywords, blog.WordCount);
        var blogJsonLd = _blogSchemaBuilder.Build(blogMetadata, articleUrl);

        var articleMetadata = new ContentMetadata(
            article.Title, article.MetaDescription, context.AuthorName, context.PublisherName,
            context.PublisherLogoUrl, articleUrl, context.PublisherLogoUrl, now, now, article.Keywords, article.WordCount);
        var softwareApplications = ToolsSectionHtmlParser.ExtractApplications(articleRow.BodyHtml, article.SectionOutline);
        articleRow.JsonLdSchema = _articleSchemaBuilder.Build(articleMetadata, blogUrl, softwareApplications);
        articleRow.RelatedArticleUrl = blogUrl;

        await AddContentAsync(project, provider.ProviderType, new GeneratedContent
        {
            ProjectId = project.Id,
            ContentType = GeneratedContentType.BlogPost,
            Title = blog.Title,
            Slug = blogSlug,
            MetaDescription = blog.MetaDescription,
            Keywords = blog.Keywords,
            WordCount = blog.WordCount,
            SectionOutline = blog.SectionOutline,
            BodyHtml = blog.BodyHtml,
            JsonLdSchema = blogJsonLd,
            RelatedArticleUrl = articleUrl,
            GeneratedByProvider = provider.ProviderType,
            GeneratedByModel = ResolveModelName(project.PreferredProvider)
        }, cancellationToken);

        await SaveProjectAsync(project, ProjectStatus.ReadyForGeneration, cancellationToken);
        return Assemble(project);
    }

    public async Task<GeneratedContentSet> GenerateSocialAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await LoadProjectForGenerationAsync(projectId, cancellationToken);
        var articleRow = RequireCompletePillar(project);

        var context = BuildContext(project);
        var provider = _providerFactory.Get(project.PreferredProvider);
        var article = GeneratedContentSetAssembler.ToArticleDraft(articleRow);
        var articleUrl = CombineUrl(context.ArticleBaseUrl, articleRow.Slug);

        _logger.LogInformation("Generating social content for project {ProjectId} via {Provider}", projectId, provider.ProviderType);

        RemoveGeneratedContents(project, GeneratedContentType.SocialFacebook, GeneratedContentType.SocialLinkedIn);

        var facebook = await GenerateSocialPostAsync(provider, context, article, articleUrl, "Facebook", cancellationToken);
        var linkedIn = await GenerateSocialPostAsync(provider, context, article, articleUrl, "LinkedIn", cancellationToken);

        await AddContentAsync(project, provider.ProviderType, new GeneratedContent
        {
            ProjectId = project.Id,
            ContentType = GeneratedContentType.SocialFacebook,
            Title = $"{article.Title} (Facebook)",
            Slug = $"{articleRow.Slug}-facebook",
            BodyHtml = facebook.Text,
            RelatedArticleUrl = articleUrl,
            GeneratedByProvider = provider.ProviderType,
            GeneratedByModel = ResolveModelName(project.PreferredProvider)
        }, cancellationToken);

        await AddContentAsync(project, provider.ProviderType, new GeneratedContent
        {
            ProjectId = project.Id,
            ContentType = GeneratedContentType.SocialLinkedIn,
            Title = $"{article.Title} (LinkedIn)",
            Slug = $"{articleRow.Slug}-linkedin",
            BodyHtml = linkedIn.Text,
            RelatedArticleUrl = articleUrl,
            GeneratedByProvider = provider.ProviderType,
            GeneratedByModel = ResolveModelName(project.PreferredProvider)
        }, cancellationToken);

        await SaveProjectAsync(project, ProjectStatus.Completed, cancellationToken);
        return Assemble(project);
    }

    public async Task<GeneratedContentSet> GenerateAllAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await GeneratePillarPlanAsync(projectId, cancellationToken);
        await GeneratePillarBodyAsync(projectId, cancellationToken);
        await GenerateBlogAsync(projectId, cancellationToken);
        return await GenerateSocialAsync(projectId, cancellationToken);
    }

    private async Task<Project> LoadProjectForGenerationAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetWithDetailsAsync(projectId, cancellationToken)
            ?? throw new ContentGenerationException($"Project {projectId} was not found.");

        if (project.CrawledSite is null)
        {
            throw new ContentGenerationException("Project has not been crawled yet. Run the crawl step before generating content.");
        }

        if (project.KeywordSources.Count == 0)
        {
            throw new ContentGenerationException("Upload at least one research input before generating content.");
        }

        return project;
    }

    private GeneratedContent RequireGeneratedContent(Project project, GeneratedContentType type, string message) =>
        project.GeneratedContents.FirstOrDefault(c => c.ContentType == type)
        ?? throw new ContentGenerationException(message);

    private GeneratedContent RequireCompletePillar(Project project)
    {
        var row = RequireGeneratedContent(project, GeneratedContentType.TechnicalArticle,
            "Generate the pillar plan and body (Steps 1–2) before continuing.");

        if (string.IsNullOrWhiteSpace(row.BodyHtml) || row.WordCount < 200)
        {
            throw new ContentGenerationException("Generate the pillar body (Step 2) before continuing.");
        }

        return row;
    }

    private static ArticleMetadataDraft ToMetadataDraft(GeneratedContent row) => new(
        row.Title,
        row.MetaDescription ?? string.Empty,
        row.Keywords,
        row.SectionOutline);

    private void RemoveGeneratedContents(Project project, params GeneratedContentType[] types)
    {
        var toRemove = project.GeneratedContents.Where(c => types.Contains(c.ContentType)).ToList();
        if (toRemove.Count == 0)
        {
            return;
        }

        _projectRepository.RemoveGeneratedContents(toRemove);
        foreach (var row in toRemove)
        {
            project.GeneratedContents.Remove(row);
        }
    }

    private async Task AddContentAsync(
        Project project,
        LlmProviderType providerType,
        GeneratedContent row,
        CancellationToken cancellationToken)
    {
        await _projectRepository.AddContentAsync(row, cancellationToken);
        project.GeneratedContents.Add(row);
    }

    private async Task SaveProjectAsync(Project project, ProjectStatus status, CancellationToken cancellationToken)
    {
        project.Status = status;
        project.UpdatedAtUtc = DateTime.UtcNow;
        _projectRepository.Update(project);
        await _projectRepository.SaveChangesAsync(cancellationToken);
    }

    private GeneratedContentSet Assemble(Project project) =>
        GeneratedContentSetAssembler.Assemble(project, _companyProfile.ArticleBaseUrl, _companyProfile.BlogBaseUrl);

    private ProjectGenerationContext BuildContext(Project project)
    {
        var crawl = project.CrawledSite!;
        var keywordSummaries = project.KeywordSources
            .Where(k => k.Category != KeywordSourceCategory.PeopleAlsoAsk)
            .Select(k => new KeywordSourceSummary(
                k.Category,
                k.ExtractedTitle,
                k.OriginalFileName,
                k.ExtractedHeadings,
                k.ExtractedParagraphs))
            .ToList();

        var paaQuestions = project.KeywordSources
            .Where(k => k.Category == KeywordSourceCategory.PeopleAlsoAsk)
            .SelectMany(k => k.ExtractedQuestions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxPeopleAlsoAskQuestions)
            .ToList();

        if (paaQuestions.Count == MaxPeopleAlsoAskQuestions)
        {
            var totalPaa = project.KeywordSources
                .Where(k => k.Category == KeywordSourceCategory.PeopleAlsoAsk)
                .SelectMany(k => k.ExtractedQuestions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            if (totalPaa > MaxPeopleAlsoAskQuestions)
            {
                _logger.LogWarning(
                    "Project {ProjectId} has {Total} PAA questions; using first {Cap} for generation.",
                    project.Id, totalPaa, MaxPeopleAlsoAskQuestions);
            }
        }

        var jsonLdSummary = JsonLdSummaryFormatter.Format(_jsonLdParser.Summarize(crawl.JsonLdBlocks));
        if (!string.IsNullOrWhiteSpace(jsonLdSummary))
        {
            _logger.LogInformation(
                "Including parsed JSON+LD structured summary for project {ProjectId} ({BlockCount} raw blocks)",
                project.Id,
                crawl.JsonLdBlocks.Count);
        }

        return new ProjectGenerationContext(
            ProjectName: project.Name,
            ProjectUrl: project.ProjectUrl,
            TargetKeyword: project.TargetKeyword,
            SiteName: crawl.SiteName,
            DetectedTone: crawl.DetectedTone,
            DetectedFocus: crawl.DetectedFocus,
            CrawledHeadings: crawl.Headings,
            CrawledParagraphs: crawl.Paragraphs,
            JsonLdStructuredSummary: string.IsNullOrWhiteSpace(jsonLdSummary) ? null : jsonLdSummary,
            KeywordSources: keywordSummaries,
            PeopleAlsoAskQuestions: paaQuestions,
            PublisherName: _companyProfile.PublisherName,
            PublisherLogoUrl: _companyProfile.PublisherLogoUrl,
            AuthorName: _companyProfile.AuthorName,
            ArticleBaseUrl: _companyProfile.ArticleBaseUrl,
            BlogBaseUrl: _companyProfile.BlogBaseUrl,
            ImplementerPositioning: _companyProfile.ImplementerPositioning,
            Provider: project.PreferredProvider);
    }

    private static string CombineUrl(string baseUrl, string slug) => $"{baseUrl.TrimEnd('/')}/{slug}";

    private static string ResolveModelName(LlmProviderType provider) => provider switch
    {
        LlmProviderType.LmStudio => "lm-studio-local",
        LlmProviderType.OpenAi => "openai",
        LlmProviderType.Anthropic => "anthropic",
        _ => "unknown"
    };

    private async Task<ArticleMetadataDraft> GenerateArticleMetadataAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        CancellationToken cancellationToken)
    {
        var metadataResult = await provider.CompleteAsync(
            _promptBuilder.BuildArticleMetadataPrompt(context),
            cancellationToken);
        var metadata = NormalizeMetadata(ParseJson<ArticleMetadataDraft>(metadataResult.Content, "TechnicalArticle metadata"));
        metadata = SanitizePlanMetadata(metadata, context.PeopleAlsoAskQuestions, context.TargetKeyword);
        return PillarPlanMetadataNormalizer.Normalize(metadata, context.TargetKeyword);
    }

    private async Task<string> GenerateArticleBodyAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> faqQuestions,
        bool isRegeneration,
        CancellationToken cancellationToken)
    {
        var mainSections = metadata.SectionOutline
            .Where(s => !PillarOutlineNormalizer.IsFaqSectionTitle(s))
            .ToList();

        var parts = new List<string>();

        for (var i = 0; i < mainSections.Count; i++)
        {
            var heading = mainSections[i];
            _logger.LogInformation(
                "Generating pillar section {Index}/{Total}: {Heading}",
                i + 1, mainSections.Count, heading);

            var sectionResult = await provider.CompleteAsync(
                _promptBuilder.BuildArticleSectionPrompt(
                    context, metadata, heading, i, mainSections.Count, metadata.SectionOutline, isRegeneration),
                cancellationToken);

            parts.Add(LlmResponseJsonParser.ParseHtmlBody(
                sectionResult.Content, $"TechnicalArticle section '{heading}'"));
        }

        if (faqQuestions.Count > 0)
        {
            _logger.LogInformation("Generating pillar FAQ section ({Count} questions)", faqQuestions.Count);

            var faqResult = await provider.CompleteAsync(
                _promptBuilder.BuildArticleFaqSectionPrompt(context, metadata, faqQuestions, isRegeneration),
                cancellationToken);

            parts.Add(LlmResponseJsonParser.ParseHtmlBody(
                faqResult.Content, "TechnicalArticle FAQ section"));
        }

        return string.Join("\n\n", parts);
    }

    private static ArticleMetadataDraft SanitizePlanMetadata(
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> paaQuestions,
        string targetKeyword)
    {
        var (mainOutline, _) = PillarOutlineNormalizer.Sanitize(metadata.SectionOutline, paaQuestions, targetKeyword);
        return metadata with { SectionOutline = mainOutline };
    }

    private static (ArticleMetadataDraft Metadata, List<string> FaqQuestions) PrepareBodyInput(
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> paaQuestions,
        string targetKeyword)
    {
        var (mainOutline, faqQuestions) = PillarOutlineNormalizer.Sanitize(metadata.SectionOutline, paaQuestions, targetKeyword);
        return (metadata with { SectionOutline = mainOutline }, faqQuestions);
    }

    private static ArticleMetadataDraft NormalizeMetadata(ArticleMetadataDraft metadata) => metadata with
    {
        Keywords = metadata.Keywords ?? new List<string>(),
        SectionOutline = metadata.SectionOutline ?? new List<string>()
    };

    private async Task<ArticleDraft> GenerateArticleAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        CancellationToken cancellationToken)
    {
        var metadata = await GenerateArticleMetadataAsync(provider, context, cancellationToken);
        var (_, faqQuestions) = PillarOutlineNormalizer.Sanitize(
            metadata.SectionOutline, context.PeopleAlsoAskQuestions, context.TargetKeyword);
        var bodyHtml = await GenerateArticleBodyAsync(
            provider,
            context,
            metadata,
            faqQuestions,
            isRegeneration: false,
            cancellationToken);

        return new ArticleDraft(
            metadata.Title,
            metadata.MetaDescription,
            bodyHtml,
            metadata.Keywords,
            HtmlWordCounter.Count(bodyHtml),
            metadata.SectionOutline);
    }

    private async Task<BlogDraft> GenerateBlogDraftAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        ArticleDraft article,
        CancellationToken cancellationToken)
    {
        var metadataResult = await provider.CompleteAsync(
            _promptBuilder.BuildBlogMetadataPrompt(context, article),
            cancellationToken);
        var metadata = NormalizeBlogMetadata(ParseJson<BlogMetadataDraft>(metadataResult.Content, "BlogPosting metadata"));

        var bodyHtml = await GenerateBlogBodyAsync(provider, context, article, metadata, cancellationToken);
        var wordCount = HtmlWordCounter.Count(bodyHtml);

        if (wordCount < ContentLengthTargets.BlogMinWords)
        {
            _logger.LogWarning(
                "Blog draft for project keyword \"{Keyword}\" is {Count} words (minimum {Minimum}); running expansion pass.",
                context.TargetKeyword,
                wordCount,
                ContentLengthTargets.BlogMinWords);

            var expansionResult = await provider.CompleteAsync(
                _promptBuilder.BuildBlogBodyPrompt(context, article, metadata),
                cancellationToken);
            var expandedHtml = LlmResponseJsonParser.ParseHtmlBody(expansionResult.Content, "BlogPosting expansion body");
            var expandedCount = HtmlWordCounter.Count(expandedHtml);
            if (expandedCount > wordCount)
            {
                bodyHtml = expandedHtml;
                wordCount = expandedCount;
            }
        }

        return new BlogDraft(
            metadata.Title,
            metadata.MetaDescription,
            bodyHtml,
            metadata.Keywords,
            wordCount,
            metadata.SectionOutline);
    }

    private async Task<string> GenerateBlogBodyAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        ArticleDraft article,
        BlogMetadataDraft metadata,
        CancellationToken cancellationToken)
    {
        var sections = metadata.SectionOutline.Count > 0
            ? metadata.SectionOutline
            :
            [
                "Why this matters now",
                "Key takeaways from the pillar",
                "Practical steps you can take",
                "What to read next"
            ];

        var parts = new List<string>();

        for (var i = 0; i < sections.Count; i++)
        {
            var heading = sections[i];
            _logger.LogInformation(
                "Generating blog section {Index}/{Total}: {Heading}",
                i + 1,
                sections.Count,
                heading);

            var sectionResult = await provider.CompleteAsync(
                _promptBuilder.BuildBlogSectionPrompt(context, article, metadata, heading, i, sections.Count),
                cancellationToken);

            parts.Add(LlmResponseJsonParser.ParseHtmlBody(
                sectionResult.Content,
                $"BlogPosting section '{heading}'"));
        }

        return string.Join("\n\n", parts);
    }

    private static BlogMetadataDraft NormalizeBlogMetadata(BlogMetadataDraft metadata) => metadata with
    {
        Keywords = metadata.Keywords ?? new List<string>(),
        SectionOutline = metadata.SectionOutline ?? new List<string>()
    };

    private async Task<SocialPostDraft> GenerateSocialPostAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        ArticleDraft article,
        string articleUrl,
        string platform,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await provider.CompleteAsync(
                _promptBuilder.BuildSocialPrompt(context, article, platform, articleUrl),
                cancellationToken);

            try
            {
                var text = LlmResponseJsonParser.ParseSocialText(result.Content, articleUrl, $"{platform} post");
                return new SocialPostDraft(platform, text);
            }
            catch (ContentGenerationException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Retrying {Platform} post generation after invalid JSON (attempt {Attempt})", platform, attempt);
            }
        }

        throw new ContentGenerationException($"Model did not return valid JSON for {platform} post after {maxAttempts} attempts.");
    }

    private T ParseJson<T>(string rawContent, string label)
    {
        try
        {
            return LlmResponseJsonParser.Parse<T>(rawContent, label);
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogError(ex, "Failed to parse {Label} JSON. Raw content: {Raw}", label, rawContent);
            throw;
        }
    }
}

public class CompanyProfileOptions
{
    public const string SectionName = "CompanyProfile";

    public string PublisherName { get; set; } = "Geek At Your Spot";
    public string PublisherLogoUrl { get; set; } = "https://seo.geekatyourspot.com/logo.png";
    public string AuthorName { get; set; } = "Geek At Your Spot Editorial Team";
    public string ArticleBaseUrl { get; set; } = "https://seo.geekatyourspot.com/articles";
    public string BlogBaseUrl { get; set; } = "https://seo.geekatyourspot.com/blog";

    /// <summary>How the publisher positions AI implementation services in pillar Tools sections.</summary>
    public string ImplementerPositioning { get; set; } =
        "Geek At Your Spot is an AI implementation consultancy for B2B organizations. " +
        "In every pillar Tools section, for each major platform covered, explain which client problems an AI implementer solves " +
        "(accelerated deployment, data model design, workflow configuration, custom code, autonomous agents, integration, and change management).";
}
