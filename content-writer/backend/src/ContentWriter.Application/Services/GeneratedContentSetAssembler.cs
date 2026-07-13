using ContentWriter.Application.DTOs;
using ContentWriter.Application.Services.Export;
using ContentWriter.Application.Services.Publish;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Services;

public static class GeneratedContentSetAssembler
{
    public static GeneratedContentSet Assemble(
        Project project,
        string articleBaseUrl,
        string blogBaseUrl,
        string? departmentOverride = null,
        string? toolsGenerationOutcome = null)
    {
        var articleRow = Find(project, GeneratedContentType.TechnicalArticle);
        var blogRow = Find(project, GeneratedContentType.BlogPost);
        var facebookRow = Find(project, GeneratedContentType.SocialFacebook);
        var linkedInRow = Find(project, GeneratedContentType.SocialLinkedIn);
        var coldOutreachRow = Find(project, GeneratedContentType.EmailColdOutreach);

        var department = GeekPublicUrlBuilder.ResolveDepartment(project, departmentOverride);

        var articleSlug = articleRow?.Slug;
        var articleUrl = articleSlug is null
            ? null
            : GeekPublicUrlBuilder.ArticleUrl(articleBaseUrl, department, articleSlug);
        var blogSlug = blogRow?.Slug;
        var blogUrl = blogSlug is null
            ? null
            : GeekPublicUrlBuilder.BlogUrl(blogBaseUrl, department, blogSlug);

        return new GeneratedContentSet(
            Department: department,
            Article: articleRow is null ? null : ToArticleDraft(articleRow),
            ArticleSlug: articleSlug,
            ArticleUrl: articleUrl,
            ArticleJsonLd: articleRow?.JsonLdSchema,
            Blog: blogRow is null ? null : ToBlogDraft(blogRow),
            BlogSlug: blogSlug,
            BlogUrl: blogUrl,
            BlogJsonLd: blogRow?.JsonLdSchema,
            FacebookPost: facebookRow is null ? null : new SocialPostDraft("Facebook", facebookRow.BodyHtml),
            LinkedInPost: linkedInRow is null ? null : new SocialPostDraft("LinkedIn", linkedInRow.BodyHtml),
            ColdOutreachEmail: coldOutreachRow is null
                ? null
                : new ColdOutreachEmailContent(
                    coldOutreachRow.Title,
                    coldOutreachRow.BodyHtml,
                    coldOutreachRow.MetaDescription ?? string.Empty,
                    coldOutreachRow.RelatedArticleUrl ?? articleUrl ?? string.Empty),
            ImagePrompts: BuildImagePrompts(project),
            Tools: BuildTools(project),
            ToolsGenerationOutcome: toolsGenerationOutcome);
    }

    private static ImagePromptsContent? BuildImagePrompts(Project project)
    {
        var toolOrderBySlug = project.GeneratedContents
            .Where(c => c.ContentType == GeneratedContentType.ToolPost)
            .GroupBy(c => c.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Min(c => c.SourceAppOrder ?? int.MaxValue),
                StringComparer.OrdinalIgnoreCase);

        int SortKey(ImagePromptSectionContent section)
        {
            if (string.Equals(section.SourceType, "pillar", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (string.Equals(section.SourceType, "blog", StringComparison.OrdinalIgnoreCase))
                return 1_000;
            if (section.SourceType.StartsWith("tool/", StringComparison.OrdinalIgnoreCase))
            {
                var toolSlug = section.SourceType["tool/".Length..];
                var toolOrder = toolOrderBySlug.TryGetValue(toolSlug, out var order) ? order : int.MaxValue;
                return 2_000 + (toolOrder * 100) + section.Order;
            }

            return 5_000 + section.Order;
        }

        var sectionRows = project.GeneratedContents
            .Where(c => c.ContentType == GeneratedContentType.ImagePromptSection)
            .Select(ImagePromptMetadata.ToSectionContent)
            .OrderBy(SortKey)
            .ToList();

        return sectionRows.Count == 0 ? null : new ImagePromptsContent(sectionRows);
    }

    public static ArticleDraft ToArticleDraft(GeneratedContent row) => new(
        row.Title,
        row.MetaDescription ?? string.Empty,
        row.BodyHtml,
        row.Keywords,
        row.WordCount,
        row.SectionOutline,
        row.HomeUseCaseExcerpt,
        row.DepartmentListExcerpt,
        row.HeroExcerpt,
        row.NewspaperExcerpt,
        row.PillarPageUseCaseExcerpt,
        row.DisplayTitle ?? row.Title);

    public static BlogDraft ToBlogDraft(GeneratedContent row) => new(
        row.Title,
        row.MetaDescription ?? string.Empty,
        row.BodyHtml,
        row.Keywords,
        row.WordCount,
        row.SectionOutline,
        row.DepartmentListExcerpt,
        row.HeroExcerpt,
        row.NewspaperExcerpt,
        row.Advertisement,
        row.DisplayTitle ?? row.Title);

    public static ToolDraft ToToolDraft(GeneratedContent row) => new(
        row.Title,
        row.DisplayTitle ?? row.Title,
        row.DepartmentListExcerpt,
        row.HeroExcerpt,
        row.NewspaperExcerpt,
        row.ToolPageExcerpt,
        row.MetaDescription ?? string.Empty,
        row.Advertisement,
        row.BodyHtml,
        row.Slug,
        row.SourceAppName ?? row.Title,
        row.SourceAppOrder ?? 0,
        row.WordCount,
        row.JsonLdSchema);

    private static IReadOnlyList<ToolDraft> BuildTools(Project project) =>
        project.GeneratedContents
            .Where(c => c.ContentType == GeneratedContentType.ToolPost)
            .OrderBy(c => c.SourceAppOrder ?? int.MaxValue)
            .Select(ToToolDraft)
            .ToList();

    private static GeneratedContent? Find(Project project, GeneratedContentType type) =>
        project.GeneratedContents.FirstOrDefault(c => c.ContentType == type);
}
