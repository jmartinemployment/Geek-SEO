using System.Text.RegularExpressions;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.PromptBuilders;
using ContentWriter.Application.Services.Publish;
using ContentWriter.Application.Services.SchemaBuilders;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Services;

public interface IToolPageGenerator
{
    Task<IReadOnlyList<GeneratedContent>> GenerateToolPagesAsync(
        Project project,
        GeneratedContent articleRow,
        ArticleMetadataDraft metadata,
        ProjectGenerationContext context,
        IContentGenerationProvider provider,
        string department,
        CancellationToken cancellationToken = default);
}

public sealed class ToolPageGenerator : IToolPageGenerator
{
    private const int MaxTools = 5;
    private readonly ISoftwareApplicationSchemaBuilder _softwareSchemaBuilder;

    public ToolPageGenerator(ISoftwareApplicationSchemaBuilder softwareSchemaBuilder)
    {
        _softwareSchemaBuilder = softwareSchemaBuilder;
    }

    public async Task<IReadOnlyList<GeneratedContent>> GenerateToolPagesAsync(
        Project project,
        GeneratedContent articleRow,
        ArticleMetadataDraft metadata,
        ProjectGenerationContext context,
        IContentGenerationProvider provider,
        string department,
        CancellationToken cancellationToken = default)
    {
        var applications = ToolsSectionHtmlParser.ExtractApplications(articleRow.BodyHtml, metadata.SectionOutline)
            .Take(MaxTools)
            .ToList();

        if (applications.Count == 0)
        {
            return [];
        }

        var rows = new List<GeneratedContent>();
        var order = 1;
        foreach (var app in applications)
        {
            var slug = SlugHelper.Slugify(app.Name);
            var toolUrl = GeekPublicUrlBuilder.ToolUrl(context.ArticleBaseUrl, department, slug);

            var bodyHtml = await GenerateToolBodyAsync(
                provider,
                context,
                metadata,
                app,
                department,
                slug,
                cancellationToken);

            var wordCount = HtmlWordCounter.Count(bodyHtml);
            var listingExcerpt = BuildListingExcerpt(app);
            var advertisingExcerpt = $"Explore how {app.Name.Trim()} supports {context.TargetKeyword} workflows.";
            var displayTitle = app.Name.Trim();
            var now = DateTime.UtcNow;
            var schemaMeta = new ContentMetadata(
                displayTitle,
                listingExcerpt,
                context.AuthorName,
                context.PublisherName,
                context.PublisherLogoUrl,
                toolUrl,
                context.PublisherLogoUrl,
                now,
                now,
                metadata.Keywords,
                wordCount);

            var jsonLd = _softwareSchemaBuilder.BuildGraph(
                [new SoftwareApplicationDescriptor(app.Name, app.Description)]);

            rows.Add(new GeneratedContent
            {
                ProjectId = project.Id,
                ContentType = GeneratedContentType.ToolPost,
                Title = displayTitle,
                DisplayTitle = displayTitle,
                Slug = slug,
                ListingExcerpt = listingExcerpt,
                MetaDescription = listingExcerpt.Length > 160 ? listingExcerpt[..160] : listingExcerpt,
                AdvertisingExcerpt = advertisingExcerpt,
                BodyHtml = bodyHtml,
                JsonLdSchema = string.IsNullOrWhiteSpace(jsonLd) ? "{}" : jsonLd,
                SourceAppName = app.Name,
                SourceAppOrder = order++,
                WordCount = wordCount,
                GeneratedByProvider = provider.ProviderType,
                GeneratedByModel = provider.ProviderType.ToString(),
            });
        }

        articleRow.BodyHtml = ToolsSectionHtmlParser.InjectToolLinks(
            articleRow.BodyHtml,
            metadata.SectionOutline,
            department,
            rows.Select(r => (r.SourceAppName!, r.Slug)).ToList());

        return rows;
    }

    private static string BuildListingExcerpt(SoftwareApplicationDescriptor app) =>
        string.IsNullOrWhiteSpace(app.Description)
            ? $"Overview of {app.Name.Trim()} for enterprise AI teams."
            : app.Description.Trim();

    private static async Task<string> GenerateToolBodyAsync(
        IContentGenerationProvider provider,
        ProjectGenerationContext context,
        ArticleMetadataDraft pillarMetadata,
        SoftwareApplicationDescriptor app,
        string department,
        string toolSlug,
        CancellationToken cancellationToken)
    {
        var system = """
            You are a senior technical writer for an IT consulting firm.
            Write a tool overview page as HTML only (no markdown, no JSON wrapper).
            Use <h2> for main sections and <h3> for subsections with multiple <p> paragraphs.
            Include: Overview, Key Capabilities, Implementation Considerations, and When to Use sections.
            """;

        var user = $"""
            Target keyword context: {context.TargetKeyword}
            Pillar topic: {pillarMetadata.Title}
            Tool name: {app.Name}
            Tool summary: {app.Description ?? "N/A"}
            Department: {department}
            Public path: /tools/{department}/{toolSlug}
            Write 400-700 words of expert third-person prose.
            """;

        var result = await provider.CompleteAsync(
            new ChatCompletionRequest(
                Messages:
                [
                    new(ChatRole.System, system),
                    new(ChatRole.User, user),
                ],
                Temperature: 0.5,
                MaxOutputTokens: 4096),
            cancellationToken);

        return LlmResponseJsonParser.ParseHtmlBody(result.Content, $"tool page '{app.Name}'");
    }
}
