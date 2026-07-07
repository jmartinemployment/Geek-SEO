using ContentWriter.Application.DTOs;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Services;

public static class GeneratedContentSetAssembler
{
    public static GeneratedContentSet Assemble(
        Project project,
        string articleBaseUrl,
        string blogBaseUrl)
    {
        var articleRow = Find(project, GeneratedContentType.TechnicalArticle);
        var blogRow = Find(project, GeneratedContentType.BlogPost);
        var facebookRow = Find(project, GeneratedContentType.SocialFacebook);
        var linkedInRow = Find(project, GeneratedContentType.SocialLinkedIn);

        var articleSlug = articleRow?.Slug;
        var articleUrl = articleSlug is null ? null : CombineUrl(articleBaseUrl, articleSlug);
        var blogSlug = blogRow?.Slug;
        var blogUrl = blogSlug is null ? null : CombineUrl(blogBaseUrl, blogSlug);

        return new GeneratedContentSet(
            Article: articleRow is null ? null : ToArticleDraft(articleRow),
            ArticleSlug: articleSlug,
            ArticleUrl: articleUrl,
            ArticleJsonLd: articleRow?.JsonLdSchema,
            Blog: blogRow is null ? null : ToBlogDraft(blogRow),
            BlogSlug: blogSlug,
            BlogUrl: blogUrl,
            BlogJsonLd: blogRow?.JsonLdSchema,
            FacebookPost: facebookRow is null ? null : new SocialPostDraft("Facebook", facebookRow.BodyHtml),
            LinkedInPost: linkedInRow is null ? null : new SocialPostDraft("LinkedIn", linkedInRow.BodyHtml));
    }

    public static ArticleDraft ToArticleDraft(GeneratedContent row) => new(
        row.Title,
        row.MetaDescription ?? string.Empty,
        row.BodyHtml,
        row.Keywords,
        row.WordCount,
        row.SectionOutline);

    public static BlogDraft ToBlogDraft(GeneratedContent row) => new(
        row.Title,
        row.MetaDescription ?? string.Empty,
        row.BodyHtml,
        row.Keywords,
        row.WordCount);

    private static GeneratedContent? Find(Project project, GeneratedContentType type) =>
        project.GeneratedContents.FirstOrDefault(c => c.ContentType == type);

    private static string CombineUrl(string baseUrl, string slug) => $"{baseUrl.TrimEnd('/')}/{slug}";
}
