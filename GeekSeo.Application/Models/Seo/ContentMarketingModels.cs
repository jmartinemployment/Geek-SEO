namespace GeekSeo.Application.Models.Seo;

public sealed class ContentMarketingBundle
{
    public string DepartmentSlug { get; set; } = "marketing";
    public string UseCaseSlug { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public string? HomeSummary { get; set; }
    public string? HubSummary { get; set; }
    public string? MetaDescription { get; set; }
    public ContentMarketingBlogSpoke? BlogSpoke { get; set; }
    public ContentMarketingSocial? Social { get; set; }
}

public sealed class ContentMarketingBlogSpoke
{
    public string Slug { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public string SpokeType { get; set; } = "comparison";
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? MetaDescription { get; set; }
}

public sealed class ContentMarketingSocial
{
    public ContentMarketingSocialPost? LinkedIn { get; set; }
    public ContentMarketingSocialPost? Facebook { get; set; }
}

public sealed class ContentMarketingSocialPost
{
    public string Body { get; set; } = string.Empty;
    public string LinkTargetKind { get; set; } = "pillar";
    public string LinkTargetSlug { get; set; } = string.Empty;
}

public sealed class ContentMarketingValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }
}

public sealed record GenerateBlogSpokeRequest
{
    public string SpokeType { get; init; } = "comparison";
    public string? SpokeKeyword { get; init; }
}
