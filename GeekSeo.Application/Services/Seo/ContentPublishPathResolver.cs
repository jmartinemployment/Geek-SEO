namespace GeekSeo.Application.Services.Seo;

public static class ContentPublishPathResolver
{
    public const string DefaultBlogPathPrefix = "/blog/";

    public static string? ResolveRelativePath(string? publishSlug)
    {
        if (string.IsNullOrWhiteSpace(publishSlug))
            return null;

        var normalized = publishSlug.Trim().ToLowerInvariant();
        if (!ContentPublishSlug.IsValid(normalized))
            return null;

        return $"{DefaultBlogPathPrefix}{normalized}";
    }

    public static string? ResolveAbsoluteUrl(string? projectSiteUrl, string? publishSlug)
    {
        var relative = ResolveRelativePath(publishSlug);
        if (relative is null)
            return null;

        if (string.IsNullOrWhiteSpace(projectSiteUrl))
            return relative;

        var baseUrl = projectSiteUrl.Trim().TrimEnd('/');
        return $"{baseUrl}{relative}";
    }
}
